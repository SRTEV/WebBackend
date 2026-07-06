using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class Rental
{
    public int Id { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public decimal? Distance { get; set; }

    public int DistanceStart { get; set; }

    public int? DistanceEnd { get; set; }

    public int UserId { get; set; }

    public int VehicleId { get; set; }

    public int RentalPlanId { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual RentalPlan RentalPlan { get; set; } = null!;

    public virtual ICollection<RouteHistory> RouteHistories { get; set; } = new List<RouteHistory>();

    public virtual User User { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
