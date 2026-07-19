using System;
using System.Collections.Generic;
using System.ComponentModel;
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

public class AppUser
{
    [JsonPropertyName("id")]
    public int UserId { get; set; }

    [JsonPropertyName("userId")]
    public string UserIdAlias { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; }

    [JsonPropertyName("name")]
    public string NameAlias
    {
        get => FullName;
        set { if (!string.IsNullOrWhiteSpace(value)) FullName = value; }
    }

    [JsonPropertyName("mobileNumber")]
    public string MobileNumber { get; set; }

    [JsonPropertyName("mobile")]
    public string MobileAlias
    {
        get => MobileNumber;
        set { if (!string.IsNullOrWhiteSpace(value)) MobileNumber = value; }
    }

    [JsonPropertyName("phone")]
    public string PhoneAlias
    {
        get => MobileNumber;
        set { if (!string.IsNullOrWhiteSpace(value)) MobileNumber = value; }
    }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }
}
#endregion

#region Product Models
public class Product
{
    [JsonPropertyName("id")]
    public int ProductId { get; set; }

    // Some endpoints return productId instead of id
    [JsonPropertyName("productId")]
    public int ProductIdAlias
    {
        get => ProductId;
        set { if (value > 0) ProductId = value; }
    }

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
    public DateTime? CreatedDate { get; set; }

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
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    // Backend alias: some endpoints return isActive as is_active (snake_case)
    [JsonPropertyName("is_active")]
    public bool IsActiveSnakeAlias
    {
        get => IsActive;
        set => IsActive = value;  // Unconditional — propagate both true AND false
    }

    [JsonPropertyName("photoUrl")]
    public string PhotoUrl { get; set; }

    // Backend alias: some endpoints return photoUrl as imageUrl
    [JsonPropertyName("imageUrl")]
    public string ImageUrlAlias
    {
        get => PhotoUrl;
        set { if (!string.IsNullOrWhiteSpace(value)) PhotoUrl = value; }
    }

    [JsonPropertyName("dealerId")]
    public int? DealerId { get; set; }

    [JsonPropertyName("shopkeeperId")]
    public int? ShopkeeperIdAlias
    {
        get => DealerId;
        set => DealerId = value;
    }

    public List<Product> Products { get; set; } = new();
}

public class CreateCategoryRequest
{
    [Required]
    public string Name { get; set; }
    public string Description { get; set; }
    public string PhotoUrl { get; set; }
}

public class UpdateCategoryRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string PhotoUrl { get; set; }
    public bool IsActive { get; set; }
}
#endregion

#region Cart Models
public class CartItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }

    private int _quantity;
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity == value) return;
            _quantity = value;
            Notify(nameof(Quantity));
            Notify(nameof(TotalPrice));
        }
    }

    /// <summary>Max available stock so +/- can be capped.</summary>
    public int Stock { get; set; } = int.MaxValue;

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

// IST timezone resolved once at class-load time (tries IANA then Windows ID).
internal static class IstZone
{
    internal static readonly TimeZoneInfo Zone = Resolve();
    private static TimeZoneInfo Resolve()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); } catch { }
        try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); } catch { }
        return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromHours(5.5), "India Standard Time", "IST");
    }
}

public class Order
{
    [JsonPropertyName("id")]
    public int OrderId { get; set; }

    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("userFullName")]
    public string UserFullName { get; set; }

    [JsonPropertyName("customerName")]
    public string CustomerNameAlias
    {
        get => UserFullName;
        set { if (!string.IsNullOrWhiteSpace(value)) UserFullName = value; }
    }

    [JsonPropertyName("userMobileNumber")]
    public string UserMobileNumber { get; set; }

    // Backend alias variants for mobile number
    [JsonPropertyName("mobileNumber")]
    public string MobileNumberAlias
    {
        get => UserMobileNumber;
        set { if (!string.IsNullOrWhiteSpace(value)) UserMobileNumber = value; }
    }

    [JsonPropertyName("phone")]
    public string PhoneAlias
    {
        get => UserMobileNumber;
        set { if (!string.IsNullOrWhiteSpace(value)) UserMobileNumber = value; }
    }

    [JsonPropertyName("mobile")]
    public string MobileAlias
    {
        get => UserMobileNumber;
        set { if (!string.IsNullOrWhiteSpace(value)) UserMobileNumber = value; }
    }

    [JsonPropertyName("contactNumber")]
    public string ContactNumberAlias
    {
        get => UserMobileNumber;
        set { if (!string.IsNullOrWhiteSpace(value)) UserMobileNumber = value; }
    }

    [JsonPropertyName("customerMobile")]
    public string CustomerMobileAlias
    {
        get => UserMobileNumber;
        set { if (!string.IsNullOrWhiteSpace(value)) UserMobileNumber = value; }
    }

    [JsonPropertyName("customerPhone")]
    public string CustomerPhoneAlias
    {
        get => UserMobileNumber;
        set { if (!string.IsNullOrWhiteSpace(value)) UserMobileNumber = value; }
    }

    // Nested user/customer objects returned by some backend endpoints
    private OrderUser _userInfo;

    [JsonPropertyName("user")]
    public OrderUser UserInfo
    {
        get => _userInfo;
        set
        {
            _userInfo = value;
            if (value == null) return;
            if (string.IsNullOrWhiteSpace(UserFullName) && !string.IsNullOrWhiteSpace(value.FullName))
                UserFullName = value.FullName;
            if (string.IsNullOrWhiteSpace(UserMobileNumber) && !string.IsNullOrWhiteSpace(value.MobileNumber))
                UserMobileNumber = value.MobileNumber;
            if (string.IsNullOrWhiteSpace(UserAddress) && !string.IsNullOrWhiteSpace(value.Address))
                UserAddress = value.Address;
        }
    }

    [JsonPropertyName("customer")]
    public OrderUser CustomerInfo
    {
        get => _userInfo;
        set => UserInfo = value;
    }

    [JsonPropertyName("userAddress")]
    public string UserAddress { get; set; }

    [JsonPropertyName("customerAddress")]
    public string CustomerAddressAlias
    {
        get => UserAddress;
        set { if (!string.IsNullOrWhiteSpace(value)) UserAddress = value; }
    }

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

    /// <summary>OrderDate converted to Indian Standard Time (IST = UTC+5:30) for display.</summary>
    [JsonIgnore]
    public DateTime OrderDateIST
    {
        get
        {
            var dt = OrderDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(OrderDate, DateTimeKind.Utc)
                : OrderDate;
            try { return TimeZoneInfo.ConvertTimeFromUtc(dt.ToUniversalTime(), IstZone.Zone); }
            catch { return OrderDate.AddHours(5).AddMinutes(30); }
        }
    }

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

/// <summary>Nested user/customer object that some backend order endpoints embed in the order response.</summary>
public class OrderUser
{
    [JsonPropertyName("id")]
    public int UserId { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; }

    [JsonPropertyName("name")]
    public string NameAlias
    {
        get => FullName;
        set { if (!string.IsNullOrWhiteSpace(value)) FullName = value; }
    }

    [JsonPropertyName("mobileNumber")]
    public string MobileNumber { get; set; }

    [JsonPropertyName("phone")]
    public string PhoneAlias
    {
        get => MobileNumber;
        set { if (!string.IsNullOrWhiteSpace(value)) MobileNumber = value; }
    }

    [JsonPropertyName("mobile")]
    public string MobileAlias
    {
        get => MobileNumber;
        set { if (!string.IsNullOrWhiteSpace(value)) MobileNumber = value; }
    }

    [JsonPropertyName("address")]
    public string Address { get; set; }
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
    [JsonPropertyName("deliveryAddress")]
    public string DeliveryAddress { get; set; }

    [JsonPropertyName("mobileNumber")]
    public string MobileNumber { get; set; }

    [JsonPropertyName("userMobileNumber")]
    public string UserMobileNumberAlias
    {
        get => MobileNumber;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                MobileNumber = value;
            }
        }
    }

    [JsonPropertyName("customerMobile")]
    public string CustomerMobileAlias
    {
        get => MobileNumber;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                MobileNumber = value;
            }
        }
    }

    [JsonPropertyName("customerMobileNumber")]
    public string CustomerMobileNumberAlias
    {
        get => MobileNumber;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                MobileNumber = value;
            }
        }
    }

    [JsonPropertyName("customerAddress")]
    public string CustomerAddressAlias
    {
        get => DeliveryAddress;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                DeliveryAddress = value;
            }
        }
    }

    [JsonPropertyName("address")]
    public string AddressAlias
    {
        get => DeliveryAddress;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                DeliveryAddress = value;
            }
        }
    }

    [JsonPropertyName("items")]
    public List<CreateOrderItem> Items { get; set; }
}

public class CreateOrderItem
{
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("quantity")]
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
