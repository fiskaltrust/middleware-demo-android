namespace fiskaltrust.Middleware.Demo.Platforms.Android
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
