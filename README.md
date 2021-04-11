Allows the user to change the appearance of an NPC(s) to that of other NPC(s) using the Settings tab.

If the "donor" NPC's facegen is in a BSA, it MUST be extracted/unpacked so that the patcher can find and transfer it to the correct NPC.

You can run this plugin every time you update your Synthesis.esp if you wish, or just run it once and rename the output to generate an "NPC X as NPC Y" patch.

To select an NPC, click "Settings" and then on the orange "NPCs" text. Then click "+" to add an NPC for appearance swap. Select the NPC whose appearance you want to transfer ("Copy From"), and the NPC to whom this appearance should be assigned ("Copy To"). 

If the template NPC has a special body texture (WNAM record) that you want to transfer along with the face, check the "Copy Body" box.

If you want to also copy the template NPC's default and sleeping outfits to the recipient NPC, check the "Copy Oufit" box.

If you want to copy the NPCs' dependencies into the generated Synthesis.esp, check the "Copy Resources to Plugin" box. This means that if the template NPC has custom headparts or such, they will also be copied into Synthesis as new records, so Synthesis will no longer require the template NPC's plugin as a master. This is useful for if you want to run Synthesis with just this patcher enabled to generate a "NPC X as NPC Y Visual Replacer" plugin.

If you want to back up the original NPC's facegen meshes and textures, check the "Back Up Face Gen" box. This should not be necessary in MO2 because the transferred FaceGen should appear in Overwrite rather than actually overwriting the original facegen, but I included it as an option because I'm not sure how it works on Vortex and I'm assuming the FaceGen files would just be overwritten if using NMM. The backed up files are found in Synthesis\Data\NPCAppearanceCopier\BackupAssets. 
