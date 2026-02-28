using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GroceryApp.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string successMessage = string.Empty;

    [ObservableProperty]
    private bool hasSuccess;

    [ObservableProperty]
    private string statusInfoMessage = string.Empty;

    [ObservableProperty]
    private bool hasStatusInfo;

    protected virtual async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task InitializeAsyncSafe()
    {
        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            await InitializeAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ViewModel Error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected void SetError(string message)
    {
        HasSuccess = false;
        SuccessMessage = string.Empty;
        HasError = true;
        ErrorMessage = message;
    }

    protected void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    protected void SetSuccess(string message)
    {
        HasError = false;
        ErrorMessage = string.Empty;
        HasStatusInfo = false;
        StatusInfoMessage = string.Empty;
        SuccessMessage = message;
        HasSuccess = true;
    }

    protected void ClearSuccess()
    {
        HasSuccess = false;
        SuccessMessage = string.Empty;
    }

    protected void SetStatusInfo(string message)
    {
        HasError = false;
        ErrorMessage = string.Empty;
        HasSuccess = false;
        SuccessMessage = string.Empty;
        StatusInfoMessage = message;
        HasStatusInfo = true;
    }

    public void DismissStatusInfo()
    {
        HasStatusInfo = false;
        StatusInfoMessage = string.Empty;
    }

    public void DismissError()
    {
        ClearError();
    }

    public void DismissSuccess()
    {
        ClearSuccess();
    }
}
