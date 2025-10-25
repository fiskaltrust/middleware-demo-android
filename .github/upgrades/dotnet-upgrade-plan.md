# .NET MAUI Migration Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 8.0 SDK required for this migration is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Convert fiskaltrust.Middleware.Demo.csproj to SDK-style project format
4. Migrate fiskaltrust.Middleware.Demo.csproj from Xamarin.Android to .NET MAUI

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                          | Current Version | New Version | Description                                   |
|:--------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.Bcl.AsyncInterfaces        |   1.1.1         |  8.0.0      | Recommended for .NET 8.0                     |
| Newtonsoft.Json                       |   12.0.3        |  13.0.4     | Security vulnerability                        |
| System.Net.Http                       |   4.3.4         |             | Package functionality included with framework |
| Xamarin.Android.Support.Core.Utils   |   28.0.0.3      |             | Package functionality included with framework |
| Xamarin.Android.Support.CustomTabs   |   28.0.0.3      |             | Package functionality included with framework |
| Xamarin.Android.Support.Design        |   28.0.0.3      |             | Package functionality included with framework |
| Xamarin.Essentials                    |   1.5.3.2       |             | Package functionality included with framework |

### Project upgrade details
This section contains details about each project upgrade and modifications that need to be done in the project.

#### fiskaltrust.Middleware.Demo.csproj modifications

Project properties changes:
  - Target framework should be changed from `MonoAndroid,Version=v9.0` to `net8.0-android`
  - Project format should be converted to SDK-style
  - Add .NET MAUI workload references

NuGet packages changes:
  - Microsoft.Bcl.AsyncInterfaces should be updated from `1.1.1` to `8.0.0` (*recommended for .NET 8.0*)
  - Newtonsoft.Json should be updated from `12.0.3` to `13.0.4` (*security vulnerability*)
  - System.Net.Http should be removed (*package functionality included with framework*)
  - Xamarin.Android.Support.Core.Utils should be removed (*package functionality included with framework*)
  - Xamarin.Android.Support.CustomTabs should be removed (*package functionality included with framework*)
  - Xamarin.Android.Support.Design should be removed (*package functionality included with framework*)
  - Xamarin.Essentials should be removed (*package functionality included with framework*)

Feature upgrades:
  - Convert Android-specific intents and activities to .NET MAUI compatible implementations
  - Update namespace imports from Xamarin.* to Microsoft.Maui.*
  - Migrate Android.App.Activity references to .NET MAUI equivalents
  - Update platform-specific code to use .NET MAUI platform abstractions

Other changes:
  - Update using statements to reference .NET MAUI namespaces
  - Migrate SarAwaiter functionality to .NET MAUI platform services
  - Update Android logging to use .NET MAUI logging abstractions