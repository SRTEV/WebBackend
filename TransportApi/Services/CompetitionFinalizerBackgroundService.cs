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
        
        // Інтервал перевірки та перерахунку рейтингів (наприклад, кожні 5 хвилин)
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
                .ToListAsync();

            foreach (var competition in activeCompetitions)
            {
                await UpdateCompetitionRanksAsync(context, competition.Id);
            }


            var endedCompetitions = await context.Competitions
                .Where(c => c.EndDate < currentDate)
                .ToListAsync();

            foreach (var competition in endedCompetitions)
            {
         
                await FinalizeEndedCompetitionAsync(context, competition.Id);
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
                result.Rank = rank;
                context.UsersResults.Update(result);
                rank++;
            }

            await context.SaveChangesAsync();
        }
        private async Task FinalizeEndedCompetitionAsync(AppDbContext context, int competitionId)
        {
            var results = await context.UsersResults
                .Where(ur => ur.CompetitionId == competitionId)
                .OrderByDescending(ur => ur.Score)
                .ToListAsync();

            var rewards = await context.RewardTypes
                .Where(rt => rt.CompetitionId == competitionId)
                .ToListAsync();

            int rank = 1;
            foreach (var result in results)
            {
                result.Rank = rank;

                if (result.RewardAmount == 0)
                {
                    var matchedReward = rewards.ElementAtOrDefault(rank - 1);
                    
                    if (matchedReward != null && decimal.TryParse(matchedReward.Unit, out decimal rewardValue))
                    {
                        result.RewardAmount = int.Parse(rewardValue.ToString());
                    }
                    else
                    {
                        result.RewardAmount = 0;
                    }
                }

                context.UsersResults.Update(result);
                rank++;
            }

            await context.SaveChangesAsync();
        }
    }
}