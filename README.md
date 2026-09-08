# fiskaltrust.Middleware demo (Android)
Demo applications written in Java and .NET MAUI that demonstrate how to call the German fiskaltrust.Middleware on Android devices using BoundServices and Intents.

## Getting Started

### Prerequisites
In order to use these demo applications, the following prerequisites are required:
- *The demo application*: Just clone or download this repository. For an optimal experience, we recommend using [Android Studio](https://developer.android.com/studio) for the Java sample, and [Visual Studio](https://visualstudio.microsoft.com/) with the .NET MAUI workload for the MAUI sample. All of them can be downloaded for free.
- *The fiskaltrust.Middleware for Android* installed on your device, which can be configured via the [fiskaltrust.Portal](https://portal-sandbox.fiskaltrust.de).
- The *Cashbox Id* and *Access Token* are visible in the portal, and are needed to start the Middleware on Android.

Both demos in this repository talk to the Android Launcher's PosSystemAPI, an HTTP style request/response contract carried over Android Intents or BoundService calls. Requests and responses are plain JSON, with the method, path, headers and body passed as extras (headers and body are base64url encoded). The demo lets you switch between the two available transports in its Settings tab, either starting the Launcher's PosSystemAPIActivity for each request (Intent-Activity) or binding to its PosSystemAPIService and exchanging Messenger messages (Service-IPC).

The **.NET MAUI example** is the more feature complete one. Both feature echo, sign and cashbox pairing. The MAUI example also demonstrates the issue and pay endpoints of the possystem api.

### Running the Demo
Make sure to download the Android Launcher from the [github releases section](https://github.com/fiskaltrust/middleware-launcher-android/releases) and install the APK on your device first. 

#### MAUI
To run the MAUI example, Visual Studio with the .NET MAUI workload is required. Please follow the [official docs](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation) to download and install it on your machine.

After this, opening the solution in the _maui_ folder of this repository and clicking _Debug_ should be all. It's also possible to build it from the command line:
```sh
dotnet build -f net10.0-android
```

#### Java
We recommend using Android Studio to run the Java Android samples, as we used it to implement them. Just open the _java_ folder and wait until gradle synced everything.

The project does not ship a Gradle wrapper, so building from the command line requires a local Gradle installation matching the version referenced in `java/gradle/gradle-daemon-jvm.properties`:
```sh
# Build APK only
gradle assembleDebug

# Optionally, to build the APK and install it on your connected device automatically:
gradle installDebug
```

### Minimal sample

The middleware automatically starts when it receives the first request, requests are sent to the Launcher's PosSystemAPI, either as a [bound service](maui/Services/BoundServiceTransport.cs) or via an [Activity started via `startActivityForResult`](maui/Services/ActivityTransport.cs). We recommend implementing the BoundService communication.

The result carries a `StatusCode` extra and a `ContentBase64Url` extra with the response body. A full working implementation of this, is available in [PosSystemApiService.cs](/Users/paulvolavsek/Developer/middleware-demo-android/maui/Services/PosSystemApiService.cs), [BoundServiceTransport.cs](maui/Services/BoundServiceTransport.cs) and [ActivityTransport.cs](maui/Services/ActivityTransport.cs) (or the respective java files).

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
