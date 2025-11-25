using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShoeVerse_WebAPI.DTO
{
    public class AddToCartRequest
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int ColorId { get; set; }
        public int? SizeId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public int Quantity { get; set; }
    }

    public class CartResponse
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public List<CartItemResponse> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }

    public class CartItemResponse
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }

        // keep these nullable if product may be missing (defensive)
        public string? ProductName { get; set; }

        public int? ColorId { get; set; }
        public string? ColorName { get; set; }
        public int? SizeId { get; set; }
        public string? SizeName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        // NEW: image URL properties the client expects (camelCase in JSON)
        [JsonPropertyName("colorImageUrl")]
        public string? ColorImageUrl { get; set; }

        [JsonPropertyName("productImageUrl")]
        public string? ProductImageUrl { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
    }
}
