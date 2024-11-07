using Dalamud.Configuration;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;
using NAudio.Wave;
using RadioMountPlugin.Windows;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace RadioMountPlugin;

public sealed class RadioMountPlugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;

    public string Name => "Radio Mount Plugin";
    private const string CommandName = "/radiomount";

    public PluginConfiguration Configuration { get; init; }
    private WaveOutEvent radioPlayer;
    private MediaFoundationReader? streamReader = null;

    private readonly WindowSystem windowSystem = new("RadioMountPlugin");
    private ConfigWindow configWindow;
    private MainWindow mainWindow;

    public RadioMountPlugin()
    {
        // Load the configuration
        Configuration = PluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        Configuration.Initialize(PluginInterface);

        // Initialize radioPlayer with the configured volume
        radioPlayer = new WaveOutEvent();
        radioPlayer.Volume = Configuration.Volume; // Set initial volume from config

        // Initialize windows
        configWindow = new ConfigWindow(this);
        mainWindow = new MainWindow(this, "path/to/goatImage.png", TextureProvider); // Update the path accordingly

        windowSystem.AddWindow(configWindow);
        windowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Radio Mount Plugin configuration."
        });

        CommandManager.AddHandler("/radiovolume", new CommandInfo(OnVolumeCommand)
        {
            HelpMessage = "Set the volume for the Radio Mount Plugin. Usage: /radiovolume <0-100>"
        });

        // Register UI callbacks
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        // Register condition change handler for mount events
        Condition.ConditionChange += OnConditionChange;
    }

    private void OnVolumeCommand(string command, string args)
    {
        if (float.TryParse(args, out float volumePercentage))
        {
            // Clamp volume between 0 and 100
            volumePercentage = Math.Clamp(volumePercentage, 0, 100);
            float volume = volumePercentage / 100.0f;

            // Set the volume on the player
            SetVolume(volume);

            // Update the configuration and save
            Configuration.Volume = volume;
            Configuration.Save();

            Chat.Print($"Radio volume set to {volumePercentage}%.");
        }
        else
        {
            Chat.Print("Please provide a valid number between 0 and 100.");
        }
    }
    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.Mounted && value)
        {
            // Player is mounting as the driver
            PlayRadio();

            // Update configuration to store driver’s stream URL for passenger access
            Configuration.RadioUrl = Configuration.RadioUrl; // Driver sets URL
            Configuration.Save();
        }
        else if (flag == ConditionFlag.Mounted && !value)
        {
            StopRadio();
        }
        else if (flag == ConditionFlag.Mounted2 && value)
        {
            // Player is mounting as a passenger
            PlayPassengerRadio(Configuration.RadioUrl);
        }
        else if (flag == ConditionFlag.Mounted2 && !value)
        {
            StopRadio();
        }
    }

    private void PlayPassengerRadio(string streamUrl)
    {
        try
        {
            if (radioPlayer == null)
            {
                Chat.Print("Radio player is not initialized for passenger.");
                return;
            }

            streamReader = new MediaFoundationReader(streamUrl);
            radioPlayer.Init(streamReader);
            radioPlayer.Play();
        }
        catch (Exception ex)
        {
            Chat.Print($"Error playing passenger radio: {ex.Message}");
            StopRadio();
        }
    }

    private void PlayRadio()
    {
        try
        {
            if (radioPlayer == null)
            {
                Chat.Print("Radio player is not initialized.");
                return;
            }

            streamReader = new MediaFoundationReader(Configuration.RadioUrl);
            radioPlayer.Init(streamReader);
            radioPlayer.Play();
        }
        catch (Exception ex)
        {
            Chat.Print($"Error playing radio: {ex.Message}");
            StopRadio(); // Clean up in case of an error
        }
    }

    private void StopRadio()
    {
        try
        {
            if (radioPlayer == null)
            {
                Chat.Print("Radio player is not initialized.");
                return;
            }

            radioPlayer.Stop();
            streamReader?.Dispose();
            streamReader = null; // Reset streamReader to prevent reuse
        }
        catch (Exception ex)
        {
            Chat.Print($"Error stopping radio: {ex.Message}");
        }
    }


    public void Dispose()
    {
        windowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler("/radiovolume");
        Condition.ConditionChange -= OnConditionChange;
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

        StopRadio();
        radioPlayer?.Dispose();
        streamReader?.Dispose();
    }

    private void OnCommand(string command, string args) => ToggleConfigUI();

    private void DrawUI() => windowSystem.Draw();

    public void ToggleConfigUI() => configWindow.Toggle();
    public void ToggleMainUI() => mainWindow.Toggle();
    public void SetVolume(float volume)
    {
        if (radioPlayer != null)
        {
            radioPlayer.Volume = volume; // Update the volume directly
            Configuration.Volume = volume; // Ensure configuration reflects the current volume
            Configuration.Save();
        }
        else
        {
            Chat.Print("Radio player is not initialized.");
        }
    }
}

// Configuration class to manage plugin settings
public class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public string RadioUrl { get; set; } = "https://media-ssl.musicradio.com/CapitalUK";
    public float Volume { get; set; } = 0.5f;

    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = false;
    public bool IsConfigWindowMovable { get; set; } = true;

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save() => pluginInterface?.SavePluginConfig(this);
}
