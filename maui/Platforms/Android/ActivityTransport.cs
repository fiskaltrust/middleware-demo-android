using Android.Content;
using fiskaltrust.AndroidLauncher.AndroidService;
using Platform = Microsoft.Maui.ApplicationModel.Platform;

namespace fiskaltrust.Middleware.Demo.Platforms.Android
{
    // Communicates with the fiskaltrust middleware via Android's startActivityForResult mechanism.
    // Each request launches the launcher's PosSystemAPI activity with the request data as intent extras;
    // the response comes back as the activity result intent. This briefly brings the launcher activity
    // to the foreground, whereas BoundServiceTransport communicates invisibly in the background.
    public class ActivityTransport : IPosSystemTransport
    {
        // Fully qualified class name of the launcher activity that handles POS system API requests.
        private const string PosSystemApiClassName = "eu.fiskaltrust.androidlauncher.PosSystemAPI";

        public async Task<PosSystemApiResponse> SendAsync(PosSystemApiRequest request, CancellationToken cancellationToken = default)
        {
            // Explicit intent targeting the fiskaltrust Android Launcher's PosSystemAPI activity
            // by package and class name.
            var intent = new Intent();
            intent.SetClassName(PosSystemApiServiceContract.LauncherPackage, PosSystemApiClassName);
            // The request is described HTTP-style as intent extras: method, path, headers, and an optional body.
            // Headers and body are transferred as base64url-encoded strings;
            // PosSystemApiRequest class takes care of the encoding.
            intent.PutExtra(PosSystemApiServiceContract.KeyMethod, request.Method);
            intent.PutExtra(PosSystemApiServiceContract.KeyPath, request.Path);
            intent.PutExtra(PosSystemApiServiceContract.KeyHeaderJsonBase64Url, request.HeadersBase64Url);
            if (request.Body != null)
            {
                intent.PutExtra(PosSystemApiServiceContract.KeyBodyBase64Url, request.BodyBase64Url);
            }

            // Start the launcher activity for a result and wait for it to finish.
            // The caller controls how long to wait via the cancellation token.
            var responseIntent = await SarAwaiter.StartForResultAsync(Platform.CurrentActivity!, intent).WaitAsync(cancellationToken);

            // The result intent mirrors an HTTP response: status code, content, and content type.
            // Content and content type arrive base64url-encoded; PosSystemApiResponse decodes them.
            return new PosSystemApiResponse
            {
                StatusCode = responseIntent.GetStringExtra(PosSystemApiServiceContract.KeyStatusCode) ?? "500",
                ContentBase64Url = responseIntent.GetStringExtra(PosSystemApiServiceContract.KeyContentBase64Url) ?? string.Empty,
                ContentTypeBase64Url = responseIntent.GetStringExtra(PosSystemApiServiceContract.KeyContentTypeBase64Url) ?? string.Empty
            };
        }
    }
}
