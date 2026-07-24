# Firebase Cloud Messaging (FCM) Setup Guide for Grocery App

## Complete Step-by-Step Guide for Beginners

This guide will help you set up Firebase Cloud Messaging so shopkeepers receive push notifications when customers place orders.

---

## Part 1: Create Firebase Account & Project

### Step 1.1: Create Google/Firebase Account
1. Go to [https://console.firebase.google.com/](https://console.firebase.google.com/)
2. Click **"Sign in"** with your Google account
   - If you don't have a Google account, create one at [https://accounts.google.com/signup](https://accounts.google.com/signup)

### Step 1.2: Create Firebase Project
1. In Firebase Console, click **"Add project"** or **"Create a project"**
2. **Project Name**: Enter `GroceryApp` or `SastiDukan`
3. Click **Continue**
4. **Google Analytics**: Toggle OFF (you can enable later if needed)
5. Click **Create project**
6. Wait 20-30 seconds for project creation
7. Click **Continue** when done

---

## Part 2: Add Android App to Firebase Project

### Step 2.1: Register Your Android App
1. In your Firebase project dashboard, click the **Android icon** (robot) to add an Android app
2. **Android package name**: Enter `com.groceryapp.mobile`
   - This MUST match the `ApplicationId` in your MAUI project
   - You can verify it in `GroceryApp.csproj` under `<ApplicationId>`
3. **App nickname (optional)**: Enter `Grocery App Mobile`
4. **Debug signing certificate SHA-1 (optional)**: Leave empty for now
5. Click **Register app**

### Step 2.2: Download google-services.json
1. Firebase will show a **Download google-services.json** button
2. Click **Download google-services.json**
3. Save the file to your computer
4. **IMPORTANT**: You'll need to add this file to your MAUI Android project later

### Step 2.3: Complete Setup
1. Click **Next** (skip the SDK setup instructions for now)
2. Click **Next** again
3. Click **Continue to console**

---

## Part 3: Enable Firebase Cloud Messaging

### Step 3.1: Enable FCM API
1. In Firebase Console, click the **⚙️ Settings** icon (top left) → **Project settings**
2. Go to the **Cloud Messaging** tab
3. You should see **Firebase Cloud Messaging API (V1)** section
4. If you see "Cloud Messaging API (V1) is not enabled", click **Enable**
5. If asked to enable in Google Cloud Console, click the link and enable the API

### Step 3.2: Get Web Push Certificate (for V1 API)
1. Still in **Project settings** → **Cloud Messaging** tab
2. Scroll to **Web configuration** section
3. Under **Web Push certificates**, click **Generate key pair**
4. Copy the **Key pair** (looks like: `BPi_vo5eC7jY870ZVw5nXdhakTXxh97hjq0g05ULDfsIyYaJCOJ4i63D8osKKOdkYyjITiTNzIEmou2kqTBXh4c`)
5. **IMPORTANT**: This key is used for web push; for Android/backend, you'll use the Service Account JSON (see Step 5.2)

**Note**: The Legacy Server Key is deprecated. We'll use Firebase Admin SDK with Service Account JSON for the backend.

---

## Part 4: Billing & Quotas (FREE TIER)

### Step 4.1: Understanding Firebase Free Tier
Firebase Cloud Messaging is **100% FREE** with no limits for:
- ✅ Unlimited notifications
- ✅ Unlimited devices
- ✅ No credit card required

### Step 4.2: (Optional) Add Billing for Other Services
If you plan to use other Firebase services (Firestore, Storage, etc.):
1. Go to **Project settings** → **Usage and billing**
2. Click **Modify plan**
3. Choose **Blaze (Pay as you go)** - you only pay for what you use beyond free tier
4. Add a payment method
5. Set budget alerts (recommended: $5-10/month alert)

**For FCM only**: No billing setup needed!

---

## Part 5: Get Required Keys & IDs

You'll need these values for your app configuration:

### 5.1: Project Settings
1. Go to **⚙️ Settings** → **Project settings**
2. **General** tab:
   - **Project ID**: Copy this (e.g., `groceryapp-12345`)
   - **Project Number**: Copy this (e.g., `123456789012`) - this is your **Sender ID**
   - **Web API Key**: Copy this

### 5.2: Service Account Key (for Backend)
1. Go to **⚙️ Settings** → **Project settings** → **Service accounts** tab
2. Click **Generate new private key**
3. Click **Generate key** in the dialog
4. A JSON file will download (e.g., `groceryapp-firebase-adminsdk-xxxxx.json`)
5. **IMPORTANT**: Keep this file VERY secure - it has admin access to your Firebase project
6. You'll upload this to your backend API server

---

## Part 6: Configure Android App in MAUI Project

### Step 6.1: Add google-services.json to Project
1. Open your MAUI project in Visual Studio
2. Locate the `google-services.json` file you downloaded in Step 2.2
3. Copy it to: `Platforms/Android/` folder in your MAUI project
4. In Visual Studio, right-click `google-services.json` → **Properties**
5. Set **Build Action** to `GoogleServicesJson`
6. **Copy to Output Directory**: `Always`

### Step 6.2: Install NuGet Packages
Add these NuGet packages to your MAUI project:

```
Xamarin.Firebase.Messaging (133.0.0 or latest)
Xamarin.GooglePlayServices.Base (118.5.0 or latest)
Xamarin.Google.Guava.ListenableFuture (1.0.0.2 or latest)
```

Via Package Manager Console:
```powershell
Install-Package Xamarin.Firebase.Messaging
Install-Package Xamarin.GooglePlayServices.Base
Install-Package Xamarin.Google.Guava.ListenableFuture
```

---

## Part 7: Backend API Configuration

### Step 7.1: Install Firebase Admin SDK NuGet Package
In your backend API project:

```powershell
Install-Package FirebaseAdmin
```

### Step 7.2: Add Service Account JSON to Backend
You have two supported backend setup modes:

#### Option A: Railway / cloud deployment (recommended)
Use an environment variable instead of committing the file.

- Variable name: `FIREBASE_SERVICE_ACCOUNT_JSON`
- Variable value: paste the **entire service account JSON** as the variable value
- Redeploy the Railway service after saving the variable

**Important:** the JSON must remain valid, including the `private_key` field with `\n` newlines preserved inside the string.

#### Option B: Local file-based setup (optional for local development)
1. Create a folder `Firebase` in your backend project root
2. Copy the service account JSON file (from Step 5.2) into this folder
3. Rename it to `firebase-adminsdk.json` for simplicity
4. Right-click the file → **Properties**
5. Set **Copy to Output Directory**: `Copy if newer`

### Step 7.3: Add Firebase Configuration to appsettings.json
For local file-based development:
```json
{
  "Firebase": {
	"ProjectId": "your-project-id",
	"ServiceAccountPath": "Firebase/firebase-adminsdk.json"
  }
}
```

For Railway, prefer the `FIREBASE_SERVICE_ACCOUNT_JSON` environment variable instead of relying on `ServiceAccountPath`.

---

## Part 8: Testing Your Setup

### Step 8.1: Test from Firebase Console
1. Go to **Cloud Messaging** in left sidebar (under Engage)
2. Click **Send your first message**
3. **Notification title**: "Test Notification"
4. **Notification text**: "This is a test from Firebase"
5. Click **Send test message**
6. Enter a device FCM token (you'll get this after implementing the app code)
7. Click **Test**

---

## Summary of What You Need

After completing this guide, you should have:

✅ Firebase project created
✅ Android app registered with `com.groceryapp.mobile` package name
✅ `google-services.json` file downloaded and added to Android project
✅ FCM API enabled
✅ **Server Key** (Legacy) for backend
✅ **Service Account JSON** for backend
✅ **Project ID** and **Sender ID** noted
✅ NuGet packages identified for installation

---

## Next Steps

After completing this setup guide, the development team will:
1. Implement FCM in the mobile app (register device tokens, handle notifications)
2. Implement FCM in the backend API (send notifications when orders are placed)
3. Store shopkeeper device tokens in the database
4. Link orders to shopkeepers to send targeted notifications

---

## Troubleshooting

### "google-services.json not found"
- Ensure the file is in `Platforms/Android/` folder
- Check Build Action is set to `GoogleServicesJson`

### "FCM API not enabled"
- Go to Google Cloud Console → APIs & Services → Library
- Search for "Firebase Cloud Messaging API"
- Click Enable

### "Firebase not initialized, skipping notification"
- On Railway, verify `FIREBASE_SERVICE_ACCOUNT_JSON` is set with the full JSON content
- Redeploy the backend service after saving variables
- Do not depend on `Firebase:ServiceAccountPath` unless the file truly exists in the deployed container
- Check logs for: `Using Firebase credentials from FIREBASE_SERVICE_ACCOUNT_JSON.` and `Firebase Admin SDK initialized successfully`

### "Invalid package name"
- Package name in Firebase must EXACTLY match `ApplicationId` in your `.csproj`
- Check for typos, case-sensitivity

---

## Cost Estimate

For a small to medium grocery app:
- **FCM (Push Notifications)**: FREE unlimited
- **Firebase Authentication** (if used): FREE for first 10K users/month
- **Firestore Database** (if used): FREE for 50K reads, 20K writes, 20K deletes per day
- **Cloud Functions** (if used): FREE for 2M invocations/month

**Expected cost for your app**: $0/month (completely free on Free tier)

---

## Support & Resources

- **Firebase Documentation**: https://firebase.google.com/docs
- **FCM Documentation**: https://firebase.google.com/docs/cloud-messaging
- **Support**: https://firebase.google.com/support

## Security Reminder
If a service account JSON or its `private_key` is ever shared in chat, screenshots, or source control:
- revoke/delete that key in Firebase / Google Cloud immediately
- generate a new private key
- update Railway with the new JSON

---

**Setup Complete!** 🎉

You're now ready to implement Firebase Cloud Messaging in your Grocery App.
