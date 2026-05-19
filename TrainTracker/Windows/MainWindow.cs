using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace TrainTracker.Windows;


public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;
    private string nameTemp = string.Empty;

    public MainWindow(Plugin plugin)
        : base("Train Tracker###Train Tracker Main Window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 160),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        this.configuration = plugin.configuration;


        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = Dalamud.Interface.FontAwesomeIcon.Cog,
            Click = _ => { plugin.ToggleConfigUi(); }
        });
        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.PowerOff,
            Click = _ => {
                configuration.isTracking = !configuration.isTracking;
                plugin.CheckMode();
                SetWindowTitle();
                configuration.Save();
            }
        });

        SetWindowTitle();
    }

    public void Dispose() { }

    public void SetWindowTitle()
    {
        string name = "Train Tracker(" + (configuration.isTracking ? "Active" : "Inactive");
        if(plugin.nameFilters.Length > 0)
        {
            name += ", filter: ";
            for(int i = 0; i < plugin.nameFilters.Length; ++i)
            {
                name += plugin.nameFilters[i] + ", ";
            }
            //Trim off the final ", "
            name = name[..^2];
        }
        WindowName = name + ")###Train Tracker Main Window";
    }

    public override void Draw()
    {
        ImGui.SetNextItemWidth(250);
        ImGui.InputText("##input", ref nameTemp);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            plugin.ParseNameFilter(nameTemp);
        }
        ImGui.SameLine();
        if (ImGui.Button("Set Name Filter"))
        {
            plugin.ParseNameFilter(nameTemp);
        }
        ImGui.SameLine();
        if (ImGui.Button("X"))
        {
            nameTemp = "";
            plugin.ParseNameFilter(nameTemp);
        }


        if (ImGui.Button("Clear chat"))
        {
            plugin.ClearChat();
        }


        if (plugin.targetMapID != 0)
        {
            if (configuration.showTeleportNeeded && (plugin.targetMapID != plugin.currentMapID))
            {
                ImGui.SameLine();
                CenterText("Teleport needed");
            } else if (configuration.showInstanceChange && (plugin.currentInstance != 0) && (plugin.targetInstance != 0) && (plugin.targetInstance != plugin.currentInstance))
            {
                ImGui.SameLine();
                CenterText("Instance change needed(" + plugin.currentInstance + "->" + plugin.targetInstance + ")");
            }
        }
        if (plugin.hasNewFlag)
        {
            ImGui.SameLine(ImGui.GetColumnWidth() - 60);
            if (ImGui.Button("Place Flag"))
            {
                plugin.PlaceFlag();
            }
        }


        //ImGui.Text("Current location: " + Plugin.ClientState.MapId.ToString() + ", (" + Plugin.ClientState.Instance + ")");
        //ImGui.Text("Target location: " + plugin.targetMapID + " (" + plugin.targetInstance + ")");


        // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
        // ImRaii takes care of this after the scope ends.
        // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        using (ImRaii.ChildDisposable child = ImRaii.Child("SomeChildWithAScrollbar", Vector2.Zero, true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                if(plugin.savedMessages.Count > configuration.maxSavedLines)
                {
                    plugin.savedMessages.RemoveRange(0, plugin.savedMessages.Count - configuration.maxSavedLines);
                }
                for(int i = plugin.savedMessages.Count - 1; i >= 0; --i)
                {
                    string text = "";
                    ImGui.AlignTextToFramePadding();
                    if (configuration.selectedTimestamp > 0)
                    {
                        text += "[" + plugin.savedMessages[i].timestamp.ToString(ConfigWindow.PossibleTimestamps[configuration.selectedTimestamp]) + "] ";
                    }
                    text += plugin.savedMessages[i].sender.ToString() + ": " + plugin.savedMessages[i].message.ToString();
                    if (configuration.wordWrap)
                    {
                        ImGui.PushTextWrapPos();
                        ImGui.Text(text);
                        ImGui.PopTextWrapPos();
                    }
                    else
                    {
                        ImGui.Text(text);
                    }
                }
            }
        }
    }

    private void CenterText(string text)
    {
        float size = ImGui.CalcTextSize(text).X;
        float avail = ImGui.GetContentRegionMax().X;
        ImGui.SetCursorPosX((avail / 2) - (size / 2));
        ImGui.Text(text);
    }
}
