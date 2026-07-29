using fiskaltrust.Middleware.Demo.Models;

namespace fiskaltrust.Middleware.Demo.Platforms.Android
{
    public interface IPosSystemTransport
    {
        public Task<PosSystemApiResponse> SendAsync(RequestInfo requestInfo);
    }
}
