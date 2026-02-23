using GroceryApp.ViewModels;

namespace GroceryApp.Views;

public partial class CustomerOrderHistoryPage : ContentPage
{
    public CustomerOrderHistoryPage(CustomerOrderHistoryViewModel viewModel)
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
