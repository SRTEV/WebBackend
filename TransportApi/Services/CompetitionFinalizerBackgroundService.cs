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
        
     
        private readonly TimeSpan _checkInterval = TimeSpan.FromDays(1);

        public CompetitionFinalizerBackgroundService(
            IServiceProvider serviceProvider, 
            ILogger<CompetitionFinalizerBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background service for finalizing competitions started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndProcessCompetitionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in the background service for finalizing competitions.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckAndProcessCompetitionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var activeCompetitions = await context.Competitions
                .Where(c => c.StartDate <= currentDate && c.EndDate >= currentDate)
                .ToListAsync();

            if (activeCompetitions.Any())
            {
                _logger.LogInformation($"Found active competitions: {activeCompetitions.Count}.");
            }
            else
            {
                _logger.LogInformation("No active competitions found.");
            }
            var endedCompetitions = await context.Competitions
                .Where(c => c.EndDate < currentDate)
                .ToListAsync();

            bool finalizedAny = false;
            foreach (var competition in endedCompetitions)
            {
                bool needsFinalization = await context.UsersResults
                    .AnyAsync(ur => ur.CompetitionId == competition.Id && ur.Rank == 0);

                if (needsFinalization)
                {
                    _logger.LogInformation($"Background service is finalizing competition ID: {competition.Id}.");
                    await FinalizeCompetitionRewardsAsync(context, competition.Id);
                    finalizedAny = true;
                }
            }

            if (!finalizedAny && !endedCompetitions.Any())
            {
                _logger.LogInformation("No ended competitions found that require finalization.");
            }
        }

        private async Task FinalizeCompetitionRewardsAsync(AppDbContext context, int competitionId)
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

                var matchedReward = rewards.ElementAtOrDefault(rank - 1);
                
                if (matchedReward != null && decimal.TryParse(matchedReward.Unit, out decimal rewardValue))
                {
                    result.RewardAmount = int.Parse(rewardValue.ToString());
                }
                else
                {
                    result.RewardAmount = 0;
                }

                context.UsersResults.Update(result);
                rank++;
            }

            await context.SaveChangesAsync();
            _logger.LogInformation($"Finalized rewards for competition ID: {competitionId}. Total participants processed: {results.Count}.");
        }
    }
}