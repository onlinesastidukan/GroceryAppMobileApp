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

    [ObservableProperty]
    private int totalOrders;

    [ObservableProperty]
    private decimal totalRevenue;

    [ObservableProperty]
    private int totalProducts;

    [ObservableProperty]
    private int totalCategories;

    public AdminDashboardViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    protected override async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            // Load dashboard statistics
            var ordersResponse = await _apiService.GetAllOrdersAdminAsync();
            if (ordersResponse?.Success == true && ordersResponse.Data != null)
            {
                TotalOrders = ordersResponse.Data.Count;
                TotalRevenue = ordersResponse.Data.Sum(x => x.TotalAmount);
            }

            var productsResponse = await _apiService.GetAllProductsAdminAsync();
            TotalProducts = productsResponse?.Data?.Count ?? 0;

            var categoriesResponse = await _apiService.GetAllCategoriesAdminAsync();
            TotalCategories = categoriesResponse?.Data?.Count ?? 0;
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

    [ObservableProperty]
    private ObservableCollection<Order> orders;

    public AdminOrdersViewModel(ApiService apiService)
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

            var response = await _apiService.GetAllOrdersAdminAsync();
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

    public string[] AvailableStatuses => _statuses;

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

            if (OrderId > 0)
            {
                var response = await _apiService.GetOrderByIdAsync(OrderId);
                if (response?.Success == true && response.Data != null)
                {
                    Order = response.Data;
                    SelectedStatus = Order.Status;
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

    [RelayCommand]
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
                await Application.Current.MainPage.DisplayAlert("Success", "Order status updated", "OK");
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

    [ObservableProperty]
    private ObservableCollection<Product> products;

    public AdminProductsViewModel(ApiService apiService)
    {
        _apiService = apiService;
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

            var response = await _apiService.GetAllProductsAdminAsync();
            System.Diagnostics.Debug.WriteLine($"[ADMIN PRODUCTS VM] API response -> Success={response?.Success}, Message={response?.Message}, DataCount={response?.Data?.Count ?? 0}");
            if (response?.Success == true && response.Data != null)
            {
                Products.Clear();
                foreach (var product in response.Data)
                {
                    Products.Add(product);
                }
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
                var response = await _apiService.DeleteProductAsync(product.ProductId);
                if (response?.Success == true)
                {
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

            var response = await _apiService.GetAllCategoriesAdminAsync();
            System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES] API Response: Success={response?.Success}, Count={response?.Data?.Count ?? 0}");
            
            if (response?.Success == true && response.Data != null)
            {
                Categories.Clear();
                foreach (var category in response.Data)
                {
                    Categories.Add(category);
                    System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES] Added category: {category.Name}");
                }
                System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES] Total categories loaded: {Categories.Count}");
            }
            else
            {
                SetError(response?.Message ?? "Failed to load categories");
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
            $"Delete {category.Name}?",
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
                    await Application.Current.MainPage.DisplayAlert("Success", "Category deleted", "OK");
                }
                else
                {
                    SetError(response?.Message ?? "Failed to delete category");
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
