using Android.Content;
using Android.OS;
using fiskaltrust.AndroidLauncher.AndroidService;
using Platform = Microsoft.Maui.ApplicationModel.Platform;

namespace fiskaltrust.Middleware.Demo.Services
{
    // Communicates with the fiskaltrust PosSystemAPIService via Android's bound service / Messenger IPC mechanism.
    // Each request is packed into an Android Message and sent to the service; the response arrives
    // asynchronously on a reply Messenger that we pass along with the request.
    public class BoundServiceTransport : IPosSystemTransport
    {
        // Manages the connection (binding) to the possystemapi service. Reused across requests
        // so we only bind once and rebind automatically if the connection is lost.
        private readonly ServiceBinding _binding = new();

        // The caller controls how long to wait via the cancellation token
        // (e.g. by passing a token from a CancellationTokenSource with a timeout).
        public async Task<PosSystemApiResponse> SendAsync(PosSystemApiRequest request, CancellationToken cancellationToken = default)
        {
            // An Android Context is required to bind to the service.
            var context = Platform.CurrentActivity?.ApplicationContext
                ?? throw new InvalidOperationException("Current Android activity context is not available.");

            // The handler that will receive the service's reply message.
            var reply = new ReplyHandler();

            // Build the request message. 'What' identifies the message type defined by the service contract.
            var msg = Message.Obtain() ?? throw new InvalidOperationException("Could not allocate Android Message.");
            msg.What = PosSystemApiServiceContract.MsgRequest;
            // 'ReplyTo' tells the service where to send its response.
            msg.ReplyTo = new Messenger(new Handler(Looper.MainLooper!, reply));
            // The request itself is described HTTP-style: method, path, headers, and an optional body.
            // Headers and body are transferred as base64url-encoded strings;
            // PosSystemApiRequest takes care of the encoding.
            msg.Data = new Bundle();
            msg.Data.PutString(PosSystemApiServiceContract.KeyMethod, request.Method);
            msg.Data.PutString(PosSystemApiServiceContract.KeyPath, request.Path);
            msg.Data.PutString(PosSystemApiServiceContract.KeyHeaderJsonBase64Url, request.HeadersBase64Url);
            if (!string.IsNullOrEmpty(request.BodyBase64Url))
            {
                msg.Data.PutString(PosSystemApiServiceContract.KeyBodyBase64Url, request.BodyBase64Url);
            }

            // Send the message to the service (binding first if necessary) ...
            await _binding.SendAsync(context, msg, cancellationToken);

            // ... and wait for the reply handler to receive the response.
            return await reply.Completion.Task.WaitAsync(cancellationToken);
        }

        // Holds the bound connection to the possystemapi service and exposes it as a Messenger.
        // Implements IServiceConnection to receive Android's connect/disconnect callbacks.
        private sealed class ServiceBinding : Java.Lang.Object, IServiceConnection
        {
            private readonly Lock _lock = new();
            private Context? _context;
            // Completes once the service is connected; awaiting it lets callers wait for the binding.
            private TaskCompletionSource<Messenger>? _service;

            public async Task SendAsync(Context context, Message msg, CancellationToken cancellationToken)
            {
                // Wait until the service is bound (or reuse the existing binding).
                var service = await GetServiceAsync(context).WaitAsync(cancellationToken);

                try
                {
                    service.Send(msg);
                }
                catch
                {
                    // The binder is most likely dead; reset so the next send rebinds.
                    lock( _lock)
                    {
                        Reset(new InvalidOperationException("PosSystemAPIService binder is dead."));
                    }
                    throw;
                }
            }

            private Task<Messenger> GetServiceAsync(Context context)
            {
                lock (_lock)
                {
                    // Bind only if we are not already bound or in the process of binding.
                    if (_service == null)
                    {
                        _service = new TaskCompletionSource<Messenger>(TaskCreationOptions.RunContinuationsAsynchronously);
                        _context = context;

                        // Explicit intent targeting the fiskaltrust Android Launcher's PosSystemAPIService
                        // by package and class name.
                        var intent = new Intent();
                        intent.SetClassName(PosSystemApiServiceContract.LauncherPackage, PosSystemApiServiceContract.ServiceClass);

                        // Bind.AutoCreate starts the service if it isn't running yet.
                        if (!context.BindService(intent, this, Bind.AutoCreate))
                        {
                            Reset();
                            throw new InvalidOperationException($"Could not bind to {PosSystemApiServiceContract.ServiceClass}.");
                        }
                    }

                    return _service.Task;
                }
            }


            // Called by Android when the binding succeeds; wrap the binder in a Messenger for sending messages.
            public void OnServiceConnected(ComponentName? name, IBinder? service)
            {
                lock (_lock)
                {
                    _service?.TrySetResult(new Messenger(service));
                }
            }

            // Called by Android when the connection is lost (e.g. the service process crashed).
            public void OnServiceDisconnected(ComponentName? name)
            {
                lock (_lock)
                {
                    Reset(new InvalidOperationException("PosSystemAPIService disconnected."));
                }
            }

            // Clears the current binding so the next send triggers a fresh bind.
            private void Reset(Exception? error = null)
            {
                if (error != null)
                {
                    _service?.TrySetException(error);
                }
                _service = null;

                try
                {
                    _context?.UnbindService(this);
                }
                catch (Java.Lang.IllegalArgumentException)
                {
                    // Already unbound.
                }
                _context = null;
            }
        }

        // Receives the service's reply message and converts it into a PosSystemApiResponse.
        private sealed class ReplyHandler : Java.Lang.Object, Handler.ICallback
        {
            // Completes when the reply arrives; awaited by SendAsync above.
            public TaskCompletionSource<PosSystemApiResponse> Completion { get; } = new TaskCompletionSource<PosSystemApiResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool HandleMessage(Message msg)
            {
                // Ignore anything that is not a reply message from the service.
                if (msg.What != PosSystemApiServiceContract.MsgReply)
                {
                    return false;
                }

                // The reply mirrors an HTTP response: status code, content, and content type.
                // Content and content type arrive base64url-encoded; PosSystemApiResponse decodes them.
                var data = msg.Data;
                Completion.TrySetResult(new PosSystemApiResponse
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
