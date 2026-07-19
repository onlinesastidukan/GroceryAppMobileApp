using GroceryApp.Models;
using GroceryApp.Services;

namespace GroceryApp.Views;

public partial class RegisterPage : ContentPage
{
    private readonly ApiService _apiService;
    private string _selectedShopImageBase64 = string.Empty;

    public RegisterPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        var shopName = FullNameEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var mobileNumber = MobileEntry.Text?.Trim();
        var address = AddressEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(shopName) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(mobileNumber) ||
            string.IsNullOrWhiteSpace(address))
        {
            ErrorLabel.Text = "Please fill all fields";
            ErrorLabel.IsVisible = true;
            return;
        }

        var userId = mobileNumber;

        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            ErrorLabel.IsVisible = false;

            var request = new RegisterRequest
            {
                UserId = userId,
                FullName = shopName,
                Password = password,
                MobileNumber = mobileNumber,
                Address = address,
                ShopImageUrl = _selectedShopImageBase64
            };

            var response = await _apiService.RegisterAsync(request);
            if (response?.Success == true)
            {
                await DisplayAlert("Success", "Shopkeeper account created successfully. Please log in.", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                ErrorLabel.Text = response?.Message ?? "Registration failed";
                ErrorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Registration error: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnPickShopImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select Shop Image"
            });

            if (result == null) return;

            using var stream = await result.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var imageBytes = ms.ToArray();

            var extension = Path.GetExtension(result.FileName)?.TrimStart('.').ToLowerInvariant() ?? "jpeg";
            var mimeType = extension == "png" ? "image/png" : "image/jpeg";
            _selectedShopImageBase64 = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";

            ShopImagePreview.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
            ShopImagePreview.IsVisible = true;
            ShopImagePlaceholder.IsVisible = false;
            ShopImageStatusLabel.Text = $"✓ {result.FileName}";
            ShopImageStatusLabel.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not pick shop image: {ex.Message}", "OK");
        }
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
