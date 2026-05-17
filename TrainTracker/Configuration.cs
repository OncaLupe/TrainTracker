using Dalamud.Configuration;
using Dalamud.Game.Text;
using Serilog;
using System;
using System.Collections.Generic;

namespace TrainTracker;


[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool isTracking { get; set; } = false;
    public bool isTrackingWithWindowClosed { get; set; } = false;
    public int maxSavedLines { get; set; } = 20;
    public bool wordWrap { get; set; } = false;
    public List<XivChatType> trackedChannels { get; set; } = [];
    public int selectedTimestamp { get; set; } = 0;
    public float newFlagDistance { get; set; } = 1;
    public int selectedSound { get; set; } = 0;
    public bool showTeleportNeeded { get; set; } = true;
    public bool showInstanceChange { get; set; } = true;
    public bool autoPlaceFlag { get; set; } = false;

    public Configuration(bool firstRun)
    {
        //Lists from config get added to default values above, so manually add the desired defaults only on first run
        if (firstRun)
        {
            trackedChannels.Add(XivChatType.Shout);
            trackedChannels.Add(XivChatType.Yell);
        }
    }

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
