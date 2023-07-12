using System.ComponentModel;
using Exiled.API.Interfaces;

namespace DoorRestartSystem
{
    public class Config : IConfig
    {
        [Description("Enable or disable DoorRestartSystem.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Should doors close during lockdown?")]
        public bool CloseDoors { get; set; } = true;

        [Description("The InitialDelay before the first Door Restart can happen")]
        public int InitialDelay { get; set; } = 60;

        [Description("The Minumum Duration of the Lockdown")]
        public int DurationMin { get; set; } = 10;

        [Description("The Maximum Duration of the Lockdown")]
        public int DurationMax { get; set; } = 35;

        [Description("The The Minumum Delay before the next the Lockdown")]
        public int DelayMin { get; set; } = 60;

        [Description("The The Maxiumum Delay before the next the Lockdown")]
        public int DelayMax { get; set; } = 200;

        [Description("The chance that a Round even has DoorSystemRestarts")]
        public float Spawnchance { get; set; } = 55;

        [Description("Enable 3 . 2 . 1 announcement")]
        public bool Countdown { get; set; } = false;

        // MESSAGES
        [Description("Glitch chance during message per word in CASSIE sentence.")]
        public float GlitchChance { get; private set; } = 10f;

        [Description("Jam chance during message per word in CASSIE sentence.")]
        public float JamChance { get; private set; } = 5f;

        [Description("Message said by Cassie if no lockdown occurs")]
        public string CassieMessageWrong { get; set; } = ". I have prevented the system failure . .g5 Sorry for a .g3 . false alert .";

        [Description("Message said by Cassie when a lockdown starts - 3 . 2 . 1 announcement")]
        public string CassieMessageStart { get; set; } = "pitch_0.2 .g4 . .g4 pitch_1 door control system pitch_0.25 .g1 pitch_0.9 malfunction pitch_1 . initializing repair";

        [Description("The time between the sentence and the 3 . 2 . 1 announcement")]
        public float TimeBetweenSentenceAndStart { get; set; } = 11f;

        [Description("Message said by Cassie just after the lockdown.")]
        public string CassiePostMessage { get; set; } = "door control system malfunction has been detected at .";

        [Description("Message said by Cassie after CassiePostMessage if lockdown gonna occure at whole site.")]
        public string CassieMessageFacility { get; set; } = "The Facility .";

        [Description("Message said by Cassie after CassiePostMessage if outage gonna occure at the Entrance Zone.")]
        public string CassieMessageEntrance { get; set; } = "The Entrance Zone .";

        [Description("Message said by Cassie after CassiePostMessage if outage gonna occure at the Light Containment Zone.")]
        public string CassieMessageLight { get; set; } = "The Light Containment Zone .";

        [Description("Message said by Cassie after CassiePostMessage if outage gonna occure at the Heavy Containment Zone.")]
        public string CassieMessageHeavy { get; set; } = "The Heavy Containment Zone.";

        [Description("Message said by Cassie after CassiePostMessage if outage gonna occure at the entrance zone.")]
        public string CassieMessageSurface { get; set; } = "The Surface .";

        [Description("Message said by Cassie after CassiePostMessage if outage gonna occure at random rooms in facility when UseRoomChances is true or unknown type of zones or unspecified zones.")]
        public string CassieMessageOther { get; set; } = ". pitch_0.35 .g6 pitch_0.95 the malfunction is Unspecified .";

        [Description("The sound CASSIE will make during a lockdown.")]
        public string CassieKeter { get; set; } = "pitch_0.15 .g7";

        [Description("The message CASSIE will say when a lockdown ends.")]
        public string CassieMessageEnd { get; set; } = "facility door control system is now operational";

        // Probability 
        [Description("A lockdown in the whole facility will occur if none of the zones are selected randomly and EnableFacilityLockdown is set to true.")]
        public bool EnableFacilityLockdown { get; private set; } = true;

        [Description("Percentage chance of an outage at the Heavy Containment Zone during the lockdown.")]
        public int ChanceHeavy { get; set; } = 99;

        [Description("Percentage chance of an outage at the Heavy Containment Zone during the lockdown.")]
        public int ChanceLight { get; set; } = 45;

        [Description("Percentage chance of an outage at the Entrance Zone during the lockdown.")]
        public int ChanceEntrance { get; set; } = 65;

        [Description("Percentage chance of an outage at the Surface Zone during the lockdown.")]
        public int ChanceSurface { get; set; } = 25;

        [Description("Percentage chance of an outage at an unknown and unspecified type of zone during the lockdown.")]
        public int ChanceOther { get; set; } = 0;

        [Description("Change this to true if want to use per room probability settings isntead of per zone settings. The script will check all rooms in the specified zone with its probability.")]
        public bool UsePerRoomChances { get; set; } = false;

        [Description("The sentence it transmits via Cassie after the system got restarted")]
        public string DoorAfterSentence { get; set; } = "DOOR CONTROL SYSTEM REPAIR COMPLETE";

        [Description("Enables debugging.")]
        public bool Debug { get; set; } = false;
    }
}
