using System;
using System.Numerics;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;

namespace RadioMountPlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private string GoatImagePath;
    private RadioMountPlugin plugin;
    private ITextureProvider textureProvider;

    public MainWindow(RadioMountPlugin plugin, string goatImagePath, ITextureProvider textureProvider)
        : base("Mount Radio", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        GoatImagePath = goatImagePath;
        this.plugin = plugin;
        this.textureProvider = textureProvider;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("Radio Stream for mounts or on foot.");

        ImGui.Text("Use a live radio for synced music with friends!");

        ImGui.Text("Plays even if you are passenger on a mount.");

        if (ImGui.Button("Radio Settings"))
        {
            plugin.ToggleConfigUI();
        }

    }
}
