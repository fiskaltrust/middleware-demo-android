namespace fiskaltrust.Middleware.Demo.Services
{
    public static class PosSystemTransportFactory
    {
        public static IPosSystemTransport GetInstance(bool useBoundService)
        {
            if (useBoundService)
            {
                return new BoundServiceTransport();
            }
            else {
                return new ActivityTransport();
            }

        }
    }
}
