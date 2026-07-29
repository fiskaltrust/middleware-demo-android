using Android.Content;
using Android.OS;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.Middleware.Demo.Models;
using Platform = Microsoft.Maui.ApplicationModel.Platform;

namespace fiskaltrust.Middleware.Demo.Platforms.Android
{
    public class BoundServiceTransport : IPosSystemTransport
    {
        public async Task<PosSystemApiResponse> SendAsync(RequestInfo requestInfo)
        {
            using var connection = new BoundServiceConnection(Platform.CurrentActivity!.ApplicationContext!);
            var serviceMessenger = await connection.BindAsync();

            var correlationId = Guid.NewGuid().ToString();
            var replyTask = connection.WaitForReplyAsync(correlationId);

            var msg = Message.Obtain() ?? throw new InvalidOperationException("Could not allocate Android Message.");
            msg.What = PosSystemApiServiceContract.MsgRequest;
            msg.ReplyTo = connection.ClientMessenger;
            msg.Data = new Bundle();
            msg.Data.PutString(PosSystemApiServiceContract.KeyCorrelationId, correlationId);
            msg.Data.PutString(PosSystemApiServiceContract.KeyMethod, requestInfo.Method);
            msg.Data.PutString(PosSystemApiServiceContract.KeyPath, requestInfo.Path);
            msg.Data.PutString(PosSystemApiServiceContract.KeyHeaderJsonBase64Url, requestInfo.HeaderB64);
            if (!string.IsNullOrEmpty(requestInfo.BodyB64))
            {
                msg.Data.PutString(PosSystemApiServiceContract.KeyBodyBase64Url, requestInfo.BodyB64);
            }

            serviceMessenger.Send(msg);
            var completedTask = await Task.WhenAny(replyTask, Task.Delay(TimeSpan.FromSeconds(30)));
            if (completedTask != replyTask)
            {
                throw new TimeoutException("Timed out waiting for PosSystemAPIService reply.");
            }

            return await replyTask;
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
