namespace fiskaltrust.Middleware.Demo.Platforms.Android
{
    public interface IPosSystemTransport
    {
        public Task<PosSystemApiResponse> SendAsync(PosSystemApiRequest requestInfo, CancellationToken cancellationToken = default);
    }
}
