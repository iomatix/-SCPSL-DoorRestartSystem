## Door-Restart-System
[![Download Latest Release](https://img.shields.io/badge/Download-Latest%20Release-blue?style=for-the-badge)](https://github.com/iomatix/-SCPSL-DoorRestartSystem/releases/latest)
[![GitHub Downloads](https://img.shields.io/github/downloads/iomatix/-SCPSL-DoorRestartSystem/latest/total?sort=date&style=for-the-badge)](https://github.com/iomatix/-SCPSL-DoorRestartSystem/releases/latest)

## Plugin Description: DoorRestartSystem

**Compatible with:** SCP: SL Exiled 9.5+

The **DoorRestartSystem** plugin introduces an immersive feature where the facility undergoes a "Door Software Restart." During this event, all affected doors are fully closed and locked for a configurable duration, simulating a temporary system-wide malfunction. Once the restart process is complete, doors unlock and return to normal operation.

Rumor has it that these malfunctions might be the result of someone spilling their coffee on the door control system while scrambling to evacuate the facility.

### Key Features:

-   **Configurable Settings:**
    
    -   Enable or disable the system entirely.
        
    -   Adjust the probability, duration, and delay of restarts.
        
    -   Fine-tune which zones or door types are impacted.
        
-   **Advanced Lockdown Customization:**
    
    -   Control whether specific areas, such as SCP rooms, checkpoints, airlocks, elevators, or armory doors, are affected.
        
    -   Individual zone-based or per-room chances allow tailored experiences for each playthrough.
        
-   **Enhanced Immersion:**
    
    -   Dynamic lighting effects with customizable flicker frequency and colors during lockdowns.
        
    -   Integrated CASSIE announcements, including startup and ending messages, with optional glitching or jamming effects.
        
-   **Debugging and Performance:**
    
    -   Debugging options to monitor and test the system during development or server setup.

### Supporting Development

My mods are **always free to use**.

If you appreciate my work, you can support me by [buying me a coffee](https://buymeacoffee.com/iomatix).


### Contributors

<a href="https://github.com/iomatix/-SCPSL-DoorRestartSystem/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=iomatix/-SCPSL-DoorRestartSystem" />
</a>

## Example Config:

```
# Enable or disable DoorRestartSystem.
is_enabled: true
# Enables debugging.
debug: true
# The chance that a Round even has DoorSystemRestarts
spawnchance: 55
# Should doors close during lockdown?
close_doors: true
# Should nuke surface door and hcz elevator be ignored?
skip_nuke_doors: true
# Should unknown doors and elevators be ignored?
skip_unknown_doors: true
# Should all elevators be ignored?
skip_elevators: false
# Should all airlocks be ignored?
skip_airlocks: false
# Should all scp rooms be ignored?
skip_s_c_p_rooms: false
# Should all armory doors be ignored?
skip_armory: true
# Should all checkpoints doors be ignored?
skip_checkpoints: true
# Should checkpoints gates be ignored? Independents from SkipCheckpoints
skip_checkpoints_gate: false
# Change this to true if want to disable doors randomly within the room.
use_per_door_chance: true
# Percentage chance of an outage per door if UsePerDoorChance is set to true.
chance_per_door: 65
# The initial delay (in seconds) before the first Door Restart can happen
initial_delay: 25
# The Minimum Duration of the Lockdown
duration_min: 24
# The Maximum Duration of the Lockdown
duration_max: 86
# The The Minimum Delay before the next the Lockdown
delay_min: 145
# The The Maximum Delay before the next the Lockdown
delay_max: 460
# Enable or disable randomized delay between lockdown events. If set to false the InitialDelay would be used instead to keep regular events.
random_events: true
# Enable lighting flicker
flicker: true
# Flickering frequency. Higher the value faster the flickering.
flicker_frequency: 1.5
# Red channel of the lights color in the room during lockdown
lights_color_r: 0.899999976
# Green channel of the lights color in the room during lockdown
lights_color_g: 0.0500000007
# Blue channel of the lights color in the room during lockdown
lights_color_b: 0.200000003
# Should cassie clear the message cue before important message to prevent spam?
cassie_message_clear_before_important: true
# Enable CassieMessageCountdown announcement
is_countdown_enabled: true
# The delay between the CassieMessageCountdown and the CassieMessageStart if IsCountdownEnabled is enabled.
time_between_sentence_and_start: 15
# Glitch chance during message per word in CASSIE sentence.
glitch_chance: 4
# Jam chance during message per word in CASSIE sentence.
jam_chance: 3
# Message said by Cassie if no lockdown occurs
cassie_message_wrong: '.g5 . Avoided the malfunction of the control system . .g3'
# Message said by Cassie just before a lockdown starts - Countdown - 3 . 2 . 1 announcement
cassie_message_countdown: 'pitch_0.2 .g4 . .g4 pitch_1 door control system pitch_0.25 .g1 pitch_0.9 malfunction pitch_1 . initializing repair'
# Message said by Cassie on the lockdown start, delayed by time set on delayTimeBetweenSentenceAndStart if the countdown is enabled.
cassie_message_start: 'pitch_0.25 .g4 . pitch_0.45 .g3 pitch_0.95 . ATTENTION . AN IMPORTANT . MESSAGE . pitch_0.98 the facility control system pitch_0.25 .g1 pitch_0.93 critical failure'
# Message said by Cassie after CassiePostMessage if lockdown gonna occur at whole site.
cassie_message_facility: ''
# Message said by Cassie after CassiePostMessage if outage gonna occur at the Entrance Zone.
cassie_message_entrance: ''
# Message said by Cassie after CassiePostMessage if outage gonna occur at the Light Containment Zone.
cassie_message_light: ''
# Message said by Cassie after CassiePostMessage if outage gonna occur at the Heavy Containment Zone.
cassie_message_heavy: 'pitch_0.95 jam_045_2 .G4 .G3 . door control system patching in . progress jam_025_2 .G5 . pitch_1.5 .G5 . .G5 . .G5 . .G5 . .G5 . .G5 . .G5 jam_035_3 .G3'
# Message said by Cassie after CassiePostMessage if outage gonna occur at the entrance zone.
cassie_message_surface: ''
# Message said by Cassie after CassiePostMessage if outage gonna occur at random rooms in facility when zone is unknown or unspecified.
cassie_message_other: ''
# The sound CASSIE will make during a lockdown.
cassie_keter: 'pitch_0.15 .g7'
# The message CASSIE will say when a lockdown ends.
cassie_message_end: 'pitch_0.45 .g4 pitch_0.65 . .g3 .g1 pitch_1.0 . IMPORTANT MESSAGE . pitch_0.98 the facility . door control system . is now . pitch_0.95 operational'
# A lockdown in the whole facility will occur if none of the zones are selected randomly and EnableFacilityLockdown is set to true.
enable_facility_lockdown: true
# Percentage chance of an outage at the Heavy Containment Zone during the lockdown.
chance_heavy: 75
# Percentage chance of an outage at the Light Containment Zone during the lockdown.
chance_light: 35
# Percentage chance of an outage at the Entrance Zone during the lockdown.
chance_entrance: 65
# Percentage chance of an outage at the Surface Zone during the lockdown.
chance_surface: 20
# Percentage chance of an outage at an unknown and unspecified type of zone during the lockdown.
chance_other: 15
# Change this to true if want to use per room probability settings instead of per zone settings. The script will check all rooms in the specified zone with its probability.
use_per_room_chances: true
```

