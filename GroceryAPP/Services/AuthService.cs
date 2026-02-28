using System;
using System.Text.Json;
using System.Threading.Tasks;
using GroceryApp.Models;

namespace GroceryApp.Services;

public class AuthService
{
    private const string UserTokenKey = "user_token";
    private const string UserInfoKey = "user_info";
    private UserLoginInfo _currentUser;
    public string LastErrorMessage { get; private set; } = string.Empty;

    public UserLoginInfo CurrentUser
    {
        get => _currentUser;
        set => _currentUser = value;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_currentUser?.Token);

    public async Task<bool> LoginAsync(string userId, string password, ApiService apiService)
    {
        try
        {
            LastErrorMessage = string.Empty;
            var loginRequest = new LoginRequest { UserId = userId, Password = password };
            System.Diagnostics.Debug.WriteLine($"[AUTH] Sending login request for userId: {userId}");
            
            var response = await apiService.LoginAsync(loginRequest);
            
            System.Diagnostics.Debug.WriteLine($"[AUTH] Response received:");
            System.Diagnostics.Debug.WriteLine($"[AUTH]   Success: {response?.Success}");
            System.Diagnostics.Debug.WriteLine($"[AUTH]   Message: {response?.Message}");
            System.Diagnostics.Debug.WriteLine($"[AUTH]   Data is null: {response?.Data == null}");

            if (response?.Success == true && response.Data != null)
            {
                _currentUser = new UserLoginInfo
                {
                    UserId = response.Data.UserId,
                    FullName = response.Data.FullName,
                    MobileNumber = response.Data.MobileNumber,
                    Address = response.Data.Address,
                    Role = response.Data.Role,
                    Token = response.Data.Token
                };

                System.Diagnostics.Debug.WriteLine($"[AUTH] Login successful - Role: {_currentUser.Role}");
                await SaveUserToLocalStorage();
                return true;
            }
            
            System.Diagnostics.Debug.WriteLine($"[AUTH] Login failed - Response.Success={response?.Success}, Response.Message={response?.Message}");
            LastErrorMessage = response?.Message ?? "Login failed";
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AUTH] Login error: {ex.Message}\n{ex.StackTrace}");
            LastErrorMessage = "Unable to complete login. Please check your network and try again.";
            return false;
        }
    }

    public async Task LogoutAsync(ApiService? apiService = null)
    {
        _currentUser = null;
        apiService?.ClearAuthToken();
        await ClearLocalStorage();
    }

    public async Task<bool> LoadUserFromLocalStorageAsync()
    {
        try
        {
            LastErrorMessage = string.Empty;
            var token = await SecureStorage.Default.GetAsync(UserTokenKey);
            var userJson = await SecureStorage.Default.GetAsync(UserInfoKey);

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userJson))
            {
                _currentUser = JsonSerializer.Deserialize<UserLoginInfo>(userJson);
                if (_currentUser == null)
                {
                    await ClearLocalStorage();
                    return false;
                }

                if (IsJwtExpired(token))
                {
                    LastErrorMessage = "Session expired. Please login again.";
                    await ClearLocalStorage();
                    _currentUser = null;
                    return false;
                }

                _currentUser.Token = token;
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load user error: {ex.Message}");
            LastErrorMessage = "Unable to restore saved session.";
            return false;
        }
    }

    private static bool IsJwtExpired(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
            {
                return true;
            }

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            switch (payload.Length % 4)
            {
                case 2:
                    payload += "==";
                    break;
                case 3:
                    payload += "=";
                    break;
            }

            var bytes = Convert.FromBase64String(payload);
            using var document = JsonDocument.Parse(bytes);

            if (!document.RootElement.TryGetProperty("exp", out var expElement))
            {
                return true;
            }

            var expUnix = expElement.GetInt64();
            var expiry = DateTimeOffset.FromUnixTimeSeconds(expUnix);
            return expiry <= DateTimeOffset.UtcNow.AddMinutes(1);
        }
        catch
        {
            return true;
        }
    }

    private async Task SaveUserToLocalStorage()
    {
        try
        {
            await SecureStorage.Default.SetAsync(UserTokenKey, _currentUser.Token);
            var userJson = JsonSerializer.Serialize(_currentUser);
            await SecureStorage.Default.SetAsync(UserInfoKey, userJson);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save user error: {ex.Message}");
        }
    }

    private async Task ClearLocalStorage()
    {
        try
        {
            SecureStorage.Default.Remove(UserTokenKey);
            SecureStorage.Default.Remove(UserInfoKey);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clear storage error: {ex.Message}");
        }
    }

    public bool IsAdmin => _currentUser?.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;
    
    public bool IsCustomer => _currentUser?.Role?.Equals("Customer", StringComparison.OrdinalIgnoreCase) ?? false;
}
