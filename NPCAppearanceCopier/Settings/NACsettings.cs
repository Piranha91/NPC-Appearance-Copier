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
        [SynthesisTooltip("The following plugins will never have their assets merged into Synthesis.esp. Don't touch unless you know what you're doing.")]
        public IEnumerable<ModKey> PluginsExcludedFromMerge = new HashSet<ModKey> ()
        { 
            ModKey.FromNameAndExtension("Skyrim.esm"),
            ModKey.FromNameAndExtension("Update.esm"),
            ModKey.FromNameAndExtension("Dawnguard.esm"),
            ModKey.FromNameAndExtension("HearthFires.esm"),
            ModKey.FromNameAndExtension("Dragonborn.esm")
        };
    }
}