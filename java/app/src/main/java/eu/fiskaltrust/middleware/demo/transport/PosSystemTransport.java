package eu.fiskaltrust.middleware.demo.transport;

import android.app.Activity;

import java.util.Map;

public interface PosSystemTransport {

  interface Callback {
    void onSuccess(String content);
    void onError(Exception error);
  }

  void send(Activity activity, String method, String path, Map<String, String> headers, String body, Callback callback);
}
