using System.Threading.Tasks;

namespace GroceryApp.Services
{
    public interface IFirebaseService
    {
        Task<string> GetTokenAsync();
        Task SubscribeToTopicAsync(string topic);
        Task UnsubscribeFromTopicAsync(string topic);
        Task<bool> IsNotificationPermissionGrantedAsync();
    }

    public class FirebaseService : IFirebaseService
    {
        public async Task<string> GetTokenAsync()
        {
#if ANDROID
            return await GetAndroidTokenAsync();
#else
            return await Task.FromResult(string.Empty);
#endif
        }

        public async Task SubscribeToTopicAsync(string topic)
        {
#if ANDROID
            await SubscribeToAndroidTopicAsync(topic);
#else
            await Task.CompletedTask;
#endif
        }

        public async Task UnsubscribeFromTopicAsync(string topic)
        {
#if ANDROID
            await UnsubscribeFromAndroidTopicAsync(topic);
#else
            await Task.CompletedTask;
#endif
        }

        public async Task<bool> IsNotificationPermissionGrantedAsync()
        {
#if ANDROID
            return await CheckAndroidNotificationPermissionAsync();
#else
            return await Task.FromResult(true); // iOS handles permissions differently
#endif
        }

#if ANDROID
        private async Task<bool> CheckAndroidNotificationPermissionAsync()
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                var context = Android.App.Application.Context;
                var permission = AndroidX.Core.Content.ContextCompat.CheckSelfPermission(
                    context, 
                    Android.Manifest.Permission.PostNotifications);

                return await Task.FromResult(permission == Android.Content.PM.Permission.Granted);
            }

            // For Android 12 and below, notification permission is granted by default
            return await Task.FromResult(true);
        }

        private async Task<string> GetAndroidTokenAsync()
        {
            var tcs = new TaskCompletionSource<string>();

            Firebase.Messaging.FirebaseMessaging.Instance.GetToken().AddOnCompleteListener(
                new OnCompleteListener((task) =>
                {
                    if (task.IsSuccessful)
                    {
                        var token = task.Result?.ToString();
                        if (!string.IsNullOrEmpty(token))
                        {
                            Microsoft.Maui.Storage.Preferences.Set("FCM_TOKEN", token);
                            tcs.SetResult(token);
                        }
                        else
                        {
                            tcs.SetResult(string.Empty);
                        }
                    }
                    else
                    {
                        tcs.SetResult(string.Empty);
                    }
                })
            );

            return await tcs.Task;
        }

        private async Task SubscribeToAndroidTopicAsync(string topic)
        {
            var tcs = new TaskCompletionSource<bool>();

            Firebase.Messaging.FirebaseMessaging.Instance.SubscribeToTopic(topic).AddOnCompleteListener(
                new OnCompleteListener((task) =>
                {
                    tcs.SetResult(task.IsSuccessful);
                })
            );

            await tcs.Task;
        }

        private async Task UnsubscribeFromAndroidTopicAsync(string topic)
        {
            var tcs = new TaskCompletionSource<bool>();

            Firebase.Messaging.FirebaseMessaging.Instance.UnsubscribeFromTopic(topic).AddOnCompleteListener(
                new OnCompleteListener((task) =>
                {
                    tcs.SetResult(task.IsSuccessful);
                })
            );

            await tcs.Task;
        }

        private class OnCompleteListener : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
        {
            private readonly Action<Android.Gms.Tasks.Task> _onComplete;

            public OnCompleteListener(Action<Android.Gms.Tasks.Task> onComplete)
            {
                _onComplete = onComplete;
            }

            public void OnComplete(Android.Gms.Tasks.Task task)
            {
                _onComplete?.Invoke(task);
            }
        }
#endif
    }
}
