using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace RadioMountPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly RadioMountPlugin plugin;
    private float testVolume = 0.5f; // Declare here to retain value

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
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
