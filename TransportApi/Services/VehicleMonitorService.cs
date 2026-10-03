using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using TransportApi.Models;

namespace TransportApi.Services
{
    public class VehicleStatusMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VehicleStatusMonitorService> _logger;

        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);

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
                    await UpdateVehiclesZoneStatusAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "An error occurred while checking vehicle statuses."
                    );
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Vehicle Status Monitor Service is stopping.");
        }

        private async Task CheckAndClothVehiclesAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider.GetRequiredService<AppDbContext>();

                _logger.LogInformation(
                    "Running background check for vehicle statuses..."
                );

                var thresholdDate = DateTime.UtcNow.AddDays(-3);

                var vehiclesToUpdate = await dbContext.Vehicles
                    .Where(v =>
                        v.VehicleStatusId != 3 &&
                        v.VehicleStatusId != 2 &&
                        (
                            v.BatteryLevel < 15 ||
                            v.LastActivity < thresholdDate
                        ))
                    .ToListAsync();

                if (!vehiclesToUpdate.Any())
                {
                    return;
                }

                foreach (var vehicle in vehiclesToUpdate)
                {
                    _logger.LogInformation(
                        "Vehicle ID {Id} marked as NeedCheck. Reason: Battery = {Battery}%, LastActivity = {Activity}",
                        vehicle.Id,
                        vehicle.BatteryLevel,
                        vehicle.LastActivity
                    );

                    vehicle.VehicleStatusId = 3;
                }

                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully updated {Count} vehicles to 'NeedCheck' status.",
                    vehiclesToUpdate.Count
                );
            }
        }

        private async Task UpdateVehiclesZoneStatusAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider.GetRequiredService<AppDbContext>();

                _logger.LogInformation(
                    "Checking vehicles against their type's assigned zones..."
                );

                var vehicles = await dbContext.Vehicles
                    .ToListAsync();

                var zones = await dbContext.Zones
                    .ToListAsync();

                if (!vehicles.Any() || !zones.Any())
                {
                    return;
                }

                foreach (var vehicle in vehicles)
                {
                    // Беремо останню точку Route_History
                    var lastRoutePoint = await dbContext.RouteHistories
                        .Where(rh => rh.VehicleId == vehicle.Id)
                        .OrderByDescending(rh => rh.RecordedAt)
                        .FirstOrDefaultAsync();

                    if (lastRoutePoint == null)
                    {
                        _logger.LogInformation(
                            "Vehicle {VehicleId}: no Route_History points found.",
                            vehicle.Id
                        );

                        continue;
                    }

                    double latitude = (double)lastRoutePoint.PositionX;
                    double longitude = (double)lastRoutePoint.PositionY;

                    bool isInZone = false;

                    // Отримуємо всі зони, які відповідають типу даного транспортного засобу
                    // (Якщо назва властивості у вашій C# моделі відрізняється, наприклад Vehicle_TypeId, уточніть її)
                    var matchingZones = zones
                        .Where(z => z.VehicleTypeId == vehicle.VehicleTypeId)
                        .ToList();

                    foreach (var zone in matchingZones)
                    {
                        if (string.IsNullOrWhiteSpace(zone.Coordinates))
                            continue;

                        try
                        {
                            var polygon = CreatePolygon(zone.Coordinates);
                            var point = new Point(longitude, latitude);

                            if (polygon.Contains(point) || polygon.Touches(point))
                            {
                                isInZone = true;
                                break; // Знайшли відповідну зону — далі можна не перевіряти
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Error processing zone {ZoneId} coordinates for vehicle {VehicleId}.",
                                zone.Id,
                                vehicle.Id
                            );
                        }
                    }

                    if (vehicle.In_Zone != isInZone)
                    {
                        _logger.LogInformation(
                            "Vehicle {VehicleId}: In_zone changed from {OldStatus} to {NewStatus}. Coordinates: {Lat}, {Lon}",
                            vehicle.Id,
                            vehicle.In_Zone,
                            isInZone,
                            latitude,
                            longitude
                        );

                        vehicle.In_Zone = isInZone;
                    }
                }

                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Vehicle zone status check completed."
                );
            }
        }

        private Polygon CreatePolygon(string coordinates)
        {
            var points = coordinates
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(pair =>
                {
                    var parts = pair
                        .Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 2)
                        throw new FormatException(
                            $"Invalid coordinate: {pair}"
                        );

                    double latitude = double.Parse(
                        parts[0].Trim(),
                        CultureInfo.InvariantCulture
                    );

                    double longitude = double.Parse(
                        parts[1].Trim(),
                        CultureInfo.InvariantCulture
                    );

                    // NetTopologySuite використовує X = longitude та Y = latitude
                    return new Coordinate(
                        longitude,
                        latitude
                    );
                })
                .ToList();

            if (points.Count < 3)
            {
                throw new FormatException(
                    "A zone must contain at least 3 coordinates."
                );
            }

            // Закриваємо полігон
            if (!points.First().Equals2D(points.Last()))
            {
                points.Add(points.First());
            }

            var geometryFactory = GeometryFactory.Default;

            return geometryFactory.CreatePolygon(
                points.ToArray()
            );
        }
    }
}