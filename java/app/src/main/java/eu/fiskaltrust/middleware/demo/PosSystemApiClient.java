package eu.fiskaltrust.middleware.demo;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;

import com.google.gson.Gson;

import java.util.LinkedHashMap;
import java.util.Map;
import java.util.UUID;

import eu.fiskaltrust.middleware.util.Base64UrlUtil;

public class PosSystemApiClient {

  private static final String LAUNCHER_PACKAGE = "eu.fiskaltrust.androidlauncher";
  private static final String POS_SYSTEM_API_CLASS = "eu.fiskaltrust.androidlauncher.PosSystemAPI";

  private static final Gson gson = new Gson();

  private final String cashboxId;
  private final String accessToken;

  public PosSystemApiClient(String cashboxId, String accessToken) {
    this.cashboxId = cashboxId;
    this.accessToken = accessToken;
  }

  public interface Callback {
    void onSuccess(String content);
    void onError(Exception error);
  }

  public void echo(Activity activity, String message, Callback callback) {
    Map<String, String> body = new LinkedHashMap<>();
    body.put("Message", message);
    send(activity, "POST", "/v2/echo", gson.toJson(body), callback);
  }

  public void sign(Activity activity, String receiptRequestJson, Callback callback) {
    send(activity, "POST", "/v2/sign", receiptRequestJson, callback);
  }

  private void send(Activity activity, String method, String path, String body, Callback callback) {
    Map<String, String> headers = new LinkedHashMap<>();
    headers.put("x-cashbox-id", cashboxId);
    headers.put("x-cashbox-accesstoken", accessToken);
    headers.put("x-operation-id", UUID.randomUUID().toString());

    Intent intent = new Intent();
    intent.setClassName(LAUNCHER_PACKAGE, POS_SYSTEM_API_CLASS);
    intent.putExtra("Method", method);
    intent.putExtra("Path", path);
    intent.putExtra("HeaderJsonObjectBase64Url", Base64UrlUtil.encode(gson.toJson(headers)));
    if (body != null) {
      intent.putExtra("BodyBase64Url", Base64UrlUtil.encode(body));
    }

    try {
      SarAwaiter.startForResult(activity, intent, (resultCode, data) -> {
        if (resultCode != Activity.RESULT_OK || data == null) {
          callback.onError(new IllegalStateException("PosSystemAPI request failed or was cancelled"));
          return;
        }

        String statusCode = data.getStringExtra("StatusCode");
        String contentBase64Url = data.getStringExtra("ContentBase64Url");
        String content = contentBase64Url != null ? Base64UrlUtil.decode(contentBase64Url) : "";

        if (statusCode == null || !statusCode.startsWith("2")) {
          callback.onError(new IllegalStateException("PosSystemAPI returned status " + statusCode + ": " + content));
          return;
        }

        callback.onSuccess(content);
      });
    } catch (ActivityNotFoundException e) {
      callback.onError(e);
    }
  }
}
