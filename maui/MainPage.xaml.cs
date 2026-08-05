using fiskaltrust.ifPOS.v2;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.Cases;


using Android.Content;
using Android.Widget;
using Platform = Microsoft.Maui.ApplicationModel.Platform;
using Button = Microsoft.Maui.Controls.Button;

namespace fiskaltrust.Middleware.Demo;

public partial class MainPage : ContentPage
{
    private const bool SANDBOX = true;

    private static Guid CASHBOX_ID => Guid.TryParse(SettingsPage.GetCashboxId(), out var cashboxId)
        ? cashboxId
        : throw new InvalidOperationException("The configured Cashbox ID is not a valid GUID. Please check it on the Settings page.");
    private static string ACCESS_TOKEN => SettingsPage.GetAccessToken();

    private PosSystemApiService? _fiskaltrusClient;

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
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _fiskaltrusClient = new PosSystemApiService();
        UpdateProtocolDisplay();
    }

    private void UpdateProtocolDisplay()
    {
        var protocol = SettingsPage.GetSelectedProtocol();
        lblCurrentProtocol.Text = protocol.ToUpper();
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
        await DisplayAlertAsync(
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
        if (operationId.HasValue)
        {
            var data = await _fiskaltrusClient!.SendEchoRequest(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, JsonConvert.DeserializeObject<EchoRequest>(_lastOperation.Body));
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else
        {
            operationId ??= Guid.NewGuid();
            var echoRequest = new EchoRequest
            {
                Message = message
            };
            var data = await _fiskaltrusClient!.SendEchoRequest(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, echoRequest);
            SetLastOperation(operationId.Value, JsonConvert.SerializeObject(echoRequest), OperationType.EchoRequest, message, "Echo Request");
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }

    private async Task<string> ExecuteRestartConfigAsync(Guid? operationId = null)
    {
        if (operationId.HasValue)
        {
            var data = await _fiskaltrusClient!.SendEchoRequest(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, JsonConvert.DeserializeObject<EchoRequest>(_lastOperation.Body));
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else
        {
            operationId ??= Guid.NewGuid();
            var echoRequest = new EchoRequest
            {
                Message = null
            };
            var data = await _fiskaltrusClient!.SendEchoRequest(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, echoRequest);
            SetLastOperation(operationId.Value, JsonConvert.SerializeObject(echoRequest), OperationType.RestartConfig, null, "Restart & Pull Config");
            return "✅ Configuration refresh initiated (Intent)\n\n" + JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }

    private async Task<string> ExecuteSignRequestAsync(Guid? operationId = null)
    {
        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = CASHBOX_ID,
            ftReceiptCase = (ReceiptCase)0x4445_0001_0000_0000,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [],
            cbPayItems = []
        };

        if (operationId.HasValue)
        {
            var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, JsonConvert.DeserializeObject<ReceiptRequest>(_lastOperation.Body));
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else
        {
            operationId ??= Guid.NewGuid();
            var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, receiptRequest);
            SetLastOperation(operationId.Value, JsonConvert.SerializeObject(receiptRequest), OperationType.SignRequest, null, "Sign Request");
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }

    private async Task<string> ExecuteStartReceiptAsync(Guid? operationId = null)
    {
        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = CASHBOX_ID,
            ftPosSystemId = Guid.Parse("d4a62055-ca6c-4372-ae4d-f835a88e4a5d"),
            cbTerminalID = "T1",
            cbReceiptReference = "2020020120152812",
            cbReceiptMoment = DateTime.UtcNow,
            ftReceiptCaseData = "",
            cbUser = "Receptionist",
            cbArea = "System",
            cbSettlement = "",
            ftReceiptCase = (ReceiptCase)0x4445_0001_0000_0003,
            cbChargeItems = [],
            cbPayItems = []
        };

        if (operationId.HasValue)
        {
            var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, JsonConvert.DeserializeObject<ReceiptRequest>(_lastOperation.Body));
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else
        {
            operationId ??= Guid.NewGuid();
            var response = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, receiptRequest);
            SetLastOperation(operationId.Value, JsonConvert.SerializeObject(receiptRequest), OperationType.StartReceipt, null, "Start Receipt");
            return JsonConvert.SerializeObject(response, Formatting.Indented);
        }
    }

    private async Task<string> ExecuteZeroReceiptAsync(Guid? operationId = null)
    {
        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = CASHBOX_ID,
            ftPosSystemId = Guid.Parse("d4a62055-ca6c-4372-ae4d-f835a88e4a5d"),
            cbTerminalID = "T1",
            cbReceiptReference = "2020020120152812",
            cbReceiptMoment = DateTime.UtcNow,
            ftReceiptCaseData = "",
            cbUser = "Receptionist",
            cbArea = "System",
            cbSettlement = "",
            ftReceiptCase = (ReceiptCase)0x4445_0001_0000_0002,
            cbChargeItems = [],
            cbPayItems = []
        };

        if (operationId.HasValue)
        {
            var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, JsonConvert.DeserializeObject<ReceiptRequest>(_lastOperation.Body));
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else
        {
            operationId ??= Guid.NewGuid();
            var response = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, receiptRequest);
            SetLastOperation(operationId.Value, JsonConvert.SerializeObject(receiptRequest), OperationType.StartReceipt, null, "Start Receipt");
            return JsonConvert.SerializeObject(response, Formatting.Indented);
        }
    }


    private async void OnSendEchoRequestClicked(object? sender, EventArgs e)
    {
        var message = $"Hello Android, it's {DateTime.Now:t}!";
        await SendEchoRequestAsync(message, null, sender as Button);
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

        await SendEchoRequestAsync(null, null, sender as Button);
    }

    private async Task SendEchoRequestAsync(string? message, Guid? operationId, Button? pressedButton = null)
    {
        SetOperationInProgress(pressedButton, true);

        try
        {
            if (operationId.HasValue)
            {
                var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, JsonConvert.DeserializeObject<ReceiptRequest>(_lastOperation.Body));
                //return JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            else
            {
                // For Intent mode: if message is null, send null; otherwise use the provided message
                operationId ??= Guid.NewGuid();
                var echoRequest = new EchoRequest
                {
                    Message = message
                };
                var data = await _fiskaltrusClient!.SendEchoRequest(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId.Value, echoRequest);
                SetLastOperation(operationId.Value, JsonConvert.SerializeObject(echoRequest), OperationType.EchoRequest, message, "Echo Request");
                txtResult.Text = JsonConvert.SerializeObject(data, Formatting.Indented);

                if (message == null)
                {
                    txtResult.Text = "✅ Configuration refresh initiated (Intent)\n\n" + txtResult.Text;
                }
            }
        }
        catch (Exception ex)
        {
            var operation = message == null ? "Restart & Pull Config" : "Echo Request";
            txtResult.Text = FormatErrorForDisplay(operation, ex);
            await ShowErrorAsync($"{operation} Failed", ex);
        }

        SetOperationInProgress(pressedButton, false);
    }

    private async void OnSendSignRequestClicked(object? sender, EventArgs e)
    {
        SetOperationInProgress(sender as Button, true);

        try
        {
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = CASHBOX_ID,
                ftReceiptCase = (ReceiptCase)0x4445_0001_0000_0000,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems = [],
                cbPayItems = []
            };
            var operationId = Guid.NewGuid();
            var data = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId, receiptRequest);
            SetLastOperation(operationId, JsonConvert.SerializeObject(receiptRequest), OperationType.SignRequest, null, "Sign Request");
            txtSignResult.Text = JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        catch (Exception ex)
        {
            txtSignResult.Text = FormatErrorForDisplay("Sign Request", ex);
            await ShowErrorAsync("Sign Request Failed", ex);
        }

        SetOperationInProgress(sender as Button, false);
    }

    private async void OnSendStartReceiptClicked(object? sender, EventArgs e)
    {

        SetOperationInProgress(sender as Button, true);

        try
        {
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = CASHBOX_ID,
                ftPosSystemId = Guid.Parse("d4a62055-ca6c-4372-ae4d-f835a88e4a5d"),
                cbTerminalID = "T1",
                cbReceiptReference = "2020020120152812",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCaseData = "",
                cbUser = "Receptionist",
                cbArea = "System",
                cbSettlement = "",
                ftReceiptCase = (ReceiptCase)0x4445_0001_0000_0003,
                cbChargeItems = [],
                cbPayItems = []
            };
            var operationId = Guid.NewGuid();
            var response = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId, receiptRequest);
            SetLastOperation(operationId, JsonConvert.SerializeObject(receiptRequest), OperationType.StartReceipt, null, "Start Receipt");
            txtSpecialReceiptResult.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
        }
        catch (Exception ex)
        {
            txtSpecialReceiptResult.Text = FormatErrorForDisplay("Start Receipt", ex);
            await ShowErrorAsync("Start Receipt Failed", ex);
        }

        SetOperationInProgress(sender as Button, false);
    }

    private async void OnSendZeroReceiptClicked(object? sender, EventArgs e)
    {

        SetOperationInProgress(sender as Button, true);

        try
        {
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = CASHBOX_ID,
                ftPosSystemId = Guid.Parse("d4a62055-ca6c-4372-ae4d-f835a88e4a5d"),
                cbTerminalID = "T1",
                cbReceiptReference = "2020020120152812",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCaseData = "",
                cbUser = "Receptionist",
                cbArea = "System",
                cbSettlement = "",
                ftReceiptCase = (ReceiptCase)0x4445_0001_0000_0002,
                cbChargeItems = [],
                cbPayItems = []
            };
            var operationId = Guid.NewGuid();
            var response = await _fiskaltrusClient!.SignReceipt(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId, receiptRequest);
            SetLastOperation(operationId, JsonConvert.SerializeObject(receiptRequest), OperationType.ZeroReceipt, null, "Zero Receipt");
            txtSpecialReceiptResult.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
        }
        catch (Exception ex)
        {
            txtSpecialReceiptResult.Text = FormatErrorForDisplay("Zero Receipt", ex);
            await ShowErrorAsync("Zero Receipt Failed", ex);
        }

        SetOperationInProgress(sender as Button, false);
    }


    private static bool UsesBoundService => SettingsPage.GetSelectedProtocol() == "service-ipc";

    // The bound service supports parallel requests, so only the pressed button is
    // disabled in that mode. Intent mode still disables all buttons while busy.
    private void SetOperationInProgress(Button? pressedButton, bool inProgress)
    {
        if (UsesBoundService && pressedButton != null)
        {
            pressedButton.IsEnabled = !inProgress;
        }
        else
        {
            SetButtonsEnabled(!inProgress);
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
