using System;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using ECommons.DalamudServices;

using NekoBoiNick.FFXIV.DalamudPlugin.HousingTimeoutReminder.Configuration;
using NekoBoiNick.FFXIV.DalamudPlugin.HousingTimeoutReminder.Handler;

namespace NekoBoiNick.FFXIV.DalamudPlugin.HousingTimeoutReminder;

// Ignore Spelling: htimeout

/// <summary>
/// The Dalamud Plugin Library
/// </summary>
public class Plugin : IDalamudPlugin {
  /// <summary>
  /// The name of the plugin.
  /// </summary>
  internal static string Name => "Housing Timeout Reminder";

  /// <summary>
  /// The plugin's main command name.
  /// </summary>
  private const string CommandName = "/htimeout";

  /// <summary>
  /// Bool to test if repositioning the warning dialog.
  /// </summary>
  internal bool Repositioning { get; set; }
  private long nextHousingCheck;

  /// <summary>
  /// The Dalamud Plugin constructor
  /// </summary>
  /// <param name="pluginInterface">Argument passed by Dalamud</param>
  public Plugin(IDalamudPluginInterface pluginInterface) {
    System.PluginInstance = this;

    ECommons.ECommonsMain.Init(pluginInterface, System.PluginInstance);

    System.Init();

    Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand) {
      HelpMessage = "The config menu for the housing timer reminder plugin."
    });

    Svc.PluginInterface.UiBuilder.Draw += DrawUI;
    Svc.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    Svc.PluginInterface.UiBuilder.OpenMainUi += DrawConfigUI;
    Svc.ClientState.TerritoryChanged += OnTerritoryChanged;
    Svc.Framework.Update += OnFrameworkUpdate;
    Svc.ClientState.Logout += OnLogout;
    // ReSharper disable once InvertIf
    if (Svc.ClientState.IsLoggedIn) {
      OnTerritoryChanged(Svc.ClientState.TerritoryType);
    }
  }

  /// <summary>
  /// Dispose of the Dalamud Plugin safely.
  /// </summary>
  public void Dispose() {
    System.WindowSystem.RemoveAllWindows();
    System.WarningUI.Dispose();
    System.SettingsUI.Dispose();
#if DEBUG
    System.DebugUI.Dispose();
#endif
    Svc.PluginInterface.UiBuilder.Draw -= DrawUI;
    Svc.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
    Svc.PluginInterface.UiBuilder.OpenMainUi -= DrawConfigUI;
    Svc.ClientState.TerritoryChanged -= OnTerritoryChanged;
    Svc.Framework.Update -= OnFrameworkUpdate;
    Svc.ClientState.Logout -= OnLogout;
    Svc.Commands.RemoveHandler(CommandName);
    ECommons.ECommonsMain.Dispose();
    GC.SuppressFinalize(this);
  }
  private void OnFrameworkUpdate(IFramework framework) {
    if (Svc.Objects.LocalPlayer is not null) {
      //Task.Run(() => {
      if (Svc.Objects.LocalPlayer is IPlayerCharacter player) {
        var temp = new PlayerID(player);
        if (System.CachedCurrentPlayerId != temp) {
          System.CachedCurrentPlayerId = temp;
        }
      }
      //}).ContinueWith((Task task) => {
      //  Svc.Log.Error(task.Exception, "Error when trying to get current player ID.");
      //});
    }
    if (System.IsLoggedIn && System.GetCurrentPlayerID() is not null &&
        Environment.TickCount64 >= nextHousingCheck && ECommons.GenericHelpers.IsScreenReady()) {
      nextHousingCheck = Environment.TickCount64 + 5000;
      try {
        Handler.HousingManager.DetectOwnedHousing();
        FreeCompanyAccess.Refresh();
        CheckTimers();
      } catch (Exception exception) {
        Svc.Log.Error(exception, "Failed to detect owned housing.");
      }
    }
  }

  /// <summary>
  /// Checks the timers when necessary.
  /// </summary>
  internal static void CheckTimers(uint? territory = null) {
    if (territory is null && System.IsLoggedIn) {
      territory = Svc.ClientState.TerritoryType;
    }

    if (territory is null) {
      Svc.Log.Information("Territory is null");
      return;
    }

    HousingTimer.OnTerritoryChanged(territory.Value);
  }

  internal static bool IsWarningToBeDisplayed(uint? territory = null) {
    if (territory is null && System.IsLoggedIn) {
      territory = Svc.ClientState.TerritoryType;
    }

    if (territory is null) {
      return false;
    }

    return System.PluginConfig.PlayerConfigsWithWarnings.Count > 0;
  }

  /// <summary>
  /// The function to call when changing instance. Checks timers after.
  /// </summary>
  /// <param name="territoryId">The territory ID.</param>
  private void OnTerritoryChanged(uint territoryId) {
    // The framework callback reads game pointers after the loading screen clears.
    nextHousingCheck = 0;
  }

  /// <summary>
  /// The function to call when logging out.
  /// Unloads the housing timer.
  /// </summary>
  private static void OnLogout(int type, int code) {
    System.CachedCurrentPlayerId = null;
    // Do nothing for now.
  }

  /// <summary>
  /// In response to the slash command, just display our main UI
  /// </summary>
  /// <param name="command">The Command Name</param>
  /// <param name="args">The Arguments</param>
  private void OnCommand(string command, string args) {
    var argsParsed = !string.IsNullOrEmpty(args) ? args.Split(" ", StringSplitOptions.RemoveEmptyEntries).AsEnumerable().Last().ToLower() : string.Empty;
    if (argsParsed.Equals("check")) {
      CheckTimers();
    } else {
      System.SettingsUI.IsOpen ^= true;
    }
  }

  /// <summary>
  /// Draws the UI of the plugin.
  /// </summary>
  private void DrawUI() {
    System.WindowSystem.Draw();
    if (Repositioning) {
      System.WarningUI.IsOpen = true;
    } else {
      System.WarningUI.IsOpen = IsWarningToBeDisplayed();
    }
#if DEBUG
    System.DebugUI.IsOpen = System.SettingsUI.IsOpen;
#endif
  }

  /// <summary>
  /// Draws the UI of the settings menu of the plugin.
  /// </summary>
  public static void DrawConfigUI() {
    System.SettingsUI.IsOpen = true;
  }
}
