using System;
using System.Collections.Generic;

namespace ShoeVerse_WebAPI.Models;

public partial class ContactU
{
    public int ContactId { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
