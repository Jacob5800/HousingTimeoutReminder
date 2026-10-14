using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using ECommons.DalamudServices;

using Dalamud.Bindings.ImGui;
using NekoBoiNick.FFXIV.DalamudPlugin.HousingTimeoutReminder.Configuration;
using NekoBoiNick.FFXIV.DalamudPlugin.HousingTimeoutReminder.Extensions;
using NekoBoiNick.FFXIV.DalamudPlugin.HousingTimeoutReminder.Handler;

namespace NekoBoiNick.FFXIV.DalamudPlugin.HousingTimeoutReminder.UI;

/// <summary>
/// The class for the settings of the plugin.
/// </summary>
public class SettingsUI : Window, IDisposable {
  private string backupImportPath = string.Empty;
  private string backupStatus = string.Empty;
  /// <summary>
  /// The default window flags of the settings ui.
  /// We disable the scroll with mouse because we have it layed out to have
  /// fixed elements.
  /// </summary>
  private const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar |
    ImGuiWindowFlags.NoScrollWithMouse;

  /// <summary>
  /// The name of the window.
  /// </summary>
  public static string Name => "Housing Timeout Reminder Settings";

  /// <summary>
  /// The default constructor.
  /// </summary>
  public SettingsUI() : base(Name, WindowFlags) {
    Size = ImGuiHelpers.ScaledVector2(630.0f, 550.0f);
    SizeCondition = ImGuiCond.Always;
  }

  /// <summary>
  /// Disposal of this class on unload.
  /// </summary>
  public void Dispose() {
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// The max number of wards.
  /// TODO: Update ward counts when necessary.
  /// </summary>
  private const int WardMax = 32;

  /// <summary>
  /// The max number of plots.
  /// </summary>
  private const int PlotMax = 60;

  private string housingDetectionStatus = string.Empty;

  // ReSharper disable once MemberCanBeMadeStatic.Local
  /// <summary>
  /// A function to shorten the repetitiveness of <see cref="CheckConsistency"/>
  /// Gets the new date time offset required for when the housing plot is outside of the check date.
  /// </summary>
  /// <param name="playerConfig">The player config to check against.</param>
  /// <param name="type">The type of housing to check.</param>
  /// <param name="next">If to check for the next limit date.</param>
  /// <param name="now">If to check for the date from now.</param>
  /// <returns>The offset date from the last visit of <see cref="type"/>.</returns>
  private static DateTimeOffset ShortenFunction(PerPlayerConfig playerConfig, HousingType type, bool next = false, bool now = false) {
    long visit = type switch {
      HousingType.FreeCompanyEstate => playerConfig.FreeCompanyEstate.LastVisit,
      HousingType.PrivateEstate => playerConfig.PrivateEstate.LastVisit,
      _ => 0
    };

    var date = DateTimeOffset.FromUnixTimeSeconds(visit);

    if (next) {
      return date.AddDays(System.PluginConfig.DaysToWait);
    }

    if (now) {
      return DateTimeOffset.Now;
    }

    return date;
  }

  /// <summary>
  /// Gets the new date time offset required for when the housing plot is outside of the check date.
  /// </summary>
  /// <param name="playerConfig">The player config to check against.</param>
  /// <param name="type">The type of housing to check.</param>
  /// <returns>A <see cref="Tuple{String, String}"/> with the last check formatted timestamp,
  /// and the next timelimit formatted timestamp.</returns>
  private static (string Last, string Next) CheckConsistency(PerPlayerConfig playerConfig, HousingType type) {
    var lastStamp = ShortenFunction(playerConfig, type);
    var nextStamp = ShortenFunction(playerConfig, type, next: true);

    if (lastStamp.ToUnixTimeSeconds() <= 946627200 && nextStamp.ToUnixTimeSeconds() <= 946627200) {
      return (Last: "Never", Next: "Now");
    }

    if (lastStamp <= nextStamp && nextStamp >= ShortenFunction(playerConfig, type, now: true)) {
      return (Last: $"{lastStamp:yyyy-MM-dd HH:mm:ss}", Next: $"{nextStamp:yyyy-MM-dd HH:mm:ss}");
    }

    if (nextStamp <= ShortenFunction(playerConfig, type, now: true)) {
      return (Last: $"{lastStamp:yyyy-MM-dd HH:mm:ss}", Next: "Now");
    }

    return (Last: "Never", Next: "Now");
  }

  public float GetMaxWidth() {
    var windowSize = 128f;
    if (this.Size.HasValue) {
      windowSize = this.Size.Value.X - ImGui.GetStyle().WindowPadding.X;
    }

    return windowSize;// ImGuiHelpers.ScaledVector2(windowSize, 0).X;
  }

  /// <summary>
  /// Draw the housing config dropdown based on a <see cref="HousingType"/>
  /// </summary>
  /// <param name="playerConfig">The player config to check against.</param>
  /// <param name="type">The type of housing to check.</param>
  /// <param name="indent">Indenting check to make the child object better formatted.</param>
  public bool DrawHousingDropDown(PerPlayerConfig playerConfig, HousingType type, float indent = 0f) {
    var playerId = playerConfig.DisplayName;
    ImGui.PushID($"{type}-{playerId}");
    try {
      if (!ImGui.CollapsingHeader(HousingTypeEnumHelper.GetDescriptor(type), ImGuiTreeNodeFlags.DefaultOpen)) return true;
      var property = playerConfig.GetOfType(type);
      if (type == HousingType.FreeCompanyEstate) {
        ImGui.TextWrapped(playerConfig.FcEstateAccess switch {
          true => $"FC rank: {playerConfig.FcRankName}. Estate access allowed.",
          false => $"FC rank: {playerConfig.FcRankName}. No estate access; FC reminders are ignored.",
          _ => "FC estate permission unknown. Open the game's FC Members tab once to read your rank permissions.",
        });
      }
      if (property.IsValid) {
        ImGui.Text($"{DistrictEnumHelper.GetDescriptor(property.District)} - Ward {property.Ward}, Plot {property.Plot}");
        var (last, next) = CheckConsistency(playerConfig, type);
        ImGui.Text($"Last inside visit: {(last == "Never" ? "Not recorded" : last)}");
        ImGui.Text($"Reminder: {(next == "Now" ? "Visit needed" : next)}");
      } else {
        ImGui.TextWrapped("No house detected. Use Detect my housing on this character, or enter an address below.");
      }
      var enabled = property.Enabled;
      if (ImGui.Checkbox("Enable reminders", ref enabled)) {
        property.Enabled = enabled;
        System.PluginConfig.Save();
      }
      var location = playerConfig.IsCurrentPlayerConfig ? Handler.HousingManager.GetCurrentLocation(Svc.ClientState.TerritoryType) : null;
      var canRecord = location is not null && HousingTimer.CanRecordVisit(System.GetCurrentPlayerID(), playerConfig.PlayerID!, location.IsInside, location.IsApartment, location.Room,
          Svc.Objects.LocalPlayer?.CurrentWorld.RowId ?? 0, playerConfig.PlayerID?.HomeWorld ?? 0)
        && Handler.HousingManager.From(playerConfig.PlayerID!, type)?.Equals(location) == true && property.Enabled;
      ImGui.BeginDisabled(!canRecord);
      if (ImGui.Button("Record visit now")) Plugin.CheckTimers();
      ImGui.EndDisabled();
      ImGui.SameLine();
      if (ImGui.Button("Clear visit history")) {
        property.LastVisit = 0;
        if (property is WardProperty wardProperty) wardProperty.IsDismissed = false;
        System.PluginConfig.Save();
      }
      if (!canRecord) ImGui.TextDisabled("Record a visit by entering this house on its character.");
      using (var manual = ImRaii.TreeNode("Edit address manually")) {
        if (manual.Success) {
          var changed = false;
          ImGui.SetNextItemWidth(200);
          using (var combo = ImRaii.Combo("District", DistrictEnumHelper.GetDescriptor(property.District))) {
            if (combo.Success) {
              foreach (var (name, district) in DistrictEnumHelper.GetDescriptors()) {
                if (ImGui.Selectable(name, property.District == district)) { property.District = district; changed = true; }
              }
            }
          }
          int ward = property.Ward, plot = property.Plot;
          ImGui.SetNextItemWidth(140);
          if (ImGuiRaii.InputIntMinMax("Ward", ref ward, 1, 5, 1, WardMax)) { property.Ward = (sbyte)ward; changed = true; }
          ImGui.SetNextItemWidth(140);
          if (ImGuiRaii.InputIntMinMax("Plot", ref plot, 1, 5, 1, PlotMax)) { property.Plot = (sbyte)plot; changed = true; }
          if (changed) {
            property.LastVisit = 0;
            if (property is WardProperty wardProperty) wardProperty.IsDismissed = false;
            System.PluginConfig.Save();
          }
        }
      }
      ImGui.Separator();
      return true;
    } catch (Exception exception) {
      Svc.Log.Error(exception, "Failed to draw housing settings.");
      return false;
    } finally { ImGui.PopID(); }
  }
  /// <summary>
  /// Draws player timeout settings for a single user.
  /// </summary>
  /// <param name="playerConfig">The player config involved with the single player.</param>
  /// <param name="indent">Indenting check to make the child object better formatted.</param>
  public void DrawUserTimeoutSettings(PerPlayerConfig playerConfig, float indent = 0f) {
    var playerId = playerConfig.DisplayName;
    if (System.PluginConfig.EnableFcReminders && !DrawHousingDropDown(playerConfig, HousingType.FreeCompanyEstate, indent + ScaledIndent5)) {
      ImGui.Text($"Failed to draw {HousingTypeEnumHelper.GetDescriptor(HousingType.FreeCompanyEstate).ToLower()} tab.");
    }

    if (!DrawHousingDropDown(playerConfig, HousingType.PrivateEstate, indent + ScaledIndent5)) {
      ImGui.Text($"Failed to draw {HousingTypeEnumHelper.GetDescriptor(HousingType.PrivateEstate).ToLower()} tab.");
    }

    // ReSharper disable once InvertIf
    if (ImGui.Button($"Show dismissed reminders again##Reset-{playerId}")) {
      Plugin.CheckTimers();
      playerConfig.ResetWarnings();
    }
  }

  public bool DrawGlobalTab() {
    try {
      using (var scrolling = ImRaii.Child($"scrolling##HousingTimeoutReminder-{nameof(DrawGlobalTab)}", ImGuiHelpers.ScaledVector2(0, -(25 + ImGui.GetStyle().ItemSpacing.Y)), false)) {
        if (!scrolling.Success) {
          return true;
        }

        ImGui.Text("Remind me after this many days without an inside visit");
        ImGui.SameLine();
        using (var i = ImRaii.ItemWidth(100)) {
          if (i.Count > 0) {
            var daysToWait = (int)System.PluginConfig.DaysToWait;

            if (ImGuiRaii.InputIntMinMax("##GlobalDaysToWait", ref daysToWait, 1, 5, 1, 30)) {
              System.PluginConfig.DaysToWait = (ushort)daysToWait;
            }
          }
        }

        var displayAllPlayers = System.PluginConfig.ShowAllPlayers;

        if (ImGui.Checkbox("Show reminders for all saved characters##GlobalShowAllPlayers", ref displayAllPlayers)) {
          System.PluginConfig.ShowAllPlayers = displayAllPlayers;
        }
        var showBar = System.PluginConfig.ShowServerBar;
        if (ImGui.Checkbox("Show HT server-bar button", ref showBar)) { System.PluginConfig.ShowServerBar = showBar; System.PluginConfig.Save(); }
        var colorBar = System.PluginConfig.ColorServerBarUrgency;
        if (ImGui.Checkbox("Color HT by reminder urgency", ref colorBar)) { System.PluginConfig.ColorServerBarUrgency = colorBar; System.PluginConfig.Save(); }
        ImGui.TextWrapped("Yellow: reminder within 3 days. Red: reminder due or no visit recorded. These are your reminder dates, not the game's demolition timer.");
        var echoVisit = System.PluginConfig.EchoVisitRecorded;
        if (ImGui.Checkbox("Echo when an interior visit is recorded", ref echoVisit)) { System.PluginConfig.EchoVisitRecorded = echoVisit; System.PluginConfig.Save(); }
        if (ImGui.Button("Export houses and visits")) {
          try {
            var directory = Config.ConfigDirectory;
            global::System.IO.Directory.CreateDirectory(directory);
            var path = global::System.IO.Path.Combine(directory, $"housing-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            global::System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(System.PluginConfig.PlayerConfigs, Newtonsoft.Json.Formatting.Indented));
            Svc.Chat.Print($"Housing backup saved: {path}");
          } catch (Exception ex) { Svc.Log.Error(ex, "Housing backup failed"); }
        }
        ImGui.SameLine();
        if (ImGui.Button("Open backup folder")) Dalamud.Utility.Util.OpenLink(Config.ConfigDirectory);
        ImGui.TextWrapped("Export contains all saved characters, house addresses and visit dates. Move the JSON backup to your new PC; it contains character names and home worlds.");
        ImGui.InputText("Backup JSON path", ref backupImportPath, 1024);
        if (ImGui.Button("Import houses and visits (merge)")) {
          try {
            var file = new global::System.IO.FileInfo(backupImportPath.Trim().Trim('"'));
            if (!file.Exists || file.Length > 5 * 1024 * 1024) throw new InvalidOperationException("Select an existing backup JSON smaller than 5 MB.");
            var entries = Newtonsoft.Json.JsonConvert.DeserializeObject<global::System.Collections.Generic.List<PerPlayerConfig>>(
              global::System.IO.File.ReadAllText(file.FullName)) ?? throw new InvalidOperationException("Invalid backup.");
            if (entries.Count > 1000 || entries.Any(entry => entry is null || entry.PlayerID is null || entry.PrivateEstate is null || entry.FreeCompanyEstate is null))
              throw new InvalidOperationException("Backup contains invalid character records.");
            var added = 0;
            foreach (var entry in entries) {
              var existing = System.PluginConfig.PlayerConfigs.FirstOrDefault(player => player.PlayerID == entry.PlayerID);
              if (existing is null) { System.PluginConfig.PlayerConfigs.Add(entry); added++; }
              else {
                foreach (var pair in new[] { (Current: existing.PrivateEstate, Imported: entry.PrivateEstate), (Current: existing.FreeCompanyEstate, Imported: entry.FreeCompanyEstate) }) {
                  if (!pair.Imported.IsValid) continue;
                  if (!pair.Current.IsValid) {
                    pair.Current.District = pair.Imported.District; pair.Current.Ward = pair.Imported.Ward;
                    pair.Current.Plot = pair.Imported.Plot; pair.Current.Enabled = pair.Imported.Enabled;
                    pair.Current.LastVisit = pair.Imported.LastVisit;
                  } else if (pair.Current.District == pair.Imported.District && pair.Current.Ward == pair.Imported.Ward && pair.Current.Plot == pair.Imported.Plot)
                    pair.Current.LastVisit = Math.Max(pair.Current.LastVisit, pair.Imported.LastVisit);
                }
              }
            }
            System.PluginConfig.Save();
            backupStatus = $"Imported {added} new character(s). Matching addresses keep the latest visit. Conflicting existing addresses are preserved.";
          } catch (Exception ex) { backupStatus = $"Import failed: {ex.Message}"; Svc.Log.Error(ex, "Housing backup import failed"); }
        }
        if (!string.IsNullOrEmpty(backupStatus)) ImGui.TextWrapped(backupStatus);
        var enableFc = System.PluginConfig.EnableFcReminders;
        if (ImGui.Checkbox("Enable FC estate reminders", ref enableFc)) {
          System.PluginConfig.EnableFcReminders = enableFc;
          System.PluginConfig.Save();
          System.WarningUI.Pagination.WrapPages();
        }
        ImGui.TextWrapped("Turn this off to hide FC reminders for every saved character. Personal-house reminders are unaffected.");
      }
    } catch (Exception ex) {
      Svc.Log.Error(ex, nameof(DrawGlobalTab));
      return false;
    }

    return true;
  }

  public bool DrawCurrentPlayerTab() {
    try {
      using (var scrolling = ImRaii.Child($"scrolling##HousingTimeoutReminder-{nameof(DrawCurrentPlayerTab)}", ImGuiHelpers.ScaledVector2(0, -(25 + ImGui.GetStyle().ItemSpacing.Y)), false)) {
        if (!scrolling.Success) {
          return true;
        }

        if (System.IsLoggedIn && Config.PlayerConfiguration is not null) {
          ImGui.Text($"Housing Configuration for {Config.PlayerConfiguration.DisplayName}:");
          ImGui.TextWrapped("Owned housing is detected automatically. If details are missing, open your Teleport list and try detection again.");
          if (ImGui.Button("Detect my housing")) {
            var count = Handler.HousingManager.DetectOwnedHousing(true);
            housingDetectionStatus = count > 0
              ? $"Detected {count} owned housing location(s). Visit times are recorded when you enter the property."
              : "No owned housing data is available yet. Open Teleport on your home world, then try again.";
          }
          if (!string.IsNullOrEmpty(housingDetectionStatus)) ImGui.TextWrapped(housingDetectionStatus);
          DrawUserTimeoutSettings(Config.PlayerConfiguration);
        }
      }
    } catch (Exception ex) {
      Svc.Log.Error(ex, nameof(DrawCurrentPlayerTab));
      return false;
    }

    return true;
  }

  public static float ScaledIndent5 => ImGuiHelpers.ScaledVector2(5.0f).X;

  public bool DrawOtherPlayersTab() {
    try {
      using (var scrolling = ImRaii.Child($"scrolling##HousingTimeoutReminder-{nameof(DrawOtherPlayersTab)}", ImGuiHelpers.ScaledVector2(0, -(25 + ImGui.GetStyle().ItemSpacing.Y)), false)) {
        if (!scrolling.Success) {
          return true;
        }

#if DEBUG
        ImGui.Text($"System.PluginConfig.PlayerConfigs.Count: {System.PluginConfig.PlayerConfigs.Count}");
#endif

        ImGui.TextWrapped("Each character is remembered after you log into it with this plugin enabled. Its saved housing reminders can appear while you play another character.");
        foreach (var config in System.PluginConfig.PlayerConfigs.Where(config => !config.IsCurrentPlayerConfig)) {
          using (var header = ImRaii.TreeNode($"Housing Configuration for {config.DisplayName}", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.NoTreePushOnOpen)) {
            if (header.Success) {
              ImGui.Indent(ScaledIndent5);
              DrawUserTimeoutSettings(config, ScaledIndent5);
              ImGui.Indent(-ScaledIndent5);
            }
          }
        }
      }
    } catch (Exception ex) {
      Svc.Log.Error(ex, nameof(DrawOtherPlayersTab));
      return false;
    }

    return true;
  }

  /// <inheritdoc />
  public override void Draw() {
    using (var tabBar = ImRaii.TabBar("ConfigTabs")) {
      if (tabBar.Success) {
        if (System.IsLoggedIn) {
          using (var tabItem = ImRaii.TabItem("Current character")) {
            if (tabItem.Success && !DrawCurrentPlayerTab()) {
              ImGui.Text("Failed to draw current player settings tab.");
            }
          }
        }

        using (var tabItem = ImRaii.TabItem("Saved characters")) {
          if (tabItem.Success && !DrawOtherPlayersTab()) {
            ImGui.Text("Failed to draw other players settings tab.");
          }
        }
        using (var tabItem = ImRaii.TabItem("Settings")) {
          if (tabItem.Success && !DrawGlobalTab()) {
            ImGui.Text("Failed to draw global settings tab.");
          }
        }
      }
    }

    if (ImGui.Button("Save")) {
      HousingTimer.Update();
    }

    ImGui.SameLine();

    if (ImGui.Button("Save and close")) {
      HousingTimer.Update();
      this.IsOpen = false;
    }

    ImGuiRaii.VerticalSeparator();
    ImGui.Text("Reposition");
    ImGui.SameLine();

    var isTesting = System.PluginInstance.Repositioning;

    if (ImGui.Checkbox("##isTesting", ref isTesting)) {
      System.PluginInstance.Repositioning = isTesting;
    }

    ImGuiRaii.VerticalSeparator();

    if (ImGui.Button("Show dismissed reminders again")) {
      Plugin.CheckTimers();
      System.PluginConfig.ResetAll();
    }

#if DEBUG
    ImGuiRaii.VerticalSeparator();

    if (ImGui.Button("Debug")) {
      System.DebugUI.IsOpen = true;
    }
#endif
  }
}
