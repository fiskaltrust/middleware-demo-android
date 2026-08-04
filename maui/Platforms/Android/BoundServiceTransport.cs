using Android.Content;
using Android.OS;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.Middleware.Demo.Models;
using Platform = Microsoft.Maui.ApplicationModel.Platform;

namespace fiskaltrust.Middleware.Demo.Platforms.Android
{
    public class BoundServiceTransport : IPosSystemTransport
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        public async Task<PosSystemApiResponse> SendAsync(RequestInfo requestInfo)
        {
            var context = Platform.CurrentActivity?.ApplicationContext
                ?? throw new InvalidOperationException("Current Android activity context is not available.");

            using var call = new ServiceCall(requestInfo);
            return await call.ExecuteAsync(context, Timeout);
        }

        /// <summary>
        /// A single request/reply exchange: binds to the service, sends the request once connected
        /// (with a per-call ReplyTo messenger, so no correlation ID is needed), and unbinds when done.
        /// </summary>
        private sealed class ServiceCall : Java.Lang.Object, IServiceConnection, Handler.ICallback
        {
            private readonly RequestInfo _requestInfo;
            private readonly TaskCompletionSource<PosSystemApiResponse> _completion = new TaskCompletionSource<PosSystemApiResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

            public ServiceCall(RequestInfo requestInfo)
            {
                _requestInfo = requestInfo;
            }

            public async Task<PosSystemApiResponse> ExecuteAsync(Context context, TimeSpan timeout)
            {
                var intent = new Intent();
                intent.SetClassName(PosSystemApiServiceContract.LauncherPackage, PosSystemApiServiceContract.ServiceClass);

                if (!context.BindService(intent, this, Bind.AutoCreate))
                {
                    context.UnbindService(this);
                    throw new InvalidOperationException($"Could not bind to {PosSystemApiServiceContract.ServiceClass}.");
                }

                try
                {
                    return await _completion.Task.WaitAsync(timeout);
                }
                finally
                {
                    context.UnbindService(this);
                }
            }

            public void OnServiceConnected(ComponentName? name, IBinder? service)
            {
                try
                {
                    var msg = Message.Obtain() ?? throw new InvalidOperationException("Could not allocate Android Message.");
                    msg.What = PosSystemApiServiceContract.MsgRequest;
                    msg.ReplyTo = new Messenger(new Handler(Looper.MainLooper!, this));
                    msg.Data = new Bundle();
                    msg.Data.PutString(PosSystemApiServiceContract.KeyMethod, _requestInfo.Method);
                    msg.Data.PutString(PosSystemApiServiceContract.KeyPath, _requestInfo.Path);
                    msg.Data.PutString(PosSystemApiServiceContract.KeyHeaderJsonBase64Url, _requestInfo.HeaderB64);
                    if (!string.IsNullOrEmpty(_requestInfo.BodyB64))
                    {
                        msg.Data.PutString(PosSystemApiServiceContract.KeyBodyBase64Url, _requestInfo.BodyB64);
                    }

                    new Messenger(service).Send(msg);
                }
                catch (Exception ex)
                {
                    _completion.TrySetException(ex);
                }
            }

            public void OnServiceDisconnected(ComponentName? name)
            {
                _completion.TrySetException(new InvalidOperationException("PosSystemAPIService disconnected."));
            }

            public bool HandleMessage(Message msg)
            {
                if (msg.What != PosSystemApiServiceContract.MsgReply)
                {
                    return false;
                }

                var data = msg.Data;
                _completion.TrySetResult(new PosSystemApiResponse
                {
                    StatusCode = data?.GetString(PosSystemApiServiceContract.KeyStatusCode) ?? "500",
                    ContentBase64Url = data?.GetString(PosSystemApiServiceContract.KeyContentBase64Url) ?? string.Empty,
                    ContentTypeBase64Url = data?.GetString(PosSystemApiServiceContract.KeyContentTypeBase64Url) ?? string.Empty
                });
                return true;
            }
        }
    }
}
