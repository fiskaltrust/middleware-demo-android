package eu.fiskaltrust.middleware.demo.transport;

import android.app.Activity;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.os.Bundle;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.os.Message;
import android.os.Messenger;
import android.os.RemoteException;

import com.google.gson.Gson;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import eu.fiskaltrust.middleware.util.Base64UrlUtil;

public class BoundServiceTransport implements PosSystemTransport {

  private static final Gson gson = new Gson();

  private static final class PendingSend {
    final Message message;
    final Callback callback;

    PendingSend(Message message, Callback callback) {
      this.message = message;
      this.callback = callback;
    }
  }

  private final Object lock = new Object();
  private final List<PendingSend> pending = new ArrayList<>();
  private Context boundContext;
  private ServiceConnection connection;
  private Messenger service;

  @Override
  public void send(Activity activity, String method, String path, Map<String, String> headers, String body, Callback callback) {
    Bundle data = new Bundle();
    data.putString(PosSystemApiServiceContract.KEY_METHOD, method);
    data.putString(PosSystemApiServiceContract.KEY_PATH, path);
    data.putString(PosSystemApiServiceContract.KEY_HEADER_JSON_BASE64URL, Base64UrlUtil.encode(gson.toJson(headers)));
    if (body != null) {
      data.putString(PosSystemApiServiceContract.KEY_BODY_BASE64URL, Base64UrlUtil.encode(body));
    }

    Message message = Message.obtain();
    message.what = PosSystemApiServiceContract.MSG_REQUEST;
    message.setData(data);
    message.replyTo = new Messenger(new Handler(Looper.getMainLooper(), incoming -> handleReply(incoming, callback)));

    enqueue(activity.getApplicationContext(), new PendingSend(message, callback));
  }

  private boolean handleReply(Message incoming, Callback callback) {
    if (incoming.what != PosSystemApiServiceContract.MSG_REPLY) {
      return false;
    }
    Bundle data = incoming.getData();
    String statusCode = data != null ? data.getString(PosSystemApiServiceContract.KEY_STATUS_CODE) : null;
    String contentBase64Url = data != null ? data.getString(PosSystemApiServiceContract.KEY_CONTENT_BASE64URL) : null;
    String content = contentBase64Url != null ? Base64UrlUtil.decode(contentBase64Url) : "";

    if (statusCode == null || !statusCode.startsWith("2")) {
      callback.onError(new IllegalStateException("PosSystemAPIService returned status " + statusCode + ": " + content));
    } else {
      callback.onSuccess(content);
    }
    return true;
  }

  private void enqueue(Context context, PendingSend send) {
    synchronized (lock) {
      if (service != null) {
        trySend(context, send);
        return;
      }

      pending.add(send);
      if (connection == null) {
        bind(context);
      }
    }
  }

  private void trySend(Context context, PendingSend send) {
    try {
      synchronized (lock) {
        service.send(send.message);
      }
    } catch (RemoteException e) {
      reset(context);
      send.callback.onError(e);
    }
  }

  private void bind(Context context) {
    boundContext = context;
    connection = new ServiceConnection() {
      @Override
      public void onServiceConnected(ComponentName name, IBinder binder) {
        List<PendingSend> toSend;
        synchronized (lock) {
          service = new Messenger(binder);
          toSend = new ArrayList<>(pending);
          pending.clear();
        }
        for (PendingSend send : toSend) {
          trySend(context, send);
        }
      }

      @Override
      public void onServiceDisconnected(ComponentName name) {
        synchronized (lock) {
          service = null;
        }
      }
    };

    Intent intent = new Intent();
    intent.setClassName(PosSystemApiServiceContract.LAUNCHER_PACKAGE, PosSystemApiServiceContract.SERVICE_CLASS);
    boolean bound = context.bindService(intent, connection, Context.BIND_AUTO_CREATE);
    if (!bound) {
      List<PendingSend> failed;
      synchronized (lock) {
        connection = null;
        failed = new ArrayList<>(pending);
        pending.clear();
      }
      for (PendingSend send : failed) {
        send.callback.onError(new IllegalStateException("Could not bind to " + PosSystemApiServiceContract.SERVICE_CLASS));
      }
    }
  }

  private void reset(Context context) {
    ServiceConnection toUnbind;
    synchronized (lock) {
      service = null;
      toUnbind = connection;
      connection = null;
    }
    if (toUnbind != null) {
      try {
        context.unbindService(toUnbind);
      } catch (IllegalArgumentException ignored) {
        // already unbound
      }
    }
  }
}
