using System;

namespace GroceryApp.Services;

public static class AppConfig
{
    // The d706 Railway public hostname is the currently live backend endpoint.
    public const string ApiBaseUrl = "https://groceryappapi-production-d706.up.railway.app/api";

    // Keep the non-suffixed hostname as fallback in case Railway switches back to it later.
    private const string LegacyRailwayApiBaseUrl = "https://groceryappapi-production.up.railway.app/api";

    // Direct Railway only (Cloudflare disabled for troubleshooting network/TLS path).
    private const string CloudflareWorkerUrl = "";

    public static readonly string[] ApiBaseUrls = string.IsNullOrWhiteSpace(CloudflareWorkerUrl)
        ? new[] { ApiBaseUrl, LegacyRailwayApiBaseUrl }
        : new[] { ApiBaseUrl, CloudflareWorkerUrl, LegacyRailwayApiBaseUrl };

    public const string AuthController = "auth";
    public const string ProductController = "products";
    public const string CategoryController = "categories";
    public const string OrderController = "orders";
    public const string AdminController = "admin";
}
