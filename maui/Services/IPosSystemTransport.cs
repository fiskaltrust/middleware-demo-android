namespace fiskaltrust.Middleware.Demo.Services
{
    public interface IPosSystemTransport
    {
        public Task<PosSystemApiResponse> SendAsync(PosSystemApiRequest requestInfo, CancellationToken cancellationToken = default);
    }
}
