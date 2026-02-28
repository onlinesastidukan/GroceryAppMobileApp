using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GroceryApp.Models;

#region Auth Models
public class LoginRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; }
    public string Role { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string MobileNumber { get; set; }
    public string Address { get; set; }
}

public class RegisterRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Mobile number is required")]
    public string MobileNumber { get; set; }

    [Required(ErrorMessage = "Address is required")]
    public string Address { get; set; }
}

public class RegisterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int UserId { get; set; }
}

public class AuthData
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string MobileNumber { get; set; }
    public string Address { get; set; }
    public string Role { get; set; }
    public string Token { get; set; }
}

public class UserLoginInfo
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string MobileNumber { get; set; }
    public string Address { get; set; }
    public string Role { get; set; }
    public string Token { get; set; }
}
#endregion

#region Product Models
public class Product
{
    [JsonPropertyName("id")]
    public int ProductId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("stockQuantity")]
    public int Stock { get; set; }

    [JsonIgnore]
    public bool IsOutOfStock => Stock <= 0;

    [JsonIgnore]
    public string StockStatus => IsOutOfStock ? "Stock unavailable" : $"In stock: {Stock}";

    [JsonPropertyName("categoryId")]
    public int CategoryId { get; set; }

    [JsonPropertyName("photoUrl")]
    public string ImageUrl { get; set; }

    // Compatibility aliases for backend variants that send image URL under alternate keys.
    [JsonPropertyName("imageUrl")]
    public string? ImageUrlAlias
    {
        get => ImageUrl;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ImageUrl = value;
            }
        }
    }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedDate { get; set; }

    public Category Category { get; set; }
}

public class CreateProductRequest
{
    [Required]
    public string Name { get; set; }
    public string Description { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public string PhotoUrl { get; set; }
}

public class UpdateProductRequest
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public string PhotoUrl { get; set; }
    public bool IsActive { get; set; }
}
#endregion

#region Category Models
public class Category
{
    [JsonPropertyName("id")]
    public int CategoryId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    public List<Product> Products { get; set; } = new();
}

public class CreateCategoryRequest
{
    [Required]
    public string Name { get; set; }
}

public class UpdateCategoryRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}
#endregion

#region Cart Models
public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => Price * Quantity;
    public string ImageUrl { get; set; }
}

public class Cart
{
    public List<CartItem> Items { get; set; } = new();
    
    public decimal TotalPrice => Items.Sum(x => x.TotalPrice);
    public int TotalItems => Items.Sum(x => x.Quantity);
}
#endregion

#region Order Models
public class Order
{
    [JsonPropertyName("id")]
    public int OrderId { get; set; }

    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("userFullName")]
    public string UserFullName { get; set; }

    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; set; }

    // Backend compatibility alias when order date comes as createdAt.
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAtAlias
    {
        get => OrderDate;
        set
        {
            if (value != default)
            {
                OrderDate = value;
            }
        }
    }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } // Pending, Confirmed, Shipped, Delivered, Cancelled

    [JsonPropertyName("deliveryAddress")]
    public string DeliveryAddress { get; set; }

    [JsonPropertyName("items")]
    public List<OrderItem> OrderItems { get; set; } = new();

    // Backend compatibility alias when payload uses orderItems instead of items.
    [JsonPropertyName("orderItems")]
    public List<OrderItem>? OrderItemsAlias
    {
        get => OrderItems;
        set
        {
            if (value != null && value.Count > 0)
            {
                OrderItems = value;
            }
        }
    }

    public DateTime? EstimatedDelivery { get; set; }
}

public class OrderItem
{
    [JsonPropertyName("id")]
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("productName")]
    public string ProductName { get; set; }

    // Backend compatibility alias when product name comes as "name".
    [JsonPropertyName("name")]
    public string? ProductNameAlias
    {
        get => ProductName;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ProductName = value;
            }
        }
    }

    [JsonPropertyName("priceAtTime")]
    public decimal Price { get; set; }

    // Backend compatibility alias when price comes as "price".
    [JsonPropertyName("price")]
    public decimal PriceAlias
    {
        get => Price;
        set
        {
            if (value > 0)
            {
                Price = value;
            }
        }
    }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    public string DisplayProductName => string.IsNullOrWhiteSpace(ProductName)
        ? $"Product #{ProductId}"
        : ProductName;

    public decimal TotalPrice => Price * Quantity;
}

public class CreateOrderRequest
{
    [Required]
    public string DeliveryAddress { get; set; }
    public List<CreateOrderItem> Items { get; set; }
}

public class CreateOrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderStatusRequest
{
    public int OrderId { get; set; }
    public string Status { get; set; }
}
#endregion

#region API Response Models
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
#endregion
