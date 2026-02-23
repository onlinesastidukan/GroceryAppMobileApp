using GroceryApp.ViewModels;

namespace GroceryApp.Views;

public partial class AdminOrderDetailPage : ContentPage
{
    public AdminOrderDetailPage(AdminOrderDetailViewModel viewModel)
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
