using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ResetLink { get; set; }

    public bool? Deleted { get; set; }

    public decimal OustandingBalances { get; set; }

    public bool? IsBlocked { get; set; }

    public string? BlockedReason { get; set; }

    public int RoleId { get; set; }

    public int? CardId { get; set; }

    public virtual Card? Card { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual Role Role { get; set; } = null!;

    public virtual UsersResult? UsersResult { get; set; }
}
