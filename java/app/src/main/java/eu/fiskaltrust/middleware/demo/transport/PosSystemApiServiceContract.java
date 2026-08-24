package eu.fiskaltrust.middleware.demo.transport;

public final class PosSystemApiServiceContract {

  public static final String LAUNCHER_PACKAGE = "eu.fiskaltrust.androidlauncher";
  public static final String ACTIVITY_CLASS = "eu.fiskaltrust.androidlauncher.PosSystemAPI";
  public static final String SERVICE_CLASS = "eu.fiskaltrust.androidlauncher.PosSystemAPIService";

  public static final int MSG_REQUEST = 1;
  public static final int MSG_REPLY = 2;

  public static final String KEY_METHOD = "Method";
  public static final String KEY_PATH = "Path";
  public static final String KEY_HEADER_JSON_BASE64URL = "HeaderJsonObjectBase64Url";
  public static final String KEY_BODY_BASE64URL = "BodyBase64Url";

  public static final String KEY_STATUS_CODE = "StatusCode";
  public static final String KEY_CONTENT_BASE64URL = "ContentBase64Url";

  private PosSystemApiServiceContract() { }
}
