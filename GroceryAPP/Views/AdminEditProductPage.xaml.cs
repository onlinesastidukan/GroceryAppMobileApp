using GroceryApp.Models;
using GroceryApp.Services;

namespace GroceryApp.Views;

public partial class AdminEditProductPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Product _product;
    private List<Category> _categories = new();

    public AdminEditProductPage(ApiService apiService, Product product)
    {
        InitializeComponent();
        _apiService = apiService;
        _product = product;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCategories();
        PopulateFields();
    }

    private void PopulateFields()
    {
        ProductNameEntry.Text = _product.Name;
        PriceEntry.Text = _product.Price.ToString("0.##");
        DescriptionEditor.Text = _product.Description;
        StockQuantityEntry.Text = _product.Stock.ToString();
        PhotoUrlEntry.Text = _product.ImageUrl;
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
                    var index = _categories.FindIndex(c => c.CategoryId == _product.CategoryId);
                    CategoryPicker.SelectedIndex = index >= 0 ? index : -1;
                });
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Failed to load categories: {ex.Message}";
            ErrorLabel.IsVisible = true;
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
            ErrorLabel.Text = "Please fill all required fields";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (!decimal.TryParse(priceText, out decimal price))
        {
            ErrorLabel.Text = "Invalid price format";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (!int.TryParse(stockText, out int stockQuantity) || stockQuantity < 0)
        {
            ErrorLabel.Text = "Invalid stock quantity";
            ErrorLabel.IsVisible = true;
            return;
        }

        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            ErrorLabel.IsVisible = false;

            var request = new UpdateProductRequest
            {
                ProductId = _product.ProductId,
                Name = productName,
                Description = description ?? string.Empty,
                Price = price,
                StockQuantity = stockQuantity,
                CategoryId = _categories[categoryIndex].CategoryId,
                PhotoUrl = photoUrl ?? string.Empty,
                IsActive = true
            };

            var response = await _apiService.UpdateProductAsync(request);
            if (response?.Success == true)
            {
                await DisplayAlert("Success", "Product updated successfully", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                ErrorLabel.Text = response?.Message ?? "Failed to update product";
                ErrorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Error: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
