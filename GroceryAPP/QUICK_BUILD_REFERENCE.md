# 🔑 Quick Reference - Build & Deploy Commands

## **Values to Use (from SECRETS.md)**

```
Keystore Path: release.keystore
Keystore Alias: groceryapp
Keystore Password: Rohit@123
Key Password: Rohit@123
Package Name: com.groceryapp.sastidukan
```

---

## **Step 1: Create Keystore (Run ONCE)**

```powershell
cd E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP

$keystorePath = "release.keystore"
$keystoreAlias = "groceryapp"
$keystorePassword = "Rohit@123"
$keyPassword = "Rohit@123"

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

---

## **Step 2: Build Release AAB (Run for each release)**

```powershell
cd E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppMobileApp\GroceryAPP

dotnet publish -f net9.0-android `
  -c Release `
  -p:AndroidPackageFormat=aab `
  -p:AndroidKeyStore=true `
  -p:AndroidSigningKeyStore=release.keystore `
  -p:AndroidSigningKeyAlias=groceryapp `
  -p:AndroidSigningKeyPass=Rohit@123 `
  -p:AndroidSigningStorePass=Rohit@123
```

**⏱️ Wait 5-10 minutes for build to complete**

---

## **Step 3: Verify AAB File**

```powershell
ls bin\Release\net9.0-android\publish\*.aab
```

Should output:
```
-a---  [DATE] [TIME]     45000000 com.groceryapp.sastidukan-signed.aab
```

---

## **Step 4: Upload to Play Console**

1. Go to **[Google Play Console](https://play.google.com/console)**
2. Select your app: **Sasti Dukan**
3. Click **Release** → **Production** (or **Testing**)
4. Click **Create new release**
5. Upload: `bin\Release\net9.0-android\publish\com.groceryapp.sastidukan-signed.aab`
6. Enter release notes (version 1.0.1)
7. Click **Review** → **Start rollout**

---

## **Security Reminders**

✅ **DO:**
- Use passwords from `SECRETS.md` only
- Keep `release.keystore` locally
- Keep `SECRETS.md` locally (not in Git)
- Verify `.gitignore` prevents commits

❌ **DON'T:**
- Commit `release.keystore` to GitHub
- Commit `SECRETS.md` to GitHub
- Share passwords in Slack/Teams/Chat
- Paste service account JSON in chat

---

## **Environment Check**

Before building, verify:

```powershell
# Check keystore exists
Test-Path release.keystore

# Check .gitignore blocks secrets
Select-String "release.keystore" .gitignore
Select-String "SECRETS.md" .gitignore

# Check Android SDK
$env:ANDROID_HOME  # Should output SDK path

# Check .NET CLI
dotnet --version   # Should be 9.0+
```

---

## **If Build Fails**

### **Error: Keystore not found**
```powershell
# Verify keystore location
Get-ChildItem release.keystore -Force
```

### **Error: Invalid password**
```powershell
# Verify password from SECRETS.md
keytool -list -v -keystore release.keystore -storepass Rohit@123
```

### **Error: google-services.json not found**
```powershell
# Check Firebase config
Get-ChildItem Platforms\Android\google-services.json
```

---

## **Deployment Checklist**

- [ ] Keystore created with correct password
- [ ] `ApplicationId` = `com.groceryapp.sastidukan` in GroceryApp.csproj
- [ ] `google-services.json` updated with new Firebase config
- [ ] Firebase Android app registered in Firebase Console
- [ ] `FIREBASE_SERVICE_ACCOUNT_JSON` deployed on Railway
- [ ] `.gitignore` blocks keystore and SECRETS.md
- [ ] Release AAB built successfully
- [ ] Play Store listing created
- [ ] AAB uploaded to Play Console
- [ ] App in review/live

---

**Next Build:** Just run Step 2 (Build Release AAB)

