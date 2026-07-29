using Android.App;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Demo.Models;
using fiskaltrust.Middleware.Demo.Platforms.Android;
using Newtonsoft.Json;
using System.Text;

namespace fiskaltrust.Middleware.Demo
{
    public class POSSystemAPIService
    {
        private Dictionary<string, string> _headers => new Dictionary<string, string>
        {
            { "x-cashbox-id", _cashBoxId.ToString() },
            { "x-cashbox-accesstoken", _accessToken },
        };

        private Guid _cashBoxId;
        private string _accessToken;
        private readonly IPosSystemTransport _intentService;

        public POSSystemAPIService(Guid cashBoxId, string accessToken)
        {
            _cashBoxId = cashBoxId;
            _accessToken = accessToken;
            _intentService = PosSystemTransportFactory.GetInstance(SettingsPage.GetSelectedProtocol() == "intent-service");
        }

        public Task<EchoResponse> SendEchoRequest(Activity activity, Guid operationId, EchoRequest echoRequest)
        {
            var request = new POSSystemAPIRequest
            {
                Method = "POST",
                Path = "/v2/echo",
                Headers = _headers,
                Body = JsonConvert.SerializeObject(echoRequest),
                RequestId = new Guid().ToString()
            };
            if(echoRequest == null)
            {
                request.Body = null;
            }
            return PerformPOSSystemAPIIntent<EchoResponse>(activity, operationId, request);
        }

        public Task<fiskaltrust.ifPOS.v2.ReceiptResponse> SignReceipt(Activity activity, Guid operationId, ReceiptRequest receipt)
        {
            var request = new POSSystemAPIRequest
            {
                Method = "POST",
                Path = "/v2/sign",
                Headers = _headers,
                Body = JsonConvert.SerializeObject(receipt),
                RequestId = new Guid().ToString()
            };
            if (receipt == null)
            {
                request.Body = null;
            }
            return PerformPOSSystemAPIIntent<fiskaltrust.ifPOS.v2.ReceiptResponse>(activity, operationId, request);
        }

        public Task<PaymentResponse> SendPaymentRequest(Activity activity, Guid operationId, PaymentRequest paymentRequest)
        {
            var request = new POSSystemAPIRequest
            {
                Method = "POST",
                Path = "/v2/pay",
                Headers = _headers,
                Body = JsonConvert.SerializeObject(paymentRequest),
                RequestId = new Guid().ToString()
            };
            if (paymentRequest == null)
            {
                request.Body = null;
            }
            return PerformPOSSystemAPIIntent<PaymentResponse>(activity, operationId, request);
        }

        public Task<IssuingResponse> SendIssuingRequest(Activity activity, Guid operationId, IssuingRequest issuingRequest)
        {
            var request = new POSSystemAPIRequest
            {
                Method = "POST",
                Path = "/v2/issue",
                Headers = _headers,
                Body = JsonConvert.SerializeObject(issuingRequest),
                RequestId = new Guid().ToString()
            };
            if (issuingRequest == null)
            {
                request.Body = null;
            }
            return PerformPOSSystemAPIIntent<IssuingResponse>(activity, operationId, request);
        }

        public async Task<T> PerformPOSSystemAPIIntent<T>(Activity activity, Guid operationId, POSSystemAPIRequest request)
        {
            request.Headers.Add("x-operation-id", operationId.ToString());
            var headersJson = JsonConvert.SerializeObject(request.Headers);
            var headerB64 = ToBase64Url(headersJson);
            var bodyB64 = request.Body != null ? ToBase64Url(request.Body) : null;

            PosSystemApiResponse response = await _intentService.SendAsync(new RequestInfo() { Method = request.Method, Path = request.Path, HeaderB64 = headerB64, BodyB64 = bodyB64 });
            var content = FromBase64Url(response.ContentBase64Url ?? string.Empty);
            if (!response.IsSuccess)
            {
                throw new Exception(content);
            }

            return JsonConvert.DeserializeObject<T>(content)!;
        }
    
        private string ToBase64Url(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private string FromBase64Url(string base64Url)
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
}

public class PosSystemApiResponse
{
    public string StatusCode { get; set; } = string.Empty;

    public string ContentBase64Url { get; set; } = string.Empty;

    public string ContentTypeBase64Url { get; set; } = string.Empty;

    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    public bool IsSuccess => StatusCode.StartsWith("2");

    public int StatusCodeInt => int.TryParse(StatusCode, out var code) ? code : 0;
}
