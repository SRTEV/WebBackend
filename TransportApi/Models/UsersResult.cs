using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class UsersResult
{
    public int Id { get; set; }

    public int? Rank { get; set; }

    public int? Score { get; set; }

    public int RewardAmount { get; set; }

    public int UserId { get; set; }

    public int? PaymentId { get; set; }

    public int CompetitionId { get; set; }

    public virtual Competition Competition { get; set; } = null!;

    public virtual Payment? Payment { get; set; }

    public virtual User User { get; set; } = null!;
}
