using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class Payment
{
    public int Id { get; set; }

    public string Status { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public int RentalId { get; set; }

    public virtual Rental Rental { get; set; } = null!;

    public virtual UsersResult? UsersResult { get; set; }
}
