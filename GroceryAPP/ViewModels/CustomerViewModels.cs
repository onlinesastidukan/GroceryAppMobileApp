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
                foreach (var category in response.Data.Where(c => c.IsActive))
                {
                    Categories.Add(category);
                }

                if (Categories.Count == 0)
                {
                    // Empty list is a valid state; page shows empty-state UI.
                    ClearError();
                }
            }
            else
            {
                SetError(response?.Message ?? "Unable to load shops right now. Please try again.");
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
    private List<Product> _allProducts = new();
    private int _loadedCategoryId = -1; // tracks which category's products are cached

    [ObservableProperty]
    private ObservableCollection<Product> products;

    [ObservableProperty]
    private bool hasNoProducts;

    private int _categoryId;
    public int CategoryId
    {
        get => _categoryId;
        set
        {
            if (_categoryId == value) return;
            _categoryId = value;
            _loadedCategoryId = -1; // invalidate cache on category change
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    private string categoryName;

    [ObservableProperty]
    private string searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => FilterProducts();

    private void FilterProducts()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        var filtered = string.IsNullOrEmpty(query)
            ? _allProducts
            : _allProducts.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(p.Description) && p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));

        Products.Clear();
        foreach (var product in filtered)
            Products.Add(product);

        HasNoProducts = Products.Count == 0;
    }

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

            // Skip re-fetch if we already have data for this category (pre-fetched before navigation)
            if (_loadedCategoryId == CategoryId && _allProducts.Count > 0)
            {
                FilterProducts();
                return;
            }

            var response = await _apiService.GetProductsAsync(CategoryId);
            if (response?.Success == true && response.Data != null)
            {
                // Defensive filtering: if backend returns an unfiltered list,
                // still show only products from the selected category.
                var sourceProduts = CategoryId > 0
                    ? response.Data.Where(p => p.CategoryId == CategoryId)
                    : response.Data;

                _allProducts = new List<Product>(sourceProduts);
                _loadedCategoryId = CategoryId;
                FilterProducts();
            }
            else
            {
                HasNoProducts = true;
                SetError(response?.Message ?? "Failed to load products");
            }
        }
        catch (Exception ex)
        {
            HasNoProducts = true;
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
        await AddToCartWithQuantityAsync(product, 1);
    }

    public async Task AddToCartWithQuantityAsync(Product product, int quantity)
    {
        if (product != null)
        {
            if (product.Stock <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Out Of Stock", "This product is currently unavailable.", "OK");
                return;
            }

            var safeQty = Math.Max(1, Math.Min(quantity, product.Stock));
            _cartService.AddToCart(product, safeQty);
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
    private readonly GuestSessionService _guestSessionService;

    [ObservableProperty]
    private ObservableCollection<CartItem> cartItems;

    [ObservableProperty]
    private decimal totalPrice;

    [ObservableProperty]
    private string customerName = "";

    [ObservableProperty]
    private string deliveryAddress = "";

    [ObservableProperty]
    private string mobileNumber = "";

    [ObservableProperty]
    private bool orderPlacedSuccessfully;

    public CartViewModel(CartService cartService, ApiService apiService, GuestSessionService guestSessionService)
    {
        _cartService = cartService;
        _apiService = apiService;
        _guestSessionService = guestSessionService;
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

            if (string.IsNullOrWhiteSpace(MobileNumber))
            {
                MobileNumber = await _guestSessionService.GetGuestMobileAsync();
            }

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
    public void IncreaseQuantity(CartItem item)
    {
        if (item == null) return;
        if (item.Quantity < item.Stock)
        {
            item.Quantity++;
            UpdateTotalPrice();
        }
    }

    [RelayCommand]
    public void DecreaseQuantity(CartItem item)
    {
        if (item == null) return;
        if (item.Quantity > 1)
        {
            item.Quantity--;
            UpdateTotalPrice();
        }
        else
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

        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            SetError("Please enter customer name");
            return;
        }

        if (string.IsNullOrWhiteSpace(DeliveryAddress))
        {
            SetError("Please enter delivery address");
            return;
        }

        if (string.IsNullOrWhiteSpace(MobileNumber))
        {
            SetError("Please enter mobile number");
            return;
        }

        try
        {
            IsLoading = true;
            ClearError();

            var orderRequest = new CreateOrderRequest
            {
                CustomerName = CustomerName,
                DeliveryAddress = DeliveryAddress,
                MobileNumber = MobileNumber,
                Items = CartItems.Select(x => new CreateOrderItem
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity
                }).ToList()
            };

            var response = await _apiService.CreateOrderAsync(orderRequest);
            if (response?.Success == true)
            {
                await _guestSessionService.SaveGuestMobileAsync(MobileNumber);

                var placedOrderId = response.Data?.OrderId ?? 0;
                if (placedOrderId > 0)
                {
                    _ = _apiService.TriggerOrderPlacedNotificationAsync(placedOrderId, MobileNumber);
                }

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

    public bool HasNoOrders => Orders == null || Orders.Count == 0;

    public void NotifyOrdersChanged() => OnPropertyChanged(nameof(HasNoOrders));

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
                OnPropertyChanged(nameof(HasNoOrders));
            }
            else
            {
                SetError(response?.Message ?? "Failed to load orders");
                OnPropertyChanged(nameof(HasNoOrders));
            }
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
            OnPropertyChanged(nameof(HasNoOrders));
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

    public bool HasNoItems => OrderItems == null || OrderItems.Count == 0;

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
                    OnPropertyChanged(nameof(HasNoItems));
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
