package eu.fiskaltrust.middleware.demo;

import android.app.AlertDialog;
import android.content.Intent;
import android.os.Bundle;
import androidx.appcompat.app.AppCompatActivity;
import android.text.Editable;
import android.text.TextWatcher;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.RadioGroup;
import android.widget.TextView;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.google.gson.JsonParser;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.LinkedHashMap;
import java.util.Locale;
import java.util.Map;
import java.util.TimeZone;
import java.util.UUID;

import eu.fiskaltrust.middleware.demo.java.R;

public class MainActivity extends AppCompatActivity {

  private static final Gson gson = new Gson();
  private static final String POS_SYSTEM_ID = "d4a62055-ca6c-4372-ae4d-f835a88e4a5d";

  // Tab bar
  private View demoSection;
  private View settingsSection;
  private View aboutSection;

  // Demo tab
  private TextView txtCurrentProtocol;
  private Button btnRetryLastOperation;
  private Button btnSendEchoRequest;
  private Button btnRestartConfig;
  private TextView txtResult;
  private Button btnSendStartReceipt;
  private Button btnSendZeroReceipt;
  private TextView txtSpecialReceiptResult;
  private Button btnSendSignRequest;
  private TextView txtSignResult;

  // Settings tab
  private EditText entryPairPin;
  private Button btnPair;
  private View pairingStatus;
  private EditText entryCashboxId;
  private EditText entryAccessToken;
  private RadioGroup radioGroupProtocol;

  private SettingsStore settingsStore;

  private PosSystemApiClient posSystemApiClient;
  private String posSystemApiClientSignature;

  private LastOperation lastOperation;

  private static final class LastOperation {
    final String displayName;
    final Runnable retry;

    LastOperation(String displayName, Runnable retry) {
      this.displayName = displayName;
      this.retry = retry;
    }
  }

  @Override
  protected void onCreate(Bundle savedInstanceState) {
    super.onCreate(savedInstanceState);
    setContentView(R.layout.activity_main);

    settingsStore = new SettingsStore(this);

    bindViews();
    wireTabBar();
    wireDemoTab();
    wireSettingsTab();

    loadSettingsIntoUi();
    showTab(demoSection);
  }

  private void bindViews() {
    demoSection = findViewById(R.id.demoSection);
    settingsSection = findViewById(R.id.settingsSection);
    aboutSection = findViewById(R.id.aboutSection);

    txtCurrentProtocol = findViewById(R.id.txtCurrentProtocol);
    btnRetryLastOperation = findViewById(R.id.btnRetryLastOperation);
    btnSendEchoRequest = findViewById(R.id.btnSendEchoRequest);
    btnRestartConfig = findViewById(R.id.btnRestartConfig);
    txtResult = findViewById(R.id.txtResult);
    btnSendStartReceipt = findViewById(R.id.btnSendStartReceipt);
    btnSendZeroReceipt = findViewById(R.id.btnSendZeroReceipt);
    txtSpecialReceiptResult = findViewById(R.id.txtSpecialReceiptResult);
    btnSendSignRequest = findViewById(R.id.btnSendSignRequest);
    txtSignResult = findViewById(R.id.txtSignResult);

    entryPairPin = findViewById(R.id.entryPairPin);
    btnPair = findViewById(R.id.btnPair);
    pairingStatus = findViewById(R.id.pairingStatus);
    entryCashboxId = findViewById(R.id.entryCashboxId);
    entryAccessToken = findViewById(R.id.entryAccessToken);
    radioGroupProtocol = findViewById(R.id.radioGroupProtocol);
  }

  private void wireTabBar() {
    findViewById(R.id.tabDemo).setOnClickListener(v -> showTab(demoSection));
    findViewById(R.id.tabSettings).setOnClickListener(v -> showTab(settingsSection));
    findViewById(R.id.tabAbout).setOnClickListener(v -> showTab(aboutSection));
  }

  private void showTab(View section) {
    demoSection.setVisibility(section == demoSection ? View.VISIBLE : View.GONE);
    settingsSection.setVisibility(section == settingsSection ? View.VISIBLE : View.GONE);
    aboutSection.setVisibility(section == aboutSection ? View.VISIBLE : View.GONE);
    if (section == demoSection) {
      txtCurrentProtocol.setText(settingsStore.getSelectedProtocol().toUpperCase(Locale.US));
    }
  }

  @Override
  protected void onActivityResult(int requestCode, int resultCode, Intent data) {
    if (SarAwaiter.deliverResult(requestCode, resultCode, data)) {
      return;
    }
    super.onActivityResult(requestCode, resultCode, data);
  }

  // ================= Demo tab =================

  private void wireDemoTab() {
    btnSendEchoRequest.setOnClickListener(v -> {
      String time = new SimpleDateFormat("h:mm a", Locale.US).format(new Date());
      runEcho("Hello Android, it's " + time + "!", null, "Echo Request", false, btnSendEchoRequest);
    });

    btnRestartConfig.setOnClickListener(v -> new AlertDialog.Builder(this)
        .setTitle("Restart & Pull Config")
        .setMessage("This will restart the launcher and pull the latest configuration.\n\nContinue?")
        .setPositiveButton("Yes", (dialog, which) -> runEcho(null, "✅ Configuration refresh initiated (Intent)\n\n",
            "Restart & Pull Config", false, btnRestartConfig))
        .setNegativeButton("Cancel", null)
        .show());

    btnSendStartReceipt.setOnClickListener(v -> runSign(
        buildReceiptRequestJson(POS_SYSTEM_ID, "T1", "2020020120152812", "Receptionist", "System", 0x4445000100000003L),
        "Start Receipt", txtSpecialReceiptResult, false, btnSendStartReceipt));

    btnSendZeroReceipt.setOnClickListener(v -> runSign(
        buildReceiptRequestJson(POS_SYSTEM_ID, "T1", "2020020120152812", "Receptionist", "System", 0x4445000100000002L),
        "Zero Receipt", txtSpecialReceiptResult, false, btnSendZeroReceipt));

    btnSendSignRequest.setOnClickListener(v -> runSign(
        buildReceiptRequestJson(null, null, UUID.randomUUID().toString(), null, null, 0x4445000100000000L),
        "Sign Request", txtSignResult, false, btnSendSignRequest));

    btnRetryLastOperation.setOnClickListener(v -> {
      if (lastOperation != null) {
        lastOperation.retry.run();
      }
    });
  }

  private void runEcho(String message, String successPrefix, String displayName, boolean isRetry, Button pressedButton) {
    String operationId = UUID.randomUUID().toString();
    setOperationInProgress(pressedButton, true);
    client().echo(this, operationId, message, new PosSystemApiClient.Callback() {
      @Override
      public void onSuccess(String content) {
        setOperationInProgress(pressedButton, false);
        String text = (successPrefix != null ? successPrefix : "") + prettyJson(content);
        if (isRetry) {
          showAlert("Retry Result: " + displayName, text);
        } else {
          txtResult.setText(text);
          setLastOperation(displayName, () -> runEcho(message, successPrefix, displayName, true, btnRetryLastOperation));
        }
      }

      @Override
      public void onError(Exception error) {
        setOperationInProgress(pressedButton, false);
        handleError(displayName, error, isRetry ? null : txtResult);
      }
    });
  }

  private void runSign(String receiptRequestJson, String displayName, TextView resultView, boolean isRetry, Button pressedButton) {
    String operationId = UUID.randomUUID().toString();
    setOperationInProgress(pressedButton, true);
    client().sign(this, operationId, receiptRequestJson, new PosSystemApiClient.Callback() {
      @Override
      public void onSuccess(String content) {
        setOperationInProgress(pressedButton, false);
        String text = prettyJson(content);
        if (isRetry) {
          showAlert("Retry Result: " + displayName, text);
        } else {
          resultView.setText(text);
          setLastOperation(displayName, () -> runSign(receiptRequestJson, displayName, resultView, true, btnRetryLastOperation));
        }
      }

      @Override
      public void onError(Exception error) {
        setOperationInProgress(pressedButton, false);
        handleError(displayName, error, isRetry ? null : resultView);
      }
    });
  }

  private void setLastOperation(String displayName, Runnable retry) {
    lastOperation = new LastOperation(displayName, retry);
    btnRetryLastOperation.setText("🔄 Retry: " + displayName);
    btnRetryLastOperation.setVisibility(View.VISIBLE);
  }

  private void setOperationInProgress(Button pressedButton, boolean inProgress) {
    if (SettingsStore.PROTOCOL_SERVICE_IPC.equals(settingsStore.getSelectedProtocol())) {
      pressedButton.setEnabled(!inProgress);
    } else {
      setButtonsEnabled(!inProgress);
    }
  }

  private void setButtonsEnabled(boolean enabled) {
    btnSendEchoRequest.setEnabled(enabled);
    btnRestartConfig.setEnabled(enabled);
    btnSendSignRequest.setEnabled(enabled);
    btnSendStartReceipt.setEnabled(enabled);
    btnSendZeroReceipt.setEnabled(enabled);
    btnRetryLastOperation.setEnabled(enabled);
  }

  private void handleError(String operation, Exception ex, TextView resultView) {
    String message = ex.getCause() != null ? ex.getCause().getMessage() : ex.getMessage();
    if (resultView != null) {
      resultView.setText("❌ Error: " + operation + "\n\n" + message + "\n\n(" + ex.getClass().getSimpleName() + ")");
    }
    showAlert("❌ " + operation, message + "\n\n📋 Error Type: " + ex.getClass().getSimpleName());
  }

  private void showAlert(String title, String message) {
    new AlertDialog.Builder(this)
        .setTitle(title)
        .setMessage(message)
        .setPositiveButton("OK", null)
        .show();
  }

  private String buildReceiptRequestJson(String posSystemId, String terminalId, String receiptReference,
                                          String user, String area, long receiptCase) {
    Map<String, Object> receipt = new LinkedHashMap<>();
    receipt.put("ftCashBoxID", settingsStore.getCashboxId());
    if (posSystemId != null) {
      receipt.put("ftPosSystemId", posSystemId);
    }
    if (terminalId != null) {
      receipt.put("cbTerminalID", terminalId);
    }
    receipt.put("cbReceiptReference", receiptReference);
    receipt.put("cbReceiptMoment", utcNowIso());
    receipt.put("ftReceiptCaseData", "");
    if (user != null) {
      receipt.put("cbUser", user);
    }
    if (area != null) {
      receipt.put("cbArea", area);
    }
    receipt.put("cbSettlement", "");
    receipt.put("ftReceiptCase", receiptCase);
    receipt.put("cbChargeItems", new Object[0]);
    receipt.put("cbPayItems", new Object[0]);
    return gson.toJson(receipt);
  }

  private String utcNowIso() {
    SimpleDateFormat format = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US);
    format.setTimeZone(TimeZone.getTimeZone("UTC"));
    return format.format(new Date());
  }

  private String prettyJson(String content) {
    try {
      return new GsonBuilder().setPrettyPrinting().create().toJson(new JsonParser().parse(content));
    } catch (Exception e) {
      return content;
    }
  }

  private PosSystemApiClient client() {
    String protocol = settingsStore.getSelectedProtocol();
    String signature = protocol + "|" + settingsStore.getCashboxId() + "|" + settingsStore.getAccessToken();
    if (posSystemApiClient == null || !signature.equals(posSystemApiClientSignature)) {
      boolean useBoundService = SettingsStore.PROTOCOL_SERVICE_IPC.equals(protocol);
      posSystemApiClient = new PosSystemApiClient(settingsStore.getCashboxId(), settingsStore.getAccessToken(), useBoundService);
      posSystemApiClientSignature = signature;
    }
    return posSystemApiClient;
  }

  // ================= Settings tab =================

  private void wireSettingsTab() {
    entryPairPin.addTextChangedListener(new SimpleTextWatcher(text -> btnPair.setEnabled(!text.trim().isEmpty())));

    btnPair.setOnClickListener(v -> onPairClicked());

    entryCashboxId.addTextChangedListener(new SimpleTextWatcher(text -> settingsStore.setCashboxId(text)));
    entryAccessToken.addTextChangedListener(new SimpleTextWatcher(text -> settingsStore.setAccessToken(text)));

    radioGroupProtocol.setOnCheckedChangeListener((group, checkedId) -> {
      String protocol = checkedId == R.id.radioServiceIpc ? SettingsStore.PROTOCOL_SERVICE_IPC : SettingsStore.PROTOCOL_INTENT_ACTIVITY;
      settingsStore.setSelectedProtocol(protocol);
    });
  }

  private void loadSettingsIntoUi() {
    entryCashboxId.setText(settingsStore.getCashboxId());
    entryAccessToken.setText(settingsStore.getAccessToken());
    if (SettingsStore.PROTOCOL_SERVICE_IPC.equals(settingsStore.getSelectedProtocol())) {
      radioGroupProtocol.check(R.id.radioServiceIpc);
    } else {
      radioGroupProtocol.check(R.id.radioIntentActivity);
    }
  }

  private void onPairClicked() {
    String pin = entryPairPin.getText().toString().trim();
    if (pin.isEmpty()) {
      return;
    }

    setPairingBusy(true);
    Map<String, String> body = new LinkedHashMap<>();
    body.put("Pin", pin);

    client().pair(this, gson.toJson(body), new PosSystemApiClient.Callback() {
      @Override
      public void onSuccess(String content) {
        setPairingBusy(false);
        Map<?, ?> response = gson.fromJson(content, Map.class);
        Object cashboxId = response != null ? response.get("CashBoxId") : null;
        Object accessToken = response != null ? response.get("AccessToken") : null;

        if (cashboxId != null && accessToken != null) {
          entryCashboxId.setText(cashboxId.toString());
          entryAccessToken.setText(accessToken.toString());
          settingsStore.setCashboxId(cashboxId.toString());
          settingsStore.setAccessToken(accessToken.toString());
          entryPairPin.setText("");
          showAlert("Pairing Successful", "Cashbox ID and Access Token have been retrieved and saved.");
        } else {
          showAlert("Pairing Failed", "The POS system response did not contain valid credentials. Please check the PIN and try again.");
        }
      }

      @Override
      public void onError(Exception error) {
        setPairingBusy(false);
        showAlert("Pairing Failed", "An error occurred while trying to pair with the POS system: " + error.getMessage());
      }
    });
  }

  private void setPairingBusy(boolean busy) {
    pairingStatus.setVisibility(busy ? View.VISIBLE : View.GONE);
    entryPairPin.setEnabled(!busy);
    btnPair.setEnabled(!busy && !entryPairPin.getText().toString().trim().isEmpty());
  }

  private static final class SimpleTextWatcher implements TextWatcher {
    interface Listener {
      void onChanged(String text);
    }

    private final Listener listener;

    SimpleTextWatcher(Listener listener) {
      this.listener = listener;
    }

    @Override
    public void beforeTextChanged(CharSequence s, int start, int count, int after) { }

    @Override
    public void onTextChanged(CharSequence s, int start, int before, int count) { }

    @Override
    public void afterTextChanged(Editable s) {
      listener.onChanged(s.toString());
    }
  }
}
