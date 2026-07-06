using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class Notification
{
    public int Id { get; set; }

    public string Text { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
