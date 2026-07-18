## Door-Restart-System
[![Download Latest Release](https://img.shields.io/badge/Download-Latest%20Release-blue?style=for-the-badge)](https://github.com/iomatix/-SCPSL-DoorRestartSystem/releases/latest)
[![GitHub Downloads](https://img.shields.io/github/downloads/iomatix/-SCPSL-DoorRestartSystem/latest/total?sort=date&style=for-the-badge)](https://github.com/iomatix/-SCPSL-DoorRestartSystem/releases/latest)

## Plugin Description: DoorRestartSystem

### **Starting with 10.0.0 DRS is compatible with:** two API frameworks - LabAPI and Exiled!

The **DoorRestartSystem** plugin introduces an immersive feature where the facility undergoes a "Door Software Restart." During this event, all affected doors are fully closed and locked for a configurable duration, simulating a temporary system-wide malfunction. Once the restart process is complete, doors unlock and return to normal operation.

Rumor has it that these malfunctions might be the result of someone spilling their coffee on the door control system while scrambling to evacuate the facility.

## Dependencies:

- **[SCPSL-AudioManagerAPI](https://github.com/iomatix/-SCPSL-AudioManagerAPI/tree/main/AudioManagerAPI)**: [Releases](https://github.com/iomatix/-SCPSL-AudioManagerAPI/releases)

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

## 🛠️ Administrative Commands

The plugin implements a comprehensive administrative control system under the `drs` command router, fully integrated with both the Remote Admin (RA) Console and the Server Game Console.

### 🔒 Permissions
To safeguard high-impact facility-wide modifications, all subcommands strictly require the native `FacilityManagement` administrative permission node.

### 📋 Subcommand Matrix

| Command | Syntax | Description |
| :--- | :--- | :--- |
| **Initialize Framework** | `drs init` / `drs start` | Mid-round force bypass that safely wakes up the DoorRestartSystem ticking threads if previously disabled. |
| **Trigger Global Lockdown** | `drs trigger [seconds]` | Dispatches an instantaneous facility isolation event. Accepts an optional customized length in seconds. |
| **Targeted Zone Lockdown** | `drs zone [zoneName] [seconds]` | Intercepts the RNG engines to surgically lock down all valid, unskipped door systems tracking within a designated facility sector. |
| **Surgical Room Lockdown** | `drs room [roomName] [seconds]` | Advanced operational override that forces absolute isolation protocols strictly onto a single mapped room target. |
| **Emergency Abort** | `drs stop` / `drs cancel` | Instantly purges active coroutine loops, flashes out environmental lights back to normal, and drops all isolation locks. |

### 💡 Execution Examples

* Force-start automated background intervals if the framework rolled inactive:
  drs start

* Dispatch an immediate lockdown relying on configuration-defined random lengths:
  drs trigger

* Execute an explicit 45-second lockdown event to squeeze specific combat zones:
  drs trigger 45

* Instantly clear all visual effects and door locks if a match becomes soft-locked:
  drs cancel
