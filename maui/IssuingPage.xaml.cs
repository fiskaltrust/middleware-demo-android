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

public partial class IssuingPage : ContentPage
{
    private const string QUEUE_URL_GRPC = "grpc://localhost:1400";
    private const string QUEUE_URL_REST = "http://localhost:1500/queue";
    private const bool SANDBOX = true;

    private static string CASHBOX_ID => SettingsPage.GetCashboxId();
    private static string ACCESS_TOKEN => SettingsPage.GetAccessToken();

#if ANDROID
    private POSSystemAPIIntentService? _fiskaltrusClient;
#endif

    // Last operation tracking
    private LastOperationInfo? _lastOperation;
    private string? _lastIssuedDocumentId;

    private enum OperationType
    {
        IssueDocument,
        ValidateDocument,
        CancelDocument
    }

    private class LastOperationInfo
    {
        public Guid OperationID { get; set; }
        public OperationType Type { get; set; }
        public string Body { get; set; } = string.Empty;
        public IssuingRequest? IssuingRequest { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public IssuingPage()
    {
        InitializeComponent();

        // Set default values
        pickerDocumentType.SelectedIndex = 0; // Invoice
        pickerCurrency.SelectedIndex = 0; // EUR
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
#if ANDROID
        if (Guid.TryParse(CASHBOX_ID, out var cashboxGuid))
        {
            _fiskaltrusClient = new POSSystemAPIIntentService(cashboxGuid, ACCESS_TOKEN);
        }
#endif
        UpdateProtocolDisplay();
    }

    private void UpdateProtocolDisplay()
    {
        var protocol = SettingsPage.GetSelectedProtocol();
        lblCurrentProtocol.Text = protocol.ToUpper();
    }

    private void SetLastOperation(Guid operationId, string body, OperationType type, IssuingRequest? issuingRequest, string displayName)
    {
        _lastOperation = new LastOperationInfo
        {
            OperationID = operationId,
            Type = type,
            Body = body,
            IssuingRequest = issuingRequest,
            DisplayName = displayName
        };
        btnRetryLastOperation.IsVisible = true;
        btnRetryLastOperation.Text = $"?? Retry: {displayName}";
    }

    private async void OnRetryLastOperationClicked(object? sender, EventArgs e)
    {
        if (_lastOperation == null)
            return;

        string result;
        try
        {
            result = await ExecuteIssuingOperationAsync(_lastOperation.OperationID, _lastOperation.Type, _lastOperation.IssuingRequest);
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

    private async void OnIssueDocumentClicked(object? sender, EventArgs e)
    {
        if (!ValidateIssuingForm())
            return;

        SetButtonsEnabled(false);

        try
        {
            var issuingRequest = CreateIssuingRequest();
            var operationId = Guid.NewGuid();
            var result = await ExecuteIssuingOperationAsync(operationId, OperationType.IssueDocument, issuingRequest);

            SetLastOperation(operationId, JsonConvert.SerializeObject(issuingRequest), OperationType.IssueDocument, issuingRequest, "Issue Document");
            lblIssuingResult.Text = result;
        }
        catch (Exception ex)
        {
            lblIssuingResult.Text = FormatErrorForDisplay("Issue Document", ex);
            await ShowErrorAsync("Document Issuing Failed", ex);
        }

        SetButtonsEnabled(true);
    }

    private async void OnValidateDocumentClicked(object? sender, EventArgs e)
    {
        if (!ValidateIssuingForm())
            return;

        SetButtonsEnabled(false);

        try
        {
            var issuingRequest = CreateIssuingRequest();
            var operationId = Guid.NewGuid();
            var result = await ExecuteIssuingOperationAsync(operationId, OperationType.ValidateDocument, issuingRequest);

            SetLastOperation(operationId, JsonConvert.SerializeObject(issuingRequest), OperationType.ValidateDocument, issuingRequest, "Validate Document");
            lblIssuingResult.Text = result;
        }
        catch (Exception ex)
        {
            lblIssuingResult.Text = FormatErrorForDisplay("Validate Document", ex);
            await ShowErrorAsync("Document Validation Failed", ex);
        }

        SetButtonsEnabled(true);
    }

    private async void OnCancelDocumentClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_lastIssuedDocumentId))
        {
            await DisplayAlert("Cancel Document", "No document has been issued yet to cancel.", "OK");
            return;
        }

        var confirmed = await DisplayAlert(
            "Cancel Document",
            $"Are you sure you want to cancel document {_lastIssuedDocumentId}?",
            "Yes",
            "No"
        );

        if (!confirmed)
            return;

        SetButtonsEnabled(false);

        try
        {
            var cancelRequest = CreateCancelRequest(_lastIssuedDocumentId);
            var operationId = Guid.NewGuid();
            var result = await ExecuteIssuingOperationAsync(operationId, OperationType.CancelDocument, cancelRequest);

            SetLastOperation(operationId, JsonConvert.SerializeObject(cancelRequest), OperationType.CancelDocument, cancelRequest, "Cancel Document");
            lblIssuingResult.Text = result;
        }
        catch (Exception ex)
        {
            lblIssuingResult.Text = FormatErrorForDisplay("Cancel Document", ex);
            await ShowErrorAsync("Document Cancellation Failed", ex);
        }

        SetButtonsEnabled(true);
    }

    private async void OnSampleInvoiceClicked(object? sender, EventArgs e)
    {
        SetSampleDocument("Invoice", "INV-2024-001", "Acme Corporation", "CUST-001", "125.50", "Professional services invoice");
        await Task.Delay(100);
        OnIssueDocumentClicked(sender, e);
    }

    private async void OnSampleReceiptClicked(object? sender, EventArgs e)
    {
        SetSampleDocument("Receipt", "RCP-2024-001", "Walk-in Customer", "", "25.99", "Coffee and pastry purchase");
        await Task.Delay(100);
        OnIssueDocumentClicked(sender, e);
    }

    private async void OnSampleCreditNoteClicked(object? sender, EventArgs e)
    {
        SetSampleDocument("Credit Note", "CN-2024-001", "Regular Customer", "CUST-002", "15.00", "Refund for returned item");
        await Task.Delay(100);
        OnIssueDocumentClicked(sender, e);
    }

    private async void OnSampleQuoteClicked(object? sender, EventArgs e)
    {
        SetSampleDocument("Quote", "QUO-2024-001", "Potential Customer", "LEAD-001", "300.00", "Quote for software development services");
        await Task.Delay(100);
        OnIssueDocumentClicked(sender, e);
    }

    private void SetSampleDocument(string docType, string docNumber, string customerName, string customerId, string amount, string description)
    {
        var docTypeIndex = pickerDocumentType.Items.IndexOf(docType);
        if (docTypeIndex >= 0)
            pickerDocumentType.SelectedIndex = docTypeIndex;

        entryDocumentNumber.Text = docNumber;
        entryCustomerName.Text = customerName;
        entryCustomerId.Text = customerId;
        entryDocumentAmount.Text = amount;
        editorDescription.Text = description;
        pickerCurrency.SelectedIndex = 0; // EUR
    }

    private void OnClearIssuingFormClicked(object? sender, EventArgs e)
    {
        pickerDocumentType.SelectedIndex = 0;
        entryDocumentNumber.Text = "";
        entryCustomerName.Text = "";
        entryCustomerId.Text = "";
        entryDocumentAmount.Text = "";
        editorDescription.Text = "";
        pickerCurrency.SelectedIndex = 0;
        lblIssuingResult.Text = "No issuing operations performed yet.";
    }

    private bool ValidateIssuingForm()
    {
        if (pickerDocumentType.SelectedIndex == -1)
        {
            DisplayAlert("Validation Error", "Please select a document type.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(entryCustomerName.Text))
        {
            DisplayAlert("Validation Error", "Please enter a customer name.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(entryDocumentAmount.Text))
        {
            DisplayAlert("Validation Error", "Please enter a document amount.", "OK");
            return false;
        }

        if (!decimal.TryParse(entryDocumentAmount.Text, out decimal amount) || amount < 0)
        {
            DisplayAlert("Validation Error", "Please enter a valid amount (must be 0 or positive).", "OK");
            return false;
        }

        if (pickerCurrency.SelectedIndex == -1)
        {
            DisplayAlert("Validation Error", "Please select a currency.", "OK");
            return false;
        }

        return true;
    }

    private IssuingRequest CreateIssuingRequest()
    {
        var docType = pickerDocumentType.Items[pickerDocumentType.SelectedIndex];
        var docNumber = string.IsNullOrWhiteSpace(entryDocumentNumber.Text) 
            ? $"{docType.ToUpper()}-{DateTime.Now:yyyyMMdd-HHmmss}" 
            : entryDocumentNumber.Text;
        var customerName = entryCustomerName.Text ?? "";
        var customerId = entryCustomerId.Text ?? "";
        var amount = decimal.Parse(entryDocumentAmount.Text);
        var currency = pickerCurrency.Items[pickerCurrency.SelectedIndex];
        var description = editorDescription.Text ?? "";

        return new IssuingRequest
        {
            ftCashBoxID = CASHBOX_ID,
            ftQueueID = Guid.Parse(CASHBOX_ID),
            ftPosSystemId = "d4a62055-ca6c-4372-ae4d-f835a88e4a5d",
            cbTerminalID = "T1",
            DocumentType = docType,
            DocumentNumber = docNumber,
            CustomerName = customerName,
            CustomerId = customerId,
            Amount = amount,
            Currency = currency,
            Description = description,
            RequestId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            cbUser = "Operator",
            cbArea = "Issuing"
        };
    }

    private IssuingRequest CreateCancelRequest(string documentId)
    {
        return new IssuingRequest
        {
            ftCashBoxID = CASHBOX_ID,
            ftQueueID = Guid.Parse(CASHBOX_ID),
            ftPosSystemId = "d4a62055-ca6c-4372-ae4d-f835a88e4a5d",
            cbTerminalID = "T1",
            DocumentType = "Cancellation",
            DocumentNumber = documentId,
            RequestId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            cbUser = "Operator",
            cbArea = "Cancellation"
        };
    }

    private async Task<string> ExecuteIssuingOperationAsync(Guid operationId, OperationType type, IssuingRequest? issuingRequest)
    {
        var isIntentMode = IsIntentModeSelected();

#if ANDROID
        if (isIntentMode)
        {
            var data = await _fiskaltrusClient!.SendIssuingRequest(Platform.CurrentActivity!, operationId, issuingRequest!);
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        else
#endif
        {
            // For now, simulate issuing processing since we don't have direct HTTP/gRPC issuing endpoints
            // In a real implementation, this would call the actual issuing endpoint
            var simulatedResponse = type switch
            {
                OperationType.IssueDocument => CreateIssuingResponse(issuingRequest!, true, "Document issued successfully."),
                OperationType.ValidateDocument => CreateValidationResponse(issuingRequest!, true, "Document validation passed."),
                OperationType.CancelDocument => CreateCancellationResponse(issuingRequest!, true, "Document cancelled successfully."),
                _ => throw new ArgumentException("Unknown operation type")
            };

            // Store the last issued document ID for cancellation
            if (type == OperationType.IssueDocument && simulatedResponse.Success)
            {
                _lastIssuedDocumentId = simulatedResponse.DocumentId;
            }

            return JsonConvert.SerializeObject(simulatedResponse, Formatting.Indented);
        }
    }

    private IssuingResponse CreateIssuingResponse(IssuingRequest request, bool success, string message)
    {
        return new IssuingResponse
        {
            Success = success,
            DocumentId = request.DocumentNumber,
            DocumentType = request.DocumentType,
            Amount = request.Amount,
            Currency = request.Currency,
            CustomerName = request.CustomerName,
            CustomerId = request.CustomerId,
            Timestamp = DateTime.UtcNow,
            Message = message,
            FiscalReference = $"FR-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}"
        };
    }

    private IssuingResponse CreateValidationResponse(IssuingRequest request, bool success, string message)
    {
        return new IssuingResponse
        {
            Success = success,
            DocumentId = request.DocumentNumber,
            DocumentType = request.DocumentType,
            Amount = request.Amount,
            Currency = request.Currency,
            CustomerName = request.CustomerName,
            CustomerId = request.CustomerId,
            Timestamp = DateTime.UtcNow,
            Message = message,
            ValidationDetails = new()
            {
                ["DocumentFormat"] = "Valid",
                ["CustomerData"] = "Valid",
                ["AmountFormat"] = "Valid",
                ["TaxCalculation"] = "Valid"
            }
        };
    }

    private IssuingResponse CreateCancellationResponse(IssuingRequest request, bool success, string message)
    {
        return new IssuingResponse
        {
            Success = success,
            DocumentId = request.DocumentNumber,
            DocumentType = "Cancellation",
            Timestamp = DateTime.UtcNow,
            Message = message,
            OriginalDocumentId = request.DocumentNumber
        };
    }

    private bool IsIntentModeSelected()
    {
        return SettingsPage.GetSelectedProtocol().ToLower() == "intent";
    }

    private bool IsGrpcSelected()
    {
        return SettingsPage.GetSelectedProtocol().ToLower() == "grpc";
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
        btnIssueDocument.IsEnabled = state;
        btnValidateDocument.IsEnabled = state;
        btnCancelDocument.IsEnabled = state;
        btnSampleInvoice.IsEnabled = state;
        btnSampleReceipt.IsEnabled = state;
        btnSampleCreditNote.IsEnabled = state;
        btnSampleQuote.IsEnabled = state;
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

        await DisplayAlert(
            $"? {title}",
            $"{errorMessage}\n\n?? Error Type: {errorType}",
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
        return $"? Error: {operation}\n\n{errorMessage}\n\n({ex.GetType().Name})";
    }
}

// Data Transfer Objects for Issuing API
public class IssuingRequest
{
    public string ftCashBoxID { get; set; } = string.Empty;
    public Guid ftQueueID { get; set; }
    public string ftPosSystemId { get; set; } = string.Empty;
    public string cbTerminalID { get; set; } = string.Empty;
    public string cbUser { get; set; } = string.Empty;
    public string cbArea { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class IssuingResponse
{
    public bool Success { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FiscalReference { get; set; }
    public string? OriginalDocumentId { get; set; }
    public Dictionary<string, string>? ValidationDetails { get; set; }
}