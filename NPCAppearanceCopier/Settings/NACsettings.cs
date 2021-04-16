using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Noggog;
using Mutagen.Bethesda.Synthesis.Settings;

namespace NPCAppearanceCopier.Settings
{
    public class NACsettings
    {
        [SynthesisOrder]
        [SynthesisTooltip("Click here to select which NPCs should have their appearance transferred.")]
        public HashSet<NACnpc> NPCs { get; set; } = new HashSet<NACnpc>();

        [SynthesisOrder]
        [SynthesisTooltip("What happens if there is a race mismatch between the donor and recipient NPC.\nChange: Recipient NPC's race gets changed to the donor NPC's race.\nPseudocopy: NAC makes a copy of the recipient NPC's race but changes appearance-related data to match the donor NPC's race.\nLeave: Race remains unchanged (not recommended - can cause dark facegen bug).")]
        public RaceHandlingMode RaceChangeAction { get; set; } = RaceHandlingMode.Change;

        [SynthesisOrder]
        [SynthesisTooltip("Directory to which facegen should be copied (if blank or invalid, facegen will be copied directly to the Data folder (or overwrite if using MO2).")]
        public string FacegenOutputDirectory { get; set; } = "";

        [SynthesisOrder]
        [SynthesisTooltip("The following plugins will never have their assets merged into Synthesis.esp. Don't touch unless you know what you're doing.")]
        public HashSet<ModKey> PluginsExcludedFromMerge = new HashSet<ModKey> ()
        { 
            ModKey.FromNameAndExtension("Skyrim.esm"),
            ModKey.FromNameAndExtension("Update.esm"),
            ModKey.FromNameAndExtension("Dawnguard.esm"),
            ModKey.FromNameAndExtension("HearthFires.esm"),
            ModKey.FromNameAndExtension("Dragonborn.esm")
        };
    }

    public enum RaceHandlingMode
    {
        Change,
        Pseudocopy,
        Leave
    }
}