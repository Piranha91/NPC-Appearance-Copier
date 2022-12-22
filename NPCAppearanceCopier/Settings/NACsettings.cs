using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Noggog;
using Mutagen.Bethesda.Synthesis.Settings;
using Mutagen.Bethesda.Plugins;

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
        [SynthesisTooltip("Directory to which facegen should be copied (if blank or invalid, facegen will be copied directly to Synthesis\\Data\\NPC-Appearance-Copier\\FaceGen Output.")]
        public string FacegenOutputDirectory { get; set; } = "";

        [SynthesisOrder]
        [SynthesisTooltip("Toggle for resource copying from BSAs")]
        public bool HandleBSAFiles { get; set; } = true;

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


        [SynthesisOrder]
        [SynthesisTooltip("If checked, the patcher will error out if an expected Extra Asset (non-FaceGen textures and meshes) is not found in a mod's directory (recommended to leave off unless you're sure your mods have absolutely no external dependencies).")]
        public bool AbortIfMissingExtraAssets { get; set; } = false;

        [SynthesisOrder]
        [SynthesisTooltip("Some plugins reference files that don't exist in their own download - for example the Bijin series references a bunch of .tri files that don't ship with the mod. While I can't account for every mod that exists, this settings suppresses \"file could not be found\" warnings from mods with known missing files so that any warnings you do see are more likely to be real.")]
        public bool SuppressKnownMissingFileWarnings { get; set; } = true;

        [SynthesisOrder]
        [SynthesisTooltip("Suppresses all log warnings about missing files.")]
        public bool SuppressAllMissingFileWarnings { get; set; } = true;

        [SynthesisIgnoreSetting]
        public HashSet<string> pathsToIgnore { get; set; } = new HashSet<string>();

        [SynthesisIgnoreSetting]
        public HashSet<suppressedWarnings> warningsToSuppress { get; set; } = new HashSet<suppressedWarnings>();
        [SynthesisIgnoreSetting]
        public suppressedWarnings warningsToSuppress_Global { get; set; } = new suppressedWarnings();
    }

    public class suppressedWarnings
    {
        public string Plugin { get; set; } = "";
        public HashSet<string> Paths { get; set; } = new HashSet<string>();
    }

    public enum RaceHandlingMode
    {
        Change,
        Pseudocopy,
        Leave
    }
}