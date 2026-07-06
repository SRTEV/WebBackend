using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class VechicleStatus
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
