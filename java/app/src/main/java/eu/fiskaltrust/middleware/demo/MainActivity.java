package eu.fiskaltrust.middleware.demo;

import android.content.ComponentName;
import android.content.Intent;
import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;
import android.view.View;
import android.widget.Button;
import android.widget.RadioButton;
import android.widget.TextView;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;

import java.text.ParseException;
import java.util.LinkedHashMap;
import java.util.Map;

import eu.fiskaltrust.middleware.demo.java.R;
import eu.fiskaltrust.middleware.util.ProtoUtil;
import fiskaltrust.ifPOS.v1.IPOS;

public class MainActivity extends AppCompatActivity {

  private static final String QUEUE_URL = "localhost:1400";
  private static final String CASHBOX_ID = "<your-cashbox-id>";
  private static final String ACCESS_TOKEN = "<your-access-token>";
  private static final Boolean SANDBOX = true;


  private TextView txtEchoResult;
  private TextView txtSignResult;
  private TextView txtSpecialReceiptResult;
  private RadioButton radioIntent;

  private PosClient client;
  private PosSystemApiClient posSystemApiClient;

  @Override
  protected void onCreate(Bundle savedInstanceState) {
    super.onCreate(savedInstanceState);
    setContentView(R.layout.activity_main);

    txtEchoResult = findViewById(R.id.txtResult);
    txtSignResult = findViewById(R.id.txtSignResult);
    txtSpecialReceiptResult = findViewById(R.id.txtSpecialReceiptResult);
    radioIntent = findViewById(R.id.radioIntent);

    client = new PosClient(QUEUE_URL);
    posSystemApiClient = new PosSystemApiClient(CASHBOX_ID, ACCESS_TOKEN);
  }

  private boolean isIntentModeSelected() {
    return radioIntent.isChecked();
  }

  @Override
  protected void onActivityResult(int requestCode, int resultCode, Intent data) {
    if (SarAwaiter.deliverResult(requestCode, resultCode, data)) {
      return;
    }
    super.onActivityResult(requestCode, resultCode, data);
  }

  @Override
  protected void onDestroy() {
    if (client != null) {
      try {
        client.shutdown();
      } catch (InterruptedException e) { }
    }
    super.onDestroy();
  }

  public void startService(View view) {
    ComponentName componentName = new ComponentName("eu.fiskaltrust.androidlauncher.grpc", "eu.fiskaltrust.androidlauncher.grpc.Start");

    Intent intent = new Intent(Intent.ACTION_SEND);
    intent.setComponent(componentName);
    intent.putExtra("cashboxid", CASHBOX_ID);
    intent.putExtra("accesstoken", ACCESS_TOKEN);
    intent.putExtra("sandbox", SANDBOX);

    sendBroadcast(intent);
  }

  public void stopService(View view) {
    Intent intent = new Intent(Intent.ACTION_SEND);
    ComponentName componentName = new ComponentName("eu.fiskaltrust.androidlauncher.grpc", "eu.fiskaltrust.androidlauncher.grpc.Stop");
    intent.setComponent(componentName);

    sendBroadcast(intent);
  }

  public void sendEchoRequest(View view) {
    if (isIntentModeSelected()) {
      txtEchoResult.setText("Sending...");
      posSystemApiClient.echo(this, "Hello, Android via Intent!", resultCallback(txtEchoResult));
      return;
    }

    String response = client.echo("Hello, Android!");
    txtEchoResult.setText(response);
  }

  public void sendStartReceipt(View view) throws ParseException {
    if (isIntentModeSelected()) {
      txtSpecialReceiptResult.setText("Sending...");
      String json = buildReceiptRequestJson("d4a62055-ca6c-4372-ae4d-f835a88e4a5d", "T1", "R123456", "Owner", "System", 0x4445000100000003L);
      posSystemApiClient.sign(this, json, resultCallback(txtSpecialReceiptResult));
      return;
    }

    IPOS.ReceiptRequest request = IPOS.ReceiptRequest.newBuilder()
            .setFtCashBoxID(CASHBOX_ID)
            .setFtPosSystemId("d4a62055-ca6c-4372-ae4d-f835a88e4a5d")
            .setCbTerminalID("T1")
            .setCbReceiptReference("R123456")
            .setCbReceiptMoment(ProtoUtil.parseDatetime("2020-06-01T17:00:00.01Z"))
            .setFtReceiptCaseData("")
            .setCbUser("Owner")
            .setCbArea("System")
            .setFtReceiptCase(0x4445000100000003L)
            .build();
    IPOS.ReceiptResponse response = client.sign(request);
    txtSpecialReceiptResult.setText(jsonToString(response));
  }

  public void sendZeroReceipt(View view) throws ParseException {
    if (isIntentModeSelected()) {
      txtSpecialReceiptResult.setText("Sending...");
      String json = buildReceiptRequestJson("d4a62055-ca6c-4372-ae4d-f835a88e4a5d", "T1", "R123456", "Owner", "System", 0x4445000100000002L);
      posSystemApiClient.sign(this, json, resultCallback(txtSpecialReceiptResult));
      return;
    }

    IPOS.ReceiptRequest request = IPOS.ReceiptRequest.newBuilder()
            .setFtCashBoxID(CASHBOX_ID)
            .setFtPosSystemId("d4a62055-ca6c-4372-ae4d-f835a88e4a5d")
            .setCbTerminalID("T1")
            .setCbReceiptReference("R123456")
            .setCbReceiptMoment(ProtoUtil.parseDatetime("2020-06-01T17:00:00.01Z"))
            .setFtReceiptCaseData("")
            .setCbUser("Owner")
            .setCbArea("System")
            .setFtReceiptCase(0x4445000100000002L)
            .build();
    IPOS.ReceiptResponse response = client.sign(request);
    txtSpecialReceiptResult.setText(jsonToString(response));
  }

  public void sendSignReceipt(View view) throws ParseException {
    if (isIntentModeSelected()) {
      txtSignResult.setText("Sending...");
      String json = buildReceiptRequestJson(null, null, "R12345678", null, null, 0x4445000100000002L);
      posSystemApiClient.sign(this, json, resultCallback(txtSignResult));
      return;
    }

    IPOS.ReceiptRequest request = IPOS.ReceiptRequest.newBuilder()
            .setFtCashBoxID(CASHBOX_ID)
            .setCbReceiptReference("R12345678")
            .setFtReceiptCase(0x4445000100000002L)
            .setCbReceiptMoment(ProtoUtil.parseDatetime("2020-06-01T17:00:00.01Z"))
            .build();

      IPOS.ReceiptResponse response = client.sign(request);
      txtSignResult.setText(jsonToString(response));
  }

  /** Builds the /v2/sign JSON body, mirroring the gRPC ReceiptRequest built above field-for-field. */
  private String buildReceiptRequestJson(String posSystemId, String terminalId, String receiptReference,
                                          String user, String area, long receiptCase) {
    Map<String, Object> receipt = new LinkedHashMap<>();
    receipt.put("ftCashBoxID", CASHBOX_ID);
    if (posSystemId != null) {
      receipt.put("ftPosSystemId", posSystemId);
    }
    if (terminalId != null) {
      receipt.put("cbTerminalID", terminalId);
    }
    receipt.put("cbReceiptReference", receiptReference);
    receipt.put("cbReceiptMoment", "2020-06-01T17:00:00.01Z");
    if (user != null) {
      receipt.put("cbUser", user);
    }
    if (area != null) {
      receipt.put("cbArea", area);
    }
    receipt.put("ftReceiptCase", receiptCase);
    receipt.put("cbChargeItems", new Object[0]);
    receipt.put("cbPayItems", new Object[0]);
    return new Gson().toJson(receipt);
  }

  private PosSystemApiClient.Callback resultCallback(TextView target) {
    return new PosSystemApiClient.Callback() {
      @Override
      public void onSuccess(String content) {
        target.setText(content);
      }

      @Override
      public void onError(Exception error) {
        target.setText("Error: " + error.getMessage());
      }
    };
  }

  private String jsonToString(Object obj){
    return new GsonBuilder().setPrettyPrinting().create().toJson(obj);
  }
}
