using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace MountRadio;

[Serializable]
// Configuration class to manage plugin settings
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public string RadioUrl { get; set; } = "https://media-ssl.musicradio.com/CapitalUK";
    public float Volume { get; set; } = 0.5f;

    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = false;
    public bool IsConfigWindowMovable { get; set; } = true;
    public bool AutoStopOnDismount { get; set; } = true; // New property, default to enabled
    public bool AutoStartOnMount { get; set; } = true; // New property, default to enabled

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save() => pluginInterface?.SavePluginConfig(this);
}
