using Android.App;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Demo.Services;
using Newtonsoft.Json;
using System.Text;

namespace fiskaltrust.Middleware.Demo;

// High-level client for the fiskaltrust POS system API. It builds HTTP-style requests
// (method, path, headers, body) for the individual API endpoints and sends them through
// the configured transport (bound service IPC or activity-based, selectable in the settings).
public class PosSystemApiService
{
    private readonly IPosSystemTransport _transport;

    public PosSystemApiService()
    {
        _transport = PosSystemTransportFactory.GetInstance(SettingsPage.GetSelectedProtocol() == "service-ipc");
    }

    // Every request must carry these headers to authenticate against the middleware:
    // the cashbox ID, its access token, and a unique operation ID for idempotency/tracing.
    private static Dictionary<string, string> GetHeaders(Guid cashBoxId, string accessToken, Guid operationId) => new()
    {
        { "x-cashbox-id", cashBoxId.ToString() },
        { "x-cashbox-accesstoken", accessToken },
        { "x-operation-id", operationId.ToString() }
    };

    public Task<EchoResponse> SendEchoRequest(Activity activity, Guid cashBoxId, string accessToken, Guid operationId, EchoRequest echoRequest, CancellationToken cancellationToken = default)
    {
        var request = new PosSystemApiRequest
        {
            Method = "POST",
            Path = "/v2/echo",
            Headers = GetHeaders(cashBoxId, accessToken, operationId),
            Body = JsonConvert.SerializeObject(echoRequest),
        };
        if (echoRequest == null)
        {
            request.Body = null;
        }
        return PerformPosSystemApiRequest<EchoResponse>(activity, request, cancellationToken);
    }

    public Task<fiskaltrust.ifPOS.v2.ReceiptResponse> SignReceipt(Activity activity, Guid cashBoxId, string accessToken, Guid operationId, ReceiptRequest receipt, CancellationToken cancellationToken = default)
    {
        var request = new PosSystemApiRequest
        {
            Method = "POST",
            Path = "/v2/sign",
            Headers = GetHeaders(cashBoxId, accessToken, operationId),
            Body = JsonConvert.SerializeObject(receipt)
        };
        if (receipt == null)
        {
            request.Body = null;
        }
        return PerformPosSystemApiRequest<fiskaltrust.ifPOS.v2.ReceiptResponse>(activity, request, cancellationToken);
    }

    public Task<PaymentResponse> SendPaymentRequest(Activity activity, Guid cashBoxId, string accessToken, Guid operationId, PaymentRequest paymentRequest, CancellationToken cancellationToken = default)
    {
        var request = new PosSystemApiRequest
        {
            Method = "POST",
            Path = "/v2/pay",
            Headers = GetHeaders(cashBoxId, accessToken, operationId),
            Body = JsonConvert.SerializeObject(paymentRequest)
        };
        if (paymentRequest == null)
        {
            request.Body = null;
        }
        return PerformPosSystemApiRequest<PaymentResponse>(activity, request, cancellationToken);
    }

    public Task<IssuingResponse> SendIssuingRequest(Activity activity, Guid cashBoxId, string accessToken, Guid operationId, IssuingRequest issuingRequest, CancellationToken cancellationToken = default)
    {
        var request = new PosSystemApiRequest
        {
            Method = "POST",
            Path = "/v2/issue",
            Headers = GetHeaders(cashBoxId, accessToken, operationId),
            Body = JsonConvert.SerializeObject(issuingRequest)
        };
        if (issuingRequest == null)
        {
            request.Body = null;
        }
        return PerformPosSystemApiRequest<IssuingResponse>(activity, request, cancellationToken);
    }



    // Sends a request through the transport and returns the response content,
    // throwing if the possystemapi reports a non-2xx status code.
    public async Task<string> PerformPosSystemApiRequest(Activity activity, PosSystemApiRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _transport.SendAsync(request, cancellationToken);
        var content = response.Content ?? string.Empty;
        if (!response.IsSuccess)
        {
            throw new Exception(content);
        }

        return content;
    }

    // Same as above, but deserializes the JSON response content into the given type.
    public async Task<T> PerformPosSystemApiRequest<T>(Activity activity, PosSystemApiRequest request, CancellationToken cancellationToken = default) => JsonConvert.DeserializeObject<T>(await PerformPosSystemApiRequest(activity, request, cancellationToken))!;
}


// Describes a POS system API request HTTP-style: method, path, headers, and an optional body.
// The transports transfer headers and body base64url-encoded; the *Base64Url properties
// provide the encoded values so callers can work with plain strings.
public class PosSystemApiRequest
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];
    public string? Body { get; set; }
    public string ResponseAction { get; set; } = string.Empty;

    public string HeadersBase64Url => ToBase64Url(JsonConvert.SerializeObject(Headers));
    public string? BodyBase64Url => Body != null ? ToBase64Url(Body) : null;

    private static string ToBase64Url(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}


// Describes a POS system API response, mirroring an HTTP response.
// The transports receive content and content type base64url-encoded; assigning the
// *Base64Url properties decodes them into the plain Content / ContentType values.
public class PosSystemApiResponse
{
    public string StatusCode { get; set; } = string.Empty;

    public string ContentBase64Url { set => Content = FromBase64Url(value); }
    public string Content { get; set; } = string.Empty;

    public string ContentTypeBase64Url { set => ContentType = FromBase64Url(value); }
    public string ContentType { get; set; } = string.Empty;

    public Dictionary<string, string> Headers { get; set; } = [];

    public bool IsSuccess => StatusCode.StartsWith('2');

    public int StatusCodeInt => int.TryParse(StatusCode, out var code) ? code : 0;

    private static string FromBase64Url(string base64Url)
    {
        var base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }
}
