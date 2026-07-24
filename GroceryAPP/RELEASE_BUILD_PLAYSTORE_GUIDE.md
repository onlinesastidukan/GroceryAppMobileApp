# Release Build & Play Store Upload Guide
**New App: com.groceryapp.sastidukan**

---

## **Prerequisites Checklist**

Before building, ensure:

- ✅ `ApplicationId` = `com.groceryapp.sastidukan` (in GroceryApp.csproj)
- ✅ `ApplicationVersion` = `2`
- ✅ `ApplicationDisplayVersion` = `1.0.1`
- ✅ Android API target = 35
- ✅ `Platforms\Android\google-services.json` is updated with new Firebase config
- ✅ Keystore file is ready (see Step 1 below)
- ✅ `FIREBASE_SERVICE_ACCOUNT_JSON` deployed on Railway
- ✅ New Android app registered in Firebase Console

---

## **Step 1: Create/Verify Release Keystore**

### **Option A: Create New Keystore (Recommended for new app)**

Run in PowerShell from workspace root:

```powershell
$keystorePath = "E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\release.keystore"
$keystoreAlias = "groceryapp"
$keystorePassword = "Rohit@123" # Change this to your password
$keyPassword = "Rohit@123"      # Change this to your password

keytool -genkey -v `
  -keystore $keystorePath `
  -keyalg RSA `
  -keysize 2048 `
  -validity 10950 `
  -alias $keystoreAlias `
  -storepass $keystorePassword `
  -keypass $keyPassword `
  -dname "CN=Sasti Dukan,O=Sasti Dukan,L=India,S=India,C=IN"
```

**Output:**
```
Keystore saved at: E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\release.keystore
Alias: groceryapp
```

**Save these values:**
```
Keystore File: release.keystore
Alias: groceryapp
Keystore Password: Rohit@123
Key Password: Rohit@123
```

### **Option B: Use Existing Keystore**
If you already have `release.keystore`, verify the password works:
```powershell
keytool -list -v -keystore E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\release.keystore -storepass Rohit@123
```

---

## **Step 2: Build Release AAB (Android App Bundle)**

### **Method 1: Via Visual Studio (GUI)**

1. Open Visual Studio
2. Right-click project → **Properties**
3. Go to **Android**
4. Under **Packaging**, set:
   - **Keystore path**: `release.keystore`
   - **Keystore alias**: `groceryapp`
   - **Keystore password**: `Rohit@123`
   - **Key password**: `Rohit@123`
5. Set **Build Configuration**: **Release**
6. Set **Platform**: **Android**
7. Right-click project → **Publish** (or **Build > Publish**)
8. Wait for build to complete

**Output location:**
```
E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\bin\Release\net9.0-android\publish\com.groceryapp.sastidukan-signed.aab
```

---

### **Method 2: Via Command Line (PowerShell)**

```powershell
cd E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP

# Build Release AAB
dotnet publish -f net9.0-android `
  -c Release `
  -p:AndroidPackageFormat=aab `
  -p:AndroidKeyStore=true `
  -p:AndroidSigningKeyStore=release.keystore `
  -p:AndroidSigningKeyAlias=groceryapp `
  -p:AndroidSigningKeyPass=Rohit@123 `
  -p:AndroidSigningStorePass=Rohit@123
```

**Wait for build completion** (5-10 minutes)

**Output:**
```
Successfully published to: bin\Release\net9.0-android\publish\com.groceryapp.sastidukan-signed.aab
```

---

## **Step 3: Verify AAB File**

Check if the AAB was created:

```powershell
ls E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\bin\Release\net9.0-android\publish\*.aab
```

Should show:
```
	Directory: E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\bin\Release\net9.0-android\publish

Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
-a---          1/15/2025 10:30 AM    45000000 com.groceryapp.sastidukan-signed.aab
```

---

## **Step 4: Create New Play Store Listing**

### **In Google Play Console:**

1. Go to **[Google Play Console](https://play.google.com/console)**
2. Click **Create app**
3. Enter app details:
   - **App name**: `Sasti Dukan`
   - **Default language**: English
   - **App category**: Shopping
   - **Type**: Application
   - **Free or paid**: Free
4. Accept agreements
5. Click **Create app**

### **Fill out app details:**

1. **Store Listing**
   - App name: `Sasti Dukan`
   - Short description: `Order groceries online`
   - Full description: [Your description]
   - Screenshots: [Add 2-5 screenshots]
   - Feature graphic: [Add 1024x500 image]
   - Icon: [Add 512x512 icon]
2. **Content Rating**
   - Fill questionnaire
3. **Target Audience**
   - Age 4+
   - Check boxes for content warnings
4. **Pricing & Distribution**
   - Countries: Select relevant countries
5. **App signing**
   - Google will handle signing (recommended)

---

## **Step 5: Upload AAB to Play Console**

1. In Play Console, go to **Release** → **Production** (or **Internal testing**)
2. Click **Create new release**
3. Upload AAB file:
   - Select `com.groceryapp.sastidukan-signed.aab` from:
	 ```
	 E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\bin\Release\net9.0-android\publish\
	 ```
4. Enter release notes:
   ```
   Version 1.0.1 - Initial Release
   - Grocery ordering and delivery
   - Real-time order tracking
   - Multiple dealer support
   - Notification system
   ```
5. Click **Review**
6. Check for any warnings/errors
7. Click **Start rollout to Production** (or **Internal testing** to test first)

---

## **Step 6: Monitor Release**

After uploading:

1. **Play Console** shows:
   - "Rollout in progress" (wait 1-2 hours)
   - Then "Live on Google Play"
2. App will be available in Play Store within 2-24 hours
3. Check **Play Store** app to verify listing is live

---

## **Troubleshooting**

### **Build fails with "Keystore not found"**
- Verify keystore exists: `ls release.keystore`
- Check password is correct

### **Build fails with "Invalid configuration"**
- Ensure `google-services.json` is in `Platforms\Android\`
- Verify `ApplicationId` = `com.groceryapp.sastidukan`

### **Play Store upload fails**
- Check AAB file size (should be 30-100 MB)
- Ensure package name is `com.groceryapp.sastidukan`
- Clear old APKs from Play Console if this is first AAB upload

### **"App integrity missing"**
- Play Console → **Release** → **Setup** → **App integrity**
- Enable "Google Play Protect"

---

## **Save Keystore Info**

Add to your project notes:

```
RELEASE KEYSTORE BACKUP
=======================
File: release.keystore
Alias: groceryapp
Keystore Password: Rohit@123
Key Password: Rohit@123
Created Date: [Today's date]
Validity: 10 years (until 2035)
Package: com.groceryapp.sastidukan
```

**⚠️ KEEP SAFE - Do not commit to Git**

---

## **Next Steps After Release**

1. Verify app appears in Play Store
2. Test download & installation on real device
3. Test notifications on Railway backend
4. Announce app to users
5. Monitor crash reports in Play Console

---

**You're ready to build and publish! 🚀**

