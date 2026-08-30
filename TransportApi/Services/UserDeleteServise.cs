using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TransportApi.Models;

namespace TransportApi.Services
{
    public class UserCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UserCleanupService> _logger;
        
        private readonly TimeSpan _checkInterval = TimeSpan.FromDays(1); 

        public UserCleanupService(IServiceProvider serviceProvider, ILogger<UserCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("User Deletion Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DeleteOldSoftDeletedUsersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in the background service for cleaning up users: {ex.Message}");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task DeleteOldSoftDeletedUsersAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var thresholdDate = DateTime.Now.AddDays(-30);
                var usersToDelete = await context.Users
                    .Where(u => u.Deleted == true && u.UpdatedAt != null && u.UpdatedAt < thresholdDate)
                    .ToListAsync();

                if (usersToDelete.Any())
                {
                    context.Users.RemoveRange(usersToDelete);
                    await context.SaveChangesAsync();
                    
                    _logger.LogInformation($"User Deletion Background Service deleted {usersToDelete.Count} users who were marked as deleted more than 30 days ago.");
                }
            }
        }
    }
}