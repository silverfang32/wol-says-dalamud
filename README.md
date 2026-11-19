# WoL Says for Dalamud

WoL Says (Warrior of Light Says) is a local, manual FFXIV overlay for Kefka Says. Click the two set headings, grouped mechanic icons, Inferno and Tsunami shapes, or floor positions to record an encounter. **Reset** clears the board. A fresh plugin load starts with an empty board.

## Install from Dalamud

In FFXIV, run `/xlsettings`. Under **Experimental → Custom Plugin Repositories**, add and enable this URL, then save your settings.

```text
https://raw.githubusercontent.com/silverfang32/wol-says-dalamud/main/pluginmaster.json
```

Open `/xlplugins`, search for **WoL Says**, and install it. Run `/wolsays` to open the overlay.

## Build

Use .NET 10 and API 15 assemblies from XIVLauncher. Launch FFXIV through XIVLauncher with Dalamud enabled at least once to populate the developer assemblies. For a custom installation, set `DALAMUD_HOME` to the folder containing `Dalamud.dll` and `Dalamud.Bindings.ImGui.dll`.

The default project configuration uses `Dalamud.NET.Sdk/15.0.0`, as shown in [SamplePlugin](https://github.com/goatcorp/SamplePlugin/blob/master/SamplePlugin/SamplePlugin.csproj) and the [API 15 release notes](https://dalamud.dev/versions/v15/).

```powershell
dotnet build .\WoLSays\WoLSays.csproj -c Release
```

For a local build without NuGet access, compile directly against the installed API 15 assemblies. The default reference directory is `%APPDATA%\XIVLauncher\addon\Hooks\dev`.

```powershell
dotnet build .\WoLSays\WoLSays.csproj -c Release -p:UseLocalDalamudReferences=true
```

## Load locally

1. For the local-reference build above, the DLL is at `WoLSays\bin\Release\net10.0-windows\WoLSays.dll` relative to this repository. For the official SDK build, use the DLL path printed by `dotnet build`. Keep `WoLSays.json` and the `Assets` folder alongside the DLL.
2. In FFXIV, run `/xlsettings`. Under **Experimental → Dev Plugin Locations**, add the full path to **WoLSays.dll**.
3. Run `/xlplugins`. Open **Dev Tools → Installed Dev Plugins** and enable **WoL Says**.
4. Run `/wolsays` to show or hide the widget. Drag the three-line handle to move it. Run `/wolsays config` or use the plugin’s configuration button to lock its position or adjust the background opacity.

[Dalamud developer plugin loading guide](https://dalamud.dev/faq/getting-started/#how-do-i-hot-reload-my-plugin)

## Tests

```powershell
dotnet run --project .\tests\WoLSays.Tests.csproj -c Release
```

The test runner compiles the encounter model directly and needs only .NET 10. It covers set value inheritance, false membership, family reconciliation, optional choices, visibility restoration, shapes, floor positions, and reset.

For an in-game smoke test, confirm the plugin enables, both emotes render, all eight grouped choices are clickable on a fresh board, and the move handle and settings work at your Dalamud UI scale.
