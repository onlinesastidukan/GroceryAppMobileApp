# Notifications Full Tracker (Mobile App)

This document tracks the complete notification wiring in the MAUI app, required values/config, payload formats, and exact code locations.

## 1) End-to-end notification flow (current app)

1. Dealer logs in from `DealerLoginPage`.
2. App checks Android notification permission.
3. App fetches FCM token (`FirebaseMessaging.Instance.GetToken()`).
4. App sends token to backend (`auth/update-fcm-token`).
5. Customer places order.
6. App optionally calls notification-trigger endpoints (`notifications/order-placed`, etc.).
7. Backend sends push through Firebase Admin SDK (backend responsibility).
8. Android app receives message in `MyFirebaseMessagingService.OnMessageReceived`.
9. App creates local Android notification (`order_notifications` channel).

---

## 2) Required values from your side

## A) App/backend values
- Working API base URL / Worker URL:
  - `Services/AppConfig.cs` (Cloudflare Worker + fallback URLs).
- Valid JWT login flow so `ApiService` has auth token before updating FCM token.
- Backend endpoint support:
  - `POST auth/update-fcm-token`
  - Notification trigger endpoints attempted by app:
	- `POST notifications/order-placed`
	- `POST notifications/orders/{orderId}`
	- `POST notifications/trigger/order-placed`
	- `POST admin/orders/{orderId}/notify`

## B) Firebase values/files
- `Platforms/Android/google-services.json` must be from the same Firebase project used by backend push sender.
- Android package name in Firebase must match app package:
  - `com.groceryapp.mobile`
- Valid Firebase server-side credentials in backend (service account JSON) and proper Firebase project id.

## C) Device/runtime values
- Android notification permission granted (Android 13+).
- Device has Google Play Services and internet access.
- Fresh valid FCM token saved in backend DB for dealer user.

---

## 3) JSON / payload formats you need

## A) Token update payload (mobile -> backend)
Endpoint: `POST auth/update-fcm-token`

```json
{
  "fcmToken": "<DEVICE_FCM_TOKEN>"
}
```

## B) Optional trigger payload (mobile -> backend)
Endpoint candidates in app (see section 5):

```json
{
  "orderId": 123,
  "customerMobileNumber": "9876543210",
  "eventType": "OrderPlaced"
}
```

## C) FCM payload backend should send (recommended)
Use both `notification` and `data` for best compatibility:

```json
{
  "token": "<DEALER_FCM_TOKEN>",
  "notification": {
	"title": "New Order",
	"body": "A new order has been placed."
  },
  "data": {
	"title": "New Order",
	"body": "Order #123 from 9876543210",
	"orderId": "123",
	"eventType": "OrderPlaced"
  },
  "android": {
	"priority": "high"
  }
}
```

## D) Minimal `google-services.json` fields to verify
- `project_info.project_number`
- `project_info.project_id`
- `client[].client_info.mobilesdk_app_id`
- `client[].client_info.android_client_info.package_name` = `com.groceryapp.mobile`
- `client[].api_key[].current_key`

---

## 4) SDKs/libraries used for notifications
From `GroceryApp.csproj`:
- `Xamarin.Firebase.Messaging` `125.1.1`
- `Xamarin.GooglePlayServices.Base` `118.10.0.2`
- `Xamarin.AndroidX.SavedState` `1.5.0.1`
- `Xamarin.AndroidX.SavedState.SavedState.Ktx` `1.5.0.1`

Also included:
- `GoogleServicesJson Include="Platforms\Android\google-services.json"`

---

## 5) Full code locations (with lines)

## A) Android receive + local notification display
File: `Platforms/Android/MyFirebaseMessagingService.cs`
- Service registration: lines 8-10
- Receive message: lines 14-52
- Data payload extraction (`title`, `body`, `message`): lines 21-39
- Notification payload fallback: lines 41-47
- Token refresh callback: lines 54-58
- Store token locally: lines 60-64
- Build/show local notification: lines 66-99
- Channel id used: `order_notifications` (line 72)

## B) Runtime notification permission (Android 13+)
File: `Platforms/Android/MainActivity.cs`
- Permission request in `OnCreate`: lines 24-31
- Permission result logging: lines 34-48

## C) Manifest permissions
File: `Platforms/Android/AndroidManifest.xml`
- `POST_NOTIFICATIONS`: line 17
- `com.google.android.c2dm.permission.RECEIVE`: line 18
- Internet/network permissions: lines 10-11

## D) Token fetch + permission check service
File: `Services/FirebaseService.cs`
- Interface methods: lines 5-11
- Get token entrypoint: lines 15-22
- Notification permission check: lines 42-66
- Android token fetch (`GetToken()`): lines 68-96
- Save token to preferences: line 80

## E) Dealer login token upload flow
File: `Views/DealerLoginPage.xaml.cs`
- On successful login set auth token: line 44
- Resolve Firebase service: lines 49-50
- Permission check + warning alert: lines 53-62
- Fetch token: line 64
- Send token to backend: lines 68-69
- Error handling (non-blocking login): lines 77-81

## F) API call that saves FCM token to backend
File: `Services/ApiService.cs`
- Method: `UpdateFcmTokenAsync`: lines 347-395
- Endpoint: `auth/update-fcm-token` (line 370)
- Request body: `{ FcmToken = fcmToken }` (line 371)

## G) Order placement + trigger notification API attempt
File: `ViewModels/CustomerViewModels.cs`
- Place order method: lines 306-388
- After order success, call trigger: lines 354-369

File: `Services/ApiService.cs`
- Method: `TriggerOrderPlacedNotificationAsync`: lines 1043-1102
- Payload object: lines 1050-1055
- Endpoint fallback list: lines 1057-1063
- Logs failed endpoint responses: lines 1072-1075

## H) DI registration
File: `MauiProgram.cs`
- Register `IFirebaseService`: line 29
- Register `ApiService`: line 25

---

## 6) Quick manual validation checklist

1. Login as dealer and verify app logs show token fetched.
2. Confirm `UpdateFcmTokenAsync` returns success.
3. Confirm backend DB has latest token for dealer.
4. Place order from customer and confirm trigger endpoint/backend notification path runs.
5. Verify backend push send result (message id / no token error).
6. Verify device receives in `OnMessageReceived` and shows local notification.

---

## 7) Most common failure points

- `google-services.json` project mismatch with backend Firebase project.
- Dealer token stale/invalid in DB.
- Backend never calling Firebase send despite order creation success.
- Notification permission denied on Android 13+.
- Backend sends malformed payload missing title/body or using unexpected shape.
- Worker/proxy route mismatch causing API calls to fail intermittently.
