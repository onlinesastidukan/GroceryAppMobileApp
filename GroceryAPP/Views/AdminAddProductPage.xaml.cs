using GroceryApp.Services;
using GroceryApp.Models;
using GroceryApp.ViewModels;

namespace GroceryApp.Views;

public partial class AdminAddProductPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<Category> _categories = new();

    public AdminAddProductPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCategories();
    }

    private async Task LoadCategories()
    {
        try
        {
            var response = await _apiService.GetAllCategoriesAdminAsync();
            if (response?.Success == true && response.Data != null)
            {
                _categories = response.Data.ToList();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CategoryPicker.ItemsSource = _categories.Select(c => c.Name).ToList();
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ADD PRODUCT] Error loading categories: {ex.Message}");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var productName = ProductNameEntry.Text;
        var priceText = PriceEntry.Text;
        var description = DescriptionEditor.Text;
        var stockText = StockQuantityEntry.Text;
        var photoUrl = PhotoUrlEntry.Text;
        var categoryIndex = CategoryPicker.SelectedIndex;

        if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(priceText) || string.IsNullOrWhiteSpace(stockText) || categoryIndex < 0)
        {
            await DisplayAlert("Validation", "Please fill all required fields", "OK");
            return;
        }

        if (!decimal.TryParse(priceText, out decimal price))
        {
            await DisplayAlert("Validation", "Invalid price format", "OK");
            return;
        }

        if (!int.TryParse(stockText, out int stockQuantity) || stockQuantity < 0)
        {
            await DisplayAlert("Validation", "Invalid stock quantity", "OK");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[ADD PRODUCT] Creating product: {productName}, Price: {price}, Stock: {stockQuantity}");
            
            var product = new CreateProductRequest
            {
                Name = productName,
                Price = price,
                Description = description ?? "",
                PhotoUrl = photoUrl ?? "",
                StockQuantity = stockQuantity,
                CategoryId = _categories[categoryIndex].CategoryId
            };

            var response = await _apiService.CreateProductAsync(product);
            System.Diagnostics.Debug.WriteLine($"[ADD PRODUCT] Response: Success={response?.Success}, Message={response?.Message}");
            
            if (response?.Success == true)
            {
                await DisplayAlert("Success", "Product added successfully", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", response?.Message ?? "Failed to add product", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ADD PRODUCT] Exception: {ex.Message}");
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

