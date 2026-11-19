using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace WoLSays;

public sealed class Plugin : IDalamudPlugin
{
    private const string Command = "/wolsays";
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commands;
    private readonly WindowSystem windows = new("WoLSays");
    private readonly Overlay overlay;
    private readonly SettingsWindow settingsWindow;

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commands, ITextureProvider textures)
    {
        this.pluginInterface = pluginInterface;
        this.commands = commands;

        var settings = pluginInterface.GetPluginConfig() as OverlaySettings ?? new OverlaySettings();
        overlay = new Overlay(textures, pluginInterface.AssemblyLocation.DirectoryName!, settings);
        settingsWindow = new SettingsWindow(pluginInterface, settings);
        windows.AddWindow(overlay);
        windows.AddWindow(settingsWindow);
        commands.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the WoL Says overlay, or add config for settings."
        });

        pluginInterface.UiBuilder.Draw += windows.Draw;
        pluginInterface.UiBuilder.OpenMainUi += Show;
        pluginInterface.UiBuilder.OpenConfigUi += ShowSettings;
    }

    private void OnCommand(string command, string arguments)
    {
        if (arguments.Trim().Equals("config", StringComparison.OrdinalIgnoreCase))
            ShowSettings();
        else
            overlay.Toggle();
    }

    private void Show() => overlay.IsOpen = true;

    private void ShowSettings() => settingsWindow.IsOpen = true;

    public void Dispose()
    {
        pluginInterface.UiBuilder.OpenConfigUi -= ShowSettings;
        pluginInterface.UiBuilder.OpenMainUi -= Show;
        pluginInterface.UiBuilder.Draw -= windows.Draw;
        commands.RemoveHandler(Command);
        windows.RemoveAllWindows();
    }
}
