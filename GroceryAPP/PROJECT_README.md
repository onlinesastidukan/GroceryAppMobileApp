# Sasti Dukan (सस्ती दुकान) - Mobile App (MAUI)

This is a complete .NET MAUI mobile application for the Sasti Dukan ordering system with separate, independent project structure.

## Project Details

**Location:** `e:\Rohit_Mundhe\WOrk\GroceryApp\`

- **Framework:** .NET MAUI 9.0
- **Target Framework:** `net9.0-android` (Android 24+, API 24+)
- **Language:** C# 12+
- **Application ID:** `com.groceryapp.mobile`
- **Version:** 1.0

## Project Structure

```
GroceryApp/
├── App.xaml (.cs)           # Application entry point with resource converters
├── AppShell.xaml (.cs)      # Shell navigation structure
├── MauiProgram.cs           # Dependency Injection & Service Registration
├── GlobalXmlns.cs           # XAML namespace definitions
├── GroceryApp.csproj        # Project file with dependencies
│
├── Views/                   # XAML UI Pages
│   ├── LoginPage.xaml
│   ├── CustomerCategoryPage.xaml
│   ├── CustomerProductPage.xaml
│   ├── CartPage.xaml
│   ├── CustomerOrderHistoryPage.xaml
│   ├── CustomerOrderDetailPage.xaml
│   ├── AdminDashboardPage.xaml
│   ├── AdminOrdersPage.xaml
│   ├── AdminOrderDetailPage.xaml
│   ├── AdminProductsPage.xaml
│   ├── AdminCategoriesPage.xaml
│   └── AdminUsersPage.xaml
│
├── ViewModels/              # MVVM Business Logic
│   ├── BaseViewModel.cs     # Base class with INotifyPropertyChanged
│   ├── LoginViewModel.cs    # Authentication ViewModel
│   ├── CustomerViewModels.cs    # All customer-facing ViewModels
│   └── AdminViewModels.cs       # All admin-facing ViewModels
│
├── Models/                  # Data Models
│   └── DataModels.cs        # All DTO & model classes
│
├── Services/                # Business Services
│   ├── ApiService.cs        # Railway API HTTP client
│   ├── CartService.cs       # Shopping cart management
│   ├── AuthService.cs       # Authentication & user session
│   └── AppConfig.cs         # Configuration constants
│
├── Converters/              # Value Converters
│   └── ValueConverters.cs   # XAML binding converters
│
├── Platforms/               # Platform-specific code
│   └── Android/
│
├── Resources/               # App resources (images, fonts, colors)
│   ├── Styles/
│   ├── Images/
│   └── Fonts/
│
├── Properties/              # App metadata
└── MainPage.xaml            # Default template page (not used)
```

## Features Implemented

### Customer Features
- ✅ User Authentication (Login)
- ✅ Browse Categories
- ✅ Browse Products by Category
- ✅ Add Products to Cart
- ✅ View Cart with Current Items
- ✅ Place Orders
- ✅ View Order History
- ✅ View Order Details with Status

### Admin Features
- ✅ Dashboard with Statistics
  - Total Orders
  - Total Revenue
  - Total Products
  - Total Categories
- ✅ Order Management
  - View All Orders
  - Update Order Status (Pending → Confirmed → Shipped → Delivered)
  - View Order Items
- ✅ Product Management
  - View All Products
  - Add New Products
  - Edit Existing Products
  - Delete Products
- ✅ Category Management
  - View All Categories
  - Add New Categories
  - Edit Existing Categories
  - Delete Categories
- ✅ Users Management (Placeholder)

## API Configuration

**Base URL:** `https://groceryappapi-production.up.railway.app/api`

The mobile app connects to the already-deployed Railway backend API.

### API Endpoints Used
- `auth/login` - User authentication
- `products` - Get products
- `categories` - Get categories
- `orders` - Get & create orders
- `admin/products` - Product CRUD operations
- `admin/categories` - Category CRUD operations
- `admin/orders` - Order management

## Dependencies

```xml
<PackageReference Include="Microsoft.Maui.Controls" Version="9.0" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.1" />
<PackageReference Include="CommunityToolkit.Maui" Version="7.0.1" />
<PackageReference Include="System.Net.Http.Json" Version="8.0.0" />
```

## Build & Deployment

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 with MAUI workload
- Android SDK (API 24+)

### Build APK for Android
```powershell
cd e:\Rohit_Mundhe\WOrk\GroceryApp
dotnet build -f net9.0-android                    # Debug build
dotnet publish -f net9.0-android -c Release       # Release APK
```

### Build Output
- Debug APK: `bin\Debug\net9.0-android\com.groceryapp.mobile-debug.apk`
- Release APK: `bin\Release\net9.0-android\com.groceryapp.mobile-Signed.apk`

## Architecture

### MVVM Pattern
- **Model:** DataModels.cs (DTO classes)
- **View:** XAML pages in Views/
- **ViewModel:** MVVM binding with CommunityToolkit.Mvvm

### Service Layer
- **ApiService:** Handles all HTTP communication with Railway backend
- **CartService:** Manages local shopping cart state
- **AuthService:** Handles authentication and session management

### Navigation
- App.xaml initializes converters and resources
- AppShell.xaml defines navigation routes and shell tabs
- LoginPage redirects to customer or admin routes based on user role

## Authentication Flow

1. User enters email/password on LoginPage
2. LoginViewModel calls AuthService.LoginAsync()
3. AuthService calls ApiService.LoginAsync()
4. API returns JWT token + user details
5. Token stored in SecureStorage
6. User redirected to appropriate shell (customer/admin)

## Error Handling

- Try-catch blocks in all ViewModels
- Error messages displayed in ErrorLabel on each page
- Loading indicators during API calls
- Network error recovery with retry capability

## Build Status

✅ **BUILD SUCCEEDED**

All compilation errors resolved:
- Fixed access modifier conflicts in ViewModel inheritance
- Fixed XAML property assignment errors (removed invalid properties from elements)
- Corrected namespace usage in all files

## Next Steps

1. **Deploy APK to Device/Emulator:**
   ```powershell
   dotnet publish -f net9.0-android -c Release
   adb install -r bin\Release\net9.0-android\com.groceryapp.mobile-Signed.apk
   ```

2. **Test on Android Emulator:**
   - Launch Android emulator
   - Deploy using Visual Studio 2022 (F5)

3. **Sign & Release:**
   - Generate signing keystore for APK signing
   - Configuration in Platforms/Android/AndroidManifest.xml
   - Publish to Google Play Store

## Notes

- This is a completely separate, independent project from the backend
- No external dependencies except Railway API
- Fully MVVM compliant with bindings
- Material Design UI with Indian Rupees (₹) currency format
- Supports both Customer and Admin flows
- State management through Services (Cart, Auth)
- Local storage of auth tokens via SecureStorage

---

**Built:** 2025
**Framework:** .NET MAUI 9.0
**Target:** Android 24+ (API 24+)
