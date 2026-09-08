package eu.fiskaltrust.middleware.demo;

import android.content.Context;
import android.content.SharedPreferences;

public class SettingsStore {

  private static final String PREFS_NAME = "fiskaltrust_demo_settings";

  private static final String PROTOCOL_KEY = "selected_protocol";
  private static final String CASHBOX_ID_KEY = "cashbox_id";
  private static final String ACCESS_TOKEN_KEY = "access_token";

  public static final String PROTOCOL_INTENT_ACTIVITY = "intent-activity";
  public static final String PROTOCOL_SERVICE_IPC = "service-ipc";

  private static final String DEFAULT_CASHBOX_ID = "57dd5e04-49b3-4d81-862f-e5ac054117a8";
  private static final String DEFAULT_ACCESS_TOKEN = "BEkCPEpqvzzSyvu1dUCyGXkDRg+fLkVZhJ+aHaocr0VZ+aylUkjg2NVjIzqtzy1891yUOHK8SiYw/Ap/p38Yyx0=";

  private final SharedPreferences prefs;

  public SettingsStore(Context context) {
    prefs = context.getApplicationContext().getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
  }

  public String getSelectedProtocol() {
    return prefs.getString(PROTOCOL_KEY, PROTOCOL_INTENT_ACTIVITY);
  }

  public void setSelectedProtocol(String protocol) {
    prefs.edit().putString(PROTOCOL_KEY, protocol).apply();
  }

  public String getCashboxId() {
    return prefs.getString(CASHBOX_ID_KEY, DEFAULT_CASHBOX_ID);
  }

  public void setCashboxId(String cashboxId) {
    prefs.edit().putString(CASHBOX_ID_KEY, cashboxId).apply();
  }

  public String getAccessToken() {
    return prefs.getString(ACCESS_TOKEN_KEY, DEFAULT_ACCESS_TOKEN);
  }

  public void setAccessToken(String accessToken) {
    prefs.edit().putString(ACCESS_TOKEN_KEY, accessToken).apply();
  }
}
