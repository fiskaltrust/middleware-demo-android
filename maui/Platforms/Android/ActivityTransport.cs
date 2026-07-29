using Android.Content;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.Middleware.Demo.Models;
using Platform = Microsoft.Maui.ApplicationModel.Platform;

namespace fiskaltrust.Middleware.Demo.Platforms.Android
{
    public class ActivityTransport : IPosSystemTransport
    {
        private const string POSSystemAPIClassName = "eu.fiskaltrust.androidlauncher.PosSystemAPI";
        public async Task<PosSystemApiResponse> SendAsync(RequestInfo requestInfo)
        {
            var intent = new Intent();
            intent.SetClassName(PosSystemApiServiceContract.LauncherPackage, POSSystemAPIClassName);
            intent.PutExtra(PosSystemApiServiceContract.KeyMethod, requestInfo.Method);
            intent.PutExtra(PosSystemApiServiceContract.KeyPath, requestInfo.Path);
            intent.PutExtra(PosSystemApiServiceContract.KeyHeaderJsonBase64Url, requestInfo.HeaderB64);
            if (requestInfo.BodyB64 != null)
            {
                intent.PutExtra(PosSystemApiServiceContract.KeyBodyBase64Url, requestInfo.BodyB64);
            }

            var responseIntent = await SarAwaiter.StartForResultAsync(Platform.CurrentActivity!, intent);
            return new PosSystemApiResponse
            {
                StatusCode = responseIntent.GetStringExtra(PosSystemApiServiceContract.KeyStatusCode) ?? "500",
                ContentBase64Url = responseIntent.GetStringExtra(PosSystemApiServiceContract.KeyContentBase64Url) ?? string.Empty,
                ContentTypeBase64Url = responseIntent.GetStringExtra(PosSystemApiServiceContract.KeyContentTypeBase64Url) ?? string.Empty
            };
        }
    }
}
