using System;
using System.Collections.Generic;

namespace ShoeVerse_WebAPI.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int? ColorId { get; set; }

    public int? SizeId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public virtual ProductColor? Color { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ProductSize? Size { get; set; }
}
