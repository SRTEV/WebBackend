using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class RewardType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public int CompetitionId { get; set; }

    public virtual Competition Competition { get; set; } = null!;
}
