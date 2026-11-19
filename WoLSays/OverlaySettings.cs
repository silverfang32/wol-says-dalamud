using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

namespace WoLSays;

[Serializable]
public sealed class OverlaySettings : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool LockPosition { get; set; }
    public bool DisableMouseoverHints { get; set; } = true;
    public int BackgroundOpacity { get; set; } = 94;
}

internal sealed class SettingsWindow(IDalamudPluginInterface pluginInterface, OverlaySettings settings)
    : Window("WoL Says Settings###WoLSaysSettings", ImGuiWindowFlags.AlwaysAutoResize)
{
    public override void Draw()
    {
        bool locked = settings.LockPosition;
        bool disableHints = settings.DisableMouseoverHints;
        int opacity = settings.BackgroundOpacity;
        bool lockChanged = ImGui.Checkbox("Lock overlay position", ref locked);
        bool hintsChanged = ImGui.Checkbox("Disable mouseover hints", ref disableHints);
        bool opacityChanged = ImGui.SliderInt("Background opacity", ref opacity, 20, 100, "%d%%");
        if (lockChanged || hintsChanged || opacityChanged)
        {
            settings.LockPosition = locked;
            settings.DisableMouseoverHints = disableHints;
            settings.BackgroundOpacity = opacity;
        }

        if (lockChanged || hintsChanged || ImGui.IsItemDeactivatedAfterEdit())
            pluginInterface.SavePluginConfig(settings);
    }
}
