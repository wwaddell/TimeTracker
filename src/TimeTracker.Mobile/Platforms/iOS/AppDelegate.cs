using Foundation;
using Microsoft.Identity.Client;
using UIKit;

namespace TimeTracker.Mobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    // Hand the auth redirect back to MSAL so AcquireTokenInteractive completes on iOS.
    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(url);
        return true;
    }
}
