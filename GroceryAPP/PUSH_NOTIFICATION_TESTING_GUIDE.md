# End-to-End Testing Guide for Push Notifications

## Testing Checklist

Follow these steps in order to verify the complete notification flow works correctly.

---

## Prerequisites

Before testing:
- ✅ Firebase project created and configured
- ✅ `google-services.json` added to mobile app
- ✅ Firebase NuGet packages installed in mobile app
- ✅ `firebase-adminsdk.json` added to backend API
- ✅ FirebaseAdmin NuGet package installed in backend
- ✅ Database migration applied (`fcm_token` column exists)
- ✅ Backend deployed or running locally
- ✅ Mobile app APK installed on Android device

---

## Test 1: Verify Firebase Setup

### Mobile App
1. Open mobile app on Android device
2. Check logcat/console output (if in debug mode)
3. Look for Firebase initialization messages

### Backend API
1. Start backend API
2. Check logs for: "Firebase Admin SDK initialized successfully"
3. If you see warnings, check:
   - `appsettings.json` has correct `Firebase:ServiceAccountPath`
   - `firebase-adminsdk.json` exists in the specified path

✅ **Pass Criteria**: Both mobile and backend initialize Firebase without errors

---

## Test 2: FCM Token Registration

### Steps
1. Open mobile app
2. Tap "Shopkeeper (दुकानदार)" button
3. Login or register as a shopkeeper
   - Use mobile number: `9876543210`
   - Password: any secure password
4. After successful login, check:

### Verification
**Mobile App Logs** (if debug mode):
```
FCM token: xxxxxxxxxxxxxx
Token saved to preferences
```

**Backend API Logs**:
```
FCM token registered for user 123
```

**Database Check**:
```sql
SELECT id, full_name, mobile_number, fcm_token 
FROM users 
WHERE mobile_number = '9876543210';
```

Expected: `fcm_token` column should have a long string (150-200 characters)

✅ **Pass Criteria**: FCM token is stored in database for the shopkeeper

---

## Test 3: Place Order and Receive Notification

### Steps
1. **On Mobile App (Shopkeeper)**
   - Login as shopkeeper
   - Note the shop name
   - Keep app in background or close it

2. **On Another Device/Browser (Customer)**
   - Open app/website as customer
   - Browse products from that shopkeeper's shop
   - Add items to cart
   - Go to checkout
   - Enter:
	 - **Customer Name**: "Test Customer"
	 - **Mobile Number**: "8888888888"
	 - **Delivery Address**: "123 Test Street"
   - Click "Place Order"

3. **Expected Result**
   - Shopkeeper device should receive a notification:
	 ```
	 Title: "New Order Received!"
	 Body: "Order #123 from Test Customer - ₹500"
	 ```

### Verification Points

**Customer App**:
- Order placed successfully message appears
- Redirected to order history page

**Backend API Logs**:
```
CreateOrder request accepted. Mobile=8888888888, ItemCount=2, UserId=1
CreateOrder succeeded. OrderId=123, Mobile=8888888888, Total=500
FCM notification sent successfully. Response: projects/...
```

**Shopkeeper Device**:
- Push notification appears in notification tray
- Tapping notification opens the app

**Database Check**:
```sql
SELECT o.id, o.customer_name, o.customer_mobile_number, o.total_amount,
	   dn.message, dn.is_read
FROM orders o
LEFT JOIN dealer_notifications dn ON dn.order_id = o.id
WHERE o.id = 123;
```

✅ **Pass Criteria**: Notification received within 5 seconds of order placement

---

## Test 4: Multiple Orders

### Steps
1. Place 3-4 more orders for the same shopkeeper
2. Each order should trigger a notification

### Verification
- Each notification shows correct order number
- Each notification shows correct customer name
- Each notification shows correct total amount
- All notifications appear in device notification tray

✅ **Pass Criteria**: All notifications received, no duplicates or missing notifications

---

## Test 5: Multiple Shopkeepers

### Steps
1. Login as **Shopkeeper A** on Device 1
2. Login as **Shopkeeper B** on Device 2
3. Place order with products from **Shopkeeper A's shop**
4. Place order with products from **Shopkeeper B's shop**

### Verification
- Device 1 receives notification only for Shopkeeper A's order
- Device 2 receives notification only for Shopkeeper B's order
- No cross-notifications (A doesn't get B's orders)

✅ **Pass Criteria**: Each shopkeeper receives only their own order notifications

---

## Test 6: Notification Permissions

### Steps (Android 13+)
1. Fresh install of the app
2. Login as shopkeeper
3. System should prompt: "Allow Online Sasti Dukan to send you notifications?"
4. Tap "Allow"

### Verification
- If "Don't allow" is tapped:
  - No notifications will be received
  - Can be enabled later in device Settings → Apps → Online Sasti Dukan → Notifications

✅ **Pass Criteria**: Permission is requested and granted

---

## Test 7: Offline/Background Scenarios

### Test 7a: App in Background
1. Login as shopkeeper
2. Press Home button (app goes to background)
3. Place an order for that shopkeeper
4. **Expected**: Notification appears in notification tray

### Test 7b: App Killed/Closed
1. Login as shopkeeper
2. Swipe app away from recent apps (fully close)
3. Place an order for that shopkeeper
4. **Expected**: Notification still appears (Firebase handles this)

### Test 7c: Device Offline then Online
1. Turn off device WiFi/data
2. Place an order
3. Turn on WiFi/data
4. **Expected**: Notification appears once device is online (FCM queues it)

✅ **Pass Criteria**: Notifications work in all scenarios

---

## Test 8: Firebase Console Test Message

### Steps
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Select your project
3. Navigate to **Cloud Messaging** (left sidebar, under Engage)
4. Click **Send your first message** or **New campaign**
5. Enter:
   - **Notification title**: "Test from Firebase"
   - **Notification text**: "This is a test message"
6. Click **Send test message**
7. Copy a shopkeeper's FCM token from database:
   ```sql
   SELECT fcm_token FROM users WHERE role_id = 3 LIMIT 1;
   ```
8. Paste token and click **Test**

### Verification
- Notification appears on shopkeeper device
- Notification shows the test title and message

✅ **Pass Criteria**: Test message received successfully

---

## Test 9: Invalid/Expired Token Handling

### Steps
1. Manually corrupt a shopkeeper's FCM token in database:
   ```sql
   UPDATE users SET fcm_token = 'invalid_token_12345' WHERE id = 123;
   ```
2. Place an order for that shopkeeper

### Verification
**Backend Logs**:
```
Firebase messaging error. Error code: Unregistered
FCM token is invalid or unregistered: invalid_token_12345
```

**Expected Behavior**:
- Order still created successfully (notification failure doesn't block order)
- Error logged but no exception thrown
- Shopkeeper doesn't receive notification (expected)

✅ **Pass Criteria**: System handles invalid tokens gracefully without crashing

---

## Test 10: Notification Data Payload

### Steps
1. Place an order
2. When notification is received, tap it
3. App should open (basic behavior)

### Future Enhancement
- Can implement deep linking to open specific order details
- Notification data payload includes:
  - `type`: "new_order"
  - `orderId`: "123"
  - `totalAmount`: "500.00"

✅ **Pass Criteria**: Tapping notification opens the app

---

## Common Issues & Solutions

### Issue: "Firebase not initialized" in backend logs
**Solution**:
- Check `appsettings.json` has correct path
- Verify `firebase-adminsdk.json` exists
- Check file Build Action is "Copy if newer"

### Issue: No notifications received
**Possible Causes**:
1. FCM token not registered (check database)
2. Notification permissions not granted (check device settings)
3. Google Play Services not installed/updated
4. Device firewall blocking FCM (rare)

**Debug Steps**:
- Test with Firebase Console test message first
- Check backend logs for FCM send success/failure
- Verify FCM token exists in database
- Try on different device

### Issue: "FCM token is invalid"
**Solution**:
- Token may have expired
- Ask shopkeeper to logout and login again
- New token will be registered

### Issue: Notifications received but delayed
**Possible Causes**:
- Device battery saver/optimization enabled
- Network latency
- FCM server load (rare)

**Solution**:
- Disable battery optimization for the app
- Check network connectivity
- FCM has ~5 second typical delivery time

---

## Performance Testing

### Load Test
1. Place 50 orders in quick succession
2. Monitor:
   - Notification delivery time (should be < 10 seconds)
   - Backend CPU/memory usage
   - No notification drops

✅ **Pass Criteria**: All 50 notifications delivered successfully

---

## Security Testing

### Test: Unauthorized token registration
1. Try to register FCM token without JWT
   ```http
   POST /api/users/fcm-token
   Content-Type: application/json

   {
	 "fcmToken": "some_token"
   }
   ```
2. **Expected**: 401 Unauthorized

✅ **Pass Criteria**: Endpoint protected by authentication

---

## Regression Testing

After any code changes, verify:
- ✅ Orders still create successfully
- ✅ Notifications still send
- ✅ Database still stores tokens
- ✅ Mobile app doesn't crash
- ✅ Backend doesn't throw exceptions

---

## Sign-Off Checklist

Before going to production:
- [ ] All 10 tests passed
- [ ] Tested on at least 2 different Android devices
- [ ] Tested with Android 10, 11, 12, 13 if possible
- [ ] Firebase quota limits reviewed (unlimited for FCM)
- [ ] Backend logs reviewed for errors
- [ ] Database performance acceptable
- [ ] Customer experience not impacted by notification failures
- [ ] Privacy policy updated (if needed) to mention notifications

---

## Monitoring in Production

After deployment, monitor:
- **Firebase Console**: Messages sent/delivered counts
- **Backend Logs**: FCM send success/failure rates
- **Database**: Growth of FCM tokens
- **User Feedback**: Shopkeepers reporting missing notifications

**Metrics to Track**:
- Notification delivery rate (should be > 95%)
- Average delivery time (should be < 5 seconds)
- Invalid token rate (should be < 5%)

---

**Testing Complete!** 🎉

Once all tests pass, your push notification system is production-ready!
