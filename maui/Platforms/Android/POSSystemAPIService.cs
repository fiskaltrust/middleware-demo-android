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
        private readonly IPosSystemTransport _transport;

        public POSSystemAPIService()
        {
            _transport = PosSystemTransportFactory.GetInstance(SettingsPage.GetSelectedProtocol() == "service-ipc");
        }

        private static Dictionary<string, string> GetHeaders(Guid cashBoxId, string accessToken, Guid operationId) => new()
        {
            { "x-cashbox-id", cashBoxId.ToString() },
            { "x-cashbox-accesstoken", accessToken },
            {"x-operation-id", operationId.ToString()}
        };

        public Task<EchoResponse> SendEchoRequest(Activity activity, Guid cashBoxId, string accessToken, Guid operationId, EchoRequest echoRequest)
        {
            var request = new POSSystemAPIRequest
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
            return PerformPOSSystemAPIRequest<EchoResponse>(activity, request);
        }

        public Task<fiskaltrust.ifPOS.v2.ReceiptResponse> SignReceipt(Activity activity, Guid cashBoxId, string accessToken, Guid operationId, ReceiptRequest receipt)
        {
            var request = new POSSystemAPIRequest
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
            return PerformPOSSystemAPIRequest<fiskaltrust.ifPOS.v2.ReceiptResponse>(activity, request);
        }

        public Task<PaymentResponse> SendPaymentRequest(Activity activity, Guid cashBoxId, string accessToken, Guid operationId, PaymentRequest paymentRequest)
        {
            var request = new POSSystemAPIRequest
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
            return PerformPOSSystemAPIRequest<PaymentResponse>(activity, request);
        }

        public Task<IssuingResponse> SendIssuingRequest(Activity activity, Guid cashBoxId, string accessToken, Guid operationId, IssuingRequest issuingRequest)
        {
            var request = new POSSystemAPIRequest
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
            return PerformPOSSystemAPIRequest<IssuingResponse>(activity, request);
        }



        public async Task<string> PerformPOSSystemAPIRequest(Activity activity, POSSystemAPIRequest request)
        {
            var headersJson = JsonConvert.SerializeObject(request.Headers);
            var headerB64 = ToBase64Url(headersJson);
            var bodyB64 = request.Body != null ? ToBase64Url(request.Body) : null;

            PosSystemApiResponse response = await _transport.SendAsync(new RequestInfo() { Method = request.Method, Path = request.Path, HeaderB64 = headerB64, BodyB64 = bodyB64 });
            var content = FromBase64Url(response.ContentBase64Url ?? string.Empty);
            if (!response.IsSuccess)
            {
                throw new Exception(content);
            }

            return content;
        }

        public async Task<T> PerformPOSSystemAPIRequest<T>(Activity activity, POSSystemAPIRequest request) => JsonConvert.DeserializeObject<T>(await PerformPOSSystemAPIRequest(activity, request))!;

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
