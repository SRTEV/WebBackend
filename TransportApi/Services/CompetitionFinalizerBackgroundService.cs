using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TransportApi.Models;

namespace TransportApi.Services
{
    public class CompetitionFinalizerBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CompetitionFinalizerBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

        public CompetitionFinalizerBackgroundService(
            IServiceProvider serviceProvider, 
            ILogger<CompetitionFinalizerBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background service for competition finalization started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessCompetitionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in the background service for competition finalization.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task ProcessCompetitionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var activeCompetitions = await context.Competitions
                .Where(c => c.StartDate <= currentDate && c.EndDate >= currentDate)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var competitionId in activeCompetitions)
            {
                await UpdateCompetitionRanksAsync(context, competitionId);
            }

            var endedCompetitions = await context.Competitions
                .Where(c => c.EndDate < currentDate)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var competitionId in endedCompetitions)
            {
                await FinalizeEndedCompetitionAsync(context, competitionId);
            }
        }

        private async Task UpdateCompetitionRanksAsync(AppDbContext context, int competitionId)
        {
            var results = await context.UsersResults
                .Where(ur => ur.CompetitionId == competitionId)
                .OrderByDescending(ur => ur.Score)
                .ToListAsync();

            int rank = 1;
            foreach (var result in results)
            {
                if (result.Rank != rank)
                {
                    result.Rank = rank;
                    context.UsersResults.Update(result);
                }
                rank++;
            }

            if (results.Any())
            {
                await context.SaveChangesAsync();
            }
        }

        private async Task FinalizeEndedCompetitionAsync(AppDbContext context, int competitionId)
        {
            // Очищаємо трекер, щоб уникнути конфліктів трекінгу та цикличної залежності в EF Core
            context.ChangeTracker.Clear();

            var results = await context.UsersResults
                .Where(ur => ur.CompetitionId == competitionId)
                .OrderByDescending(ur => ur.Score)
                .ToListAsync();

            var rewards = await context.RewardTypes
                .Where(rt => rt.CompetitionId == competitionId)
                .OrderBy(rt => rt.Id)
                .ToListAsync();

            bool changesMade = false;
            int rank = 1;

            foreach (var result in results)
            {
                result.Rank = rank;

                // Захист від повторного нарахування: якщо нагорода вже має PaymentId (тобто була використана) 
                // або на балансі ще є кошти (RewardAmount > 0), ми її не чіпаємо.
                bool isAlreadyProcessed = result.PaymentId != null || result.RewardAmount > 0;

                if (!isAlreadyProcessed && rewards.Count > 0)
                {
                    RewardType? matchedReward = null;

                    if (rank == 1)
                    {
                        matchedReward = rewards.ElementAtOrDefault(0);
                    }
                    else if (rank >= 2 && rank <= 4)
                    {
                        matchedReward = rewards.ElementAtOrDefault(1);
                    }
                    else if (rank == 5)
                    {
                        matchedReward = rewards.ElementAtOrDefault(2);
                    }

                    if (matchedReward != null && int.TryParse(matchedReward.Unit, out int rewardValue))
                    {
                        result.RewardAmount = rewardValue;
                        changesMade = true;
                    }
                }

                context.UsersResults.Update(result);
                rank++;
            }

            if (changesMade && results.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation($"Competition ID {competitionId} has been successfully finalized.");
            }
        }
    }
}