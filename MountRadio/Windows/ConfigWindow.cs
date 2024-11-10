using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace RadioMountPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly RadioMountPlugin plugin;

    public ConfigWindow(RadioMountPlugin plugin) : base("Radio Mount Plugin Config")
    {
        this.plugin = plugin;

        // Set a larger default size
        this.Size = new Vector2(600, 400); // Adjust this as needed
        this.SizeCondition = ImGuiCond.Once; // Allow resizing after the initial size is set
    }

    public override void Draw()
    {
        ImGui.Text("Radio Stream URL:");
        var radioUrl = plugin.Configuration.RadioUrl;
        ImGui.InputText("##RadioUrl", ref radioUrl, 100);
        plugin.Configuration.RadioUrl = radioUrl;

        if (ImGui.Button("Save"))
        {
            plugin.Configuration.Save();
            ImGui.Text("Settings saved.");
        }

        bool autoStopOnDismount = plugin.Configuration.AutoStopOnDismount;

        if (ImGui.Checkbox("Auto-stop music on dismount", ref autoStopOnDismount))
        {
            // Update the configuration only if the value has changed
            plugin.Configuration.AutoStopOnDismount = autoStopOnDismount;
            plugin.Configuration.Save();
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Toggle whether music stops automatically when you dismount.");

    }

    public void Dispose() => GC.SuppressFinalize(this);
}
