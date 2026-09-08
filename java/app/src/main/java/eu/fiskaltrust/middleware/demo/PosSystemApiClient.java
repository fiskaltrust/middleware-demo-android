package eu.fiskaltrust.middleware.demo;

import android.app.Activity;

import com.google.gson.Gson;

import java.util.LinkedHashMap;
import java.util.Map;

import eu.fiskaltrust.middleware.demo.transport.PosSystemTransport;
import eu.fiskaltrust.middleware.demo.transport.PosSystemTransportFactory;

public class PosSystemApiClient {

  private static final Gson gson = new Gson();

  private final String cashboxId;
  private final String accessToken;
  private final PosSystemTransport transport;

  public interface Callback {
    void onSuccess(String content);
    void onError(Exception error);
  }

  public PosSystemApiClient(String cashboxId, String accessToken, boolean useBoundService) {
    this.cashboxId = cashboxId;
    this.accessToken = accessToken;
    this.transport = PosSystemTransportFactory.getInstance(useBoundService);
  }

  public void echo(Activity activity, String operationId, String message, Callback callback) {
    Map<String, String> body = new LinkedHashMap<>();
    body.put("Message", message);
    send(activity, "POST", "/v2/echo", authHeaders(operationId), gson.toJson(body), callback);
  }

  public void sign(Activity activity, String operationId, String receiptRequestJson, Callback callback) {
    send(activity, "POST", "/v2/sign", authHeaders(operationId), receiptRequestJson, callback);
  }

  public void pair(Activity activity, String pinRequestJson, Callback callback) {
    send(activity, "POST", "/v2/pair", new LinkedHashMap<>(), pinRequestJson, callback);
  }

  private Map<String, String> authHeaders(String operationId) {
    Map<String, String> headers = new LinkedHashMap<>();
    headers.put("x-cashbox-id", cashboxId);
    headers.put("x-cashbox-accesstoken", accessToken);
    headers.put("x-operation-id", operationId);
    return headers;
  }

  private void send(Activity activity, String method, String path, Map<String, String> headers, String body, Callback callback) {
    transport.send(activity, method, path, headers, body, new PosSystemTransport.Callback() {
      @Override
      public void onSuccess(String content) {
        callback.onSuccess(content);
      }

      @Override
      public void onError(Exception error) {
        callback.onError(error);
      }
    });
  }
}
