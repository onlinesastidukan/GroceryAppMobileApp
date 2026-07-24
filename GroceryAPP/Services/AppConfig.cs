using System;

namespace GroceryApp.Services;

public static class AppConfig
{
    // Primary Railway Production URL (stable public hostname)
    public const string ApiBaseUrl = "https://groceryappapi-production.up.railway.app/api";

    // Optional alternate hostnames (kept for resilience during Railway hostname changes)
    private const string LegacyRailwayApiBaseUrl = "https://groceryappapi-production-d706.up.railway.app/api";

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
