using Dalamud.Bindings.ImGui;
using Dalamud.Game.Chat;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using TrainTracker.Windows;
/*
 * TODO:
 * test multi name filter
 * register chat hook only when tracking?
 * localization?
 */

namespace TrainTracker;

public struct SavedMessages(DateTime _time, string _sender, SeString _message)
{
    public DateTime timestamp = _time;
    public string sender = _sender;
    public SeString message = _message;
}


public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    //[PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    //[PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; set; } = null!;
    //[PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;

    private const string CommandName = "/traintracker";

    public Configuration configuration { get; init; }

    public readonly WindowSystem windowSystem = new("TrainTracker");
    private ConfigWindow configWindow { get; init; }
    public MainWindow mainWindow { get; init; }

    //public string nameFilter = string.Empty;
    public string[] nameFilters = [];
    public List<SavedMessages> savedMessages = [];

    public bool trackingActive = false;

    public MapLinkPayload currentMapPayload = new(0, 0, 0, 0);
    public bool hasNewFlag = false;
    public uint currentMapID = 0;
    public uint currentInstance = 0;
    public uint targetMapID = 0;
    public uint targetInstance = 0;
    
    public static readonly string[] PossibleTimestamps = ["None", "hh:mm tt", "hh:mm t", "HH:mm", "hh:mm:ss tt", "hh:mm:ss t", "HH:mm:ss"];
    public static readonly string[] PossibleSounds = ["None", "Sound 1", "Sound 2", "Sound 3", "Sound 4", "Sound 5", "Sound 6", "Sound 7", "Sound 8", "Sound 9", "Sound 10", "Sound 11", "Sound 12", "Sound 13", "Sound 14", "Sound 15", "Sound 16"];
    
    //The first square on each instance line is the FFXIV Instance symbol for that #, the second is the square # symbol just in case
    public static readonly string[] PossibleInstance1 = ["i1", "instance1", "instance", "instance"];
    public static readonly string[] PossibleInstance2 = ["i2", "instance2", "instance", "instance"];
    public static readonly string[] PossibleInstance3 = ["i3", "instance3", "instance", "instance"];
    public static readonly string[] PossibleInstance4 = ["i4", "instance4", "instance", "instance"];

    public Plugin()
    {
        bool firstRun = !PluginInterface.ConfigFile.Exists;
        configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration(firstRun);

        configWindow = new ConfigWindow(this);
        mainWindow = new MainWindow(this);

        windowSystem.AddWindow(configWindow);
        windowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open tracker window"
        });

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += windowSystem.Draw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        //Log.Information($"===Initializing {PluginInterface.Manifest.Name}===");


#if DEBUG
        //For testing, auto open window
        ToggleMainUi();
#else
        CheckMode();
#endif
    }

    private void Chat_OnChatMessage(IHandleableChatMessage chatMessage)
    {
        if (!trackingActive) return;

#if DEBUG
        if ((configuration.trackedChannels.IndexOf(chatMessage.LogKind) == -1) && (chatMessage.LogKind != XivChatType.Echo)) return;
        //if (chatMessage.LogKind == XivChatType.Echo) chatMessage.Sender = "Echo";
#else
        if (configuration.TrackedChannels.IndexOf(chatMessage.LogKind) == -1) return;
#endif
        //Log.Information("Chat message recieved");

        //Ignore repeated message, such as conductor sending message to both Shout and Yell, or spamming instance message
        //Count check as ^1 errors out if empty
        if ((savedMessages.Count > 0) && (chatMessage.Message.ToString() == savedMessages[^1].message.ToString())) return;

        //Convert sender to PlayerPayload and grab the sender's name text. This is just the raw name without the worldname.
        string senderName = "";
        //Log.Information(chatMessage.Sender.Payloads.Count.ToString());
        //foreach(Payload payload in chatMessage.Sender.Payloads)
        //{
            //Log.Information(payload.Type.ToString() + " - " + payload.ToString());
        //}
        if(chatMessage.Sender.Payloads.Count > 1)
        {
            senderName = ((PlayerPayload)chatMessage.Sender.Payloads[0]).PlayerName;
            //Log.Information("senderName: " + senderName);
        }
        else
        {
            senderName = "Echo";
        }

        
        bool filtered = true;
        if (nameFilters.Length == 0) filtered = false;
        else
        {
            foreach (string name in nameFilters)
            {
                Log.Information("testing: " + name);
                if (senderName.Contains(name, StringComparison.CurrentCultureIgnoreCase)) filtered = false;
                Log.Information("filtered: " + (filtered ? "true" : "false"));
            }
        }

        if (filtered) return;

        savedMessages.Add(new SavedMessages(DateTime.Now, senderName, chatMessage.Message));
        if (savedMessages.Count > configuration.maxSavedLines)
        {
            savedMessages.RemoveRange(0, savedMessages.Count - configuration.maxSavedLines);
        }

        foreach (Payload payload in chatMessage.Message.Payloads)
        {
            if (payload.Type == PayloadType.MapLink)
            {
                //Log.Information("map payload found");
                MapLinkPayload mapLinkPayload = (MapLinkPayload)payload;
                if (!CompareMapPayloads(currentMapPayload, mapLinkPayload))
                {
                    //Log.Information("Updating map link");
                    currentMapPayload = mapLinkPayload;
                    hasNewFlag = true;
                    targetMapID = mapLinkPayload.Map.RowId;
                    if (configuration.autoPlaceFlag) PlaceFlag();
                    if (configuration.selectedSound > 0)
                    {
                        UIGlobals.PlayChatSoundEffect((uint)configuration.selectedSound);
                    }
                }
            }
            else if (configuration.showInstanceChange && payload.Type == PayloadType.RawText)
            {
                string text = ((TextPayload)payload).Text ?? "";
                text = text.ToLower();
                text = text.Replace(" ", string.Empty);

                if (PossibleInstance1.Any(text.Contains))
                {
                    targetInstance = 1;
                }
                else if (PossibleInstance2.Any(text.Contains))
                {
                    targetInstance = 2;
                }
                else if (PossibleInstance3.Any(text.Contains))
                {
                    targetInstance = 3;
                }
                else if (PossibleInstance4.Any(text.Contains))
                {
                    targetInstance = 4;
                }
            }
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!trackingActive) return;

        uint currMap = ClientState.MapId;
        uint currIns = ClientState.Instance;

        if((currentMapID != currMap) || (currentInstance != currIns))
        {
            currentMapID = currMap;
            currentInstance = currIns;
            if (currentInstance == 0) targetInstance = 0;
        }
    }

    //Compare two Map Link Payloads. Returns 'true' if both are in the same territory and closer together than the New Flag Distance setting.
    private bool CompareMapPayloads(MapLinkPayload map1, MapLinkPayload map2)
    {
        if (map1.TerritoryType.RowId != map2.TerritoryType.RowId)
        {
            return false;
        }

        if ((Math.Abs(map1.XCoord - map2.XCoord) > configuration.newFlagDistance) || (Math.Abs(map1.YCoord - map2.YCoord) > configuration.newFlagDistance))
        {
            return false;
        }

        return true;
    }

    public void PlaceFlag()
    {
        hasNewFlag = false;
        GameGui.OpenMapWithMapLink(currentMapPayload);
    }

    public void ClearChat()
    {
        savedMessages.Clear();
        currentMapPayload = new(0, 0, 0, 0);
        hasNewFlag = false;
        targetMapID = 0;
        targetInstance = 0;
    }

    public void ParseNameFilter(string nameTemp)
    {
        if (nameTemp.IsNullOrWhitespace())
        {
            nameFilters = [];
        }
        else
        {
            nameTemp = nameTemp.ToLower();
            nameFilters = nameTemp.Split(',', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        }
        mainWindow.SetWindowTitle();
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        windowSystem.RemoveAllWindows();

        configWindow.Dispose();
        mainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        if (trackingActive)
        {
            ChatGui.ChatMessage -= Chat_OnChatMessage;
            Framework.Update -= OnFrameworkUpdate;
        }
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        ToggleMainUi();
    }

    public void ToggleConfigUi() => configWindow.Toggle();
    public void ToggleMainUi()
    {
        mainWindow.IsOpen ^= true;
        CheckMode();
    }

    public void CheckMode()
    {
        //Log.Information("CheckMode- trackingActive: " + trackingActive.ToString() + ", isTracking: " + configuration.isTracking.ToString() + ", windowOpen: " + mainWindow.IsOpen + ", whileClosed: " + configuration.isTrackingWithWindowClosed);
        if(configuration.isTracking && (mainWindow.IsOpen || configuration.isTrackingWithWindowClosed))
        {
            if (!trackingActive)
            {
                trackingActive = true;
                ChatGui.ChatMessage += Chat_OnChatMessage;
                Framework.Update += OnFrameworkUpdate;
                Log.Information("Tracking is now active");
            }
        }else if (trackingActive)
        {
            trackingActive = false;
            ChatGui.ChatMessage -= Chat_OnChatMessage;
            Framework.Update -= OnFrameworkUpdate;
            Log.Information("Tracking is now disabled");
        }
    }
}
