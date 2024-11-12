using Dalamud.Interface.Utility;
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

        this.Size = ImGuiHelpers.ScaledVector2(600, 400);
        this.SizeCondition = ImGuiCond.Once;
    }

    public override void Draw()
    {
        ImGui.Text("Radio Stream URL:");
        var radioUrl = plugin.Configuration.RadioUrl;
        ImGui.InputText("##RadioUrl", ref radioUrl, 100);
        plugin.Configuration.RadioUrl = radioUrl;

        bool autoStopOnDismount = plugin.Configuration.AutoStopOnDismount;
        if (ImGui.Checkbox("Auto-stop music on dismount", ref autoStopOnDismount))
        {
            plugin.Configuration.AutoStopOnDismount = autoStopOnDismount;
            plugin.Configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Toggle whether music stops automatically when you dismount.");

        bool autoStartOnMount = plugin.Configuration.AutoStartOnMount;
        if (ImGui.Checkbox("Auto-start music on mount", ref autoStartOnMount))
        {
            plugin.Configuration.AutoStartOnMount = autoStartOnMount;
            plugin.Configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Toggle whether music starts automatically when you mount.");

        if (ImGui.Button("Save"))
        {
            plugin.Configuration.Save();
            ImGui.Text("Settings saved.");
        }

        ImGui.Text("Radio Stream for mounts or on foot.");

        ImGui.Text("Use a live radio for synced music with friends!");

        ImGui.Text("Plays even if you are passenger on a mount.");

        ImGui.Text("Compatable & tested with Icecast Media Server/Mixxx");

        ImGui.Text("Not all radio services will work.");
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
