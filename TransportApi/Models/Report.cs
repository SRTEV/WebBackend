using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class Report
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Text { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? Status { get; set; }

    public int? UserId { get; set; }

    public int? VehicleId { get; set; }

    public virtual ICollection<ReportResponse> ReportResponses { get; set; } = new List<ReportResponse>();

    public virtual User? User { get; set; }

    public virtual Vehicle? Vehicle { get; set; }
}
