using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class Vehicle
{
    public int Id { get; set; }

    public sbyte? BatteryLevel { get; set; }

    public decimal? PositionX { get; set; }

    public decimal? PositionY { get; set; }

    public string? QrCode { get; set; }

    public DateTime? LastActivity { get; set; }

    public int? BatteryCapacity { get; set; }

    public int? ElectricityConsumption { get; set; }

    public DateTime? ScanTime { get; set; }

    public string? Model { get; set; }

    public bool? Deleted { get; set; }

    public int VehicleTypeId { get; set; }

    public int VechicleStatusId { get; set; }

    public virtual Rental? Rental { get; set; }

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual ICollection<RouteHistory> RouteHistories { get; set; } = new List<RouteHistory>();

    public virtual VechicleStatus VechicleStatus { get; set; } = null!;

    public virtual VehicleType VehicleType { get; set; } = null!;
}
