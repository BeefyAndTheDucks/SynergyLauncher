using System.Runtime.InteropServices;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using Microsoft.Identity.Client;
using XboxAuthNet.Game.Authenticators;
using XboxAuthNet.Game.Msal;

namespace SynergyLauncherCore;

public static class SignInHandler
{
    public static async Task<MSession> SignIn()
    {
        JELoginHandler loginHandler = new JELoginHandlerBuilder()
            .Build();
        
        if (IsWindows())
            return await loginHandler.Authenticate();

        // MSAL
        //IPublicClientApplication app = await MsalClientHelper.BuildApplicationWithCache("f0c46b7c-fc78-463f-b32c-f2ca92994e71"); // Custom one
        IPublicClientApplication app = await MsalClientHelper.BuildApplicationWithCache("499c8d36-be2a-4231-9ebd-ef291b7bb64c");
        NestedAuthenticator authenticator = loginHandler.CreateAuthenticatorWithDefaultAccount();
        authenticator.AddMsalOAuth(app, msal => msal.InteractiveWithSingleAccount());
        authenticator.AddXboxAuthForJE(xbox => xbox.Full());
        authenticator.AddForceJEAuthenticator();
        return await authenticator.ExecuteForLauncherAsync();
    }
    
    private static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}