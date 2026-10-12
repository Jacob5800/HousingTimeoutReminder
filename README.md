# Housing Timeout Reminder

Maintained fork of [thakyZ/HousingTimeoutReminder](https://github.com/thakyZ/HousingTimeoutReminder), updated to Dalamud API 15 and .NET 10.

The plugin reminds you to visit configured housing plots before the automatic demolition timer expires. The original AGPL-1.0 license and upstream attribution are retained.

## Use

1. Install Housing Timeout Reminder from [the custom Dalamud feed](https://raw.githubusercontent.com/Jacob5800/DalamudPlugins/main/repo.json).
2. Log into a character. Its owned personal house and FC estate are filled in automatically when the game provides their ownership data.
3. Open `/htimeout` and select **Current character** to see the district, ward, plot, and recorded visit. If an address is missing, open the game's Teleport list on your home world and click **Detect my housing**. Manual editing is available as a fallback.
4. Enter the estate to record an inside visit. The ward, yard, apartments, and FC private chambers do not count.
5. Log into each alt once with the plugin enabled. Their houses are stored separately under **Saved characters**, and reminders can appear while playing another character. The plugin cannot discover characters you have never logged into locally.

**Settings** controls how many days to wait before reminding you, and whether to show all saved characters. Automatic detection does not create a visit timestamp. A newly detected house says **Not recorded** until an inside visit is observed.

Apartments and FC private chambers are not tracked. Reminders use locally recorded visits rather than the server's demolition timer; previous visits made without this plugin, visits by other FC members, and server demolition suspensions are not automatically known. The in-game estate timer remains the authority.

An observed visit to your FC estate refreshes saved alts that share that exact FC estate on the same world. Personal-house visits remain character-specific.

## Verification

Release and Debug builds are checked with the API 15 SDK. Run `dotnet run --project tests/HousingChecks/HousingChecks.csproj --configuration Release` after a Release build to validate housing address decoding, character identity, and inside-only visit rules.

Live ownership-data availability and the cleaned settings UI still require an in-game observation on a character with housing.

## Build

- Install the .NET 10 SDK.
- Install Dalamud API 15 through XIVLauncher, or provide its development files under `%APPDATA%\XIVLauncher\addon\Hooks\dev`.
- Build with `dotnet build HousingTimeoutReminder.sln --configuration Release`.

ECommons is restored from its published NuGet package. Its MIT license is included in [THIRD-PARTY-NOTICES-ECommons.txt](THIRD-PARTY-NOTICES-ECommons.txt).
