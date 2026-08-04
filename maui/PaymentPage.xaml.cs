using fiskaltrust.ifPOS.v2;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

#if ANDROID
using Android.Content;
using Android.Widget;
using Platform = Microsoft.Maui.ApplicationModel.Platform;
using Button = Microsoft.Maui.Controls.Button;
#endif

namespace fiskaltrust.Middleware.Demo;

public partial class PaymentPage : ContentPage
{
    private const bool SANDBOX = true;

    private static Guid CASHBOX_ID => Guid.TryParse(SettingsPage.GetCashboxId(), out var cashboxId)
        ? cashboxId
        : throw new InvalidOperationException("The configured Cashbox ID is not a valid GUID. Please check it on the Settings page.");
    private static string ACCESS_TOKEN => SettingsPage.GetAccessToken();

#if ANDROID
    private POSSystemAPIService? _fiskaltrusClient;
#endif

    // Last operation tracking
    private LastOperationInfo? _lastOperation;

    private enum OperationType
    {
        Payment
    }

    private class LastOperationInfo
    {
        public Guid OperationID { get; set; }
        public OperationType Type { get; set; }
        public string Body { get; set; } = string.Empty;
        public PaymentRequest? PaymentRequest { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public PaymentPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
#if ANDROID
        _fiskaltrusClient = new POSSystemAPIService();
#endif
        UpdateProtocolDisplay();
    }

    private void UpdateProtocolDisplay()
    {
        var protocol = SettingsPage.GetSelectedProtocol();
        lblCurrentProtocol.Text = protocol.ToUpper();
    }

    private void SetLastOperation(Guid operationId, string body, OperationType type, PaymentRequest? paymentRequest, string displayName)
    {
        _lastOperation = new LastOperationInfo
        {
            OperationID = operationId,
            Type = type,
            Body = body,
            PaymentRequest = paymentRequest,
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
            result = await ExecutePaymentOperationAsync(_lastOperation.OperationID, _lastOperation.PaymentRequest!);
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

    private async void OnSendPaymentClicked(object? sender, EventArgs e)
    {
        if (!ValidatePaymentForm())
            return;

        SetOperationInProgress(sender as Button, true);

        try
        {
            var paymentRequest = CreatePaymentRequest();
            var operationId = Guid.NewGuid();
            var result = await ExecutePaymentOperationAsync(operationId, paymentRequest);

            SetLastOperation(operationId, JsonConvert.SerializeObject(paymentRequest), OperationType.Payment, paymentRequest, "Payment");
            lblPaymentResult.Text = result;
        }
        catch (Exception ex)
        {
            lblPaymentResult.Text = FormatErrorForDisplay("Payment", ex);
            await ShowErrorAsync("Payment Failed", ex);
        }

        SetOperationInProgress(sender as Button, false);
    }

    private void OnClearPaymentFormClicked(object? sender, EventArgs e)
    {
        entryPaymentAmount.Text = "";
        lblPaymentResult.Text = "No payment operations performed yet.";
    }

    private bool ValidatePaymentForm()
    {
        if (string.IsNullOrWhiteSpace(entryPaymentAmount.Text))
        {
            DisplayAlertAsync("Validation Error", "Please enter a payment amount.", "OK");
            return false;
        }

        if (!decimal.TryParse(entryPaymentAmount.Text, out decimal amount) || amount <= 0)
        {
            DisplayAlertAsync("Validation Error", "Please enter a valid positive amount.", "OK");
            return false;
        }

        return true;
    }

    private PaymentRequest CreatePaymentRequest()
    {
        var amount = decimal.Parse(entryPaymentAmount.Text);
        return new PaymentRequest
        {
            Action = "payment",
            Protocol = "use_auto",
            cbPayItem = new PayItem
            {
                Amount = amount,
                Description = "Demo Payment",
            }
        };
    }

    private async Task<string> ExecutePaymentOperationAsync(Guid operationId, PaymentRequest paymentRequest)
    {
#if ANDROID
        var data = await _fiskaltrusClient!.SendPaymentRequest(Platform.CurrentActivity!, CASHBOX_ID, ACCESS_TOKEN, operationId, paymentRequest);
        return JsonConvert.SerializeObject(data, Formatting.Indented);
#endif
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
        btnSendPayment.IsEnabled = state;
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

// Data Transfer Objects for Payment API
public class PaymentRequest
{
    public string Action { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;

    public PayItem cbPayItem { get; set; }
}

public class PaymentResponse
{
    public string Action { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public Guid ftQueueID { get; set; }

    public List<PayItem> ftPayItems { get; set; }
}
