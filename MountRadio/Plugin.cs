using Dalamud.Configuration;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;
using MountRadio;
using NAudio.CoreAudioApi;
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

    public Configuration Configuration { get; init; }
    private WasapiOut radioPlayer;
    private MediaFoundationReader? streamReader = null;

    private readonly WindowSystem windowSystem = new("RadioMountPlugin");
    private ConfigWindow configWindow;
    
    public RadioMountPlugin()
    {
        // Load the configuration
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        // Initialize radioPlayer with the configured volume
        radioPlayer = new WasapiOut(AudioClientShareMode.Shared, 200);
        radioPlayer.Volume = Configuration.Volume; // Set initial volume from config

        // Initialize the config window
        configWindow = new ConfigWindow(this);
        windowSystem.AddWindow(configWindow);

        // Register the main UI callback
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;

        // Register UI callbacks
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Register condition change handler for mount events
        Condition.ConditionChange += OnConditionChange;

        CommandManager.AddHandler("/radiosettings", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Radio Mount Plugin configuration."
        });

        CommandManager.AddHandler("/radiovolume", new CommandInfo(OnVolumeCommand)
        {
            HelpMessage = "Set the volume for the Radio Mount Plugin. Usage: /radiovolume <0-100>"
        });

        CommandManager.AddHandler("/radiotoggle", new CommandInfo(ToggleRadioCommand)
        {
            HelpMessage = "Toggle radio playback anytime."
        });

        CommandManager.AddHandler("/radioautostop", new CommandInfo(ToggleAutoStopCommand)
        {
            HelpMessage = "Toggle whether the radio stops automatically when you dismount."
        });

        CommandManager.AddHandler("/radioautostart", new CommandInfo(ToggleAutoStartCommand)
        {
            HelpMessage = "Toggle whether the radio starts automatically when you mount."
        });

    }

    private void ToggleAutoStopCommand(string command, string args)
    {
        // Toggle the AutoStopOnDismount setting
        Configuration.AutoStopOnDismount = !Configuration.AutoStopOnDismount;
        Configuration.Save();

        // Inform the user of the new state
        string status = Configuration.AutoStopOnDismount ? "enabled" : "disabled";
        Chat.Print($"Auto-stop on dismount is now {status}.");
    }
    private void ToggleAutoStartCommand(string command, string args)
    {
        // Toggle the AutoStartOnMount setting
        Configuration.AutoStartOnMount = !Configuration.AutoStartOnMount;
        Configuration.Save();

        // Inform the user of the new state
        string status = Configuration.AutoStartOnMount ? "enabled" : "disabled";
        Chat.Print($"Auto-start on mount is now {status}.");
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
            // Check the auto-start setting before playing
            if (Configuration.AutoStartOnMount && radioPlayer.PlaybackState != PlaybackState.Playing)
            {
                PlayRadio();
            }
        }
        else if (flag == ConditionFlag.Mounted && !value && Configuration.AutoStopOnDismount)
        {
            StopRadio();
        }
        else if (flag == ConditionFlag.Mounted2 && value)
        {
            if (Configuration.AutoStartOnMount && radioPlayer.PlaybackState != PlaybackState.Playing)
            {
                PlayRadio();
            }
        }
        else if (flag == ConditionFlag.Mounted2 && !value && Configuration.AutoStopOnDismount)
        {
            StopRadio();
        }
    }

    private async void PlayRadio()
    {
        try
        {
            // Dispose of any existing instances
            StopRadio();

            await Task.Run(() =>
            {
                // Reinitialize radioPlayer and streamReader asynchronously
                radioPlayer = new WasapiOut(AudioClientShareMode.Shared, 200);
                radioPlayer.Volume = Configuration.Volume;

                streamReader = new MediaFoundationReader(Configuration.RadioUrl);
                radioPlayer.Init(streamReader);
                radioPlayer.Play();
            });
        }
        catch (Exception ex) when ((uint)ex.HResult == 0xC00D0035)
        {
            Chat.PrintError("Error playing radio: The URL appears to be invalid. Please check the radio link in the configuration.");
            StopRadio(); // Clean up in case of an error
        }
        catch (Exception ex)
        {
            Chat.PrintError($"Error playing radio: {ex.Message}");
            StopRadio(); // Clean up in case of a generic error
        }
    }

    private void StopRadio()
    {
        try
        {
            if (radioPlayer != null)
            {
                radioPlayer.Stop();
                radioPlayer.Dispose();
            }

            streamReader?.Dispose();
            streamReader = null; // Reset streamReader to prevent reuse
        }
        catch (Exception ex)
        {
            Chat.Print($"Error stopping radio: {ex.Message}");
        }
    }

    private void ToggleRadioCommand(string command, string args)
    {

        // Toggle playback based on current state
        if (radioPlayer.PlaybackState == PlaybackState.Playing)
        {
            StopRadio();
            Chat.Print("Radio playback stopped.");
        }
        else
        {
            PlayRadio();
            Chat.Print("Radio playback started.");
        }
    }

    public void Dispose()
    {
        windowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler("/radiosettings");
        CommandManager.RemoveHandler("/radiovolume");
        CommandManager.RemoveHandler("/radiotoggle");
        CommandManager.RemoveHandler("/radioautostop");
        CommandManager.RemoveHandler("/radioautostart");
        Condition.ConditionChange -= OnConditionChange;
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;

        StopRadio();
        radioPlayer?.Dispose();
        streamReader?.Dispose();
    }

    private void OnCommand(string command, string args) => ToggleConfigUI();

    private void DrawUI() => windowSystem.Draw();

    public void ToggleConfigUI() => configWindow.Toggle();
    
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


