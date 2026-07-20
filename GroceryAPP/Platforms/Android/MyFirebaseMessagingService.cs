using Android.App;
using Android.Content;
using Android.Util;
using Firebase.Messaging;

namespace GroceryApp.Platforms.Android
{
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        const string TAG = "MyFirebaseMsgService";

        public override void OnMessageReceived(RemoteMessage message)
        {
            Log.Debug(TAG, "From: " + message.From);

            if (message.Data != null && message.Data.Count > 0)
            {
                Log.Debug(TAG, "Message data payload: " + message.Data);
            }

            if (message.GetNotification() != null)
            {
                Log.Debug(TAG, "Message Notification Body: " + message.GetNotification().Body);
                SendNotification(message.GetNotification().Title, message.GetNotification().Body);
            }
        }

        public override void OnNewToken(string token)
        {
            Log.Debug(TAG, "Refreshed token: " + token);
            SendRegistrationToServer(token);
        }

        void SendRegistrationToServer(string token)
        {
            Microsoft.Maui.Storage.Preferences.Set("FCM_TOKEN", token);
            Log.Debug(TAG, "Token saved to preferences");
        }

        void SendNotification(string title, string messageBody)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable);

            var channelId = "order_notifications";
            var notificationBuilder = new Notification.Builder(this, channelId)
                    .SetContentTitle(title)
                    .SetContentText(messageBody)
                    .SetSmallIcon(Resource.Mipmap.appicon)
                    .SetAutoCancel(true)
                    .SetContentIntent(pendingIntent);

            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                notificationBuilder.SetChannelId(channelId);
            }

            var notificationManager = NotificationManager.FromContext(this);

            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId,
                    "Order Notifications",
                    NotificationImportance.High)
                {
                    Description = "Notifications for new orders"
                };
                notificationManager.CreateNotificationChannel(channel);
            }

            notificationManager.Notify(0, notificationBuilder.Build());
        }
    }
}
