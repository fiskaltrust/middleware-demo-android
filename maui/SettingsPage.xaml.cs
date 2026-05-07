using System;

#if ANDROID
using Android.Content;
using Platform = Microsoft.Maui.ApplicationModel.Platform;
#endif

namespace fiskaltrust.Middleware.Demo;

public partial class SettingsPage : ContentPage
{
    private const string PROTOCOL_PREFERENCE_KEY = "selected_protocol";
    private const string CASHBOX_ID_PREFERENCE_KEY = "cashbox_id";
    private const string ACCESS_TOKEN_PREFERENCE_KEY = "access_token";

    private const string DEFAULT_CASHBOX_ID = "57dd5e04-49b3-4d81-862f-e5ac054117a8";
    private const string DEFAULT_ACCESS_TOKEN = "BEkCPEpqvzzSyvu1dUCyGXkDRg+fLkVZhJ+aHaocr0VZ+aylUkjg2NVjIzqtzy1891yUOHK8SiYw/Ap/p38Yyx0=";

    public SettingsPage()
    {
        InitializeComponent();
        LoadSavedProtocol();
        LoadSavedCredentials();
    }

    private void LoadSavedCredentials()
    {
        entryCashboxId.Text = Preferences.Get(CASHBOX_ID_PREFERENCE_KEY, DEFAULT_CASHBOX_ID);
        entryAccessToken.Text = Preferences.Get(ACCESS_TOKEN_PREFERENCE_KEY, DEFAULT_ACCESS_TOKEN);
    }

    private void OnCashboxIdChanged(object? sender, TextChangedEventArgs e)
    {
        Preferences.Set(CASHBOX_ID_PREFERENCE_KEY, e.NewTextValue ?? string.Empty);
    }

    private void OnAccessTokenChanged(object? sender, TextChangedEventArgs e)
    {
        Preferences.Set(ACCESS_TOKEN_PREFERENCE_KEY, e.NewTextValue ?? string.Empty);
    }

    public static string GetCashboxId()
    {
        return Preferences.Get(CASHBOX_ID_PREFERENCE_KEY, DEFAULT_CASHBOX_ID);
    }

    public static string GetAccessToken()
    {
        return Preferences.Get(ACCESS_TOKEN_PREFERENCE_KEY, DEFAULT_ACCESS_TOKEN);
    }

    private void LoadSavedProtocol()
    {
        var savedProtocol = Preferences.Get(PROTOCOL_PREFERENCE_KEY, "grpc");

        switch (savedProtocol.ToLower())
        {
            case "grpc":
                radioGrpc.IsChecked = true;
                break;
            case "http":
                radioHttp.IsChecked = true;
                break;
            case "intent":
                radioIntent.IsChecked = true;
                break;
            default:
                radioGrpc.IsChecked = true;
                break;
        }
    }

    private void OnProtocolChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            var radioButton = sender as RadioButton;
            if (radioButton == radioGrpc)
            {
                Preferences.Set(PROTOCOL_PREFERENCE_KEY, "grpc");
            }
            else if (radioButton == radioHttp)
            {
                Preferences.Set(PROTOCOL_PREFERENCE_KEY, "http");
            }
            else if (radioButton == radioIntent)
            {
                Preferences.Set(PROTOCOL_PREFERENCE_KEY, "intent");
            }
        }
    }

    public static string GetSelectedProtocol()
    {
        return Preferences.Get(PROTOCOL_PREFERENCE_KEY, "grpc");
    }

    private async void OnRequestLogsClicked(object? sender, EventArgs e)
    {
#if ANDROID
        var protocol = GetSelectedProtocol().ToLower();

        if (protocol == "intent")
        {
            await DisplayAlertAsync("Not Available", "Log retrieval is only available for gRPC and HTTP protocols.", "OK");
            return;
        }

        btnGetLogs.IsEnabled = false;
        txtLogs.Text = "Requesting logs...";

        var componentName = protocol == "grpc"
            ? new ComponentName("eu.fiskaltrust.androidlauncher.grpc", "eu.fiskaltrust.androidlauncher.grpc.LogContentLinkActivity")
            : new ComponentName("eu.fiskaltrust.androidlauncher.http", "eu.fiskaltrust.androidlauncher.http.LogContentLinkActivity");

        var req = new Intent();
        req.PutExtra("cashboxid", GetCashboxId());
        req.PutExtra("accesstoken", GetAccessToken());
        req.SetComponent(componentName);

        try
        {
            Platform.CurrentActivity?.StartActivity(req);
            txtLogs.Text = "Log request sent. Please check the launcher app for logs.";
        }
        catch (Exception ex)
        {
            txtLogs.Text = $"Error requesting logs: {ex.Message}";
        }

        btnGetLogs.IsEnabled = true;
#else
        await DisplayAlertAsync("Not Available", "This feature is only available on Android.", "OK");
#endif
    }
}
