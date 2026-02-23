using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GroceryApp.Models;
using GroceryApp.Services;

namespace GroceryApp.ViewModels;

#region CustomerCategoryViewModel
public partial class CustomerCategoryViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly CartService _cartService;

    [ObservableProperty]
    private ObservableCollection<Category> categories;

    [ObservableProperty]
    private int selectedCategoryId;

    public CustomerCategoryViewModel(ApiService apiService, CartService cartService)
    {
        _apiService = apiService;
        _cartService = cartService;
        Categories = new ObservableCollection<Category>();
    }

    [RelayCommand]
    protected override async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var response = await _apiService.GetCategoriesAsync();
            if (response?.Success == true && response.Data != null)
            {
                Categories.Clear();
                foreach (var category in response.Data)
                {
                    Categories.Add(category);
                }
            }
            else
            {
                SetError(response?.Message ?? "Failed to load categories");
            }
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Navigation handled in code-behind (CustomerCategoryPage.xaml.cs)
    // SelectCategoryAsync command is not used - selection is handled via SelectionChanged event
}
#endregion

#region CustomerProductViewModel
public partial class CustomerProductViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly CartService _cartService;

    [ObservableProperty]
    private ObservableCollection<Product> products;

    [ObservableProperty]
    private int categoryId;

    [ObservableProperty]
    private string categoryName;

    public CustomerProductViewModel(ApiService apiService, CartService cartService)
    {
        _apiService = apiService;
        _cartService = cartService;
        Products = new ObservableCollection<Product>();
    }

    protected override async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var response = await _apiService.GetProductsAsync(CategoryId);
            if (response?.Success == true && response.Data != null)
            {
                Products.Clear();
                foreach (var product in response.Data)
                {
                    Products.Add(product);
                }
            }
            else
            {
                SetError(response?.Message ?? "Failed to load products");
            }
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddToCartAsync(Product product)
    {
        if (product != null)
        {
            _cartService.AddToCart(product, 1);
        }
        await Task.CompletedTask;
    }

    // Navigation handled in code-behind
    public Product CurrentProduct { get; set; }
}
#endregion

#region CartViewModel
public partial class CartViewModel : BaseViewModel
{
    private readonly CartService _cartService;
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<CartItem> cartItems;

    [ObservableProperty]
    private decimal totalPrice;

    [ObservableProperty]
    private string deliveryAddress = "";

    [ObservableProperty]
    private bool orderPlacedSuccessfully;

    public CartViewModel(CartService cartService, ApiService apiService)
    {
        _cartService = cartService;
        _apiService = apiService;
        CartItems = new ObservableCollection<CartItem>();
    }

    protected override async Task InitializeAsync()
    {
        try
        {
            CartItems.Clear();
            foreach (var item in _cartService.CartItems)
            {
                CartItems.Add(item);
            }
            UpdateTotalPrice();
            await base.InitializeAsync();
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task RemoveFromCartAsync(CartItem item)
    {
        if (item != null)
        {
            _cartService.RemoveFromCart(item.ProductId);
            CartItems.Remove(item);
            UpdateTotalPrice();
        }
    }

    [RelayCommand]
    public async Task UpdateQuantityAsync(CartItem item)
    {
        UpdateTotalPrice();
    }

    [RelayCommand]
    public async Task PlaceOrderAsync()
    {
        if (CartItems.Count == 0)
        {
            SetError("Cart is empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(DeliveryAddress))
        {
            SetError("Please enter delivery address");
            return;
        }

        try
        {
            IsLoading = true;
            ClearError();

            var orderRequest = new CreateOrderRequest
            {
                DeliveryAddress = DeliveryAddress,
                Items = CartItems.ToList()
            };

            var response = await _apiService.CreateOrderAsync(orderRequest);
            if (response?.Success == true)
            {
                _cartService.ClearCart();
                CartItems.Clear();
                OrderPlacedSuccessfully = true;
            }
            else
            {
                SetError(response?.Message ?? "Failed to place order");
            }
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateTotalPrice()
    {
        TotalPrice = CartItems.Sum(x => x.TotalPrice);
    }
}
#endregion

#region CustomerOrderHistoryViewModel
public partial class CustomerOrderHistoryViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<Order> orders;

    public CustomerOrderHistoryViewModel(ApiService apiService)
    {
        _apiService = apiService;
        Orders = new ObservableCollection<Order>();
    }

    [RelayCommand]
    protected override async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var response = await _apiService.GetOrdersAsync();
            if (response?.Success == true && response.Data != null)
            {
                Orders.Clear();
                foreach (var order in response.Data.OrderByDescending(x => x.OrderDate))
                {
                    Orders.Add(order);
                }
            }
            else
            {
                SetError(response?.Message ?? "Failed to load orders");
            }
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Navigation handled in code-behind
    public Order SelectedOrder { get; set; }
}
#endregion

#region CustomerOrderDetailViewModel
public partial class CustomerOrderDetailViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private Order order;

    [ObservableProperty]
    private int orderId;

    [ObservableProperty]
    private ObservableCollection<OrderItem> orderItems;

    public CustomerOrderDetailViewModel(ApiService apiService)
    {
        _apiService = apiService;
        OrderItems = new ObservableCollection<OrderItem>();
    }

    protected override async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            if (OrderId > 0)
            {
                var response = await _apiService.GetOrderByIdAsync(OrderId);
                if (response?.Success == true && response.Data != null)
                {
                    Order = response.Data;
                    OrderItems.Clear();
                    foreach (var item in Order.OrderItems)
                    {
                        OrderItems.Add(item);
                    }
                }
                else
                {
                    SetError(response?.Message ?? "Failed to load order");
                }
            }
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
#endregion
