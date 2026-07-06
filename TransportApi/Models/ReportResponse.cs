using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class ReportResponse
{
    public int Id { get; set; }

    public string Text { get; set; } = null!;

    public string ResponseTo { get; set; } = null!;

    public int ReportId { get; set; }

    public virtual Report Report { get; set; } = null!;
}
