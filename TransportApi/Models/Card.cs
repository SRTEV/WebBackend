using System;
using System.Collections.Generic;

namespace TransportApi.Models;

public partial class Card
{
    public int Id { get; set; }

    public string? CardNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? CvvCode { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
