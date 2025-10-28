using System;
using System.Collections.Generic;

namespace ShoeVerse_WebAPI.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public long PhoneNo { get; set; }

    public string Password { get; set; } = null!;

    public string? ProfileImage { get; set; }

    public int RoleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Role Role { get; set; } = null!;
}
