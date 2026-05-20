# Train Tracker


Train tracker plugin for Dalamud.

See only the conductor's messages so you don't miss flags, instance messages, or other instructions. Optionally set map flags to come up automatically.

## Main Points

* See messages only from the conductor(s)
* Ignores duplicate chat messages
* Detect new flags
    * Click a button to place on the map, or optionally auto place
* Track instance messages
* Optionally show when a teleport or instance change is needed
* Optional sound notification on new flags

## Main window:
<img width="515" height="234" alt="image" src="https://github.com/user-attachments/assets/ccd9ea6c-aa6c-4ae3-bbf5-c707989d08a2" />
<img width="514" height="230" alt="image" src="https://github.com/user-attachments/assets/dd870c5f-d288-4bbe-b406-9e25080997b6" />


* Name filter - Enter the name of the conductor to filter just their messages. Partial names allowed, capitalization doesn't matter. Enter or clicking 'Set Name Filter' to set.
    * Clear the text field or press the X button to clear.
* Chat box - Filtered messages (or all if no filter set) will show up here, newest on top.
* Track toggle - Power button icon will toggle Tracking on/off

##  Config Window
<img width="322" height="317" alt="image" src="https://github.com/user-attachments/assets/41fd5ef6-a84d-4f97-a5f9-79d3c33929c9" />

* Tracking active - Toggles Tracking on/off, same as the power icon on the main window
* Track with window closed - If disabled, closing the window will stop tracking.
* Max saved lines - How many chat messages to save
* Word wrap - If chat messages in the main window should wrap if longer than the chat box
* Shout/Yell/Say - Which channels to monitor
* Timestamps - Select timestamp format for use in the chat display, or None
* New flag distance - How far in map units a flag needs to be to count as different than the previous saved flag
* New flag sound - Sound to trigger when a new flag is detected, or None. Selecting an entry plays the sound for preview
* Show teleport - If a detected flag is not in the current zone, show 'Teleport needed' above the chatbox
* Show instance change - If the conductor has sent an instance message, and the current zone has multiple, show a message above the chatbox
* Auto place flag - If any newly detected flag should automatically be placed on the map. If disabled, shows a button above the chatbox to place
