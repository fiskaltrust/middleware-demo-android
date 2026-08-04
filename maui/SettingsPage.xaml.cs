using System;
using System.Text.Json;

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
    private void OnPairPinChanged(object? sender, TextChangedEventArgs e)
    {
        btnPair.IsEnabled = !string.IsNullOrWhiteSpace(e.NewTextValue);
    }

    private async void OnPairClickedAsync(object? sender, EventArgs e)
    {
        var pin = entryPairPin.Text?.Trim();
        if (string.IsNullOrEmpty(pin))
        {
            return;
        }

        SetPairingBusy(true);

        Dictionary<string, string> response;
        try
        {
            var responseString = await new POSSystemAPIService().PerformPOSSystemAPIRequest(Platform.CurrentActivity!, new POSSystemAPIRequest
            {
                Method = "POST",
                Path = "/v2/pair",
                Body = JsonSerializer.Serialize(new { Pin = pin })
            });

            response = JsonSerializer.Deserialize<Dictionary<string, string>>(responseString)!;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Pairing Failed", $"An error occurred while trying to pair with the POS system: {ex.Message}", "OK");
            return;
        }
        finally
        {
            SetPairingBusy(false);
        }

        response.TryGetValue("CashBoxId", out var cashboxId);
        response.TryGetValue("AccessToken", out var accessToken);


        if (cashboxId != null && accessToken != null)
        {
            entryCashboxId.Text = cashboxId;
            entryAccessToken.Text = accessToken;

            Preferences.Set(CASHBOX_ID_PREFERENCE_KEY, cashboxId);
            Preferences.Set(ACCESS_TOKEN_PREFERENCE_KEY, accessToken);

            entryPairPin.Text = string.Empty;
            await DisplayAlertAsync("Pairing Successful", "Cashbox ID and Access Token have been retrieved and saved.", "OK");
        }
        else
        {
            await DisplayAlertAsync("Pairing Failed", "The POS system response did not contain valid credentials. Please check the PIN and try again. ", "OK");
        }
    }

    private void SetPairingBusy(bool isBusy)
    {
        btnPair.IsEnabled = !isBusy && !string.IsNullOrWhiteSpace(entryPairPin.Text);
        entryPairPin.IsEnabled = !isBusy;
        pairingStatus.IsVisible = isBusy;
        pairingIndicator.IsRunning = isBusy;
    }

    private void LoadSavedProtocol()
    {
        var savedProtocol = Preferences.Get(PROTOCOL_PREFERENCE_KEY, "intent-activity");

        switch (savedProtocol.ToLower())
        {
            case "intent":
            case "intent-activity":
                radioIntentActivity.IsChecked = true;
                break;
            case "service-ipc":
                radioIntentService.IsChecked = true;
                break;
            default:
                radioIntentActivity.IsChecked = true;
                break;
        }
    }

    private void OnProtocolChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            var radioButton = sender as RadioButton;
            if (radioButton == radioIntentActivity)
            {
                Preferences.Set(PROTOCOL_PREFERENCE_KEY, "intent-activity");
            }
            else if (radioButton == radioIntentService)
            {
                Preferences.Set(PROTOCOL_PREFERENCE_KEY, "service-ipc");
            }
        }
    }

    public static string GetSelectedProtocol()
    {
        return Preferences.Get(PROTOCOL_PREFERENCE_KEY, "intent-activity");
    }
}
