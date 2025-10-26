using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using fiskaltrust.Middleware.Interface.Client.Http;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

#if ANDROID
using Android.Content;
using Android.Widget;
using Platform = Microsoft.Maui.ApplicationModel.Platform;
#endif

namespace fiskaltrust.Middleware.Demo;

public partial class MainPage : ContentPage
{
    private const string QUEUE_URL_GRPC = "grpc://localhost:1400";
    private const string QUEUE_URL_REST = "http://localhost:1500/queue";
    private const string CASHBOX_ID = "57dd5e04-49b3-4d81-862f-e5ac054117a8";
    private const string ACCESS_TOKEN = "BEkCPEpqvzzSyvu1dUCyGXkDRg+fLkVZhJ+aHaocr0VZ+aylUkjg2NVjIzqtzy1891yUOHK8SiYw/Ap/p38Yyx0=";
    private const bool SANDBOX = true;

#if ANDROID
    private POSSystemAPIIntentService? _fiskaltrusClient;
#endif

    public MainPage()
    {
        InitializeComponent();
#if ANDROID
        _fiskaltrusClient = new POSSystemAPIIntentService(Guid.Parse(CASHBOX_ID), ACCESS_TOKEN);
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateProtocolDisplay();
        UpdateUIForProtocol();
    }

    private void UpdateProtocolDisplay()
    {
        var protocol = SettingsPage.GetSelectedProtocol();
        lblCurrentProtocol.Text = protocol.ToUpper();
    }

    private void UpdateUIForProtocol()
    {
        var isIntentMode = IsIntentModeSelected();

        // Hide start/stop service buttons for Intent mode
        btnStartService.IsVisible = !isIntentMode;
        btnStopService.IsVisible = !isIntentMode;
        txtServiceStatus.IsVisible = !isIntentMode;

        // Restart config button is available for all protocols
        btnRestartConfig.IsVisible = true;
    }

    private bool IsIntentModeSelected()
    {
        return SettingsPage.GetSelectedProtocol().ToLower() == "intent";
    }

    private bool IsGrpcSelected()
    {
        return SettingsPage.GetSelectedProtocol().ToLower() == "grpc";
    }

    private void OnStartServiceClicked(object? sender, EventArgs e)
    {
#if ANDROID
        SetButtonsEnabled(false);

        var componentName = IsGrpcSelected()
            ? new ComponentName("eu.fiskaltrust.androidlauncher.grpc", "eu.fiskaltrust.androidlauncher.grpc.Start")
            : new ComponentName("eu.fiskaltrust.androidlauncher.http", "eu.fiskaltrust.androidlauncher.http.Start");

        var intent = new Intent(Intent.ActionSend);
        intent.SetComponent(componentName);
        intent.PutExtra("cashboxid", CASHBOX_ID);
        intent.PutExtra("accesstoken", ACCESS_TOKEN);
        intent.PutExtra("sandbox", SANDBOX);

        Platform.CurrentActivity?.SendBroadcast(intent);

        SetButtonsEnabled(true);
#endif
    }

    private void OnStopServiceClicked(object? sender, EventArgs e)
    {
#if ANDROID
        SetButtonsEnabled(false);

        var intent = new Intent(Intent.ActionSend);
        var componentName = IsGrpcSelected()
            ? new ComponentName("eu.fiskaltrust.androidlauncher.grpc", "eu.fiskaltrust.androidlauncher.grpc.Stop")
            : new ComponentName("eu.fiskaltrust.androidlauncher.http", "eu.fiskaltrust.androidlauncher.http.Stop");
        intent.SetComponent(componentName);
        Platform.CurrentActivity?.SendBroadcast(intent);

        SetButtonsEnabled(true);
#endif
    }

    private async void OnSendEchoRequestClicked(object? sender, EventArgs e)
    {
        await SendEchoRequestAsync($"Hello Android, it's {DateTime.Now:t}!");
    }

    private async void OnRestartConfigClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Restart & Pull Config",
            "This will restart the launcher and pull the latest configuration.\n\nContinue?",
            "Yes",
            "Cancel"
        );

        if (!confirmed)
            return;

        await SendEchoRequestAsync(null);
    }

    private async Task SendEchoRequestAsync(string? message)
    {
        SetButtonsEnabled(false);
        btnRestartConfig.IsEnabled = false;

        try
        {
#if ANDROID
            if (IsIntentModeSelected())
            {
                // For Intent mode: if message is null, send null; otherwise use the provided message
                var data = await _fiskaltrusClient!.SendEchoRequest(Platform.CurrentActivity!, new EchoRequest
                {
                    Message = message
                });
                txtResult.Text = JsonConvert.SerializeObject(data, Formatting.Indented);

                if (message == null)
                {
                    txtResult.Text = "✅ Configuration refresh initiated (Intent)\n\n" + txtResult.Text;
                }
            }
            else
#endif
            {
                var pos = await GetPOSAsync();
                var response = await pos.EchoAsync(new EchoRequest { Message = message });

                if (message == null)
                {
                    txtResult.Text = "✅ Configuration refresh initiated\n\nLauncher has been restarted and will pull the latest configuration.";
                }
                else
                {
                    txtResult.Text = response.Message;
                }
            }
        }
        catch (Exception ex)
        {
            var operation = message == null ? "Restart & Pull Config" : "Echo Request";
            txtResult.Text = FormatErrorForDisplay(operation, ex);
            await ShowErrorAsync($"{operation} Failed", ex);
        }

        SetButtonsEnabled(true);
        btnRestartConfig.IsEnabled = true;
    }

    private async void OnSendSignRequestClicked(object? sender, EventArgs e)
    {
        SetButtonsEnabled(false);

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = CASHBOX_ID,
            ftReceiptCase = 0x4445_0001_0000_0000,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>()
        };

        try
        {
#if ANDROID
            if (IsIntentModeSelected())
            {
                var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, receiptRequest);
                txtSignResult.Text = JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            else
#endif
            {
                var pos = await GetPOSAsync();
                var response = await pos.SignAsync(receiptRequest);
                txtSignResult.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
            }
        }
        catch (Exception ex)
        {
            txtSignResult.Text = FormatErrorForDisplay("Sign Request", ex);
            await ShowErrorAsync("Sign Request Failed", ex);
        }

        SetButtonsEnabled(true);
    }

    private async void OnSendStartReceiptClicked(object? sender, EventArgs e)
    {
        SetButtonsEnabled(false);

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = CASHBOX_ID,
            ftPosSystemId = "d4a62055-ca6c-4372-ae4d-f835a88e4a5d",
            cbTerminalID = "T1",
            cbReceiptReference = "2020020120152812",
            cbReceiptMoment = DateTime.UtcNow,
            ftReceiptCaseData = "",
            cbUser = "Receptionist",
            cbArea = "System",
            cbSettlement = "",
            ftReceiptCase = 0x4445_0001_0000_0003,
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>()
        };

        try
        {
#if ANDROID
            if (IsIntentModeSelected())
            {
                await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, receiptRequest);
            }
            else
#endif
            {
                var pos = await GetPOSAsync();
                var response = await pos.SignAsync(receiptRequest);
                txtSpecialReceiptResult.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
            }
        }
        catch (Exception ex)
        {
            txtSpecialReceiptResult.Text = FormatErrorForDisplay("Start Receipt", ex);
            await ShowErrorAsync("Start Receipt Failed", ex);
        }

        SetButtonsEnabled(true);
    }

    private async void OnSendZeroReceiptClicked(object? sender, EventArgs e)
    {
        SetButtonsEnabled(false);

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = CASHBOX_ID,
            ftPosSystemId = "d4a62055-ca6c-4372-ae4d-f835a88e4a5d",
            cbTerminalID = "T1",
            cbReceiptReference = "2020020120152812",
            cbReceiptMoment = DateTime.UtcNow,
            ftReceiptCaseData = "",
            cbUser = "Receptionist",
            cbArea = "System",
            cbSettlement = "",
            ftReceiptCase = 0x4445_0001_0000_0002,
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>()
        };

        try
        {
#if ANDROID
            if (IsIntentModeSelected())
            {
                var response = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, receiptRequest);
                txtSpecialReceiptResult.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
            }
            else
#endif
            {
                var pos = await GetPOSAsync();
                var response = await pos.SignAsync(receiptRequest);
                txtSpecialReceiptResult.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
            }
        }
        catch (Exception ex)
        {
            txtSpecialReceiptResult.Text = FormatErrorForDisplay("Zero Receipt", ex);
            await ShowErrorAsync("Zero Receipt Failed", ex);
        }

        SetButtonsEnabled(true);
    }


    private async Task<IPOS> GetPOSAsync()
    {
        if (IsGrpcSelected())
        {
            return await GrpcPosFactory.CreatePosAsync(new GrpcClientOptions
            {
                Url = new Uri(QUEUE_URL_GRPC)
            });
        }
        else
        {
            return await HttpPosFactory.CreatePosAsync(new HttpPosClientOptions
            {
                Url = new Uri(QUEUE_URL_REST)
            });
        }
    }

    private void SetButtonsEnabled(bool state)
    {
        btnSendEchoRequest.IsEnabled = state;
        btnRestartConfig.IsEnabled = state;
        btnSendSignRequest.IsEnabled = state;
        btnSendStartReceipt.IsEnabled = state;
        btnSendZeroReceipt.IsEnabled = state;
    }

    private async Task ShowErrorAsync(string title, Exception ex)
    {
        var errorMessage = ex.Message;
        var errorType = ex.GetType().Name;

        // Extract inner exception if available
        if (ex.InnerException != null)
        {
            errorMessage = ex.InnerException.Message;
        }

        await DisplayAlertAsync(
            $"❌ {title}",
            $"{errorMessage}\n\n📋 Error Type: {errorType}",
            "OK"
        );
    }

    private string FormatErrorForDisplay(string operation, Exception ex)
    {
        var errorMessage = ex.Message;

        // Extract inner exception if available for more detail
        if (ex.InnerException != null)
        {
            errorMessage = ex.InnerException.Message;
        }

        // Format the error with emoji and structure
        return $"❌ Error: {operation}\n\n{errorMessage}\n\n({ex.GetType().Name})";
    }
}
