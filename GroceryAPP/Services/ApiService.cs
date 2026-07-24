using System;
using System.Collections.Generic;
using System.IO;
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
            // SocketsHttpHandler gives us fine-grained connection pool control.
            // PooledConnectionLifetime prevents "socket closed" errors caused by
            // stale keep-alive connections (Railway/Render recycles them quickly).
            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(90),
                ConnectTimeout = TimeSpan.FromSeconds(15),
                MaxConnectionsPerServer = 10,
                EnableMultipleHttp2Connections = true,
            };
#if DEBUG
            // Allow insecure certificates only for local development endpoints.
            socketsHandler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (message, cert, chain, errors) =>
                {
                    if (errors == System.Net.Security.SslPolicyErrors.None)
                        return true;
                    var host = (message as System.Net.HttpWebRequest)?.RequestUri?.Host
                              ?? cert?.Subject ?? string.Empty;
                    return host.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                        || host.Contains("127.0.0.1")
                        || host.Contains("10.0.2.2");
                }
            };
#endif
            _httpClient = new HttpClient(socketsHandler);
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
        // Reset to primary base URL so the next login attempt starts fresh
        if (_activeBaseUrlIndex != 0)
        {
            _activeBaseUrlIndex = 0;
            var baseUrl = AppConfig.ApiBaseUrls[0].TrimEnd('/') + "/";
            _httpClient.BaseAddress = new Uri(baseUrl);
            Log($"[API] ClearAuthToken: reset BaseAddress to {_httpClient.BaseAddress}");
        }
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
            catch (HttpRequestException ex) when (attempt < MaxRetryAttempts)
            {
                // net_http_operation_started or socket closed — switch URL and retry
                Log($"[API] HttpRequestException attempt {attempt}: {ex.Message}");
                TrySwitchToNextBaseUrl();
                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt));
            }
            catch (IOException ex) when (attempt < MaxRetryAttempts)
            {
                // Socket closed mid-request (ECONNRESET, broken pipe, etc.)
                Log($"[API] IOException attempt {attempt}: {ex.Message}");
                TrySwitchToNextBaseUrl();
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
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

    public async Task<bool> UpdateFcmTokenAsync(string fcmToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
            {
                Log("[API] UpdateFcmToken: Empty token provided");
                return false;
            }

            var networkAccess = Connectivity.Current.NetworkAccess;
            if (networkAccess != NetworkAccess.Internet)
            {
                Log($"[API] UpdateFcmToken blocked, network access: {networkAccess}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_authToken))
            {
                Log("[API] UpdateFcmToken: No auth token set");
                return false;
            }

            var url = "auth/update-fcm-token";
            var request = new { FcmToken = fcmToken };

            Log($"[API] Updating FCM token (length: {fcmToken.Length})");
            var response = await PostAsJsonAsyncWithRetry(url, request);
            var content = await response.Content.ReadAsStringAsync();

            Log($"[API] UpdateFcmToken response status: {(int)response.StatusCode} {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                Log("[API] FCM token updated successfully");
                return true;
            }
            else
            {
                Log($"[API] UpdateFcmToken failed: {content}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Log($"[API] UpdateFcmToken error: {ex.Message}");
            return false;
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
        if (inner is IOException ioEx)
        {
            var ioMsg = ioEx.Message ?? string.Empty;
            if (ioMsg.Contains("closed", StringComparison.OrdinalIgnoreCase) ||
                ioMsg.Contains("reset", StringComparison.OrdinalIgnoreCase) ||
                ioMsg.Contains("broken pipe", StringComparison.OrdinalIgnoreCase))
                return "Connection was reset by the server. Please retry.";
            return "Network I/O error. Please check your internet connection and retry.";
        }
        var msg = inner.Message ?? string.Empty;
        if (msg.Contains("net_http_operation_started", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("operation has already started", StringComparison.OrdinalIgnoreCase))
            return "Network request failed. Please retry.";
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
            var networkAccess = Connectivity.Current.NetworkAccess;
            if (networkAccess != NetworkAccess.Internet)
            {
                Log($"[API] GetCategories blocked, network access: {networkAccess}");
                return new ApiResponse<List<Category>>
                {
                    Success = false,
                    Message = "No internet connection"
                };
            }

            var response = await GetAsyncWithRetry($"{AppConfig.CategoryController}?includeImage=true");
            var content = await response.Content.ReadAsStringAsync();
            Log($"[API] GetCategories response status: {(int)response.StatusCode} {response.StatusCode}. Base={_httpClient.BaseAddress}");

            if (!response.IsSuccessStatusCode)
            {
                Log($"[API] GetCategories failed body: {Preview(content, 300)}");
                return new ApiResponse<List<Category>>
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(ExtractApiErrorMessage(content))
                        ? "Failed to load categories"
                        : ExtractApiErrorMessage(content)!
                };
            }

            try
            {
                var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<List<Category>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (wrappedResponse?.Data != null)
                {
                    wrappedResponse.Success = true;
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
            catch { }

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    foreach (var key in new[] { "data", "categories", "items", "result", "results" })
                    {
                        if (root.TryGetProperty(key, out var elem) && elem.ValueKind == JsonValueKind.Array)
                        {
                            var list = JsonSerializer.Deserialize<List<Category>>(elem.GetRawText(), opts);
                            if (list != null)
                            {
                                return new ApiResponse<List<Category>> { Success = true, Message = "Categories loaded", Data = list };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[API] Category parse error: {ex.Message}");
            }

            return new ApiResponse<List<Category>> { Success = false, Message = "Failed to parse categories" };
        }
        catch (HttpRequestException ex)
        {
            Log($"[API] GetCategories HttpRequestException: {ex.Message}");
            return new ApiResponse<List<Category>>
            {
                Success = false,
                Message = ClassifyNetworkError(ex)
            };
        }
        catch (TaskCanceledException ex)
        {
            Log($"[API] GetCategories timeout: {ex.Message}");
            return new ApiResponse<List<Category>>
            {
                Success = false,
                Message = "Request timeout"
            };
        }
        catch (Exception ex)
        {
            Log($"[API] GetCategories error: {ex.Message}");
            return new ApiResponse<List<Category>>
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<Product>>> GetProductsAsync(int categoryId = 0)
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            if (networkAccess != NetworkAccess.Internet)
            {
                Log($"[API] GetProducts blocked, network access: {networkAccess}");
                return new ApiResponse<List<Product>> { Success = false, Message = "No internet connection" };
            }

            var url = categoryId == 0
                ? $"{AppConfig.ProductController}?includeImage=true"
                : $"{AppConfig.ProductController}?categoryId={categoryId}&includeImage=true";

            var response = await GetAsyncWithRetry(url);
            var content = await response.Content.ReadAsStringAsync();
            Log($"[API] GetProducts response status: {(int)response.StatusCode} {response.StatusCode}. Base={_httpClient.BaseAddress}");

            if (!response.IsSuccessStatusCode)
            {
                Log($"[API] GetProducts failed body: {Preview(content, 300)}");
                return new ApiResponse<List<Product>>
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(ExtractApiErrorMessage(content))
                        ? "Failed to load products"
                        : ExtractApiErrorMessage(content)!
                };
            }

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
            catch { }

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    foreach (var key in new[] { "data", "products", "items", "result", "results" })
                    {
                        if (root.TryGetProperty(key, out var elem) && elem.ValueKind == JsonValueKind.Array)
                        {
                            var list = JsonSerializer.Deserialize<List<Product>>(elem.GetRawText(), opts);
                            if (list != null)
                            {
                                NormalizeProductImageUrls(list);
                                return new ApiResponse<List<Product>> { Success = true, Message = "Products loaded", Data = list };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[API] Product parse error: {ex.Message}");
            }

            return new ApiResponse<List<Product>> { Success = false, Message = "Failed to parse products" };
        }
        catch (HttpRequestException ex)
        {
            Log($"[API] GetProducts HttpRequestException: {ex.Message}");
            return new ApiResponse<List<Product>> { Success = false, Message = ClassifyNetworkError(ex) };
        }
        catch (TaskCanceledException ex)
        {
            Log($"[API] GetProducts timeout: {ex.Message}");
            return new ApiResponse<List<Product>> { Success = false, Message = "Request timeout" };
        }
        catch (Exception ex)
        {
            Log($"[API] GetProducts error: {ex.Message}");
            return new ApiResponse<List<Product>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Product>> GetProductByIdAsync(int productId)
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            if (networkAccess != NetworkAccess.Internet)
            {
                Log($"[API] GetProductById blocked, network access: {networkAccess}");
                return new ApiResponse<Product> { Success = false, Message = "No internet connection" };
            }

            var response = await GetAsyncWithRetry($"{AppConfig.ProductController}/{productId}");
            var content = await response.Content.ReadAsStringAsync();
            Log($"[API] GetProductById/{productId} response status: {(int)response.StatusCode} {response.StatusCode}. Base={_httpClient.BaseAddress}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse<Product>
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(ExtractApiErrorMessage(content))
                        ? "Product not found"
                        : ExtractApiErrorMessage(content)!
                };
            }

            try
            {
                var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<Product>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (wrappedResponse?.Data != null)
                {
                    wrappedResponse.Success = true;
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
        catch (HttpRequestException ex)
        {
            Log($"[API] GetProductById HttpRequestException: {ex.Message}");
            return new ApiResponse<Product> { Success = false, Message = ClassifyNetworkError(ex) };
        }
        catch (TaskCanceledException ex)
        {
            Log($"[API] GetProductById timeout: {ex.Message}");
            return new ApiResponse<Product> { Success = false, Message = "Request timeout" };
        }
        catch (Exception ex)
        {
            Log($"[API] GetProductById error: {ex.Message}");
            return new ApiResponse<Product> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<Order>>> GetOrdersAsync()
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            if (networkAccess != NetworkAccess.Internet)
            {
                Log($"[API] GetOrders blocked, network access: {networkAccess}");
                return new ApiResponse<List<Order>> { Success = false, Message = "No internet connection" };
            }

            var primaryPath = $"{AppConfig.OrderController}/my?includeItems=false";
            var fallbackPath = $"{AppConfig.OrderController}?includeItems=false";

            var response = await GetAsyncWithRetry(primaryPath);
            if (!response.IsSuccessStatusCode && (int)response.StatusCode == 404)
            {
                response = await GetAsyncWithRetry(fallbackPath);
            }

            var content = await response.Content.ReadAsStringAsync();
            Log($"[API] GetOrders response status: {(int)response.StatusCode} {response.StatusCode}. Base={_httpClient.BaseAddress}");

            if (!response.IsSuccessStatusCode)
            {
                Log($"[API] GetOrders failed body: {Preview(content, 300)}");
                return new ApiResponse<List<Order>>
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(ExtractApiErrorMessage(content))
                        ? "Failed to load orders"
                        : ExtractApiErrorMessage(content)!
                };
            }

            try
            {
                var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<List<Order>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (wrappedResponse?.Data != null)
                {
                    wrappedResponse.Success = true;
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
                Log($"[API] Orders envelope parse error: {ex.Message}");
            }

            return new ApiResponse<List<Order>> { Success = false, Message = "Failed to parse orders" };
        }
        catch (HttpRequestException ex)
        {
            Log($"[API] GetOrders HttpRequestException: {ex.Message}");
            return new ApiResponse<List<Order>> { Success = false, Message = ClassifyNetworkError(ex) };
        }
        catch (TaskCanceledException ex)
        {
            Log($"[API] GetOrders timeout: {ex.Message}");
            return new ApiResponse<List<Order>> { Success = false, Message = "Request timeout" };
        }
        catch (Exception ex)
        {
            Log($"[API] GetOrders error: {ex.Message}");
            return new ApiResponse<List<Order>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<Order>>> GetOrdersByMobileAsync(string mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
        {
            return new ApiResponse<List<Order>> { Success = false, Message = "Mobile number is required" };
        }

        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            if (networkAccess != NetworkAccess.Internet)
            {
                Log($"[API] GetOrdersByMobile blocked, network access: {networkAccess}");
                return new ApiResponse<List<Order>> { Success = false, Message = "No internet connection" };
            }

            var normalizedMobile = NormalizeMobileForCompare(mobileNumber);
            var encodedMobile = Uri.EscapeDataString(mobileNumber.Trim());

            var endpoints = new[]
            {
                $"{AppConfig.OrderController}/mobile/{encodedMobile}?includeItems=false",
                $"{AppConfig.OrderController}/by-mobile/{encodedMobile}",
                $"{AppConfig.OrderController}/search?mobileNumber={encodedMobile}&includeItems=false",
                $"{AppConfig.OrderController}?mobileNumber={encodedMobile}&includeItems=false",
                $"{AppConfig.OrderController}?mobile={encodedMobile}&includeItems=false",
                $"{AppConfig.OrderController}"
            };

            foreach (var endpoint in endpoints)
            {
                HttpResponseMessage response;
                string content;

                try
                {
                    response = await GetAsyncWithRetry(endpoint);
                    content = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    Log($"[API] GetOrdersByMobile '{endpoint}' exception: {ex.Message}");
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Log($"[API] GetOrdersByMobile '{endpoint}' failed: {(int)response.StatusCode}");
                    continue;
                }

                var orders = TryParseOrderList(content);
                if (orders == null)
                {
                    continue;
                }

                var filteredOrders = orders
                    .Where(order => IsOrderForMobile(order, normalizedMobile)
                        && !string.Equals(order.Status, "Delivered", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(order => order.OrderDate)
                    .ToList();

                return new ApiResponse<List<Order>>
                {
                    Success = true,
                    Message = "Orders loaded",
                    Data = filteredOrders
                };
            }

            return new ApiResponse<List<Order>>
            {
                Success = true,
                Message = "Orders loaded",
                Data = new List<Order>()
            };
        }
        catch (HttpRequestException ex)
        {
            Log($"[API] GetOrdersByMobile HttpRequestException: {ex.Message}");
            return new ApiResponse<List<Order>> { Success = false, Message = ClassifyNetworkError(ex) };
        }
        catch (TaskCanceledException ex)
        {
            Log($"[API] GetOrdersByMobile timeout: {ex.Message}");
            return new ApiResponse<List<Order>> { Success = false, Message = "Request timeout" };
        }
        catch (Exception ex)
        {
            Log($"[API] GetOrdersByMobile error: {ex.Message}");
            return new ApiResponse<List<Order>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Order>> GetOrderByIdAsync(int orderId)
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            if (networkAccess != NetworkAccess.Internet)
            {
                Log($"[API] GetOrderById blocked, network access: {networkAccess}");
                return new ApiResponse<Order> { Success = false, Message = "No internet connection" };
            }

            var endpoints = new[]
            {
                $"dealer/orders/{orderId}",
                $"dealer/orders/{orderId}?includeItems=true",
                $"{AppConfig.OrderController}/{orderId}?includeItems=true",
                $"{AppConfig.OrderController}/{orderId}",
                $"{AppConfig.OrderController}/details/{orderId}?includeItems=true",
                $"{AppConfig.OrderController}/details/{orderId}",
                $"{AppConfig.AdminController}/orders/{orderId}?includeItems=true",
                $"{AppConfig.AdminController}/orders/{orderId}"
            };

            foreach (var endpoint in endpoints)
            {
                HttpResponseMessage response;
                string content;

                try
                {
                    response = await GetAsyncWithRetry(endpoint);
                    content = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    Log($"[API] GetOrderById {endpoint} exception: {ex.Message}");
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Log($"[API] GetOrderById {endpoint} failed: {(int)response.StatusCode} {response.ReasonPhrase}");
                    continue;
                }

                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<Order>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse?.Data != null)
                    {
                        wrappedResponse.Success = true;
                        return wrappedResponse;
                    }
                }
                catch { }

                try
                {
                    var directObj = JsonSerializer.Deserialize<Order>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (directObj != null && directObj.OrderId > 0)
                    {
                        return new ApiResponse<Order>
                        {
                            Success = true,
                            Message = "Order loaded",
                            Data = directObj
                        };
                    }
                }
                catch { }

                try
                {
                    using var document = JsonDocument.Parse(content);
                    if (document.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        var order = TryReadOrder(document.RootElement, "data")
                            ?? TryReadOrder(document.RootElement, "order")
                            ?? TryReadOrder(document.RootElement, "result")
                            ?? (document.RootElement.TryGetProperty("data", out var dataNode) && dataNode.ValueKind == JsonValueKind.Object
                                ? TryReadOrder(dataNode, "order") ?? TryReadOrder(dataNode, "result")
                                : null);

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
                    Log($"[API] Order detail envelope parse error for {endpoint}: {ex.Message}");
                }
            }

            return new ApiResponse<Order> { Success = false, Message = "Order not found" };
        }
        catch (HttpRequestException ex)
        {
            Log($"[API] GetOrderById HttpRequestException: {ex.Message}");
            return new ApiResponse<Order> { Success = false, Message = ClassifyNetworkError(ex) };
        }
        catch (TaskCanceledException ex)
        {
            Log($"[API] GetOrderById timeout: {ex.Message}");
            return new ApiResponse<Order> { Success = false, Message = "Request timeout" };
        }
        catch (Exception ex)
        {
            Log($"[API] GetOrderById error: {ex.Message}");
            return new ApiResponse<Order> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Order>> GetAdminOrderByIdAsync(int orderId)
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            if (networkAccess != NetworkAccess.Internet)
            {
                Log($"[API] GetAdminOrderById blocked, network access: {networkAccess}");
                return new ApiResponse<Order> { Success = false, Message = "No internet connection" };
            }

            var endpoints = new[]
            {
                $"dealer/orders/{orderId}",
                $"dealer/orders/{orderId}?includeItems=true",
                $"{AppConfig.AdminController}/orders/{orderId}?includeItems=true",
                $"{AppConfig.AdminController}/orders/{orderId}",
                $"{AppConfig.OrderController}/{orderId}?includeItems=true",
                $"{AppConfig.OrderController}/{orderId}",
                $"{AppConfig.OrderController}/details/{orderId}?includeItems=true",
                $"{AppConfig.OrderController}/details/{orderId}"
            };

            foreach (var endpoint in endpoints)
            {
                HttpResponseMessage response;
                string content;

                try
                {
                    response = await GetAsyncWithRetry(endpoint);
                    content = await response.Content.ReadAsStringAsync();
                    Log($"[API] GetAdminOrderById {endpoint} → {(int)response.StatusCode}. Body: {Preview(content, 300)}");
                }
                catch (Exception ex)
                {
                    Log($"[API] GetAdminOrderById {endpoint} exception: {ex.Message}");
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                try
                {
                    var wrapped = JsonSerializer.Deserialize<ApiResponse<Order>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrapped?.Data != null)
                    {
                        wrapped.Success = true;
                        return wrapped;
                    }
                }
                catch { }

                try
                {
                    var direct = JsonSerializer.Deserialize<Order>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (direct != null && direct.OrderId > 0)
                        return new ApiResponse<Order> { Success = true, Message = "Order loaded", Data = direct };
                }
                catch { }

                try
                {
                    using var document = JsonDocument.Parse(content);
                    if (document.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        var order = TryReadOrder(document.RootElement, "data")
                            ?? TryReadOrder(document.RootElement, "order")
                            ?? TryReadOrder(document.RootElement, "result")
                            ?? (document.RootElement.TryGetProperty("data", out var dataNode) && dataNode.ValueKind == JsonValueKind.Object
                                ? TryReadOrder(dataNode, "order") ?? TryReadOrder(dataNode, "result")
                                : null);

                        if (order != null)
                            return new ApiResponse<Order> { Success = true, Message = "Order loaded", Data = order };
                    }
                }
                catch (Exception ex)
                {
                    Log($"[API] GetAdminOrderById envelope parse error for {endpoint}: {ex.Message}");
                }
            }

            return new ApiResponse<Order> { Success = false, Message = "Order not found" };
        }
        catch (HttpRequestException ex)
        {
            Log($"[API] GetAdminOrderById HttpRequestException: {ex.Message}");
            return new ApiResponse<Order> { Success = false, Message = ClassifyNetworkError(ex) };
        }
        catch (TaskCanceledException ex)
        {
            Log($"[API] GetAdminOrderById timeout: {ex.Message}");
            return new ApiResponse<Order> { Success = false, Message = "Request timeout" };
        }
        catch (Exception ex)
        {
            Log($"[API] GetAdminOrderById error: {ex.Message}");
            return new ApiResponse<Order> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Order>> CreateOrderAsync(CreateOrderRequest orderRequest)
    {
        try
        {
            var payloadCandidates = new object[]
            {
                orderRequest,
                new
                {
                    customerMobileNumber = orderRequest.MobileNumber,
                    customerMobile = orderRequest.MobileNumber,
                    mobileNumber = orderRequest.MobileNumber,
                    deliveryAddress = orderRequest.DeliveryAddress,
                    customerAddress = orderRequest.DeliveryAddress,
                    address = orderRequest.DeliveryAddress,
                    items = orderRequest.Items
                }
            };

            foreach (var payload in payloadCandidates)
            {
                var response = await PostAsJsonAsyncWithRetry($"{AppConfig.OrderController}", payload);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<Order>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (wrappedResponse != null && (wrappedResponse.Success || wrappedResponse.Data != null))
                        {
                            wrappedResponse.Success = true;
                            return wrappedResponse;
                        }
                    }
                    catch { }

                    try
                    {
                        var directObj = JsonSerializer.Deserialize<Order>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (directObj != null)
                        {
                            return new ApiResponse<Order> { Success = true, Message = "Order created", Data = directObj };
                        }
                    }
                    catch { }

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
                                return new ApiResponse<Order> { Success = true, Message = "Order created", Data = order };
                            }
                        }
                    }
                    catch { }

                    return new ApiResponse<Order> { Success = true, Message = "Order created" };
                }

                var message = ExtractApiErrorMessage(content);
                if (!string.IsNullOrWhiteSpace(message) &&
                    !message.Contains("required", StringComparison.OrdinalIgnoreCase))
                {
                    return new ApiResponse<Order> { Success = false, Message = message };
                }
            }

            return new ApiResponse<Order> { Success = false, Message = "Failed to create order" };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] CreateOrder error: {ex.Message}");
            return new ApiResponse<Order> { Success = false, Message = ClassifyNetworkError(ex) };
        }
    }


    public async Task<ApiResponse<List<Category>>> GetDealerShopsAsync()
    {
        try
        {
            var response = await GetAsyncWithRetry("dealer/shops");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse<List<Category>> { Success = false, Message = "Failed to load your shops" };
            }

            try
            {
                var wrapped = JsonSerializer.Deserialize<ApiResponse<List<Category>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (wrapped?.Data != null)
                {
                    wrapped.Success = true;
                    return wrapped;
                }
            }
            catch { }

            try
            {
                var list = JsonSerializer.Deserialize<List<Category>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (list != null)
                {
                    return new ApiResponse<List<Category>> { Success = true, Data = list, Message = "Shops loaded" };
                }
            }
            catch { }

            return new ApiResponse<List<Category>> { Success = false, Message = "Failed to parse shops" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Category>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<Product>>> GetDealerProductsAsync()
    {
        try
        {
            var response = await GetAsyncWithRetry("dealer/products");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse<List<Product>> { Success = false, Message = "Failed to load your products" };
            }

            try
            {
                var wrapped = JsonSerializer.Deserialize<ApiResponse<List<Product>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (wrapped?.Data != null)
                {
                    NormalizeProductImageUrls(wrapped.Data);
                    wrapped.Success = true;
                    return wrapped;
                }
            }
            catch { }

            try
            {
                var list = JsonSerializer.Deserialize<List<Product>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (list != null)
                {
                    NormalizeProductImageUrls(list);
                    return new ApiResponse<List<Product>> { Success = true, Data = list, Message = "Products loaded" };
                }
            }
            catch { }

            return new ApiResponse<List<Product>> { Success = false, Message = "Failed to parse products" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Product>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Product>> CreateDealerProductAsync(CreateProductRequest productRequest)
    {
        try
        {
            var payload = new
            {
                name = productRequest.Name,
                description = productRequest.Description,
                price = productRequest.Price,
                stockQuantity = productRequest.StockQuantity,
                categoryId = productRequest.CategoryId,
                shopId = productRequest.CategoryId,
                photoUrl = productRequest.PhotoUrl?.Trim() ?? string.Empty
            };

            var response = await PostAsJsonAsyncWithRetry("dealer/products", payload);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse<Product> { Success = false, Message = ExtractApiErrorMessage(content) ?? "Failed to create product" };
            }

            var product = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (product != null)
            {
                NormalizeProductImageUrl(product);
                return new ApiResponse<Product> { Success = true, Data = product, Message = "Product created" };
            }

            return new ApiResponse<Product> { Success = true, Message = "Product created" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Product> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> UpdateDealerProductAsync(UpdateProductRequest productRequest)
    {
        try
        {
            var payload = new
            {
                name = productRequest.Name,
                description = productRequest.Description,
                price = productRequest.Price,
                stockQuantity = productRequest.StockQuantity,
                categoryId = productRequest.CategoryId,
                shopId = productRequest.CategoryId,
                photoUrl = productRequest.PhotoUrl?.Trim() ?? string.Empty,
                isActive = productRequest.IsActive
            };

            var response = await PutAsJsonAsyncWithRetry($"dealer/products/{productRequest.ProductId}", payload);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Message = ExtractApiErrorMessage(content) ?? "Failed to update product" };
            }

            return new ApiResponse { Success = true, Message = "Product updated" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> DeleteDealerProductAsync(int productId)
    {
        try
        {
            var response = await DeleteAsyncWithRetry($"dealer/products/{productId}");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Message = ExtractApiErrorMessage(content) ?? "Failed to delete product" };
            }

            return new ApiResponse { Success = true, Message = "Product deleted" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> UpdateOrderStatusAsync(int orderId, string status)
    {
        if (orderId <= 0 || string.IsNullOrWhiteSpace(status))
            return new ApiResponse { Success = false, Message = "Invalid order status request" };

        var statusPayload = new { status };
        var statusPayloadAlt = new { orderStatus = status };

        // Try broad compatibility matrix because backend variants use different routes/actions.
        var adminActionPath = status.Trim().ToLowerInvariant() switch
        {
            "confirmed" => "confirm",
            "shipped" => "ship",
            "delivered" => "deliver",
            "cancelled" => "cancel",
            _ => string.Empty
        };

        var attempts = new (Func<Task<HttpResponseMessage>> send, string label)[]
        {
            (() => PutAsJsonAsyncWithRetry($"dealer/orders/{orderId}/status", statusPayload), "PUT  dealer/orders/{id}/status {status}"),
            (() => PatchAsJsonAsyncWithRetry($"dealer/orders/{orderId}/status", statusPayload), "PATCH dealer/orders/{id}/status {status}"),
            (() => PutAsJsonAsyncWithRetry($"dealer/orders/{orderId}", statusPayload), "PUT  dealer/orders/{id} {status}"),
            (() => PatchAsJsonAsyncWithRetry($"dealer/orders/{orderId}", statusPayload), "PATCH dealer/orders/{id} {status}"),

            (() => PutAsJsonAsyncWithRetry($"{AppConfig.OrderController}/{orderId}/status", statusPayload), "PUT  orders/{id}/status {status}"),
            (() => PatchAsJsonAsyncWithRetry($"{AppConfig.OrderController}/{orderId}/status", statusPayload), "PATCH orders/{id}/status {status}"),
            (() => PutAsJsonAsyncWithRetry($"{AppConfig.OrderController}/{orderId}", statusPayload), "PUT  orders/{id} {status}"),
            (() => PatchAsJsonAsyncWithRetry($"{AppConfig.OrderController}/{orderId}", statusPayload), "PATCH orders/{id} {status}"),

            (() => PutAsJsonAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}/status", statusPayload), "PUT  admin/orders/{id}/status {status}"),
            (() => PatchAsJsonAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}/status", statusPayload), "PATCH admin/orders/{id}/status {status}"),
            (() => PatchAsJsonAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}", statusPayload), "PATCH admin/orders/{id} {status}"),
            (() => PutAsJsonAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}", new UpdateOrderStatusRequest { OrderId = orderId, Status = status }), "PUT  admin/orders/{id} typed payload"),
            (() => PutAsJsonAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}", statusPayloadAlt), "PUT  admin/orders/{id} {orderStatus}"),

            (() => !string.IsNullOrEmpty(adminActionPath)
                ? PutAsJsonAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}/{adminActionPath}", new { })
                : PutAsJsonAsyncWithRetry($"{AppConfig.AdminController}/orders/{orderId}/status", statusPayload),
                "PUT  admin/orders/{id}/{mappedAction}")
        };

        string lastServerError = "Failed to update order status";

        foreach (var (send, label) in attempts)
        {
            try
            {
                var response = await send();
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[API] {label} → {(int)response.StatusCode}: {Preview(content, 200)}");

                if (response.IsSuccessStatusCode)
                {
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        try
                        {
                            var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (result != null)
                            {
                                result.Success = true;
                                if (string.IsNullOrWhiteSpace(result.Message))
                                    result.Message = "Order status updated";
                                return result;
                            }
                        }
                        catch { }
                    }

                    return new ApiResponse { Success = true, Message = "Order status updated" };
                }

                var extracted = ExtractApiErrorMessage(content);
                if (!string.IsNullOrWhiteSpace(extracted))
                    lastServerError = extracted!;
                else if (!string.IsNullOrWhiteSpace(content))
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
            // Try admin endpoint first (returns all products including inactive),
            // fall back to public endpoint if admin route doesn't exist on this backend.
            var adminPath = $"{AppConfig.AdminController}/products";
            var publicPath = "products";
            var requestPath = adminPath;
            var hasAuthHeader = _httpClient.DefaultRequestHeaders.Authorization != null;
            System.Diagnostics.Debug.WriteLine($"[API] Fetching all products (admin) from GET {requestPath} (AuthHeaderPresent={hasAuthHeader})");
            var response = await GetAsyncWithRetry(requestPath);
            if (!response.IsSuccessStatusCode && ((int)response.StatusCode == 404 || (int)response.StatusCode == 405))
            {
                Log($"[API] Admin products endpoint returned {(int)response.StatusCode}, falling back to public endpoint");
                requestPath = publicPath;
                response = await GetAsyncWithRetry(requestPath);
            }
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
        // Try admin endpoint first (may return inactive categories too), fall back to public.
        foreach (var path in new[] { $"{AppConfig.AdminController}/categories", "categories" })
        {
            try
            {
                Log($"[API] GetAllCategoriesAdmin: trying GET {path}");
                var response = await GetAsyncWithRetry(path);
                var content = await response.Content.ReadAsStringAsync();
                Log($"[API] GET {path} → {(int)response.StatusCode}");

                if (!response.IsSuccessStatusCode) continue;

                try
                {
                    var wrapped = JsonSerializer.Deserialize<ApiResponse<List<Category>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrapped?.Success == true && wrapped.Data != null)
                        return wrapped;
                }
                catch { }

                var list = JsonSerializer.Deserialize<List<Category>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (list != null)
                {
                    Log($"[API] Loaded {list.Count} categories from {path}");
                    return new ApiResponse<List<Category>> { Success = true, Message = "Categories loaded", Data = list };
                }
            }
            catch (Exception ex)
            {
                Log($"[API] GetAllCategoriesAdmin '{path}' error: {ex.Message}");
            }
        }
        return new ApiResponse<List<Category>> { Success = false, Message = "Failed to load categories" };
    }

    public async Task<ApiResponse<Category>> CreateCategoryAsync(CreateCategoryRequest categoryRequest)
    {
        try
        {
            var normalizedPhotoUrl = categoryRequest.PhotoUrl?.Trim() ?? string.Empty;
            var payload = new
            {
                name = categoryRequest.Name,
                description = categoryRequest.Description ?? string.Empty,
                photoUrl = normalizedPhotoUrl,
                imageUrl = normalizedPhotoUrl,
                isActive = true
            };
            var response = await PostAsJsonAsyncWithRetry($"{AppConfig.AdminController}/categories", payload);
            var content = await response.Content.ReadAsStringAsync();
            Log($"[API] CreateCategory response status: {(int)response.StatusCode}. Body: {Preview(content, 200)}");
            
            if (response.IsSuccessStatusCode)
            {
                // Try to parse the returned category; if the server just returns success/no body, synthesise one.
                try
                {
                    var category = JsonSerializer.Deserialize<Category>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (category != null && (category.CategoryId > 0 || !string.IsNullOrWhiteSpace(category.Name)))
                        return new ApiResponse<Category> { Success = true, Message = "Category created", Data = category };
                }
                catch { }
                // Return a synthetic object so callers can rely on Success = true.
                return new ApiResponse<Category> { Success = true, Message = "Category created", Data = new Category { Name = categoryRequest.Name } };
            }
            else
            {
                var message = ExtractApiErrorMessage(content) ?? "Failed to create category";
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
            var normalizedPhotoUrl = categoryRequest.PhotoUrl?.Trim() ?? string.Empty;
            var payload = new
            {
                name = categoryRequest.Name,
                description = categoryRequest.Description ?? string.Empty,
                photoUrl = normalizedPhotoUrl,
                isActive = categoryRequest.IsActive
            };
            var url = $"{AppConfig.AdminController}/categories/{categoryRequest.CategoryId}";

            // Try PATCH first (partial update) — many REST backends use PATCH for field updates.
            // Fall back to PUT if PATCH returns 404/405.
            HttpResponseMessage response;
            string content;
            response = await PatchAsJsonAsyncWithRetry(url, payload);
            content = await response.Content.ReadAsStringAsync();
            Log($"[API] PATCH UpdateCategory → {(int)response.StatusCode}. Body: {Preview(content, 200)}");

            if (!response.IsSuccessStatusCode &&
                ((int)response.StatusCode == 404 || (int)response.StatusCode == 405 || (int)response.StatusCode == 400))
            {
                Log($"[API] PATCH failed with {(int)response.StatusCode}, retrying with PUT");
                // PUT typically requires all fields; include categoryId in body for compatibility.
                var putPayload = new
                {
                    categoryId = categoryRequest.CategoryId,
                    name = categoryRequest.Name,
                    description = categoryRequest.Description ?? string.Empty,
                    photoUrl = normalizedPhotoUrl,
                    isActive = categoryRequest.IsActive
                };
                response = await PutAsJsonAsyncWithRetry(url, putPayload);
                content = await response.Content.ReadAsStringAsync();
                Log($"[API] PUT UpdateCategory → {(int)response.StatusCode}. Body: {Preview(content, 200)}");
            }

            if (response.IsSuccessStatusCode)
                return new ApiResponse { Success = true, Message = "Category updated" };

            var message = ExtractApiErrorMessage(content) ?? "Failed to update category";
            return new ApiResponse { Success = false, Message = message };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<AppUser>> GetAdminUserByIdAsync(int userId)
    {
        // Try multiple common endpoint patterns for fetching a single user.
        var endpoints = new[]
        {
            $"{AppConfig.AdminController}/users/{userId}",
            $"users/{userId}",
            $"{AppConfig.AdminController}/users",   // list-all fallback
            "users"
        };

        foreach (var endpoint in endpoints)
        {
            try
            {
                Log($"[API] GetAdminUserById: trying GET {endpoint}");
                var response = await GetAsyncWithRetry(endpoint);
                if (!response.IsSuccessStatusCode) continue;

                var content = await response.Content.ReadAsStringAsync();
                Log($"[API] GET {endpoint} → {(int)response.StatusCode}. Body: {Preview(content, 200)}");

                // Single-user wrapped response
                try
                {
                    var wrapped = JsonSerializer.Deserialize<ApiResponse<AppUser>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrapped?.Success == true && wrapped.Data != null) return wrapped;
                }
                catch { }

                // Single-user direct object
                try
                {
                    var user = JsonSerializer.Deserialize<AppUser>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (user != null && (user.UserId > 0 || !string.IsNullOrWhiteSpace(user.MobileNumber)))
                        return new ApiResponse<AppUser> { Success = true, Data = user };
                }
                catch { }

                // List-all endpoints — find matching user by int id or string userId
                try
                {
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    List<AppUser>? list = null;
                    try { list = JsonSerializer.Deserialize<List<AppUser>>(content, opts); } catch { }
                    if (list == null)
                    {
                        var wrapped2 = JsonSerializer.Deserialize<ApiResponse<List<AppUser>>>(content, opts);
                        list = wrapped2?.Data;
                    }
                    if (list != null)
                    {
                        var match = list.FirstOrDefault(u => u.UserId == userId);
                        if (match != null)
                            return new ApiResponse<AppUser> { Success = true, Data = match };
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                Log($"[API] GetAdminUserById '{endpoint}' error: {ex.Message}");
            }
        }

        return new ApiResponse<AppUser> { Success = false, Message = "User not found" };
    }

    public async Task<ApiResponse> DeleteCategoryAsync(int categoryId)
    {
        try
        {
            var endpoint = $"{AppConfig.AdminController}/categories/{categoryId}";
            System.Diagnostics.Debug.WriteLine($"[API] DELETE {endpoint}");
            var response = await DeleteAsyncWithRetry(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[API] DELETE {endpoint} → {(int)response.StatusCode}: {Preview(content, 200)}");

            if (response.IsSuccessStatusCode)
                return new ApiResponse { Success = true, Message = "Category deleted" };

            var msg = ExtractApiErrorMessage(content);
            if (string.IsNullOrWhiteSpace(msg))
                msg = $"Server returned {(int)response.StatusCode}";
            return new ApiResponse { Success = false, Message = msg };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] DeleteCategory exception: {ex.Message}");
            return new ApiResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<Order>>> GetAllOrdersAdminAsync()
    {
        try
        {
            var response = await GetAsyncWithRetry($"{AppConfig.AdminController}/orders?includeItems=false");
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

    public async Task<ApiResponse<DealerDashboard>> GetDealerDashboardAsync()
    {
        try
        {
            var response = await GetAsyncWithRetry("dealer/dashboard");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Log($"[API] GetDealerDashboard failed: {(int)response.StatusCode} - {content}");
                return new ApiResponse<DealerDashboard>
                {
                    Success = false,
                    Message = $"Failed to load dashboard: {response.ReasonPhrase}"
                };
            }

            var dashboard = JsonSerializer.Deserialize<DealerDashboard>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dashboard != null)
            {
                return new ApiResponse<DealerDashboard>
                {
                    Success = true,
                    Message = "Dashboard loaded",
                    Data = dashboard
                };
            }

            return new ApiResponse<DealerDashboard>
            {
                Success = false,
                Message = "Failed to parse dashboard data"
            };
        }
        catch (Exception ex)
        {
            Log($"[API] GetDealerDashboard error: {ex.Message}");
            return new ApiResponse<DealerDashboard> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<Order>>> GetDealerOrdersAsync()
    {
        try
        {
            var endpoints = new[]
            {
                "dealer/orders?includeItems=false",
                $"{AppConfig.AdminController}/orders/my-shop",
                $"{AppConfig.OrderController}/dealer",
                $"{AppConfig.OrderController}/my-shop"
            };

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var response = await GetAsyncWithRetry(endpoint);
                    var content = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        Log($"[API] GetDealerOrders {endpoint} failed: {(int)response.StatusCode}");
                        continue;
                    }

                    try
                    {
                        var wrapped = JsonSerializer.Deserialize<ApiResponse<List<Order>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (wrapped?.Data != null)
                        {
                            wrapped.Success = true;
                            return wrapped;
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
                }
                catch (Exception ex)
                {
                    Log($"[API] GetDealerOrders exception on {endpoint}: {ex.Message}");
                }
            }

            return new ApiResponse<List<Order>>
            {
                Success = false,
                Message = "Failed to load dealer orders"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Order>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    private static List<Order>? TryParseOrderList(string content)
    {
        try
        {
            var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<List<Order>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (wrappedResponse?.Data != null)
            {
                return wrappedResponse.Data;
            }
        }
        catch { }

        try
        {
            var directList = JsonSerializer.Deserialize<List<Order>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (directList != null)
            {
                return directList;
            }
        }
        catch { }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var list = TryReadOrderArray(document.RootElement, "data")
                    ?? TryReadOrderArray(document.RootElement, "items")
                    ?? TryReadOrderArray(document.RootElement, "orders")
                    ?? TryReadOrderArray(document.RootElement, "result");

                if (list == null && document.RootElement.TryGetProperty("data", out var dataNode) && dataNode.ValueKind == JsonValueKind.Object)
                {
                    list = TryReadOrderArray(dataNode, "items")
                        ?? TryReadOrderArray(dataNode, "orders")
                        ?? TryReadOrderArray(dataNode, "result");
                }

                return list;
            }
        }
        catch { }

        return null;
    }

    private static bool IsOrderForMobile(Order? order, string normalizedMobile)
    {
        if (order == null)
        {
            return false;
        }

        var orderMobile = NormalizeMobileForCompare(order.UserMobileNumber);
        return !string.IsNullOrWhiteSpace(orderMobile) && string.Equals(orderMobile, normalizedMobile, StringComparison.Ordinal);
    }

    private static string NormalizeMobileForCompare(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            return string.Empty;
        }

        var chars = mobile.Where(char.IsDigit).ToArray();
        return new string(chars);
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
