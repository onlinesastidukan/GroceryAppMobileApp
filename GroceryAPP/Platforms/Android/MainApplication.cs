using Android.App;
using Android.Runtime;

namespace GroceryApp;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
		// Catch any unhandled .NET exception before Android kills the process silently
		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			var ex = args.ExceptionObject as Exception;
			System.Diagnostics.Debug.WriteLine($"[FATAL] UnhandledException: {ex?.Message}\n{ex?.StackTrace}");
			Android.Util.Log.Error("GroceryApp", $"FATAL: {ex?.Message}\n{ex?.StackTrace}");
		};

		TaskScheduler.UnobservedTaskException += (sender, args) =>
		{
			System.Diagnostics.Debug.WriteLine($"[FATAL] UnobservedTaskException: {args.Exception?.Message}");
			Android.Util.Log.Error("GroceryApp", $"UnobservedTask: {args.Exception?.Message}");
			args.SetObserved();
		};
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
