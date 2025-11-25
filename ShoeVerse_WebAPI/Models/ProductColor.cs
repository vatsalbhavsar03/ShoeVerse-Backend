using System;
using System.Collections.Generic;

namespace ShoeVerse_WebAPI.Models;

public partial class ProductColor
{
    public int ColorId { get; set; }

    public int? ProductId { get; set; }

    public string ColorName { get; set; } = null!;

    public string? HexCode { get; set; }

    public int? Stock { get; set; }

    public decimal? Price { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Product? Product { get; set; }

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
}
