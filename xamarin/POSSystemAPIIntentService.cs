using Android.App;
using Android.Content;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace fiskaltrust.Middleware.Demo
{
    public class POSSystemAPIIntentService
    {
        private const string ResponseAction = "com.yourapp.FISKALTRUST_RESPONSE";
        private const string PackageName = "eu.fiskaltrust.androidlauncher.http";
        private const string POSSystemAPIClassName = "eu.fiskaltrust.androidlauncher.http.POSSystemAPI";

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
                Endpoint = "/v2/echo",
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
                Endpoint = "/v2/sign",
                Headers = _headers,
                Body = JsonConvert.SerializeObject(receipt),
                ResponseAction = ResponseAction,
                RequestId = new Guid().ToString()
            };
            return PerformPOSSystemAPIIntent<ReceiptResponse>(activity, request);
        }

        private static async Task<T> PerformPOSSystemAPIIntent<T>(Activity activity, POSSystemAPIRequest request)
        {
            var intent = new Intent(Intent.ActionSend);
            intent.SetComponent(new ComponentName(PackageName, POSSystemAPIClassName));
            intent.PutExtra("method", request.Method);
            intent.PutExtra("headers", JsonConvert.SerializeObject(request.Headers));
            intent.PutExtra("endpoint", request.Endpoint);
            intent.PutExtra("body", request.Body);
            intent.PutExtra("responseAction", request.ResponseAction);
            intent.PutExtra("requestId", request.RequestId);
            var responseIntent = await SarAwaiter.StartForResultAsync(activity, intent);
 
            var body = responseIntent.GetStringExtra("body");
            return JsonConvert.DeserializeObject<T>(body);
        }
    }
}