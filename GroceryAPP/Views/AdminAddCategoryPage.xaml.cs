using GroceryApp.Services;
using GroceryApp.Models;

namespace GroceryApp.Views;

public partial class AdminAddCategoryPage : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isLoading;

    public AdminAddCategoryPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var categoryName = CategoryNameEntry.Text;

        if (string.IsNullOrWhiteSpace(categoryName))
        {
            await DisplayAlert("Validation", "Please enter category name", "OK");
            return;
        }

        try
        {
            _isLoading = true;
            System.Diagnostics.Debug.WriteLine($"[ADD CATEGORY] Creating category: {categoryName}");

            var category = new CreateCategoryRequest
            {
                Name = categoryName
            };

            var response = await _apiService.CreateCategoryAsync(category);
            System.Diagnostics.Debug.WriteLine($"[ADD CATEGORY] Response: Success={response?.Success}, Message={response?.Message}");
            
            if (response?.Success == true)
            {
                await DisplayAlert("Success", "Category added successfully", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", response?.Message ?? "Failed to add category", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ADD CATEGORY] Exception: {ex.Message}");
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

