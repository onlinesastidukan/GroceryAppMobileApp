using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using GroceryApp.Models;

namespace GroceryApp.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private string _authToken = string.Empty;
    private const string UserAgent = "SastiDukan-Mobile/1.0";
    private const int MaxRetryAttempts = 3;
    private int _activeBaseUrlIndex;

    public ApiService()
    {
        try
        {
            var handler = new HttpClientHandler();
#if DEBUG
            // Allow insecure certificates only for local development endpoints.
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (errors == System.Net.Security.SslPolicyErrors.None)
                    return true;
                var host = message?.RequestUri?.Host ?? string.Empty;
                return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || host.Equals("127.0.0.1")
                    || host.Equals("10.0.2.2");
            };
#endif
            _httpClient = new HttpClient(handler);
            var baseUrl = AppConfig.ApiBaseUrls[_activeBaseUrlIndex].TrimEnd('/') + "/";
            _httpClient.BaseAddress = new Uri(baseUrl);
            // 30s — allows Railway cold-starts
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // Set default headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            Log($"[API] BaseAddress set to {_httpClient.BaseAddress}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] Init Error: {ex.Message}");
            throw;
        }
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
    }

    public void ClearAuthToken()
    {
        _authToken = string.Empty;
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
    }

    private static bool IsTransientHttpStatus(System.Net.HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code == 408 || code == 429 || code >= 500;
    }

    private bool TrySwitchToNextBaseUrl()
    {
        if (_activeBaseUrlIndex >= AppConfig.ApiBaseUrls.Length - 1)
        {
            return false;
        }

        _activeBaseUrlIndex++;
        var baseUrl = AppConfig.ApiBaseUrls[_activeBaseUrlIndex].TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        Log($"[API] Switched BaseAddress to {_httpClient.BaseAddress}");
        return true;
    }

    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> operation)
    {
        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                var response = await operation();
                if (attempt < MaxRetryAttempts && IsTransientHttpStatus(response.StatusCode))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt));
                    continue;
                }

                return response;
            }
            catch (HttpRequestException) when (attempt < MaxRetryAttempts)
            {
                // On first failure, immediately try the fallback URL (helps Jio/bad routing)
                TrySwitchToNextBaseUrl();
                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt));
            }
            catch (TaskCanceledException) when (attempt < MaxRetryAttempts)
            {
                // Timeout — switch URL and retry immediately
                TrySwitchToNextBaseUrl();
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }
        }

        return await operation();
    }

    private Task<HttpResponseMessage> GetAsyncWithRetry(string url)
        => ExecuteWithRetryAsync(() => _httpClient.GetAsync(url));

    private Task<HttpResponseMessage> DeleteAsyncWithRetry(string url)
        => ExecuteWithRetryAsync(() => _httpClient.DeleteAsync(url));

    private Task<HttpResponseMessage> PostAsJsonAsyncWithRetry<T>(string url, T payload, JsonSerializerOptions? options = null)
        => ExecuteWithRetryAsync(() => options == null
            ? _httpClient.PostAsJsonAsync(url, payload)
            : _httpClient.PostAsJsonAsync(url, payload, options));

    private Task<HttpResponseMessage> PutAsJsonAsyncWithRetry<T>(string url, T payload)
        => ExecuteWithRetryAsync(() => _httpClient.PutAsJsonAsync(url, payload));

    private Task<HttpResponseMessage> PatchAsJsonAsyncWithRetry<T>(string url, T payload)
        => ExecuteWithRetryAsync(() => _httpClient.PatchAsJsonAsync(url, payload));

    public async Task<bool> TestConnectivityAsync()
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            if (networkAccess != NetworkAccess.Internet)
            {
                Log($"[API] Connectivity check failed: {networkAccess}");
                return false;
            }
            
            var response = await GetAsyncWithRetry(string.Empty);
            Log($"[API] Connectivity check status: {(int)response.StatusCode} {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log($"[API] Connectivity check error: {ex.Message}");
            return false;
        }
    }

    public async Task<ApiResponse<AuthData>> LoginAsync(LoginRequest loginRequest)
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            if (networkAccess != NetworkAccess.Internet)
            {
                Log($"[API] Login blocked, network access: {networkAccess}");
                return new ApiResponse<AuthData> 
                { 
                    Success = false, 
                    Message = "No internet connection" 
                };
            }
            
            var url = "auth/login";
            var response = await PostAsJsonAsyncWithRetry(url, loginRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = await response.Content.ReadAsStringAsync();
            Log($"[API] Login response status: {(int)response.StatusCode} {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (loginResponse?.Token != null)
                {
                    var authData = new AuthData
                    {
                        UserId = loginResponse.UserId,
                        Role = loginResponse.Role,
                        Token = loginResponse.Token,
                        FullName = loginResponse.FullName,
                        MobileNumber = loginResponse.MobileNumber,
                        Address = loginResponse.Address
                    };
                    
                    SetAuthToken(authData.Token);
                    
                    return new ApiResponse<AuthData> 
                    { 
                        Success = true, 
                        Message = "Login successful",
                        Data = authData 
                    };
                }
                else
                {
                    return new ApiResponse<AuthData> 
                    { 
                        Success = false, 
                        Message = "Invalid response from server" 
                    };
                }
            }
            else
            {
                Log($"[API] Login failed with status: {(int)response.StatusCode} {response.StatusCode}. Body: {content}");
                return new ApiResponse<AuthData> 
                { 
                    Success = false, 
                    Message = "Invalid credentials" 
                };
            }
        }
        catch (HttpRequestException ex)
        {
            Log($"[API] Login HttpRequestException: {ex.Message}");
            return new ApiResponse<AuthData>
            {
                Success = false,
                Message = ClassifyNetworkError(ex)
            };
        }
        catch (TaskCanceledException ex)
        {
            Log($"[API] Login timeout: {ex.Message}");
            return new ApiResponse<AuthData> 
            { 
                Success = false, 
                Message = "Request timeout" 
            };
        }
        catch (Exception ex)
        {
            Log($"[API] Login error: {ex.Message}");
            return new ApiResponse<AuthData> 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterRequest registerRequest)
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            if (networkAccess != NetworkAccess.Internet)
            {
                Log($"[API] Register blocked, network access: {networkAccess}");
                return new ApiResponse<RegisterResponse> 
                { 
                    Success = false, 
                    Message = "No internet connection" 
                };
            }
            
            var url = "auth/register";
            var response = await PostAsJsonAsyncWithRetry(url, registerRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = await response.Content.ReadAsStringAsync();
            Log($"[API] Register response status: {(int)response.StatusCode} {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var registerResponse = JsonSerializer.Deserialize<RegisterResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return new ApiResponse<RegisterResponse>
                {
                    Success = registerResponse?.Success ?? true,
                    Message = registerResponse?.Message ?? "Registration successful",
                    Data = registerResponse
                };
            }
            else
            {
                Log($"[API] Register failed with status: {(int)response.StatusCode} {response.StatusCode}. Body: {content}");
                var message = ExtractApiErrorMessage(content);

                if (string.IsNullOrWhiteSpace(message) && response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    message = "User ID already exists. Please choose another User ID.";
                }

                return new ApiResponse<RegisterResponse> 
                { 
                    Success = false, 
                    Message = string.IsNullOrWhiteSpace(message) ? "Registration failed. Please try again." : message
                };
            }
        }
        catch (Exception ex)
        {
            Log($"[API] Register error: {ex.Message}");
            return new ApiResponse<RegisterResponse> 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }

    private static void Log(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
        System.Diagnostics.Trace.WriteLine(message);
    }

    /// <summary>Returns a user-friendly message for a network exception.</summary>
    private static string ClassifyNetworkError(Exception ex)
    {
        var inner = ex.InnerException ?? ex;
        if (inner is SocketException se)
        {
            return se.SocketErrorCode switch
            {
                SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain
                    => "Cannot reach the server. Your network's DNS may be blocking this.\n\nTry: Switch to mobile data, or go to Android Settings > Network > Private DNS and set 'one.one.one.one'.",
                SocketError.TimedOut
                    => "Connection timed out. The server may be starting up — please retry in a moment.",
                SocketError.ConnectionRefused
                    => "Connection refused by the server.",
                _ => $"Network error ({se.SocketErrorCode}). Please check your internet connection."
            };
        }
        var msg = inner.Message ?? string.Empty;
        if (msg.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("TLS", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("certificate", StringComparison.OrdinalIgnoreCase))
            return "Secure connection failed. Please check your network security settings.";
        return "Network error. Please check your internet connection and retry.";
    }

    public async Task<ApiResponse<List<Category>>> GetCategoriesAsync()
    {
        try
        {
            var response = await GetAsyncWithRetry($"{AppConfig.CategoryController}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<List<Category>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse != null && wrappedResponse.Success)
                    {
                        return wrappedResponse;
                    }
                }
                catch { }
                
                try
                {
                    var directList = JsonSerializer.Deserialize<List<Category>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (directList != null)
                    {
                        return new ApiResponse<List<Category>> 
                        { 
                            Success = true, 
                            Message = "Categories loaded",
                            Data = directList 
                        };
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[API] Category parse error: {ex.Message}");
                }
                
                return new ApiResponse<List<Category>> { Success = false, Message = "Failed to parse categories" };
            }
            else
            {
                return new ApiResponse<List<Category>> { Success = false, Message = "Failed to load categories" };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetCategories error: {ex.Message}");
            return new ApiResponse<List<Category>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<Product>>> GetProductsAsync(int categoryId = 0)
    {
        try
        {
            var url = string.IsNullOrEmpty(categoryId.ToString()) || categoryId == 0 
                ? $"{AppConfig.ProductController}" 
                : $"{AppConfig.ProductController}?categoryId={categoryId}";
            
            var response = await GetAsyncWithRetry(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<List<Product>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse != null && wrappedResponse.Success)
                    {
                        NormalizeProductImageUrls(wrappedResponse.Data);
                        return wrappedResponse;
                    }
                }
                catch { }
                
                try
                {
                    var directList = JsonSerializer.Deserialize<List<Product>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (directList != null)
                    {
                        NormalizeProductImageUrls(directList);
                        return new ApiResponse<List<Product>> 
                        { 
                            Success = true, 
                            Message = "Products loaded",
                            Data = directList 
                        };
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[API] Product parse error: {ex.Message}");
                }
                
                return new ApiResponse<List<Product>> { Success = false, Message = "Failed to parse products" };
            }
            else
            {
                return new ApiResponse<List<Product>> { Success = false, Message = "Failed to load products" };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetProducts error: {ex.Message}");
            return new ApiResponse<List<Product>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Product>> GetProductByIdAsync(int productId)
    {
        try
        {
            var response = await GetAsyncWithRetry($"{AppConfig.ProductController}/{productId}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<Product>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse != null && wrappedResponse.Success)
                    {
                        NormalizeProductImageUrl(wrappedResponse.Data);
                        return wrappedResponse;
                    }
                }
                catch { }
                
                var directObj = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (directObj != null)
                {
                    NormalizeProductImageUrl(directObj);
                    return new ApiResponse<Product> 
                    { 
                        Success = true, 
                        Message = "Product loaded",
                        Data = directObj 
                    };
                }
                
                return new ApiResponse<Product> { Success = false, Message = "Product not found" };
            }
            else
            {
                return new ApiResponse<Product> { Success = false, Message = "Product not found" };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetProductById error: {ex.Message}");
            return new ApiResponse<Product> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<Order>>> GetOrdersAsync()
    {
        try
        {
            // Backend customer orders endpoint is /orders/my.
            // Keep /orders as a fallback for compatibility with older API variants.
            var primaryPath = $"{AppConfig.OrderController}/my";
            var fallbackPath = $"{AppConfig.OrderController}";

            var response = await GetAsyncWithRetry(primaryPath);
            if (!response.IsSuccessStatusCode && (int)response.StatusCode == 404)
            {
                response = await GetAsyncWithRetry(fallbackPath);
            }

            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<List<Order>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse != null && wrappedResponse.Success)
                    {
                        return wrappedResponse;
                    }
                }
                catch { }
                
                var directList = JsonSerializer.Deserialize<List<Order>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (directList != null)
                {
                    return new ApiResponse<List<Order>> 
                    { 
                        Success = true, 
                        Message = "Orders loaded",
                        Data = directList 
                    };
                }

                // Some backend variants return orders inside envelopes like:
                // { data: [...] }, { items: [...] }, { orders: [...] } or { data: { items: [...] } }.
                try
                {
                    using var document = JsonDocument.Parse(content);
                    if (document.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        var list = TryReadOrderArray(document.RootElement, "data")
                            ?? TryReadOrderArray(document.RootElement, "items")
                            ?? TryReadOrderArray(document.RootElement, "orders");

                        if (list == null && document.RootElement.TryGetProperty("data", out var dataNode) && dataNode.ValueKind == JsonValueKind.Object)
                        {
                            list = TryReadOrderArray(dataNode, "items")
                                ?? TryReadOrderArray(dataNode, "orders")
                                ?? TryReadOrderArray(dataNode, "result");
                        }

                        if (list != null)
                        {
                            return new ApiResponse<List<Order>>
                            {
                                Success = true,
                                Message = "Orders loaded",
                                Data = list
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[API] Orders envelope parse error: {ex.Message}");
                }
                
                return new ApiResponse<List<Order>> { Success = false, Message = "Failed to parse orders" };
            }
            else
            {
                return new ApiResponse<List<Order>> { Success = false, Message = "Failed to load orders" };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetOrders error: {ex.Message}");
            return new ApiResponse<List<Order>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Order>> GetOrderByIdAsync(int orderId)
    {
        try
        {
            var response = await GetAsyncWithRetry($"{AppConfig.OrderController}/{orderId}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<Order>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse != null && wrappedResponse.Success)
                    {
                        return wrappedResponse;
                    }
                }
                catch { }
                
                var directObj = JsonSerializer.Deserialize<Order>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (directObj != null)
                {
                    return new ApiResponse<Order> 
                    { 
                        Success = true, 
                        Message = "Order loaded",
                        Data = directObj 
                    };
                }

                // Envelope compatibility: { data: { ... } }, { order: { ... } }, { result: { ... } }
                try
                {
                    using var document = JsonDocument.Parse(content);
                    if (document.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        var order = TryReadOrder(document.RootElement, "data")
                            ?? TryReadOrder(document.RootElement, "order")
                            ?? TryReadOrder(document.RootElement, "result");

                        if (order != null)
                        {
                            return new ApiResponse<Order>
                            {
                                Success = true,
                                Message = "Order loaded",
                                Data = order
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[API] Order detail envelope parse error: {ex.Message}");
                }
                
                return new ApiResponse<Order> { Success = false, Message = "Order not found" };
            }
            else
            {
                return new ApiResponse<Order> { Success = false, Message = "Order not found" };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetOrderById error: {ex.Message}");
            return new ApiResponse<Order> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Order>> GetAdminOrderByIdAsync(int orderId)
    {
        try
        {
            var response = await GetAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}");
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrapped = JsonSerializer.Deserialize<ApiResponse<Order>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrapped?.Success == true && wrapped.Data != null) return wrapped;
                }
                catch { }
                var order = JsonSerializer.Deserialize<Order>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (order != null)
                    return new ApiResponse<Order> { Success = true, Data = order };
            }
            return new ApiResponse<Order> { Success = false, Message = "Order not found" };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetAdminOrderById error: {ex.Message}");
            return new ApiResponse<Order> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Order>> CreateOrderAsync(CreateOrderRequest orderRequest)
    {
        try
        {
            var response = await PostAsJsonAsyncWithRetry($"{AppConfig.OrderController}", orderRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<Order>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse != null && wrappedResponse.Success)
                    {
                        return wrappedResponse;
                    }
                }
                catch { }
                
                var directObj = JsonSerializer.Deserialize<Order>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (directObj != null)
                {
                    return new ApiResponse<Order> { Success = true, Message = "Order created", Data = directObj };
                }
                
                return new ApiResponse<Order> { Success = false, Message = "Failed to parse response" };
            }
            else
            {
                return new ApiResponse<Order> { Success = false, Message = "Failed to create order" };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] CreateOrder error: {ex.Message}");
            return new ApiResponse<Order> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> UpdateOrderStatusAsync(int orderId, string status)
    {
        var statusPayload = new { status };

        // Try the dedicated admin status endpoint first, then fallbacks.
        var attempts = new (Func<Task<HttpResponseMessage>> send, string label)[]
        {
            (() => PutAsJsonAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}/status", statusPayload), "PUT  admin/orders/{id}/status"),
            (() => PatchAsJsonAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}/status", statusPayload), "PATCH admin/orders/{id}/status"),
            (() => PatchAsJsonAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}", statusPayload),        "PATCH admin/orders/{id}"),
            (() => PutAsJsonAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}", new UpdateOrderStatusRequest { OrderId = orderId, Status = status }), "PUT  admin/orders/{id}"),
        };

        string lastServerError = "Failed to update order status";

        foreach (var (send, label) in attempts)
        {
            try
            {
                var response = await send();
                var content  = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[API] {label} → {(int)response.StatusCode}: {Preview(content, 200)}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (result != null) return result;
                    }
                    catch { }
                    return new ApiResponse { Success = true, Message = "Order status updated" };
                }

                if (!string.IsNullOrWhiteSpace(content))
                    lastServerError = content.Length > 200 ? content[..200] : content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] {label} exception: {ex.Message}");
            }
        }

        return new ApiResponse { Success = false, Message = lastServerError };
    }

    // Admin endpoints
    public async Task<ApiResponse<List<Product>>> GetAllProductsAdminAsync()
    {
        try
        {
            var requestPath = "products";
            var requestUri = new Uri(_httpClient.BaseAddress!, requestPath);
            var hasAuthHeader = _httpClient.DefaultRequestHeaders.Authorization != null;
            System.Diagnostics.Debug.WriteLine($"[API] Fetching all products from GET {requestUri} (AuthHeaderPresent={hasAuthHeader})");
            var response = await GetAsyncWithRetry(requestPath);
            var content = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[API] Products response status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"[API] Products response preview: {Preview(content, 300)}");
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<List<Product>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse?.Data != null)
                    {
                        wrappedResponse.Success = true;
                        NormalizeProductImageUrls(wrappedResponse.Data);
                        return wrappedResponse;
                    }
                }
                catch { }
                
                var directList = JsonSerializer.Deserialize<List<Product>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (directList != null)
                {
                    NormalizeProductImageUrls(directList);
                    return new ApiResponse<List<Product>> 
                    { 
                        Success = true, 
                        Message = "Products loaded",
                        Data = directList 
                    };
                }

                // Some backend versions wrap arrays in { data }, { items }, or { products }.
                try
                {
                    using var document = JsonDocument.Parse(content);
                    if (document.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        var list = TryReadProductArray(document.RootElement, "data")
                            ?? TryReadProductArray(document.RootElement, "items")
                            ?? TryReadProductArray(document.RootElement, "products")
                            ?? TryReadProductArray(document.RootElement, "result");

                        if (list != null)
                        {
                            NormalizeProductImageUrls(list);
                            return new ApiResponse<List<Product>>
                            {
                                Success = true,
                                Message = "Products loaded",
                                Data = list
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[API] Product envelope parse error: {ex.Message}");
                }
                
                return new ApiResponse<List<Product>> { Success = false, Message = "Failed to parse products" };
            }
            else
            {
                return new ApiResponse<List<Product>> { Success = false, Message = "Failed to load products" };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetAllProductsAdmin error: {ex.Message}");
            return new ApiResponse<List<Product>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Product>> CreateProductAsync(CreateProductRequest productRequest)
    {
        try
        {
            var normalizedPhotoUrl = productRequest.PhotoUrl?.Trim() ?? string.Empty;
            var payload = new
            {
                name = productRequest.Name,
                description = productRequest.Description,
                price = productRequest.Price,
                stockQuantity = productRequest.StockQuantity,
                categoryId = productRequest.CategoryId,
                photoUrl = normalizedPhotoUrl,
                imageUrl = normalizedPhotoUrl
            };

            var response = await PostAsJsonAsyncWithRetry($"{AppConfig.AdminController}/products", payload);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var product = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (product != null)
                {
                    NormalizeProductImageUrl(product);
                    return new ApiResponse<Product> { Success = true, Message = "Product created", Data = product };
                }

                return new ApiResponse<Product> { Success = false, Message = "Failed to parse product response" };
            }
            else
            {
                var message = string.IsNullOrWhiteSpace(content) ? "Failed to create product" : content;
                return new ApiResponse<Product> { Success = false, Message = message };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<Product> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> UpdateProductAsync(UpdateProductRequest productRequest)
    {
        try
        {
            var normalizedPhotoUrl = productRequest.PhotoUrl?.Trim() ?? string.Empty;
            var payload = new
            {
                productId = productRequest.ProductId,
                name = productRequest.Name,
                description = productRequest.Description,
                price = productRequest.Price,
                stockQuantity = productRequest.StockQuantity,
                categoryId = productRequest.CategoryId,
                photoUrl = normalizedPhotoUrl,
                imageUrl = normalizedPhotoUrl,
                isActive = productRequest.IsActive
            };

            var response = await PutAsJsonAsyncWithRetry($"{AppConfig.AdminController}/products/{productRequest.ProductId}", payload);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var product = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (product != null)
                {
                    return new ApiResponse { Success = true, Message = "Product updated" };
                }

                return new ApiResponse { Success = true, Message = "Product updated" };
            }
            else
            {
                var message = string.IsNullOrWhiteSpace(content) ? "Failed to update product" : content;
                return new ApiResponse { Success = false, Message = message };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> DeleteProductAsync(int productId)
    {
        try
        {
            var response = await DeleteAsyncWithRetry($"{AppConfig.AdminController}/products/{productId}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var message = string.IsNullOrWhiteSpace(content) ? "Product deleted" : content;
                return new ApiResponse { Success = true, Message = message };
            }
            else
            {
                var message = string.IsNullOrWhiteSpace(content) ? "Failed to delete product" : content;
                return new ApiResponse { Success = false, Message = message };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<Category>>> GetAllCategoriesAdminAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[API] Fetching categories from GET /api/categories");
            var response = await GetAsyncWithRetry($"categories");
            var content = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[API] Categories response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<List<Category>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse != null && wrappedResponse.Success)
                    {
                        return wrappedResponse;
                    }
                }
                catch { }
                
                var directList = JsonSerializer.Deserialize<List<Category>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (directList != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[API] Loaded {directList.Count} categories");
                    return new ApiResponse<List<Category>> 
                    { 
                        Success = true, 
                        Message = "Categories loaded",
                        Data = directList 
                    };
                }
                
                return new ApiResponse<List<Category>> { Success = false, Message = "Failed to parse categories" };
            }
            else
            {
                return new ApiResponse<List<Category>> { Success = false, Message = "Failed to load categories" };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetAllCategoriesAdmin error: {ex.Message}");
            return new ApiResponse<List<Category>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Category>> CreateCategoryAsync(CreateCategoryRequest categoryRequest)
    {
        try
        {
            var response = await PostAsJsonAsyncWithRetry($"{AppConfig.AdminController}/categories", categoryRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var category = JsonSerializer.Deserialize<Category>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (category != null)
                {
                    return new ApiResponse<Category> { Success = true, Message = "Category created", Data = category };
                }

                return new ApiResponse<Category> { Success = false, Message = "Failed to parse category response" };
            }
            else
            {
                var message = string.IsNullOrWhiteSpace(content) ? "Failed to create category" : content;
                return new ApiResponse<Category> { Success = false, Message = message };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<Category> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> UpdateCategoryAsync(UpdateCategoryRequest categoryRequest)
    {
        try
        {
            var response = await PutAsJsonAsyncWithRetry($"{AppConfig.AdminController}/categories/{categoryRequest.CategoryId}", categoryRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var category = JsonSerializer.Deserialize<Category>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (category != null)
                {
                    return new ApiResponse { Success = true, Message = "Category updated" };
                }

                return new ApiResponse { Success = true, Message = "Category updated" };
            }
            else
            {
                var message = string.IsNullOrWhiteSpace(content) ? "Failed to update category" : content;
                return new ApiResponse { Success = false, Message = message };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> DeleteCategoryAsync(int categoryId)
    {
        try
        {
            var response = await DeleteAsyncWithRetry($"{AppConfig.AdminController}/categories/{categoryId}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var message = string.IsNullOrWhiteSpace(content) ? "Category deleted" : content;
                return new ApiResponse { Success = true, Message = message };
            }
            else
            {
                var message = string.IsNullOrWhiteSpace(content) ? "Failed to delete category" : content;
                return new ApiResponse { Success = false, Message = message };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<Order>>> GetAllOrdersAdminAsync()
    {
        try
        {
            var response = await GetAsyncWithRetry($"{AppConfig.AdminController}/orders");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<List<Order>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse != null && wrappedResponse.Success)
                    {
                        return wrappedResponse;
                    }
                }
                catch { }
                
                var directList = JsonSerializer.Deserialize<List<Order>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (directList != null)
                {
                    return new ApiResponse<List<Order>> 
                    { 
                        Success = true, 
                        Message = "Orders loaded",
                        Data = directList 
                    };
                }
                
                return new ApiResponse<List<Order>> { Success = false, Message = "Failed to parse orders" };
            }
            else
            {
                return new ApiResponse<List<Order>> { Success = false, Message = "Failed to load orders" };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetAllOrdersAdmin error: {ex.Message}");
            return new ApiResponse<List<Order>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    private static List<Product>? TryReadProductArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var candidate) || candidate.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<Product>>(candidate.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static List<Order>? TryReadOrderArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var candidate) || candidate.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<Order>>(candidate.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static Order? TryReadOrder(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var candidate) || candidate.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Order>(candidate.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static string Preview(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return "<empty>";
        return value.Length <= maxLen ? value : value.Substring(0, maxLen) + "...";
    }

    private static void NormalizeProductImageUrls(IEnumerable<Product>? products)
    {
        if (products == null)
        {
            return;
        }

        foreach (var product in products)
        {
            NormalizeProductImageUrl(product);
        }
    }

    private static void NormalizeProductImageUrl(Product? product)
    {
        if (product == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            product.ImageUrl = "dotnet_bot.png";
            return;
        }

        var imageUrl = product.ImageUrl.Trim();

        // Preserve data: URIs (base64 images) without calling Uri.TryCreate —
        // very long data URIs can exceed .NET's URI length limit and get misclassified.
        if (imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            product.ImageUrl = imageUrl;
            return;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
        {
            product.ImageUrl = imageUrl;
            return;
        }

        var apiBase = AppConfig.ApiBaseUrl.TrimEnd('/');
        var apiRoot = apiBase.EndsWith("/api", StringComparison.OrdinalIgnoreCase)
            ? apiBase[..^4]
            : apiBase;

        product.ImageUrl = imageUrl.StartsWith("/", StringComparison.Ordinal)
            ? $"{apiRoot}{imageUrl}"
            : $"{apiRoot}/{imageUrl}";
    }

    private static string? ExtractApiErrorMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                {
                    var message = messageProp.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        return message;
                    }
                }

                if (root.TryGetProperty("error", out var errorProp) && errorProp.ValueKind == JsonValueKind.String)
                {
                    var error = errorProp.GetString();
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        return error;
                    }
                }

                if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    var title = titleProp.GetString();
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        return title;
                    }
                }
            }
        }
        catch
        {
            // Fall back to plain text below when body is not valid JSON.
        }

        return content.Trim();
    }
}
