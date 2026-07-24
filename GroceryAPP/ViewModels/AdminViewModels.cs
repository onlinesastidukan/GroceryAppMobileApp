using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GroceryApp.Models;
using GroceryApp.Services;

namespace GroceryApp.ViewModels;

#region AdminDashboardViewModel
public partial class AdminDashboardViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;

    [ObservableProperty]
    private int totalOrders;

    [ObservableProperty]
    private int pendingOrders;

    [ObservableProperty]
    private decimal totalRevenue;

    [ObservableProperty]
    private int totalProducts;

    [ObservableProperty]
    private int totalCategories;

    [ObservableProperty]
    private bool isAdminUser;

    public AdminDashboardViewModel(ApiService apiService, AuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
        IsAdminUser = !_authService.IsDealer;
    }

    [RelayCommand]
    protected override async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            IsAdminUser = !_authService.IsDealer;

            var loadErrors = new List<string>();

            // Check if user is a dealer
            if (_authService.IsDealer)
            {
                // Use dealer-specific dashboard endpoint
                var dashboardResponse = await _apiService.GetDealerDashboardAsync();
                if (dashboardResponse?.Success == true && dashboardResponse.Data != null)
                {
                    TotalOrders = dashboardResponse.Data.TotalOrders;
                    PendingOrders = dashboardResponse.Data.PendingOrders;
                    TotalRevenue = dashboardResponse.Data.TotalRevenue;
                    TotalProducts = dashboardResponse.Data.TotalProducts;
                    TotalCategories = 0; // Dealers don't need category count
                }
                else
                {
                    TotalOrders = 0;
                    PendingOrders = 0;
                    TotalRevenue = 0;
                    TotalProducts = 0;
                    TotalCategories = 0;
                    if (!string.IsNullOrWhiteSpace(dashboardResponse?.Message))
                    {
                        loadErrors.Add($"Dashboard: {dashboardResponse.Message}");
                    }
                }
            }
            else
            {
                // Admin flow - Load dashboard statistics
                var ordersResponse = await _apiService.GetAllOrdersAdminAsync();
                if (ordersResponse?.Success == true && ordersResponse.Data != null)
                {
                    TotalOrders = ordersResponse.Data.Count;
                    PendingOrders = ordersResponse.Data.Count(x =>
                        string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase));
                    TotalRevenue = ordersResponse.Data.Sum(x => x.TotalAmount);
                }
                else
                {
                    TotalOrders = 0;
                    PendingOrders = 0;
                    TotalRevenue = 0;
                    if (!string.IsNullOrWhiteSpace(ordersResponse?.Message))
                    {
                        loadErrors.Add($"Orders: {ordersResponse.Message}");
                    }
                }

                // Use the public categories endpoint — it is server-side filtered to active-only,
                // so the count is always accurate regardless of isActive deserialization.
                var categoriesResponse = await _apiService.GetCategoriesAsync();

                bool categoriesLoaded = false;
                var activeCategoryIds = new HashSet<int>();
                if (categoriesResponse?.Success == true && categoriesResponse.Data != null)
                {
                    // Apply client-side filter too as a safety net
                    var activeCategories = categoriesResponse.Data.Where(c => c.IsActive).ToList();
                    // If server already filtered (all returned are active), IsActive may default false —
                    // in that case use the full list since the server guarantees they are all active.
                    if (activeCategories.Count == 0 && categoriesResponse.Data.Count > 0)
                        activeCategories = categoriesResponse.Data;
                    TotalCategories = activeCategories.Count;
                    activeCategoryIds = activeCategories.Select(c => c.CategoryId).ToHashSet();
                    categoriesLoaded = true;
                }
                else
                {
                    TotalCategories = 0;
                    if (!string.IsNullOrWhiteSpace(categoriesResponse?.Message))
                    {
                        loadErrors.Add($"Categories: {categoriesResponse.Message}");
                    }
                }

                var productsResponse = await _apiService.GetAllProductsAdminAsync();
                if (productsResponse?.Success != true || productsResponse.Data == null)
                {
                    productsResponse = await _apiService.GetProductsAsync();
                }

                if (productsResponse?.Success == true && productsResponse.Data != null)
                {
                    // Count only products belonging to active categories (consistent with category view)
                    TotalProducts = categoriesLoaded
                        ? productsResponse.Data.Count(p => activeCategoryIds.Contains(p.CategoryId))
                        : productsResponse.Data.Count;
                }
                else
                {
                    TotalProducts = 0;
                    if (!string.IsNullOrWhiteSpace(productsResponse?.Message))
                    {
                        loadErrors.Add($"Products: {productsResponse.Message}");
                    }
                }
            }

            if (loadErrors.Count > 0)
            {
                SetError(string.Join(" | ", loadErrors));
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

    // Navigation handled in code-behind (AdminDashboardPage.xaml.cs)
    // These commands are kept for binding but navigation is managed there
}
#endregion

#region AdminOrdersViewModel
public partial class AdminOrdersViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;

    [ObservableProperty]
    private ObservableCollection<Order> orders;

    public AdminOrdersViewModel(ApiService apiService, AuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
        Orders = new ObservableCollection<Order>();
    }

    public bool HasNoOrders => Orders == null || Orders.Count == 0;

    [RelayCommand]
    protected override async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            ApiResponse<List<Order>> response;
            List<Order> visibleOrders;

            if (_authService.IsDealer)
            {
                response = await _apiService.GetDealerOrdersAsync();

                if (response?.Success == true && response.Data != null)
                {
                    // Dealer order list may intentionally be summary-only (Items omitted)
                    // for performance; detail page fetch will load full order items.
                    foreach (var order in response.Data)
                    {
                        order.OrderItems ??= new List<OrderItem>();
                    }

                    visibleOrders = response.Data;
                }
                else
                {
                    // Strict fallback: derive dealer shop ownership from dealer products
                    var dealerProductsResponse = await _apiService.GetDealerProductsAsync();
                    if (dealerProductsResponse?.Success != true || dealerProductsResponse.Data == null)
                    {
                        SetError(response?.Message ?? "Unable to load your shop orders.");
                        OnPropertyChanged(nameof(HasNoOrders));
                        return;
                    }

                    var dealerProductIds = dealerProductsResponse.Data
                        .Select(p => p.ProductId)
                        .ToHashSet();

                    if (dealerProductIds.Count == 0)
                    {
                        Orders.Clear();
                        OnPropertyChanged(nameof(HasNoOrders));
                        return;
                    }

                    var allOrdersResponse = await _apiService.GetAllOrdersAdminAsync(includeItems: true);
                    if (allOrdersResponse?.Success != true || allOrdersResponse.Data == null)
                    {
                        SetError(response?.Message ?? allOrdersResponse?.Message ?? "Unable to load your shop orders.");
                        OnPropertyChanged(nameof(HasNoOrders));
                        return;
                    }

                    visibleOrders = allOrdersResponse.Data
                        .Where(order => order.OrderItems != null && order.OrderItems.Any(item => dealerProductIds.Contains(item.ProductId)))
                        .ToList();
                }
            }
            else
            {
                response = await _apiService.GetAllOrdersAdminAsync();
                visibleOrders = response?.Data ?? new List<Order>();
                foreach (var order in visibleOrders)
                {
                    order.OrderItems ??= new List<OrderItem>();
                }
            }

            Orders.Clear();
            foreach (var order in visibleOrders.OrderByDescending(x => x.OrderDate))
            {
                Orders.Add(order);
            }
            OnPropertyChanged(nameof(HasNoOrders));
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
    public Order CurrentOrder { get; set; }
}
#endregion

#region AdminOrderDetailViewModel
public partial class AdminOrderDetailViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private Order order;

    [ObservableProperty]
    private int orderId;

    [ObservableProperty]
    private ObservableCollection<OrderItem> orderItems;

    [ObservableProperty]
    private string selectedStatus;

    private readonly string[] _statuses = { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" };
    private string _originalStatus;

    public string[] AvailableStatuses => _statuses;

    public bool CanApplyStatus => !string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != _originalStatus;

    partial void OnSelectedStatusChanged(string value)
    {
        UpdateOrderStatusCommand.NotifyCanExecuteChanged();
    }

    public AdminOrderDetailViewModel(ApiService apiService)
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

            // Show header immediately from passed order while we fetch full details.
            if (Order != null)
            {
                SelectedStatus = Order.Status;
                _originalStatus = Order.Status;
                UpdateOrderStatusCommand.NotifyCanExecuteChanged();
            }

            // Always fetch full order from admin endpoint to get address + items.
            var fetchId = OrderId > 0 ? OrderId : Order?.OrderId ?? 0;
            if (fetchId > 0)
            {
                var response = await _apiService.GetAdminOrderByIdAsync(fetchId);
                if (response?.Success != true || response.Data == null || response.Data.OrderItems == null || response.Data.OrderItems.Count == 0)
                {
                    var fallback = await _apiService.GetOrderByIdAsync(fetchId);
                    if (fallback?.Success == true && fallback.Data != null)
                    {
                        response = fallback;
                    }
                }

                // If admin endpoint responded but has no mobile, also try the customer endpoint
                // which often embeds user contact info directly in the order payload.
                if (response?.Success == true && response.Data != null
                    && string.IsNullOrWhiteSpace(response.Data.UserMobileNumber))
                {
                    try
                    {
                        var customerResponse = await _apiService.GetOrderByIdAsync(fetchId);
                        if (customerResponse?.Success == true && customerResponse.Data != null
                            && !string.IsNullOrWhiteSpace(customerResponse.Data.UserMobileNumber))
                        {
                            // Patch mobile (and name/address if also missing) from customer endpoint
                            response.Data.UserMobileNumber = customerResponse.Data.UserMobileNumber;
                            if (string.IsNullOrWhiteSpace(response.Data.UserFullName) && !string.IsNullOrWhiteSpace(customerResponse.Data.UserFullName))
                                response.Data.UserFullName = customerResponse.Data.UserFullName;
                            if (string.IsNullOrWhiteSpace(response.Data.UserAddress) && !string.IsNullOrWhiteSpace(customerResponse.Data.UserAddress))
                                response.Data.UserAddress = customerResponse.Data.UserAddress;
                        }
                    }
                    catch { /* non-critical */ }
                }

                if (response?.Success == true && response.Data != null)
                {
                    Order = response.Data;
                    SelectedStatus = Order.Status;
                    _originalStatus = Order.Status;
                    UpdateOrderStatusCommand.NotifyCanExecuteChanged();

                    // If mobile number is missing from order, fetch it from the user record
                    if (string.IsNullOrWhiteSpace(Order.UserMobileNumber) && Order.UserId > 0)
                    {
                        try
                        {
                            var userResponse = await _apiService.GetAdminUserByIdAsync(Order.UserId);
                            if (userResponse?.Success == true && userResponse.Data != null
                                && !string.IsNullOrWhiteSpace(userResponse.Data.MobileNumber))
                            {
                                Order.UserMobileNumber = userResponse.Data.MobileNumber;
                                if (string.IsNullOrWhiteSpace(Order.UserFullName) && !string.IsNullOrWhiteSpace(userResponse.Data.FullName))
                                    Order.UserFullName = userResponse.Data.FullName;
                            }
                        }
                        catch { /* non-critical — silently skip */ }
                    }

                    OnPropertyChanged(nameof(Order));
                    OrderItems.Clear();
                    foreach (var item in Order.OrderItems)
                        OrderItems.Add(item);
                    OnPropertyChanged(nameof(HasNoItems));
                }
                else if (Order != null)
                {
                    // API failed but we have header from list — render what we can.
                    OrderItems.Clear();
                    foreach (var item in Order.OrderItems ?? new())
                        OrderItems.Add(item);
                    OnPropertyChanged(nameof(HasNoItems));
                }
                else
                {
                    SetError(response?.Message ?? "Failed to load order");
                }
            }
            else if (Order != null)
            {
                _originalStatus = Order.Status;
                UpdateOrderStatusCommand.NotifyCanExecuteChanged();
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

    public bool HasNoItems => OrderItems == null || OrderItems.Count == 0;

    [RelayCommand(CanExecute = nameof(CanApplyStatus))]
    public async Task UpdateOrderStatusAsync()
    {
        if (Order == null || string.IsNullOrEmpty(SelectedStatus))
        {
            SetError("Please select a status");
            return;
        }

        try
        {
            IsLoading = true;
            ClearError();

            var response = await _apiService.UpdateOrderStatusAsync(Order.OrderId, SelectedStatus);
            if (response?.Success == true)
            {
                Order.Status = SelectedStatus;
                _originalStatus = SelectedStatus;
                UpdateOrderStatusCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(Order));


                bool isTerminal = SelectedStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
                    || SelectedStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase);

                if (isTerminal)
                    SetSuccess($"Order marked as {SelectedStatus} ✓");
                else
                    SetStatusInfo($"Status updated to '{SelectedStatus}'");
            }
            else
            {
                SetError(response?.Message ?? "Failed to update order");
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

#region AdminProductsViewModel
public partial class AdminProductsViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;
    private List<Product> _allProducts = new();

    [ObservableProperty]
    private ObservableCollection<Product> products;

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
    }

    public AdminProductsViewModel(ApiService apiService, AuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
        Products = new ObservableCollection<Product>();
    }

    [RelayCommand]
    protected override async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();
            System.Diagnostics.Debug.WriteLine("[ADMIN PRODUCTS VM] InitializeAsync started");

            var response = _authService.IsDealer
                ? await _apiService.GetDealerProductsAsync()
                : await _apiService.GetAllProductsAdminAsync();
            System.Diagnostics.Debug.WriteLine($"[ADMIN PRODUCTS VM] API response -> Success={response?.Success}, Message={response?.Message}, DataCount={response?.Data?.Count ?? 0}, IsDealer={_authService.IsDealer}");
            if (response?.Success == true && response.Data != null)
            {
                _allProducts = new List<Product>(response.Data);
                FilterProducts();
                System.Diagnostics.Debug.WriteLine($"[ADMIN PRODUCTS VM] Products collection populated. Final count={Products.Count}");
            }
            else
            {
                SetError(response?.Message ?? "Failed to load products");
                System.Diagnostics.Debug.WriteLine($"[ADMIN PRODUCTS VM] Failed to load products. ErrorMessage={ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ADMIN PRODUCTS VM] Exception in InitializeAsync: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            System.Diagnostics.Debug.WriteLine("[ADMIN PRODUCTS VM] InitializeAsync finished");
        }
    }

    // Navigation handled in code-behind
    public Product CurrentProduct { get; set; }

    [RelayCommand]
    public async Task DeleteProductAsync(Product product)
    {
        if (product == null) return;

        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Confirm",
            $"Delete {product.Name}?",
            "Yes",
            "No"
        );

        if (confirm)
        {
            try
            {
                IsLoading = true;
                var response = _authService.IsDealer
                    ? await _apiService.DeleteDealerProductAsync(product.ProductId)
                    : await _apiService.DeleteProductAsync(product.ProductId);
                if (response?.Success == true)
                {
                    _allProducts.Remove(product);
                    Products.Remove(product);
                    await Application.Current.MainPage.DisplayAlert("Success", "Product deleted", "OK");
                }
                else
                {
                    SetError(response?.Message ?? "Failed to delete product");
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
}
#endregion

#region AdminCategoriesViewModel
public partial class AdminCategoriesViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<Category> categories;

    public AdminCategoriesViewModel(ApiService apiService)
    {
        _apiService = apiService;
        Categories = new ObservableCollection<Category>();
    }

    [RelayCommand]
    protected override async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();
            System.Diagnostics.Debug.WriteLine("[ADMIN CATEGORIES] Starting InitializeAsync...");

            // Use the same public categories endpoint the customer view uses.
            // The server filters to active-only, so soft-deleted categories never appear here.
            var response = await _apiService.GetCategoriesAsync();
            System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES] API Response: Success={response?.Success}, Count={response?.Data?.Count ?? 0}");

            if (response?.Success == true && response.Data != null)
            {
                Categories.Clear();
                // Apply client-side IsActive filter as a safety net.
                // If the server didn't include isActive in the payload (all default to false)
                // fall back to the full list since the public endpoint is already active-only.
                var activeCategories = response.Data.Where(c => c.IsActive).ToList();
                var toShow = activeCategories.Count > 0 ? activeCategories : response.Data;
                System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES] Total from API: {response.Data.Count}, Showing: {toShow.Count}");
                foreach (var category in toShow)
                {
                    Categories.Add(category);
                    System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES] Added category: {category.Name}");
                }
                System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES] Total categories loaded: {Categories.Count}");
            }
            else
            {
                SetError(response?.Message ?? "Failed to load shops");
                System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES] Failed to load categories: {response?.Message}");
            }
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES] Exception: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Navigation handled in code-behind
    public Category CurrentCategory { get; set; }

    [RelayCommand]
    public async Task DeleteCategoryAsync(Category category)
    {
        if (category == null) return;

        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Confirm",
            $"Delete shop {category.Name}?",
            "Yes",
            "No"
        );

        if (confirm)
        {
            try
            {
                IsLoading = true;
                var response = await _apiService.DeleteCategoryAsync(category.CategoryId);
                if (response?.Success == true)
                {
                    Categories.Remove(category);
                    await Application.Current.MainPage.DisplayAlert("Success", "Shop deleted", "OK");
                }
                else
                {
                    var msg = response?.Message ?? "Failed to delete shop";
                    SetError(msg);
                    await Application.Current.MainPage.DisplayAlert("Delete Failed", msg, "OK");
                }
            }
            catch (Exception ex)
            {
                SetError($"Error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
#endregion

#region AdminUsersViewModel
public partial class AdminUsersViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    public AdminUsersViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }
}
#endregion
