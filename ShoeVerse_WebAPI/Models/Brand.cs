using System;
using System.Collections.Generic;

namespace ShoeVerse_WebAPI.Models;

public partial class Brand
{
    public int BrandId { get; set; }

    public int? CategoryId { get; set; }

    public string BrandName { get; set; } = null!;

    public virtual Category? Category { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
