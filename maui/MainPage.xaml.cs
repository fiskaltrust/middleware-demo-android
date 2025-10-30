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
    private const string CASHBOX_ID = "e4de7978-23b8-4e13-ae7e-c3620f30d861";
    private const string ACCESS_TOKEN = "BKjaxDryAtwxN1AeDh/fAVgTZQ6Md3C6aQmXiMhq+q3NmvJdU9LOZZDlzbZQbfKAr5mzvGMyyjwWn9uPG3FxE6w=";
    private const bool SANDBOX = true;

#if ANDROID
    private POSSystemAPIIntentService? _fiskaltrusClient;
#endif

    // Last operation tracking
    private LastOperationInfo? _lastOperation;

    private enum OperationType
    {
        EchoRequest,
        RestartConfig,
        SignRequest,
        StartReceipt,
        ZeroReceipt
    }

    private class LastOperationInfo
    {
        public Guid OperationID { get; set; } 
        public OperationType Type { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

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

    private void SetLastOperation(Guid operationId, string body, OperationType type, string? message, string displayName)
    {
        _lastOperation = new LastOperationInfo
        {
            OperationID = operationId,
            Type = type,
            Message = message,
            Body = body,
            DisplayName = displayName
        };
        btnRetryLastOperation.IsVisible = true;
        btnRetryLastOperation.Text = $"🔄 Retry: {displayName}";
    }

    private async void OnRetryLastOperationClicked(object? sender, EventArgs e)
    {
        if (_lastOperation == null)
            return;

        string result;
        try
        {
            result = await ExecuteOperationAsync(_lastOperation.OperationID, _lastOperation.Type, _lastOperation.Message);
        }
        catch (Exception ex)
        {
            result = FormatErrorForDisplay($"Retry {_lastOperation.DisplayName}", ex);
        }

        // Show result in message box
        await DisplayAlert(
            $"Retry Result: {_lastOperation.DisplayName}",
            result,
            "OK"
        );
    }

    private async Task<string> ExecuteOperationAsync(Guid operationId, OperationType type, string? message)
    {
        return type switch
        {
            OperationType.EchoRequest => await ExecuteEchoRequestAsync(message!, operationId),
            OperationType.RestartConfig => await ExecuteRestartConfigAsync(operationId),
            OperationType.SignRequest => await ExecuteSignRequestAsync(operationId),
            OperationType.StartReceipt => await ExecuteStartReceiptAsync(operationId),
            OperationType.ZeroReceipt => await ExecuteZeroReceiptAsync(operationId),
            _ => throw new InvalidOperationException("Unknown operation type")
        };
    }

    private async Task<string> ExecuteEchoRequestAsync(string message, Guid? operationId = null)
    {
#if ANDROID
        if (IsIntentModeSelected() && operationId.HasValue)
        {
            var data = await _fiskaltrusClient!.SendEchoRequest(Platform.CurrentActivity!, operationId.Value, JsonConvert.DeserializeObject<EchoRequest>(_lastOperation.Body));
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else if (IsIntentModeSelected())
        {
            operationId ??= Guid.NewGuid();
            var echoRequest = new EchoRequest
            {
                Message = message
            };
            var data = await _fiskaltrusClient!.SendEchoRequest(Platform.CurrentActivity!, operationId.Value, echoRequest);
            SetLastOperation(operationId.Value, JsonConvert.SerializeObject(echoRequest), OperationType.EchoRequest, message, "Echo Request");
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else
#endif
        {
            var pos = await GetPOSAsync();
            var response = await pos.EchoAsync(new EchoRequest { Message = message });
            return response.Message;
        }
    }

    private async Task<string> ExecuteRestartConfigAsync(Guid? operationId = null)
    {
#if ANDROID
        if (IsIntentModeSelected() && operationId.HasValue)
        {
            var data = await _fiskaltrusClient!.SendEchoRequest(Platform.CurrentActivity!, operationId.Value, JsonConvert.DeserializeObject<EchoRequest>(_lastOperation.Body));
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else if (IsIntentModeSelected())
        {
            operationId ??= Guid.NewGuid();
            var echoRequest = new EchoRequest
            {
                Message = null
            };
            var data = await _fiskaltrusClient!.SendEchoRequest(Platform.CurrentActivity!, operationId.Value, echoRequest);
            SetLastOperation(operationId.Value, JsonConvert.SerializeObject(echoRequest), OperationType.RestartConfig, null, "Restart & Pull Config");
            return "✅ Configuration refresh initiated (Intent)\n\n" + JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else
#endif
        {
            var pos = await GetPOSAsync();
            await pos.EchoAsync(new EchoRequest { Message = null });
            return "✅ Configuration refresh initiated\n\nLauncher has been restarted and will pull the latest configuration.";
        }
    }

    private async Task<string> ExecuteSignRequestAsync(Guid? operationId = null)
    {
        var codes = GetReceiptCodes();
        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = CASHBOX_ID,
            ftReceiptCase = codes.SignRequest,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>()
        };

#if ANDROID
        if (IsIntentModeSelected() && operationId.HasValue)
        {
            var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, operationId.Value, JsonConvert.DeserializeObject<ReceiptRequest>(_lastOperation.Body));
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else if (IsIntentModeSelected())
        {
            operationId ??= Guid.NewGuid();
            var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, operationId.Value, receiptRequest);
            SetLastOperation(operationId.Value, JsonConvert.SerializeObject(receiptRequest), OperationType.SignRequest, null, "Sign Request");
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else
#endif
        {
            var pos = await GetPOSAsync();
            var response = await pos.SignAsync(receiptRequest);
            return JsonConvert.SerializeObject(response, Formatting.Indented);
        }
    }

    private async Task<string> ExecuteStartReceiptAsync(Guid? operationId = null)
    {
        var codes = GetReceiptCodes();
        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = CASHBOX_ID,
            cbTerminalID = "T1",
            cbReceiptReference = "2020020120152812",
            cbReceiptMoment = DateTime.UtcNow,
            ftReceiptCaseData = "",
            cbUser = "Receptionist",
            cbArea = "System",
            cbSettlement = "",
            ftReceiptCase = codes.StartReceipt,
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>()
        };
        System.Diagnostics.Debug.WriteLine($"[Danielllllllllllllllll] Using receipt: {codes.StartReceipt}");

#if ANDROID
        if (IsIntentModeSelected() && operationId.HasValue)
        {
            var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, operationId.Value, JsonConvert.DeserializeObject<ReceiptRequest>(_lastOperation.Body));
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else if (IsIntentModeSelected())
        {
            operationId ??= Guid.NewGuid();
            var response = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, operationId.Value, receiptRequest);
            SetLastOperation(operationId.Value, JsonConvert.SerializeObject(receiptRequest), OperationType.StartReceipt, null, "Start Receipt");
            return JsonConvert.SerializeObject(response, Formatting.Indented);
        }
        else
#endif
        {
            var pos = await GetPOSAsync();
            var response = await pos.SignAsync(receiptRequest);
            return JsonConvert.SerializeObject(response, Formatting.Indented);
        }
    }

    private async Task<string> ExecuteZeroReceiptAsync(Guid? operationId = null)
    {
        var codes = GetReceiptCodes();
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
            ftReceiptCase = codes.ZeroReceipt,
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>()
        };
        System.Diagnostics.Debug.WriteLine($"[Danielllllllllllllllll] Using receipt: {codes.StartReceipt}");

#if ANDROID
        if (IsIntentModeSelected() && operationId.HasValue)
        {
            var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, operationId.Value, JsonConvert.DeserializeObject<ReceiptRequest>(_lastOperation.Body));
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else if (IsIntentModeSelected())
        {
            operationId ??= Guid.NewGuid();
            var response = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, operationId.Value, receiptRequest);
            SetLastOperation(operationId.Value, JsonConvert.SerializeObject(receiptRequest), OperationType.ZeroReceipt, null, "Zero Receipt");
            return JsonConvert.SerializeObject(response, Formatting.Indented);
        }
        else
#endif
        {
            var pos = await GetPOSAsync();
            var response = await pos.SignAsync(receiptRequest);
            return JsonConvert.SerializeObject(response, Formatting.Indented);
        }
    }

    private bool IsIntentModeSelected()
    {
        return SettingsPage.GetSelectedProtocol().ToLower() == "intent";
    }

    private bool IsGrpcSelected()
    {
        return SettingsPage.GetSelectedProtocol().ToLower() == "grpc";
    }

    private ReceiptCodes GetReceiptCodes()
    {
        var country = SettingsPage.GetSelectedCountry().ToUpper();
        System.Diagnostics.Debug.WriteLine($"[Danielllllllllllllllll] Country: {country}");
        if (country == "IT")
        {
            return new ReceiptCodes
            {
                SignRequest = 0x4954_2000_0000_0001,
                StartReceipt = 0x4954_2000_0000_4001,
                ZeroReceipt = 0x4954_2000_0000_2000
            };
        }
        else // Default to DE
        {
            return new ReceiptCodes
            {
                SignRequest = 0x4445_0001_0000_0000,
                StartReceipt = 0x4445_0001_0000_0003,
                ZeroReceipt = 0x4445_0001_0000_0002
            };
        }
    }

    private class ReceiptCodes
    {
        public long SignRequest { get; set; }
        public long StartReceipt { get; set; }
        public long ZeroReceipt { get; set; }
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
        var message = $"Hello Android, it's {DateTime.Now:t}!";
        await SendEchoRequestAsync(message, null);
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

        await SendEchoRequestAsync(null, null);
    }

    private async Task SendEchoRequestAsync(string? message, Guid? operationId)
    {
        SetButtonsEnabled(false);
        btnRestartConfig.IsEnabled = false;

        try
        {
#if ANDROID
            if (IsIntentModeSelected() && operationId.HasValue)
            {
                var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, operationId.Value, JsonConvert.DeserializeObject<ReceiptRequest>(_lastOperation.Body));
                //return JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            else if (IsIntentModeSelected())
            {
                // For Intent mode: if message is null, send null; otherwise use the provided message
                operationId ??= Guid.NewGuid();
                var echoRequest = new EchoRequest
                {
                    Message = message
                };
                var data = await _fiskaltrusClient!.SendEchoRequest(Platform.CurrentActivity!, operationId.Value, echoRequest);
                SetLastOperation(operationId.Value, JsonConvert.SerializeObject(echoRequest), OperationType.EchoRequest, message, "Echo Request");
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

        var codes = GetReceiptCodes();
        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = CASHBOX_ID,
            ftReceiptCase = codes.SignRequest,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>()
        };

        try
        {
#if ANDROID
            if (IsIntentModeSelected())
            {
                var operationId = Guid.NewGuid();
                var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, operationId, receiptRequest);
                SetLastOperation(operationId, JsonConvert.SerializeObject(receiptRequest), OperationType.SignRequest, null, "Sign Request");
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

        var codes = GetReceiptCodes();
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
            ftReceiptCase = codes.StartReceipt,
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>()
        };

        try
        {
#if ANDROID
            if (IsIntentModeSelected())
            {
                var operationId = Guid.NewGuid();
                var response = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, operationId, receiptRequest);
                SetLastOperation(operationId, JsonConvert.SerializeObject(receiptRequest), OperationType.StartReceipt, null, "Start Receipt");
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
            txtSpecialReceiptResult.Text = FormatErrorForDisplay("Start Receipt", ex);
            await ShowErrorAsync("Start Receipt Failed", ex);
        }

        SetButtonsEnabled(true);
    }

    private async void OnSendZeroReceiptClicked(object? sender, EventArgs e)
    {

        SetButtonsEnabled(false);

        var codes = GetReceiptCodes();
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
            ftReceiptCase = codes.ZeroReceipt,
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>()
        };

        try
        {
#if ANDROID
            if (IsIntentModeSelected())
            {
                var operationId = Guid.NewGuid();
                var response = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, operationId, receiptRequest);
                SetLastOperation(operationId, JsonConvert.SerializeObject(receiptRequest), OperationType.ZeroReceipt, null, "Zero Receipt");
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
        btnRetryLastOperation.IsEnabled = state;
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
