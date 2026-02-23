using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "Address is required")]
    public string Address { get; set; }
}

public class RegisterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int UserId { get; set; }
}

public class UpdateAddressRequest
{
    [Required(ErrorMessage = "Address is required")]
    public string Address { get; set; }
}

public class AuthData
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Role { get; set; }
    public string Token { get; set; }
}

public class UserLoginInfo
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Role { get; set; }
    public string Token { get; set; }
}
#endregion

#region Product Models
public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public string ImageUrl { get; set; }
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
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public string ImageUrl { get; set; }
}
#endregion

#region Category Models
public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedDate { get; set; }
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
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } // Pending, Confirmed, Shipped, Delivered, Cancelled
    public string DeliveryAddress { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new();
    public DateTime? EstimatedDelivery { get; set; }
}

public class OrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => Price * Quantity;
}

public class CreateOrderRequest
{
    [Required]
    public string DeliveryAddress { get; set; }
    public List<CartItem> Items { get; set; }
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
