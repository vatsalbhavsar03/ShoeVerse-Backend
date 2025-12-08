using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShoeVerse_WebAPI.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string TransactionId { get; set; } = null!;

    public decimal Amount { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public DateTime PaymentDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [JsonIgnore]
    public virtual Order Order { get; set; } = null!;
}
