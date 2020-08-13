package eu.fiskaltrust.middleware.demo;

import android.content.ComponentName;
import android.content.Intent;
import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;
import android.view.View;
import android.widget.Button;
import android.widget.TextView;

import com.google.gson.GsonBuilder;

import java.text.ParseException;

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

  private PosClient client;

  @Override
  protected void onCreate(Bundle savedInstanceState) {
    super.onCreate(savedInstanceState);
    setContentView(R.layout.activity_main);

    txtEchoResult = findViewById(R.id.txtResult);
    txtSignResult = findViewById(R.id.txtSignResult);
    txtSpecialReceiptResult = findViewById(R.id.txtSpecialReceiptResult);

    client = new PosClient(QUEUE_URL);
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
    ComponentName componentName = new ComponentName("eu.fiskaltrust.androidlauncher", "eu.fiskaltrust.androidlauncher.Start");

    Intent intent = new Intent(Intent.ACTION_SEND);
    intent.setComponent(componentName);
    intent.putExtra("cashboxid", CASHBOX_ID);
    intent.putExtra("accesstoken", ACCESS_TOKEN);
    intent.putExtra("sandbox", SANDBOX);

    sendBroadcast(intent);
  }

  public void stopService(View view) {
    Intent intent = new Intent(Intent.ACTION_SEND);
    ComponentName componentName = new ComponentName("eu.fiskaltrust.androidlauncher", "eu.fiskaltrust.androidlauncher.Stop");
    intent.setComponent(componentName);

    sendBroadcast(intent);
  }

  public void sendEchoRequest(View view) {
    String response = client.echo("Hello, Android!");
    txtEchoResult.setText(response);
  }

  public void sendStartReceipt(View view) throws ParseException {

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
    IPOS.ReceiptRequest request = IPOS.ReceiptRequest.newBuilder()
            .setFtCashBoxID(CASHBOX_ID)
            .setCbReceiptReference("R12345678")
            .setFtReceiptCase(0x4445000100000002L)
            .setCbReceiptMoment(ProtoUtil.parseDatetime("2020-06-01T17:00:00.01Z"))
            .build();

      IPOS.ReceiptResponse response = client.sign(request);
      txtSignResult.setText(jsonToString(response));
  }

  private String jsonToString(Object obj){
    return new GsonBuilder().setPrettyPrinting().create().toJson(obj);
  }
}
