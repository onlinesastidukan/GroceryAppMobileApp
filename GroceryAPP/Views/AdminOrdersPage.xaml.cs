using GroceryApp.ViewModels;

namespace GroceryApp.Views;

public partial class AdminOrdersPage : ContentPage
{
    public AdminOrdersPage(AdminOrdersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BaseViewModel baseViewModel)
        {
            await baseViewModel.InitializeAsyncSafe();
        }
    }
}
