using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class RentalPlan
{
    public int Id { get; set; }

    public string Plan { get; set; } = null!;

    public decimal Price { get; set; }

    public int? Time { get; set; }

    public string? Description { get; set; }

    public int VehicleTypeId { get; set; }

    public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();

    public virtual VehicleType VehicleType { get; set; } = null!;
}
