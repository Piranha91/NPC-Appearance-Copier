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
        public HashSet<NACnpc> NPCs { get; set; } = new HashSet<NACnpc>();
    }
}