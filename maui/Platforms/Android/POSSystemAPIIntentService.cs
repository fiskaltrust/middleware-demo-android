using Android.App;
using Android.Content;
using Android.OS;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Demo
{
    public class POSSystemAPIIntentService
    {
        private const string POSSystemAPIClassName = "eu.fiskaltrust.androidlauncher.PosSystemAPI";

        private Dictionary<string, string> _headers => new Dictionary<string, string>
                {
                    { "x-cashbox-id", _cashBoxId.ToString() },
                    { "x-cashbox-accesstoken", _accessToken },
                };

        private Guid _cashBoxId;
        private string _accessToken;
        private readonly bool _useBoundService;

        public POSSystemAPIIntentService(Guid cashBoxId, string accessToken, bool useBoundService = false)
        {
            _cashBoxId = cashBoxId;
            _accessToken = accessToken;
            _useBoundService = useBoundService;
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

            PosSystemApiResponse response;
            if (_useBoundService)
            {
                response = await SendViaBoundServiceAsync(activity.ApplicationContext!, request.Method, request.Path, headerB64, bodyB64);
            }
            else
            {
                response = await SendViaActivityAsync(activity, request.Method, request.Path, headerB64, bodyB64);
            }

            var content = FromBase64Url(response.ContentBase64Url ?? string.Empty);
            if (!response.IsSuccess)
            {
                throw new Exception(content);
            }

            return JsonConvert.DeserializeObject<T>(content)!;
        }

        private async Task<PosSystemApiResponse> SendViaActivityAsync(Activity activity, string method, string path, string headerB64, string? bodyB64)
        {
            var intent = new Intent();
            intent.SetClassName(PosSystemApiServiceContract.LauncherPackage, POSSystemAPIClassName);
            intent.PutExtra(PosSystemApiServiceContract.KeyMethod, method);
            intent.PutExtra(PosSystemApiServiceContract.KeyPath, path);
            intent.PutExtra(PosSystemApiServiceContract.KeyHeaderJsonBase64Url, headerB64);
            if (bodyB64 != null)
            {
                intent.PutExtra(PosSystemApiServiceContract.KeyBodyBase64Url, bodyB64);
            }

            var responseIntent = await SarAwaiter.StartForResultAsync(activity, intent);
            return new PosSystemApiResponse
            {
                StatusCode = responseIntent.GetStringExtra(PosSystemApiServiceContract.KeyStatusCode) ?? "500",
                ContentBase64Url = responseIntent.GetStringExtra(PosSystemApiServiceContract.KeyContentBase64Url) ?? string.Empty,
                ContentTypeBase64Url = responseIntent.GetStringExtra(PosSystemApiServiceContract.KeyContentTypeBase64Url) ?? string.Empty
            };
        }

        private async Task<PosSystemApiResponse> SendViaBoundServiceAsync(Context context, string method, string path, string headerB64, string? bodyB64)
        {
            using var connection = new BoundServiceConnection(context);
            var serviceMessenger = await connection.BindAsync();

            var correlationId = Guid.NewGuid().ToString();
            var replyTask = connection.WaitForReplyAsync(correlationId);

            var msg = Message.Obtain() ?? throw new InvalidOperationException("Could not allocate Android Message.");
            msg.What = PosSystemApiServiceContract.MsgRequest;
            msg.ReplyTo = connection.ClientMessenger;
            msg.Data = new Bundle();
            msg.Data.PutString(PosSystemApiServiceContract.KeyCorrelationId, correlationId);
            msg.Data.PutString(PosSystemApiServiceContract.KeyMethod, method);
            msg.Data.PutString(PosSystemApiServiceContract.KeyPath, path);
            msg.Data.PutString(PosSystemApiServiceContract.KeyHeaderJsonBase64Url, headerB64);
            if (!string.IsNullOrEmpty(bodyB64))
            {
                msg.Data.PutString(PosSystemApiServiceContract.KeyBodyBase64Url, bodyB64);
            }

            serviceMessenger.Send(msg);
            var completedTask = await Task.WhenAny(replyTask, Task.Delay(TimeSpan.FromSeconds(30)));
            if (completedTask != replyTask)
            {
                throw new TimeoutException("Timed out waiting for PosSystemAPIService reply.");
            }

            return await replyTask;
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

        private sealed class BoundServiceConnection : Java.Lang.Object, IServiceConnection, IDisposable
        {
            private readonly Context _context;
            private readonly TaskCompletionSource<Messenger> _bindCompletion = new TaskCompletionSource<Messenger>();
            private readonly Dictionary<string, TaskCompletionSource<PosSystemApiResponse>> _pendingReplies = new Dictionary<string, TaskCompletionSource<PosSystemApiResponse>>();
            private readonly object _sync = new object();

            private readonly Messenger _clientMessenger;
            private bool _isBound;

            public BoundServiceConnection(Context context)
            {
                _context = context;
                _clientMessenger = new Messenger(new ServiceReplyHandler(msg => HandleReply(msg)));
            }

            public Messenger ClientMessenger => _clientMessenger;

            public Task<Messenger> BindAsync()
            {
                var intent = new Intent();
                intent.SetClassName(PosSystemApiServiceContract.LauncherPackage, PosSystemApiServiceContract.ServiceClass);

                _isBound = _context.BindService(intent, this, Bind.AutoCreate);
                if (!_isBound)
                {
                    throw new InvalidOperationException($"Could not bind to {PosSystemApiServiceContract.ServiceClass}.");
                }

                return _bindCompletion.Task;
            }

            public Task<PosSystemApiResponse> WaitForReplyAsync(string correlationId)
            {
                var tcs = new TaskCompletionSource<PosSystemApiResponse>();
                lock (_sync)
                {
                    _pendingReplies[correlationId] = tcs;
                }
                return tcs.Task;
            }

            public void OnServiceConnected(ComponentName? name, IBinder? service)
            {
                _bindCompletion.TrySetResult(new Messenger(service));
            }

            public void OnServiceDisconnected(ComponentName? name)
            {
                _bindCompletion.TrySetException(new InvalidOperationException("PosSystemAPIService disconnected."));
            }

            public new void Dispose()
            {
                if (_isBound)
                {
                    _context.UnbindService(this);
                    _isBound = false;
                }

                lock (_sync)
                {
                    foreach (var pending in _pendingReplies.Values)
                    {
                        pending.TrySetException(new InvalidOperationException("Request was cancelled because service connection was disposed."));
                    }
                    _pendingReplies.Clear();
                }
            }

            private void HandleReply(Message msg)
            {
                if (msg.What != PosSystemApiServiceContract.MsgReply)
                {
                    return;
                }

                var data = msg.Data;
                var correlationId = data?.GetString(PosSystemApiServiceContract.KeyCorrelationId);
                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    return;
                }

                TaskCompletionSource<PosSystemApiResponse>? pending;
                lock (_sync)
                {
                    if (!_pendingReplies.TryGetValue(correlationId, out pending))
                    {
                        return;
                    }
                    _pendingReplies.Remove(correlationId);
                }

                pending.TrySetResult(new PosSystemApiResponse
                {
                    StatusCode = data?.GetString(PosSystemApiServiceContract.KeyStatusCode) ?? "500",
                    ContentBase64Url = data?.GetString(PosSystemApiServiceContract.KeyContentBase64Url) ?? string.Empty,
                    ContentTypeBase64Url = data?.GetString(PosSystemApiServiceContract.KeyContentTypeBase64Url) ?? string.Empty
                });
            }
        }

        private sealed class ServiceReplyHandler : Handler
        {
            private readonly Action<Message> _onMessage;

            public ServiceReplyHandler(Action<Message> onMessage) : base(Looper.MainLooper ?? throw new InvalidOperationException("Main looper not available."))
            {
                _onMessage = onMessage;
            }

            public override void HandleMessage(Message msg)
            {
                _onMessage(msg);
            }
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
