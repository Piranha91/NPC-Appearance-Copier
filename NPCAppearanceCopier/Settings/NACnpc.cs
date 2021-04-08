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
    public class NACnpc
    {
        [SynthesisOrder]
        [SynthesisTooltip("NPC whose appearance will be copied.")]
        public IFormLinkGetter<INpcGetter> CopyFrom { get; set; } = FormLink<INpcGetter>.Null;

        [SynthesisOrder]
        [SynthesisTooltip("NPC to whom the appearance will be copied.")]
        public IFormLinkGetter<INpcGetter> CopyTo { get; set; } = FormLink<INpcGetter>.Null;

        [SynthesisOrder]
        [SynthesisTooltip("If checked, the donor NPC's body (WNAM) record will be copied (if one exists).")]
        public bool CopyBody { get; set; } = true;

        [SynthesisOrder]
        [SynthesisTooltip("If checked, the donor NPC's Default and Sleeping outfit records will be copied (if they exist).")]
        public bool CopyOutfit { get; set; } = false;

        [SynthesisOrder]
        [SynthesisTooltip("If checked, the donor NPC's assets will be copied into Synthesis.esp so that the donor plugin can be disabled (ignored if the donor NPC comes from the base game).")]
        public bool CopyResourcesToPlugin { get; set; } = false;

        [SynthesisOrder]
        [SynthesisTooltip("If checked, the acceptor NPC's FaceGen mesh and texture will be backed up to Synthesis\\Data\\NPCAppearanceCopier\\BackupAssets. Not necessary if using MO2 because the copied FaceGen will go into Overwrite instead of replacing the original files.")]
        public bool BackUpFaceGen { get; set; } = false;
    }
}
