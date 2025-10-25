using Android.App;
using Android.Content;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace fiskaltrust.Middleware.Demo
{
    public class POSSystemAPIIntentService
    {
        private const string ResponseAction = "com.yourapp.FISKALTRUST_RESPONSE";
        private const string PackageName = "eu.fiskaltrust.androidlauncher";
        private const string POSSystemAPIClassName = "eu.fiskaltrust.androidlauncher.PosSystemAPI";

        private Dictionary<string, string> _headers = new Dictionary<string, string>();

        public POSSystemAPIIntentService(Guid cashBoxId, string accessToken)
        {
            _headers = new Dictionary<string, string>
                {
                    { "x-cashbox-id", cashBoxId.ToString() },
                    { "x-cashbox-accesstoken", accessToken },
                };
        }

        public Task<EchoResponse> SendEchoRequest(Activity activity, EchoRequest echoRequest)
        {
            var request = new POSSystemAPIRequest
            {
                Method = "POST",
                Path = "/v2/echo",
                Headers = _headers,
                Body = JsonConvert.SerializeObject(echoRequest),
                ResponseAction = ResponseAction,
                RequestId = new Guid().ToString()
            };
            return PerformPOSSystemAPIIntent<EchoResponse>(activity, request);
        }

        public Task<ReceiptResponse> SignReceipt(Activity activity, ReceiptRequest receipt)
        {
            var request = new POSSystemAPIRequest
            {
                Method = "POST",
                Path = "/v2/sign",
                Headers = _headers,
                Body = JsonConvert.SerializeObject(receipt),
                ResponseAction = ResponseAction,
                RequestId = new Guid().ToString()
            };
            return PerformPOSSystemAPIIntent<ReceiptResponse>(activity, request);
        }

        public async Task<T> PerformPOSSystemAPIIntent<T>(Activity activity, POSSystemAPIRequest request)
        {
            var headersJson = JsonConvert.SerializeObject(request.Headers);
            var headerB64 = ToBase64Url(headersJson);
            var bodyB64 = request.Body != null ? ToBase64Url(request.Body) : null;
            var intent = new Intent();
            intent.SetClassName("eu.fiskaltrust.androidlauncher",
                "eu.fiskaltrust.androidlauncher.PosSystemAPI");
            intent.PutExtra("Method", request.Method);
            intent.PutExtra("Path", request.Path);
            intent.PutExtra("HeaderJsonObjectBase64Url", headerB64);
            if (bodyB64 != null)
            {
                intent.PutExtra("BodyBase64Url", bodyB64);
            }

            var responseIntent = await SarAwaiter.StartForResultAsync(activity, intent);
            var statusCode = responseIntent.GetStringExtra("StatusCode") ?? "500";
            var contentB64 = responseIntent.GetStringExtra("ContentBase64Url") ?? "";
            var contentTypeB64 = responseIntent.GetStringExtra("ContentTypeBase64Url") ?? "";
            var content = FromBase64Url(contentB64);
            var contentType = FromBase64Url(contentTypeB64);
            if (statusCode != "200" && statusCode != "201")
            {
                throw new Exception(content);
            }


            // Decode Base64URL
     

            Android.Util.Log.Info("PosSystemAPI", $"Status: {statusCode}, Type: {contentType}");
            Android.Util.Log.Info("PosSystemAPI", $"Response: {content}");
            return JsonConvert.DeserializeObject<T>(content);
        }

        private string ToBase64Url(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
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