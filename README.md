# fiskaltrust.Middleware demo (Android)
Demo applications written in Java, Xamarin and .NET MAUI that demonstrate how to call the German fiskaltrust.Middleware on Android devices using gRPC or HTTP/REST.

## Getting Started

### Prerequisites
In order to use these demo applications, the following prerequisites are required:
- *The demo application*: Just clone or download this repository. For an optimal experience, we recommend using [Android Studio](https://developer.android.com/studio) for the Java samples, and [Visual Studio](https://visualstudio.microsoft.com/) for the Xamarin ones. Both can be downloaded for free.
- *The fiskaltrust.Middleware for Android* installed on your device, which can be configured and downloaded via the [fiskaltrust.Portal](https://portal-sandbox.fiskaltrust.de). Please note that the Android download is only available for cashboxes that only contain supported packages (SQLite, Fiskaly and Swissbit) and supported protocols (gRPC and REST).
- The *Cashbox Id* and *Access Token* are visible in the portal, and are needed to start the Middleware on Android.

The **Java example** in this repository talks to the Android Launcher's PosSystemAPI, an HTTP style request/response contract carried over Android Intents. Requests and responses are plain JSON, with the method, path, headers and body passed as Intent extras (headers and body are base64url encoded). The demo lets you switch between the two available transports in its Settings tab, either starting the Launcher's PosSystemAPI Activity for each request (Intent-Activity) or binding to its PosSystemAPIService and exchanging Messenger messages (Service-IPC).

The **Xamarin/C# example** uses the [fiskaltrust.Middleware.Interface.Client.Grpc](https://www.nuget.org/packages/fiskaltrust.Middleware.Interface.Client.Grpc/) NuGet package, which doesn't need the _.proto_ files. A more detailed documentation about this package can be found in its [repository](https://github.com/fiskaltrust/middleware-interface-dotnet). HTTP works without any additional required files anyway, and uses the [fiskaltrust.Middleware.Interface.Client.Http](https://www.nuget.org/packages/fiskaltrust.Middleware.Interface.Client.Http/) package.

### Running the Demo
Make sure to download the respective Android Launcher (gRPC or HTTP) from the Portal and install the APK on your device first (or get it from Google Play). This App contains a background service that can be started and stopped via intents, and spins up a gRPC server. Thus, the Android App behaves exactly the same as the fiskaltrust.Middleware does on Desktop operating systems.

There are some limitations when configuring an Android Cashbox in the Portal:
- The cashbox must not contain WCF URLs (neither the SCU nor the Queue), as they are not supported on Android.
- Currently, only the following packages are supported:
  - fiskaltrust.Middleware.Queue.SQLite
  - fiskaltrust.Middleware.SCU.Fiskaly
  - fiskaltrust.Middleware.SCU.Swissbit

If you require other Queue or SCU packages on Android, please reach out to our [support](support@fiskaltrust.de) to help us prioritizing.

#### Java
We recommend using Android Studio to run the Java Android samples, as we used it to implement them. Just open the _java_ folder and wait until gradle synced everything.

The project does not ship a Gradle wrapper, so building from the command line requires a local Gradle installation matching the version referenced in `java/gradle/gradle-daemon-jvm.properties`:
```sh
# Build APK only
gradle assembleDebug

# Optionally, to build the APK and install it on your connected device automatically:
gradle installDebug
```

#### Xamarin/C#
To run the Xamarin example, Visual Studio with the Xamarin workload is required. Please follow the [official docs](https://docs.microsoft.com/en-us/xamarin/?view=vs-2019) to download and install it on your machine. 

After this, opening the solution in the _xamarin_ folder of this repository and clicking _Debug_ should be all.

### Minimal sample
Starting and stopping the Middleware is fairly easy, as it can be controlled via Intents. 

The Middleware can e.g. be started with the following Java code:
```java
ComponentName componentName = new ComponentName("eu.fiskaltrust.androidlauncher", "eu.fiskaltrust.androidlauncher.Start");

Intent intent = new Intent(Intent.ACTION_SEND);
intent.setComponent(componentName);
intent.putExtra("cashboxid", "<your-cashbox-id>");
intent.putExtra("accesstoken", "<your-access-token>");
intent.putExtra("sandbox", true);   // or "false" for production Cashboxes
intent.putExtra("enableCloseButton", false);   // or "true" to display a close button in the notification
// Optionally, for development purposes only:
intent.putExtra("loglevel", "Debug");   // default is "Information"

sendBroadcast(intent);
```

**Please note that the Middleware will not be immediately available after this.** Intents are processed asynchronously, and initializing most TSEs takes some time (e.g. up to 45 seconds for Swissbit; fiskaly SCUs are faster). We recommend polling our state endpoint until the TSE is intialized (see below).

A stop intent looks similar:

```java
ComponentName componentName = new ComponentName("eu.fiskaltrust.androidlauncher", "eu.fiskaltrust.androidlauncher.Stop");

Intent intent = new Intent(Intent.ACTION_SEND);
intent.setComponent(componentName);

sendBroadcast(intent);
```

After the Middleware successfully booted (the state is also shown in the Android notification), requests are sent to the Launcher's PosSystemAPI, either as an Activity started via `startActivityForResult` or via a bound service. An echo request is a plain HTTP style call to `/v2/echo`, e.g.:
```java
Intent intent = new Intent();
intent.setClassName("eu.fiskaltrust.androidlauncher", "eu.fiskaltrust.androidlauncher.PosSystemAPI");
intent.putExtra("Method", "POST");
intent.putExtra("Path", "/v2/echo");
intent.putExtra("HeaderJsonObjectBase64Url", headersBase64Url);   // x-cashbox-id, x-cashbox-accesstoken, x-operation-id
intent.putExtra("BodyBase64Url", bodyBase64Url);   // base64url encoded JSON, e.g. {"Message":"Hello Android!"}

startActivityForResult(intent, requestCode);
```
The result Intent carries a `StatusCode` extra and a `ContentBase64Url` extra with the response body. A full working implementation of this, including the bound service alternative, is available in [PosSystemApiClient.java](java/app/src/main/java/eu/fiskaltrust/middleware/demo/PosSystemApiClient.java), [ActivityTransport.java](java/app/src/main/java/eu/fiskaltrust/middleware/demo/transport/ActivityTransport.java) and [BoundServiceTransport.java](java/app/src/main/java/eu/fiskaltrust/middleware/demo/transport/BoundServiceTransport.java).

### State and log information
The fiskaltrust.Middleware for Android publishes two endpoints to request both the state and the logs under the well-known HTTP address and port http://localhost:4654/:
- `GET http://localhost:4654/fiskaltrust/state` returns a JSON object with the current state of the Middleware and the reason, which looks like this:
   ```
   {
     "CurrentState": "Uninitialized" | "Initializing" | "Running" | "Error",
     "Reason": "CONFIG_NOT_FOUND" | "REMOUNT_REQUIRED" | "<informational reason phrase>"
   }
   ```
   `REMOUNT_REQUIRED` is only applicable for Swissbit TSEs, which require a remount when they're used in an Android App for the first time (i.e. they need to be plugged out and in again).
- `GET http://localhost:4654/fiskaltrust/logs` returns the raw log files written by the Middleware for later usage. The same messages are also written to LogCat, which might be more convenient during development. This endpoint is authenticated via HTTP header values: `cashboxid` and `accesstoken`.

#### Direct log access
For security reasons, log files are stored in the private directory of the Middleware App. In case the log endpoint described above is not reachable (e.g. because the Middleware cannot be started at all), log files can also be accessed via an Android _content provider_. We've implemented a specific activity that can be queried to return a `content://` link to the latest log file. The usage of this provider is e.g. demonstrated [here](xamarin/MainActivity.cs#L206), more details about content providers are available in the [Android docs](https://developer.android.com/reference/androidx/core/content/FileProvider).

## Documentation
The full documentation for the interface can be found on https://docs.fiskaltrust.cloud. It is activeliy maintained and developed in our [interface-doc repository](https://github.com/fiskaltrust/interface-doc). 

More information is also available after logging into the portal with a user that has the _PosCreator_ role assigned.

## Contributions
We welcome all kinds of contributions and feedback, e.g. via Issues or Pull Requests. 

## Related resources
Our latest samples are available for the following programming languages and tools:
<p align="center">
  <a href="https://github.com/fiskaltrust/middleware-demo-dotnet"><img src="https://upload.wikimedia.org/wikipedia/commons/thumb/0/0d/C_Sharp_wordmark.svg/100px-C_Sharp_wordmark.svg.png" alt="csharp"></a>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <a href="https://github.com/fiskaltrust/middleware-demo-java"><img src="https://upload.wikimedia.org/wikiversity/de/thumb/b/b8/Java_cup.svg/100px-Java_cup.svg.png" alt="java"></a>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <a href="https://github.com/fiskaltrust/middleware-demo-node"><img src="https://upload.wikimedia.org/wikipedia/commons/thumb/d/d9/Node.js_logo.svg/100px-Node.js_logo.svg.png" alt="node"></a>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <a href="https://github.com/fiskaltrust/middleware-demo-android"><img src="https://upload.wikimedia.org/wikipedia/commons/thumb/d/d7/Android_robot.svg/100px-Android_robot.svg.png" alt="android"></a>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <a href="https://github.com/fiskaltrust/middleware-demo-postman"><img src="https://avatars3.githubusercontent.com/u/10251060?s=100&v=4" alt="node"></a>
</p>

Additionally, other samples (including legacy ones) can be found in our [demo repository](https://github.com/fiskaltrust/demo).
