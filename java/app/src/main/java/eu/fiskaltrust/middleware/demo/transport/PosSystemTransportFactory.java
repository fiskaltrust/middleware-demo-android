package eu.fiskaltrust.middleware.demo.transport;

public final class PosSystemTransportFactory {

  private PosSystemTransportFactory() { }

  public static PosSystemTransport getInstance(boolean useBoundService) {
    return useBoundService ? new BoundServiceTransport() : new ActivityTransport();
  }
}
