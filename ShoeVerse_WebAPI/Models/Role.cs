using System;
using System.Collections.Generic;

namespace ShoeVerse_WebAPI.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public string Rname { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
