using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class Competition
{
    public int Id { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Description { get; set; } = null!;

    public int? GoalValue { get; set; }

    public int VehicleTypeId { get; set; }

    public virtual ICollection<GoalType> GoalTypes { get; set; } = new List<GoalType>();

    public virtual ICollection<RewardType> RewardTypes { get; set; } = new List<RewardType>();

    public virtual ICollection<UsersResult> UsersResults { get; set; } = new List<UsersResult>();

    public virtual VehicleType VehicleType { get; set; } = null!;
}
