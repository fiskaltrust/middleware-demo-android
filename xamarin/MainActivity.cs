﻿using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System;
using Android.Content;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Interface.Client.Http;
using System.IO;
using System.Text;

namespace fiskaltrust.Middleware.Demo
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const int LOG_REQ_CODE = 1234;
        
        private const string QUEUE_URL_GRPC = "grpc://localhost:1400";
        private const string QUEUE_URL_REST = "http://localhost:1500/queue";
        private const string CASHBOX_ID = "<your-cashbox-id>";
        private const string ACCESS_TOKEN = "<your-access-token>";
        private const bool SANDBOX = true;

        private TaskCompletionSource<string> _getLogsCompletionSource;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            FindViewById<Button>(Resource.Id.btnStartService).Click += new EventHandler((s, e) => ButtonStartServiceOnClick());
            FindViewById<Button>(Resource.Id.btnStopService).Click += new EventHandler((s, e) => ButtonStopServiceOnClick());
            FindViewById<Button>(Resource.Id.btnSendEchoRequest).Click += new EventHandler(async (s, e) => await ButtonEchoRequestOnClickAsync());
            FindViewById<Button>(Resource.Id.btnSendSignRequest).Click += new EventHandler(async (s, e) => await ButtonSignRequestOnClickAsync());
            FindViewById<Button>(Resource.Id.btnSendStartReceipt).Click += new EventHandler(async (s, e) => await ButtonStartReceiptOnClickAsync());
            FindViewById<Button>(Resource.Id.btnSendZeroReceipt).Click += new EventHandler(async (s, e) => await ButtonZeroReceiptOnClickAsync());
            FindViewById<Button>(Resource.Id.btnGetLogs).Click += new EventHandler(async (s, e) => await ButtonGetLogsOnClickAsync());
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void ButtonStartServiceOnClick()
        {
            SetButtonsEnabled(false);

            var componentName = FindViewById<RadioButton>(Resource.Id.radioGrpc).Checked
                ? new ComponentName("eu.fiskaltrust.androidlauncher.grpc", "eu.fiskaltrust.androidlauncher.grpc.Start")
                : new ComponentName("eu.fiskaltrust.androidlauncher.http", "eu.fiskaltrust.androidlauncher.http.Start");

            var intent = new Intent(Intent.ActionSend);
            intent.SetComponent(componentName);
            intent.PutExtra("cashboxid", CASHBOX_ID);
            intent.PutExtra("accesstoken", ACCESS_TOKEN);
            intent.PutExtra("sandbox", SANDBOX);

            SendBroadcast(intent);

            SetButtonsEnabled(true);
        }

        private void ButtonStopServiceOnClick()
        {
            SetButtonsEnabled(false);

            var intent = new Intent(Intent.ActionSend);
            var componentName = FindViewById<RadioButton>(Resource.Id.radioGrpc).Checked
                ? new ComponentName("eu.fiskaltrust.androidlauncher.grpc", "eu.fiskaltrust.androidlauncher.grpc.Stop")
                : new ComponentName("eu.fiskaltrust.androidlauncher.http", "eu.fiskaltrust.androidlauncher.http.Stop");
            intent.SetComponent(componentName);
            SendBroadcast(intent);

            SetButtonsEnabled(true);
        }

        private async Task ButtonEchoRequestOnClickAsync()
        {
            SetButtonsEnabled(false);

            var txt = FindViewById<TextView>(Resource.Id.txtResult);

            try
            {
                var pos = await GetPOSAsync();
                var response = await pos.EchoAsync(new EchoRequest { Message = $"Hello Android, it's {DateTime.Now:t}!" });
                txt.Text = response.Message;
            }
            catch (Exception ex)
            {
                txt.Text = $"An exception occured: {ex}";
            }

            SetButtonsEnabled(true);
        }

        private async Task ButtonSignRequestOnClickAsync()
        {
            SetButtonsEnabled(false);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = CASHBOX_ID,
                ftReceiptCase = 0x4445_0001_0000_0000,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };

            var txt = FindViewById<TextView>(Resource.Id.txtSignResult);

            try
            {
                var pos = await GetPOSAsync();
                var response = await pos.SignAsync(receiptRequest);
                txt.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
            }
            catch (Exception ex)
            {
                txt.Text = $"An exception occured: {ex}";
            }

            SetButtonsEnabled(true);
        }

        private async Task ButtonStartReceiptOnClickAsync()
        {
            SetButtonsEnabled(false);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = CASHBOX_ID,
                ftPosSystemId = "d4a62055-ca6c-4372-ae4d-f835a88e4a5d",
                cbTerminalID = "T1",
                cbReceiptReference = "2020020120152812",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCaseData = "",
                cbUser = "Receptionist",
                cbArea = "System",
                cbSettlement = "",
                ftReceiptCase = 0x4445_0001_0000_0003,
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };

            var txt = FindViewById<TextView>(Resource.Id.txtSpecialReceiptResult);

            try
            {
                var pos = await GetPOSAsync();
                var response = await pos.SignAsync(receiptRequest);
                txt.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
            }
            catch (Exception ex)
            {
                txt.Text = $"An exception occured: {ex}";
            }

            SetButtonsEnabled(true);
        }

        private async Task ButtonZeroReceiptOnClickAsync()
        {
            SetButtonsEnabled(false);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = CASHBOX_ID,
                ftPosSystemId = "d4a62055-ca6c-4372-ae4d-f835a88e4a5d",
                cbTerminalID = "T1",
                cbReceiptReference = "2020020120152812",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCaseData = "",
                cbUser = "Receptionist",
                cbArea = "System",
                cbSettlement = "",
                ftReceiptCase = 0x4445_0001_0000_0002,
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };

            var txt = FindViewById<TextView>(Resource.Id.txtSpecialReceiptResult);

            try
            {
                var pos = await GetPOSAsync();
                var response = await pos.SignAsync(receiptRequest);
                txt.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
            }
            catch (Exception ex)
            {
                txt.Text = $"An exception was thrown: {ex}";
            }

            SetButtonsEnabled(true);
        }
        
        public async Task ButtonGetLogsOnClickAsync()
        {
            SetButtonsEnabled(false);

            _getLogsCompletionSource = new TaskCompletionSource<string>();

            var componentName = FindViewById<RadioButton>(Resource.Id.radioGrpc).Checked
                ? new ComponentName("eu.fiskaltrust.androidlauncher.grpc", "eu.fiskaltrust.androidlauncher.grpc.LogContentLinkActivity")
                : new ComponentName("eu.fiskaltrust.androidlauncher.http", "eu.fiskaltrust.androidlauncher.http.LogContentLinkActivity");
            
            var req = new Intent();
            req.PutExtra("cashboxid", CASHBOX_ID);
            req.PutExtra("accesstoken", ACCESS_TOKEN);
            req.SetComponent(componentName);

            StartActivityForResult(req, LOG_REQ_CODE);
            var log = await _getLogsCompletionSource.Task;
            var txt = FindViewById<TextView>(Resource.Id.txtLogs);

            if (log != null)
            {
                txt.Text = log;
            }
            else
            {
                txt.Text = "No logs available";
            }

            SetButtonsEnabled(true);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == LOG_REQ_CODE && resultCode == Result.Ok && data != null)
            {
                using var str = ContentResolver.OpenInputStream(Android.Net.Uri.Parse(data.DataString));
                using var sr = new StreamReader(str, Encoding.UTF8);
                var log = sr.ReadToEnd();
                _getLogsCompletionSource?.TrySetResult(log);
            }
            else
            {
                _getLogsCompletionSource?.TrySetResult(null);
            }
        }

        private async Task<IPOS> GetPOSAsync()
        {
            if (FindViewById<RadioButton>(Resource.Id.radioGrpc).Checked)
            {
                return await GrpcPosFactory.CreatePosAsync(new GrpcClientOptions
                {
                    Url = new Uri(QUEUE_URL_GRPC)
                });
            }
            else
            {
                return await HttpPosFactory.CreatePosAsync(new HttpPosClientOptions
                {
                    Url = new Uri(QUEUE_URL_REST)
                });
            }
        }

        private void SetButtonsEnabled(bool state)
        {
            FindViewById<Button>(Resource.Id.btnSendEchoRequest).Enabled = state;
            FindViewById<Button>(Resource.Id.btnSendSignRequest).Enabled = state;
            FindViewById<Button>(Resource.Id.btnSendStartReceipt).Enabled = state;
            FindViewById<Button>(Resource.Id.btnSendZeroReceipt).Enabled = state;
        }
    }
}