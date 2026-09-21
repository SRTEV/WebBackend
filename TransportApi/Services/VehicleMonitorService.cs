using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TransportApi.Models; // Замініть на свій простір імен моделей

namespace TransportApi.Services
{
    public class VehicleStatusMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VehicleStatusMonitorService> _logger;
        
     
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(3);

        public VehicleStatusMonitorService(
            IServiceProvider serviceProvider, 
            ILogger<VehicleStatusMonitorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Vehicle Status Monitor Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndClothVehiclesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while checking vehicle statuses.");
                }
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Vehicle Status Monitor Service is stopping.");
        }

        private async Task CheckAndClothVehiclesAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                _logger.LogInformation("Running background check for vehicle statuses...");

                var thresholdDate = DateTime.UtcNow.AddDays(-3);

                var vehiclesToUpdate = await dbContext.Vehicles
                    .Where(v => v.VehicleStatusId != 3 
                                && v.VehicleStatusId != 2
                                && (v.BatteryLevel < 15 || v.LastActivity < thresholdDate))
                    .ToListAsync();

                if (!vehiclesToUpdate.Any())
                {
                    return;
                }

                foreach (var vehicle in vehiclesToUpdate)
                {
                    _logger.LogInformation(
                        "Vehicle ID {Id} marked as NeedCheck. Reason: Battery = {Battery}%, LastActivity = {Activity}",
                        vehicle.Id, vehicle.BatteryLevel, vehicle.LastActivity);
                    vehicle.VehicleStatusId = 3; 
                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Successfully updated {vehiclesToUpdate.Count} vehicles to 'NeedCheck' status.");
            }
        }
    }
}