using System;

#if ANDROID
using Android.Content;
using Platform = Microsoft.Maui.ApplicationModel.Platform;
#endif

namespace fiskaltrust.Middleware.Demo;

public partial class SettingsPage : ContentPage
{
    private const string PROTOCOL_PREFERENCE_KEY = "selected_protocol";
    private const string COUNTRY_PREFERENCE_KEY = "selected_country";
    private const string CASHBOX_ID = "e4de7978-23b8-4e13-ae7e-c3620f30d861";
    private const string ACCESS_TOKEN = "BKjaxDryAtwxN1AeDh/fAVgTZQ6Md3C6aQmXiMhq+q3NmvJdU9LOZZDlzbZQbfKAr5mzvGMyyjwWn9uPG3FxE6w=";

    public SettingsPage()
    {
        InitializeComponent();
        LoadSavedProtocol();
        LoadSavedCountry();
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

    private void LoadSavedCountry()
    {
        var savedCountry = Preferences.Get(COUNTRY_PREFERENCE_KEY, "DE");

        switch (savedCountry.ToUpper())
        {
            case "DE":
                radioDE.IsChecked = true;
                break;
            case "IT":
                radioIT.IsChecked = true;
                break;
            default:
                radioDE.IsChecked = true;
                break;
        }
    }

    private void OnCountryChanged(object? sender, CheckedChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[Danielllllllllllllllll DEBUG] OnCountryChanged called. Checked: {e.Value}");
        
        if (e.Value)
        {
            var radioButton = sender as RadioButton;
            System.Diagnostics.Debug.WriteLine($"[Danielllllllllllllllll DEBUG] RadioButton: {radioButton?.GetHashCode()}");
            System.Diagnostics.Debug.WriteLine($"[Danielllllllllllllllll DEBUG] radioDE HashCode: {radioDE?.GetHashCode()}");
            System.Diagnostics.Debug.WriteLine($"[Danielllllllllllllllll DEBUG] radioIT HashCode: {radioIT?.GetHashCode()}");
            
            if (radioButton == radioDE)
            {
                System.Diagnostics.Debug.WriteLine("[Danielllllllllllllllll DEBUG] Setting country to DE");
                Preferences.Set(COUNTRY_PREFERENCE_KEY, "DE");
            }
            else if (radioButton == radioIT)
            {
                System.Diagnostics.Debug.WriteLine("[Danielllllllllllllllll DEBUG] Setting country to IT");
                Preferences.Set(COUNTRY_PREFERENCE_KEY, "IT");
            }
            
            var saved = Preferences.Get(COUNTRY_PREFERENCE_KEY, "DE");
            System.Diagnostics.Debug.WriteLine($"[Danielllllllllllllllll DEBUG] Saved country: {saved}");
        }
    }

    public static string GetSelectedCountry()
    {
        var country = Preferences.Get(COUNTRY_PREFERENCE_KEY, "DE");
        System.Diagnostics.Debug.WriteLine($"[COUNTRY DEBUG] GetSelectedCountry returning: {country}");
        return country;
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
        req.PutExtra("cashboxid", CASHBOX_ID);
        req.PutExtra("accesstoken", ACCESS_TOKEN);
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
