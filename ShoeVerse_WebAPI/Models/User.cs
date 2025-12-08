using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShoeVerse_WebAPI.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public long PhoneNo { get; set; }

    [JsonIgnore]
    public string Password { get; set; } = null!;

    public string? ProfileImage { get; set; }

    public int RoleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore]
    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    [JsonIgnore]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [JsonIgnore]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Role Role { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
