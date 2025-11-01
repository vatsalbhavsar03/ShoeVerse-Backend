using System;
using System.Collections.Generic;

namespace ShoeVerse_WebAPI.Models;

public partial class ProductSize
{
    public int SizeId { get; set; }

    public int? ColorId { get; set; }

    public string SizeName { get; set; } = null!;

    public int? Stock { get; set; }

    public bool? IsAvailable { get; set; }

    public virtual ProductColor? Color { get; set; }
}
