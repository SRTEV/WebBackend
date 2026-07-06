using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class Zone
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Coordinates { get; set; }

    public bool? IsRestrictedArea { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int VehicleTypeId { get; set; }

    public virtual VehicleType VehicleType { get; set; } = null!;
}
