using GroceryApp.Services;
using GroceryApp.Models;

namespace GroceryApp.Views;

public partial class AdminAddCategoryPage : ContentPage
{
    private const int MaxImageUploadBytes = 50 * 1024;
    private readonly ApiService _apiService;
    private string _selectedImageBase64 = string.Empty;
    private byte[] _previewImageBytes;

    public AdminAddCategoryPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var categoryName = CategoryNameEntry.Text?.Trim();
        var description = DescriptionEditor.Text?.Trim();

        if (string.IsNullOrWhiteSpace(categoryName))
        {
            ErrorLabel.Text = "Please enter shop name";
            ErrorLabel.IsVisible = true;
            return;
        }

        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            ErrorLabel.IsVisible = false;
            System.Diagnostics.Debug.WriteLine($"[ADD SHOP] Creating shop: {categoryName}");

            var category = new CreateCategoryRequest
            {
                Name = categoryName,
                Description = description ?? string.Empty,
                PhotoUrl = _selectedImageBase64
            };

            var response = await _apiService.CreateCategoryAsync(category);
            System.Diagnostics.Debug.WriteLine($"[ADD SHOP] Response: Success={response?.Success}, Message={response?.Message}");
            
            if (response?.Success == true)
            {
                await DisplayAlert("Success", "Shop added successfully", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                ErrorLabel.Text = response?.Message ?? "Failed to add shop";
                ErrorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ADD SHOP] Exception: {ex.Message}");
            ErrorLabel.Text = $"Error: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnPickImageClicked(object sender, EventArgs e)
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
            _previewImageBytes = ms.ToArray();

            if (_previewImageBytes.Length > MaxImageUploadBytes)
            {
                _previewImageBytes = Array.Empty<byte>();
                _selectedImageBase64 = string.Empty;
                CategoryImagePreview.Source = null;
                CategoryImagePreview.IsVisible = false;
                ImagePlaceholder.IsVisible = true;
                ImageStatusLabel.Text = "Image must be 50 KB or smaller.";
                ImageStatusLabel.IsVisible = true;
                await DisplayAlert("Image Too Large", "Please choose an image of 50 KB or smaller.", "OK");
                return;
            }

            var extension = Path.GetExtension(result.FileName)?.TrimStart('.').ToLowerInvariant() ?? "jpeg";
            var mimeType = extension == "png" ? "image/png" : "image/jpeg";
            _selectedImageBase64 = $"data:{mimeType};base64,{Convert.ToBase64String(_previewImageBytes)}";

            CategoryImagePreview.Source = ImageSource.FromStream(() => new MemoryStream(_previewImageBytes));
            CategoryImagePreview.IsVisible = true;
            ImagePlaceholder.IsVisible = false;
            ImageStatusLabel.Text = $"✓ {result.FileName}";
            ImageStatusLabel.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not pick image: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

