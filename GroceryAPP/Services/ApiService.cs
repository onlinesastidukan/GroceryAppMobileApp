using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using GroceryApp.Models;

namespace GroceryApp.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private string _authToken = string.Empty;
    private const string UserAgent = "SastiDukan-Mobile/1.0";

    public ApiService()
    {
        try
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            
            _httpClient = new HttpClient(handler);
            var baseUrl = AppConfig.ApiBaseUrl.TrimEnd('/') + "/";
            _httpClient.BaseAddress = new Uri(baseUrl);
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
            
            var response = await _httpClient.GetAsync(string.Empty);
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
            var response = await _httpClient.PostAsJsonAsync(url, loginRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
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
                Message = "Network error. Please check your connection." 
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
            var response = await _httpClient.PostAsJsonAsync(url, registerRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
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
                return new ApiResponse<RegisterResponse> 
                { 
                    Success = false, 
                    Message = "Registration failed. Please try again." 
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

    public async Task<ApiResponse<List<Category>>> GetCategoriesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{AppConfig.CategoryController}");
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
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<List<Product>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse != null && wrappedResponse.Success)
                    {
                        return wrappedResponse;
                    }
                }
                catch { }
                
                try
                {
                    var directList = JsonSerializer.Deserialize<List<Product>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (directList != null)
                    {
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
            var response = await _httpClient.GetAsync($"{AppConfig.ProductController}/{productId}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<ApiResponse<Product>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrappedResponse != null && wrappedResponse.Success)
                    {
                        return wrappedResponse;
                    }
                }
                catch { }
                
                var directObj = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (directObj != null)
                {
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
            var response = await _httpClient.GetAsync($"{AppConfig.OrderController}");
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
            System.Diagnostics.Debug.WriteLine($"[API] GetOrders error: {ex.Message}");
            return new ApiResponse<List<Order>> { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Order>> GetOrderByIdAsync(int orderId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{AppConfig.OrderController}/{orderId}");
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

    public async Task<ApiResponse<Order>> CreateOrderAsync(CreateOrderRequest orderRequest)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{AppConfig.OrderController}", orderRequest);
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
        try
        {
            var request = new UpdateOrderStatusRequest { OrderId = orderId, Status = status };
            var response = await _httpClient.PutAsJsonAsync($"{AppConfig.OrderController}/{orderId}", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch { }
                
                return new ApiResponse { Success = true, Message = "Order status updated" };
            }
            else
            {
                return new ApiResponse { Success = false, Message = "Failed to update order" };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] UpdateOrderStatus error: {ex.Message}");
            return new ApiResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
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
            var response = await _httpClient.GetAsync(requestPath);
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
                        return wrappedResponse;
                    }
                }
                catch { }
                
                var directList = JsonSerializer.Deserialize<List<Product>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (directList != null)
                {
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
            var response = await _httpClient.PostAsJsonAsync($"{AppConfig.AdminController}/products", productRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var product = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (product != null)
                {
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
            var response = await _httpClient.PutAsJsonAsync($"{AppConfig.AdminController}/products/{productRequest.ProductId}", productRequest);
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
            var response = await _httpClient.DeleteAsync($"{AppConfig.AdminController}/products/{productId}");
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
            var response = await _httpClient.GetAsync($"categories");
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
            var response = await _httpClient.PostAsJsonAsync($"{AppConfig.AdminController}/categories", categoryRequest);
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
            var response = await _httpClient.PutAsJsonAsync($"{AppConfig.AdminController}/categories/{categoryRequest.CategoryId}", categoryRequest);
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
            var response = await _httpClient.DeleteAsync($"{AppConfig.AdminController}/categories/{categoryId}");
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
            var response = await _httpClient.GetAsync($"{AppConfig.AdminController}/orders");
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

    private static string Preview(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return "<empty>";
        return value.Length <= maxLen ? value : value.Substring(0, maxLen) + "...";
    }
}
