package eu.fiskaltrust.middleware.demo;

import android.app.Activity;
import android.content.Intent;

import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

public final class SarAwaiter {

  public interface ResultListener {
    void onResult(int resultCode, Intent data);
  }

  private static final AtomicInteger nextRequestCode = new AtomicInteger(2000);
  private static final ConcurrentHashMap<Integer, ResultListener> pending = new ConcurrentHashMap<>();

  private SarAwaiter() { }

  public static void startForResult(Activity activity, Intent intent, ResultListener listener) {
    int requestCode = nextRequestCode.incrementAndGet();
    pending.put(requestCode, listener);
    activity.startActivityForResult(intent, requestCode);
  }

  static boolean deliverResult(int requestCode, int resultCode, Intent data) {
    ResultListener listener = pending.remove(requestCode);
    if (listener == null) {
      return false;
    }
    listener.onResult(resultCode, data);
    return true;
  }
}
