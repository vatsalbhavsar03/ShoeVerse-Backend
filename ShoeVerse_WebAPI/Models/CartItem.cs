using System;
using System.Collections.Generic;

namespace ShoeVerse_WebAPI.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int CartId { get; set; }

    public int ProductId { get; set; }

    public int ColorId { get; set; }

    public int? SizeId { get; set; }

    public int Quantity { get; set; }

    public virtual Cart Cart { get; set; } = null!;

    public virtual ProductColor Color { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ProductSize? Size { get; set; }
}
