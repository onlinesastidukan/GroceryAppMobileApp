using System;

namespace GroceryApp.Services;

public static class AppConfig
{
    // Railway Production URL - Already deployed
    public const string ApiBaseUrl = "https://groceryappapi-production.up.railway.app/api";

    // Cloudflare Worker proxy URL — resolves from all ISPs/WiFi networks because
    // Cloudflare's anycast IPs are globally trusted. Deploy the Worker once and set
    // the URL below. Free tier: 100,000 requests/day.
    // Deploy at: https://dash.cloudflare.com → Workers & Pages → Create Worker
    // Worker code: https://github.com/your-repo/cf-worker-proxy (5-line proxy)
    private const string CloudflareWorkerUrl = "https://gentle-glitter-76c9.rohitkranti1976.workers.dev/api";

    public static readonly string[] ApiBaseUrls = string.IsNullOrWhiteSpace(CloudflareWorkerUrl)
        ? new[]
        {
            ApiBaseUrl,
            "https://groceryappapi-production.railway.app/api"
        }
        : new[]
        {
            // Cloudflare Worker is tried FIRST — bypasses ISP DNS blocks
            CloudflareWorkerUrl,
            ApiBaseUrl,
            "https://groceryappapi-production.railway.app/api"
        };
    
    public const string AuthController = "auth";
    public const string ProductController = "products";
    public const string CategoryController = "categories";
    public const string OrderController = "orders";
    public const string AdminController = "admin";
}
