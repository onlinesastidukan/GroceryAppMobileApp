# Firebase Cloud Messaging Implementation - Next Steps

## What's Been Done

✅ Created `MyFirebaseMessagingService.cs` in `Platforms/Android/` to handle incoming notifications
✅ Created `FirebaseService.cs` to get FCM tokens and manage topics
✅ Added FCM permissions to `AndroidManifest.xml`
✅ Registered `FirebaseService` in `MauiProgram.cs`

## Required Manual Steps

### 1. Add NuGet Packages

You need to manually add these NuGet packages to the MAUI project:

**Option A: Using Package Manager Console**
```powershell
Install-Package Xamarin.Firebase.Messaging -Version 133.0.0
Install-Package Xamarin.GooglePlayServices.Base -Version 118.5.0
Install-Package Xamarin.Google.Guava.ListenableFuture -Version 1.0.0.2
```

**Option B: Using Visual Studio UI**
1. Right-click on the project → Manage NuGet Packages
2. Search for and install:
   - `Xamarin.Firebase.Messaging` (version 133.0.0 or latest)
   - `Xamarin.GooglePlayServices.Base` (version 118.5.0 or latest)
   - `Xamarin.Google.Guava.ListenableFuture` (version 1.0.0.2 or latest)

### 2. Add google-services.json

1. Follow the `FIREBASE_FCM_SETUP_GUIDE.md` to download `google-services.json` from Firebase Console
2. Copy `google-services.json` to `Platforms/Android/` folder
3. Right-click the file → Properties
4. Set **Build Action** to `GoogleServicesJson`
5. Set **Copy to Output Directory** to `Always` or `Copy if newer`

### 3. Update .csproj File

Add this to your `GroceryApp.csproj` file inside the `<Project>` tag:

```xml
<ItemGroup>
  <GoogleServicesJson Include="Platforms\Android\google-services.json" />
</ItemGroup>
```

### 4. Register FCM Token When Dealer/Shopkeeper Logs In

Update `AuthService.cs` to send FCM token to backend after login:

```csharp
// In LoginAsync method, after successful login:
if (IsDealer && _firebaseService != null)
{
	try
	{
		var fcmToken = await _firebaseService.GetTokenAsync();
		if (!string.IsNullOrEmpty(fcmToken))
		{
			// Send token to backend
			await _apiService.RegisterFcmTokenAsync(CurrentUser.UserId, fcmToken);
		}
	}
	catch (Exception ex)
	{
		// Log but don't fail login
		System.Diagnostics.Debug.WriteLine($"FCM token registration failed: {ex.Message}");
	}
}
```

### 5. Add FCM Token API Method to ApiService.cs

```csharp
public async Task<bool> RegisterFcmTokenAsync(int userId, string fcmToken)
{
	try
	{
		var request = new { UserId = userId, FcmToken = fcmToken };
		var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/users/fcm-token", request);
		return response.IsSuccessStatusCode;
	}
	catch (Exception ex)
	{
		System.Diagnostics.Debug.WriteLine($"RegisterFcmTokenAsync error: {ex.Message}");
		return false;
	}
}
```

### 6. Request Notification Permissions (Android 13+)

Add to `MainActivity.cs`:

```csharp
protected override void OnCreate(Bundle savedInstanceState)
{
	base.OnCreate(savedInstanceState);

	// Request notification permission for Android 13+
	if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
	{
		if (CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
		{
			RequestPermissions(new[] { Manifest.Permission.PostNotifications }, 1);
		}
	}
}
```

## Backend Implementation (Next Phase)

The backend needs to:

1. **Add FirebaseAdmin NuGet package**
2. **Create a `NotificationService`** to send FCM notifications
3. **Store FCM tokens** in the database (users table or separate fcm_tokens table)
4. **Send notification when order is created** to the shopkeeper whose shop the products belong to

Backend implementation will be covered in the next steps.

## Testing

Once everything is set up:

1. Run the app on a physical Android device (emulator may not support FCM properly)
2. Login as a shopkeeper
3. Check logs for FCM token (should be printed in debug console)
4. Place a test order
5. Shopkeeper should receive a push notification

## Troubleshooting

### "google-services.json not found"
- Ensure file is in `Platforms/Android/` folder
- Check Build Action is set to `GoogleServicesJson`
- Clean and rebuild project

### "Firebase.Messaging not found"
- Ensure NuGet packages are installed
- Restore NuGet packages (right-click solution → Restore NuGet Packages)
- Clean and rebuild

### No notifications received
- Check Firebase Console → Cloud Messaging → check if messages are being sent
- Verify FCM token is being generated and sent to backend
- Check Android notification permissions are granted
- Ensure device has Google Play Services installed

## Current Status

**Mobile App**: Ready for NuGet package installation and google-services.json configuration  
**Backend API**: Not yet implemented (will be done in Step 9)

Once you complete steps 1-6 above, the mobile app will be ready to receive push notifications!
