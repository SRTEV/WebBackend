using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class GoalType
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int CompetitionId { get; set; }

    public virtual Competition Competition { get; set; } = null!;
}
