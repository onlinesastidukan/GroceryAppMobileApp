using GroceryApp.ViewModels;

namespace GroceryApp.Views;

public partial class CustomerOrderDetailPage : ContentPage
{
    public CustomerOrderDetailPage(CustomerOrderDetailViewModel viewModel)
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
