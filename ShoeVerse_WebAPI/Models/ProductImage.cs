using System;
using System.Collections.Generic;

namespace ShoeVerse_WebAPI.Models;

public partial class ProductImage
{
    public int ImageId { get; set; }

    public int? ProductId { get; set; }

    public int? ColorId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool? IsMainImage { get; set; }

    public virtual ProductColor? Color { get; set; }

    public virtual Product? Product { get; set; }
}
