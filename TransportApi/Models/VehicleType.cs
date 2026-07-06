using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class VehicleType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();

    public virtual ICollection<RentalPlan> RentalPlans { get; set; } = new List<RentalPlan>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    public virtual ICollection<Zone> Zones { get; set; } = new List<Zone>();
}
