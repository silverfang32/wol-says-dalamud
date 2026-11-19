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
    public int BackgroundOpacity { get; set; } = 94;
}

internal sealed class SettingsWindow(IDalamudPluginInterface pluginInterface, OverlaySettings settings)
    : Window("WoL Says Settings###WoLSaysSettings", ImGuiWindowFlags.AlwaysAutoResize)
{
    public override void Draw()
    {
        bool locked = settings.LockPosition;
        int opacity = settings.BackgroundOpacity;
        bool lockChanged = ImGui.Checkbox("Lock overlay position", ref locked);
        bool opacityChanged = ImGui.SliderInt("Background opacity", ref opacity, 20, 100, "%d%%");
        if (lockChanged || opacityChanged)
        {
            settings.LockPosition = locked;
            settings.BackgroundOpacity = opacity;
        }

        if (lockChanged || ImGui.IsItemDeactivatedAfterEdit())
            pluginInterface.SavePluginConfig(settings);
    }
}
