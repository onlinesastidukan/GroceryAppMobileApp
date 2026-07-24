using GroceryApp.Models;
using GroceryApp.Services;

namespace GroceryApp.Views;

public partial class AdminEditCategoryPage : ContentPage
{
    private const int MaxImageUploadBytes = 50 * 1024;
    private readonly ApiService _apiService;
    private readonly Category _category;
    private string _selectedImageBase64 = string.Empty;
    private byte[] _previewImageBytes;

    public AdminEditCategoryPage(ApiService apiService, Category category)
    {
        InitializeComponent();
        _apiService = apiService;
        _category = category;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        PopulateFields();
    }

    private void PopulateFields()
    {
        CategoryNameEntry.Text = _category.Name;
        DescriptionEditor.Text = _category.Description;

        if (!string.IsNullOrWhiteSpace(_category.PhotoUrl))
        {
            _selectedImageBase64 = _category.PhotoUrl;
            if (_category.PhotoUrl.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = _category.PhotoUrl.IndexOf(',');
                if (commaIndex >= 0)
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(_category.PhotoUrl.Substring(commaIndex + 1));
                        CategoryImagePreview.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                        CategoryImagePreview.IsVisible = true;
                        ImagePlaceholder.IsVisible = false;
                        ImageStatusLabel.Text = "✓ Current image loaded";
                        ImageStatusLabel.IsVisible = true;
                    }
                    catch { /* ignore preview error */ }
                }
            }
            else if (Uri.TryCreate(_category.PhotoUrl, UriKind.Absolute, out var uri))
            {
                CategoryImagePreview.Source = ImageSource.FromUri(uri);
                CategoryImagePreview.IsVisible = true;
                ImagePlaceholder.IsVisible = false;
                ImageStatusLabel.Text = "✓ Current image loaded";
                ImageStatusLabel.IsVisible = true;
            }
        }
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

            var request = new UpdateCategoryRequest
            {
                CategoryId = _category.CategoryId,
                Name = categoryName,
                Description = description ?? string.Empty,
                PhotoUrl = _selectedImageBase64,
                IsActive = _category.IsActive
            };

            var response = await _apiService.UpdateCategoryAsync(request);
            if (response?.Success == true)
            {
                await DisplayAlert("Success", "Shop updated successfully", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                ErrorLabel.Text = response?.Message ?? "Failed to update shop";
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
