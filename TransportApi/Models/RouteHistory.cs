using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class RouteHistory
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public int? RentalId { get; set; }

    public decimal PositionX { get; set; }

    public decimal PositionY { get; set; }

    public sbyte? BatteryLevel { get; set; }

    public decimal? Speed { get; set; }

    public DateTime? RecordedAt { get; set; }

    public virtual Rental? Rental { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;
}
