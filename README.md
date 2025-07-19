## Door-Restart-System
![GitHub Downloads (all assets, latest release)](https://img.shields.io/github/downloads/iomatix/-SCPSL-DoorRestartSystem/latest/total?sort=date&style=for-the-badge)

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

## Configs:
```
DRS:
  # Enable or disable DoorRestartSystem.
  is_enabled: true

  # Should doors close during lockdown?
  close_doors: true

  # Should nuke surface doors and HCZ elevators be ignored?
  skip_nuke_doors: true

  # Should unknown doors and elevators be ignored?
  skip_unknown_doors: true

  # Should all elevators be ignored?
  skip_elevators: false

  # Should all airlocks be ignored?
  skip_airlocks: false

  # Should all SCP rooms be ignored?
  skip_scp_rooms: false

  # Should all armory doors be ignored?
  skip_armory: true

  # Should all checkpoint doors be ignored?
  skip_checkpoints: true

  # Should checkpoint gates be ignored? (Independent of skip_checkpoints)
  skip_checkpoints_gate: false

  # The InitialDelay before the first Door Restart can happen.
  initial_delay: 60

  # The Minimum Duration of the Lockdown.
  duration_min: 10

  # The Maximum Duration of the Lockdown.
  duration_max: 35

  # The Minimum Delay before the next Lockdown.
  delay_min: 60

  # The Maximum Delay before the next Lockdown.
  delay_max: 200

  # The chance that a round will have DoorSystemRestarts.
  spawnchance: 55

  # Enable 3 . 2 . 1 announcement.
  countdown: false

  # Custom Flicker Lights
  # Enable lighting flicker.
  flicker: true

  # Flickering frequency. Higher values result in faster flickering.
  flicker_frequency: 2.5

  # Red channel of the light color during lockdown.
  lights_color_r: 0.85

  # Green channel of the light color during lockdown.
  lights_color_g: 0.07

  # Blue channel of the light color during lockdown.
  lights_color_b: 0.23

  # Messages
  # Glitch chance per word in the CASSIE sentence.
  glitch_chance: 10.0

  # Jam chance per word in the CASSIE sentence.
  jam_chance: 5.0

  # Message said by CASSIE if no lockdown occurs.
  cassie_message_wrong: ". I have avoided the system failure . .g5 Sorry for a .g3 . false alert ."

  # Message said by CASSIE when a lockdown starts (3 . 2 . 1 announcement).
  cassie_message_start: "pitch_0.2 .g4 . .g4 pitch_1 door control system pitch_0.25 .g1 pitch_0.9 malfunction pitch_1 . initializing repair"

  # The time between the sentence and the 3 . 2 . 1 announcement.
  time_between_sentence_and_start: 11

  # Message said by CASSIE just after the lockdown.
  cassie_post_message: "door control system malfunction has been detected at ."

  # Message for whole facility lockdown after cassie_post_message.
  cassie_message_facility: "The Facility ."

  # Message for Entrance Zone outage after cassie_post_message.
  cassie_message_entrance: "The Entrance Zone ."

  # Message for Light Containment Zone outage after cassie_post_message.
  cassie_message_light: "The Light Containment Zone ."

  # Message for Heavy Containment Zone outage after cassie_post_message.
  cassie_message_heavy: "The Heavy Containment Zone."

  # Message for Surface Zone outage after cassie_post_message.
  cassie_message_surface: "The Surface ."

  # Message for unspecified zone outage after cassie_post_message.
  cassie_message_other: ". pitch_0.35 .g6 pitch_0.95 the malfunction is Unspecified ."

  # The sound CASSIE will make during a lockdown.
  cassie_keter: "pitch_0.15 .g7"

  # The message CASSIE will say when a lockdown ends.
  cassie_message_end: "facility door control system is now operational"

  # Probability
  # Lockdown in the whole facility if no zones are selected randomly and EnableFacilityLockdown is true.
  enable_facility_lockdown: true

  # Percentage chance of outage in Heavy Containment Zone during lockdown.
  chance_heavy: 99

  # Percentage chance of outage in Light Containment Zone during lockdown.
  chance_light: 45

  # Percentage chance of outage in Entrance Zone during lockdown.
  chance_entrance: 65

  # Percentage chance of outage in Surface Zone during lockdown.
  chance_surface: 25

  # Percentage chance of outage in an unknown/unspecified zone during lockdown.
  chance_other: 0

  # Use per room probability settings instead of per zone settings.
  use_per_room_chances: false

  # Enables debugging.
  debug: false```

