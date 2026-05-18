using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Text;
using Serilog;
using System;
using System.Numerics;

namespace TrainTracker.Windows;


public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base("Train Tracker Config")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(320, 320);
        SizeCondition = ImGuiCond.Always;

        this.plugin = plugin;
        configuration = plugin.configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        /*
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
        */
    }

    public override void Draw()
    {
        
        bool tracking = configuration.isTracking;
        if(ImGui.Checkbox("Tracking active", ref tracking))
        {
            configuration.isTracking = tracking;
            plugin.mainWindow.SetWindowTitle();
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Record chat from tracked channels that match the Name Filter");


        bool trackWithWindowClosed = configuration.isTrackingWithWindowClosed;
        if(ImGui.Checkbox("Track with window closed", ref trackWithWindowClosed))
        {
            configuration.isTrackingWithWindowClosed = trackWithWindowClosed;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Record chat while the main window is closed. Requires Tracking Active to be set");


        ImGui.SetNextItemWidth(175);
        int numLines = configuration.maxSavedLines;
        if (ImGui.DragInt("Max saved lines", ref numLines, 1, 5, 100))
        {
            configuration.maxSavedLines = numLines;
            configuration.Save();
        }


        bool wordWrap = configuration.wordWrap;
        if (ImGui.Checkbox("Word wrap", ref wordWrap))
        {
            configuration.wordWrap = wordWrap;
            configuration.Save();
        }


        bool trackShout = (configuration.trackedChannels.IndexOf(XivChatType.Shout) != -1);
        if (ImGui.Checkbox("Shout", ref trackShout))
        {
            if (trackShout)
            {
                if(configuration.trackedChannels.IndexOf(XivChatType.Shout) == -1)
                    configuration.trackedChannels.Add(XivChatType.Shout);
            }
            else configuration.trackedChannels.Remove(XivChatType.Shout);
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Track messages from Shout chat");
        ImGui.SameLine();
        bool trackYell = (configuration.trackedChannels.IndexOf(XivChatType.Yell) != -1);
        if (ImGui.Checkbox("Yell", ref trackYell))
        {
            if (trackYell)
            {
                if (configuration.trackedChannels.IndexOf(XivChatType.Yell) == -1)
                    configuration.trackedChannels.Add(XivChatType.Yell);
            }
            else configuration.trackedChannels.Remove(XivChatType.Yell);
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Track messages from Yell chat");
        ImGui.SameLine();
        bool trackSay = (configuration.trackedChannels.IndexOf(XivChatType.Say) != -1);
        if (ImGui.Checkbox("Say", ref trackSay))
        {
            if (trackSay)
            {
                if (configuration.trackedChannels.IndexOf(XivChatType.Say) == -1)
                    configuration.trackedChannels.Add(XivChatType.Say);
            }
            else configuration.trackedChannels.Remove(XivChatType.Say);
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Track messages from Say chat");
        /*
        ImGui.SameLine();
        bool trackParty = (configuration.trackedChannels.IndexOf(XivChatType.Party) != -1);
        if (ImGui.Checkbox("Party", ref trackParty))
        {
            if (trackParty)
            {
                if (configuration.trackedChannels.IndexOf(XivChatType.Party) == -1)
                    configuration.trackedChannels.Add(XivChatType.Party);
            }
            else configuration.trackedChannels.Remove(XivChatType.Party);
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Track messages from Party chat");
        */

        ImGui.SetNextItemWidth(175);
        int timestamp = configuration.selectedTimestamp;
        if(ImGui.Combo("Timestamps", ref timestamp, Plugin.PossibleTimestamps, Plugin.PossibleTimestamps.Length))
        {
            configuration.selectedTimestamp = timestamp;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("'hh' is 12 hour clock, 'HH' is 24 hour clock, 'tt' is AM/PM, 't' is A/P");


        ImGui.SetNextItemWidth(175);
        float distance = configuration.newFlagDistance;
        if (ImGui.DragFloat("New flag distance", ref distance, 0.1f, 0.1f, 5.0f))
        {
            configuration.newFlagDistance = distance;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("How far on the map a recorded flag needs to be to count as new");


        ImGui.SetNextItemWidth(175);
        int sound = configuration.selectedSound;
        if (ImGui.Combo("New flag sound", ref sound, Plugin.PossibleSounds, Plugin.PossibleSounds.Length))
        {
            configuration.selectedSound = sound;
            if(sound > 0)
            {
                UIGlobals.PlayChatSoundEffect((uint)sound);
            }
            configuration.Save();
        }

        bool showTeleNeed = configuration.showTeleportNeeded;
        if (ImGui.Checkbox("Show teleport", ref showTeleNeed))
        {
            configuration.showTeleportNeeded = showTeleNeed;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Show if the new recorded flag is in a different zone");

        bool showInstanceChange = configuration.showInstanceChange;
        if (ImGui.Checkbox("Show instance change", ref showInstanceChange))
        {
            configuration.showInstanceChange = showInstanceChange;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Try to show if the new recorded flag is in a different instance (conductor has to say an instance message, doesn't use the flag itself)");

        bool autoPlaceFlag = configuration.autoPlaceFlag;
        if (ImGui.Checkbox("Auto place flag", ref autoPlaceFlag))
        {
            configuration.autoPlaceFlag = autoPlaceFlag;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Automatically place new flags");
    }
}
