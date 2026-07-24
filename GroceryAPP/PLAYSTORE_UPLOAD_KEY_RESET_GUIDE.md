# Play Store Upload Key Reset Guide

## Current Status
- App package: `com.groceryapp.mobile`
- Current app version in project: `1.0.1` (`ApplicationVersion` = `2`)
- Existing uploaded AAB was rejected because it was signed with the wrong key.
- Old release keystore password is unavailable.

## Why Notifications Were Failing
Two separate issues were involved:
1. The app was calling old notification trigger endpoints that do not exist on the backend, causing Railway `404` logs.
2. Actual push delivery still depends on backend Firebase Admin configuration and a valid dealer FCM token.

### App-side notification fix already applied
The obsolete client-side notification trigger calls were removed. Order placement now relies on backend `CreateOrder` notification logic only.

### If push still does not arrive
Check Railway backend configuration for Firebase Admin credentials:
- preferred: `FIREBASE_SERVICE_ACCOUNT_JSON`
- optional file-path mode: `GOOGLE_APPLICATION_CREDENTIALS`
- optional local fallback: configured `Firebase/firebase-adminsdk.json`

For Railway, the recommended setup is to paste the full service account JSON into the `FIREBASE_SERVICE_ACCOUNT_JSON` variable and redeploy the backend.

## Recommended Play Store Path
Because the old keystore password is lost, use **Upload Key Reset**.

### Play Console navigation
- Open **Google Play Console**
- Select your app
- Go to **Setup**
- Go to **App integrity**
- Under **Upload key**, use **Request upload key reset**

## Create a New Upload Keystore
Run this command locally and choose a new password you will keep in a password manager.

```cmd
keytool -genkeypair -v -keystore "E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\sastidukan-new.keystore" -alias grocery_release_new -keyalg RSA -keysize 2048 -validity 10000
```

## Export Upload Certificate
```cmd
keytool -export -rfc -keystore "E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\sastidukan-new.keystore" -alias grocery_release_new -file "E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\upload_certificate.pem"
```

Upload `upload_certificate.pem` when Play Console asks for the new upload certificate.

## Do Not Store Passwords in This Repo
Do **not** save keystore passwords in markdown or source-controlled files.
Store these securely in:
- password manager
- secure company vault
- private offline note

Recommended secure record to keep outside git:
- keystore path
- alias
- store password
- key password
- certificate SHA1

## Build Command After Google Approves Upload Key Reset
After Play Console approves the upload key reset, build the AAB with the new keystore.

```cmd
dotnet publish "E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\GroceryApp.csproj" -f net9.0-android -c Release /p:AndroidPackageFormat=aab /p:AndroidKeyStore=true /p:AndroidSigningKeyStore="E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\sastidukan-new.keystore" /p:AndroidSigningKeyAlias="grocery_release_new" /p:AndroidSigningStorePass="<STORE_PASSWORD>" /p:AndroidSigningKeyPass="<KEY_PASSWORD>"
```

## Expected Upload Artifact
After publish, use the signed AAB from a path like:

- `E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP\bin\Release\net9.0-android\publish\com.groceryapp.mobile-Signed.aab`

## Play Store Upload Steps After Approval
1. Open Play Console
2. Go to your app
3. Open **Production** or **Internal testing**
4. Create new release
5. Upload the new signed `.aab`
6. Add release notes
7. Review and roll out

## Notes
- A new keystore alone is **not enough** until Google approves the upload key reset.
- If Play App Signing is enabled, upload key reset is the correct recovery path.
- Keep the new keystore password safely stored outside the repo to avoid the same problem later.
