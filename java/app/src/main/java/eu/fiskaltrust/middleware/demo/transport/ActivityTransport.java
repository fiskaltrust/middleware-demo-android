package eu.fiskaltrust.middleware.demo.transport;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;

import com.google.gson.Gson;

import java.util.Map;

import eu.fiskaltrust.middleware.demo.SarAwaiter;
import eu.fiskaltrust.middleware.util.Base64UrlUtil;

public class ActivityTransport implements PosSystemTransport {

  private static final Gson gson = new Gson();

  @Override
  public void send(Activity activity, String method, String path, Map<String, String> headers, String body, Callback callback) {
    Intent intent = new Intent();
    intent.setClassName(PosSystemApiServiceContract.LAUNCHER_PACKAGE, PosSystemApiServiceContract.ACTIVITY_CLASS);
    intent.putExtra(PosSystemApiServiceContract.KEY_METHOD, method);
    intent.putExtra(PosSystemApiServiceContract.KEY_PATH, path);
    intent.putExtra(PosSystemApiServiceContract.KEY_HEADER_JSON_BASE64URL, Base64UrlUtil.encode(gson.toJson(headers)));
    if (body != null) {
      intent.putExtra(PosSystemApiServiceContract.KEY_BODY_BASE64URL, Base64UrlUtil.encode(body));
    }

    try {
      SarAwaiter.startForResult(activity, intent, (resultCode, data) -> {
        // This listener runs synchronously inside Activity.onActivityResult; an uncaught
        // exception here would crash the app while the framework delivers the result.
        try {
          if (resultCode != Activity.RESULT_OK || data == null) {
            callback.onError(new IllegalStateException("PosSystemAPI request failed or was cancelled"));
            return;
          }

          String statusCode = data.getStringExtra(PosSystemApiServiceContract.KEY_STATUS_CODE);
          String contentBase64Url = data.getStringExtra(PosSystemApiServiceContract.KEY_CONTENT_BASE64URL);
          String content = contentBase64Url != null ? Base64UrlUtil.decode(contentBase64Url) : "";

          if (statusCode == null || !statusCode.startsWith("2")) {
            callback.onError(new IllegalStateException("PosSystemAPI returned status " + statusCode + ": " + content));
            return;
          }

          callback.onSuccess(content);
        } catch (RuntimeException e) {
          callback.onError(e);
        }
      });
    } catch (ActivityNotFoundException e) {
      callback.onError(e);
    }
  }
}
