using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShoeVerse_WebAPI.Models;

public partial class ProductSize
{
    public int SizeId { get; set; }

    public int? ColorId { get; set; }

    public string SizeName { get; set; } = null!;

    public int? Stock { get; set; }

    public bool? IsAvailable { get; set; }

    [JsonIgnore]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [JsonIgnore]
    public virtual ProductColor? Color { get; set; }

    [JsonIgnore]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
