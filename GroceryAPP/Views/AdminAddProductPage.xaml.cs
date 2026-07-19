using GroceryApp.Services;
using GroceryApp.Models;
using GroceryApp.ViewModels;

namespace GroceryApp.Views;

public partial class AdminAddProductPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;
    private List<Category> _categories = new();
    private List<Product> _products = new();
    private string _selectedImageBase64 = string.Empty;
    private byte[] _previewImageBytes;

    public AdminAddProductPage(ApiService apiService, AuthService authService)
    {
        InitializeComponent();
        _apiService = apiService;
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCategories();
        await LoadProducts();
    }

    private async Task LoadCategories()
    {
        try
        {
            var response = _authService.IsDealer
                ? await _apiService.GetDealerShopsAsync()
                : await _apiService.GetAllCategoriesAdminAsync();

            if (response?.Success == true && response.Data != null)
            {
                _categories = response.Data.Where(c => c.IsActive).ToList();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CategoryPicker.ItemsSource = _categories.Select(c => c.Name).ToList();
                    if (_authService.IsDealer)
                    {
                        var preferredIndex = _categories.FindIndex(c =>
                            !string.IsNullOrWhiteSpace(_authService.CurrentUser?.FullName) &&
                            string.Equals(c.Name?.Trim(), _authService.CurrentUser.FullName?.Trim(), StringComparison.OrdinalIgnoreCase));

                        CategoryPicker.SelectedIndex = preferredIndex >= 0 ? preferredIndex : (_categories.Count > 0 ? 0 : -1);
                        CategoryPicker.IsEnabled = false;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ADD PRODUCT] Error loading categories: {ex.Message}");
        }
    }

    private async Task LoadProducts()
    {
        try
        {
            ProductsLoading.IsRunning = true;
            ProductsLoading.IsVisible = true;

            var response = _authService.IsDealer
                ? await _apiService.GetDealerProductsAsync()
                : await _apiService.GetAllProductsAdminAsync();
            if (response?.Success == true && response.Data != null)
            {
                _products = response.Data.ToList();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ProductsCollectionView.ItemsSource = _products;
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ProductsCollectionView.ItemsSource = new List<Product>();
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ADD PRODUCT] Error loading products: {ex.Message}");
        }
        finally
        {
            ProductsLoading.IsRunning = false;
            ProductsLoading.IsVisible = false;
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var productName = ProductNameEntry.Text;
        var priceText = PriceEntry.Text;
        var description = DescriptionEditor.Text;
        var stockText = StockQuantityEntry.Text;
        var photoUrl = _selectedImageBase64;
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

            var response = _authService.IsDealer
                ? await _apiService.CreateDealerProductAsync(product)
                : await _apiService.CreateProductAsync(product);
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

    private async void OnPickImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select Product Image"
            });

            if (result == null) return;

            using var stream = await result.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            _previewImageBytes = ms.ToArray();

            var extension = Path.GetExtension(result.FileName)?.TrimStart('.').ToLowerInvariant() ?? "jpeg";
            var mimeType = extension == "png" ? "image/png" : "image/jpeg";
            _selectedImageBase64 = $"data:{mimeType};base64,{Convert.ToBase64String(_previewImageBytes)}";

            ProductImagePreview.Source = ImageSource.FromStream(() => new MemoryStream(_previewImageBytes));
            ProductImagePreview.IsVisible = true;
            ImagePlaceholder.IsVisible = false;
            ImageStatusLabel.Text = $"✓ {result.FileName}";
            ImageStatusLabel.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not pick image: {ex.Message}", "OK");
        }
    }

    private async void OnEditExistingProductClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Product product)
        {
            var editPage = new AdminEditProductPage(_apiService, _authService, product);
            await Navigation.PushAsync(editPage);
        }
    }

    private async void OnDeleteExistingProductClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Product product)
        {
            var confirm = await DisplayAlert("Confirm", $"Delete {product.Name}?", "Yes", "No");
            if (!confirm)
            {
                return;
            }

            var response = _authService.IsDealer
                ? await _apiService.DeleteDealerProductAsync(product.ProductId)
                : await _apiService.DeleteProductAsync(product.ProductId);
            if (response?.Success == true)
            {
                await DisplayAlert("Success", "Product deleted", "OK");
                await LoadProducts();
            }
            else
            {
                await DisplayAlert("Error", response?.Message ?? "Failed to delete product", "OK");
            }
        }
    }
}

