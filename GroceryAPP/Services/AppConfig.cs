using System;

namespace GroceryApp.Services;

public static class AppConfig
{
    // Railway Production URL - Already deployed
    public const string ApiBaseUrl = "https://groceryappapi-production.up.railway.app/api";
    
    public const string AuthController = "auth";
    public const string ProductController = "products";
    public const string CategoryController = "categories";
    public const string OrderController = "orders";
    public const string AdminController = "admin";
}
