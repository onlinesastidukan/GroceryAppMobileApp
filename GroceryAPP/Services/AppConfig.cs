using System;

namespace GroceryApp.Services;

public static class AppConfig
{
    // Railway Production URL (direct backend)
    public const string ApiBaseUrl = "https://groceryappapi-production-d706.up.railway.app/api";

    // Direct Railway only (Cloudflare disabled for troubleshooting network/TLS path).
    private const string CloudflareWorkerUrl = "";

    public static readonly string[] ApiBaseUrls = new[]
    {
        ApiBaseUrl
    };
    
    public const string AuthController = "auth";
    public const string ProductController = "products";
    public const string CategoryController = "categories";
    public const string OrderController = "orders";
    public const string AdminController = "admin";
}
