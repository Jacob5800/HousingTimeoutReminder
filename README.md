# Housing Timeout Reminder

Maintained fork of [thakyZ/HousingTimeoutReminder](https://github.com/thakyZ/HousingTimeoutReminder), updated to Dalamud API 15 and .NET 10.

The plugin reminds you to visit configured housing plots before the automatic demolition timer expires. The original AGPL-1.0 license and upstream attribution are retained.

## Build

- Install the .NET 10 SDK.
- Install Dalamud API 15 through XIVLauncher, or provide its development files under `%APPDATA%\XIVLauncher\addon\Hooks\dev`.
- Build with `dotnet build HousingTimeoutReminder.sln --configuration Release`.

ECommons is restored from its published NuGet package. Its MIT license is included in [THIRD-PARTY-NOTICES-ECommons.txt](THIRD-PARTY-NOTICES-ECommons.txt).
