using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;
using NPCAppearanceCopier.Settings;
using System.IO;

namespace NPCAppearanceCopier
{
    public class Program
    {
        static Lazy<NACsettings> Settings = null!;
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .SetAutogeneratedSettings(
                    nickname: "Settings",
                    path: "settings.json",
                    out Settings)
                .AddRunnabilityCheck(CanRunPatch)
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "NPCApperanceCopier.esp")
                .Run(args);
        }

        private static void CanRunPatch(IRunnabilityState state)
        {
            NACsettings settings = Settings.Value;
            if (settings.FacegenOutputDirectory != "" && !Directory.Exists(settings.FacegenOutputDirectory))
            {
                throw new Exception("Cannot find output directory specified in settings: " + settings.FacegenOutputDirectory);
            }
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            NACsettings settings = Settings.Value;

            HashSet<ModKey> PluginsToMerge = new HashSet<ModKey>();

            RaceHandlingMode RaceChangeAction = settings.RaceChangeAction;

            Dictionary<IFormLinkGetter<IRaceGetter>, Dictionary<IFormLink<IRaceGetter>, FormKey>> PseudoCopiedRaces = new Dictionary<IFormLinkGetter<IRaceGetter>, Dictionary<IFormLink<IRaceGetter>, FormKey>>();

            string outputDir = state.ExtraSettingsDataPath + "\\FaceGen Output";
            if (Directory.Exists(settings.FacegenOutputDirectory))
            {
                outputDir = settings.FacegenOutputDirectory;
                Console.WriteLine("Exporting FaceGen to {0}", settings.FacegenOutputDirectory);
            }
            else
            {
                if (settings.FacegenOutputDirectory != "")
                {
                    Console.WriteLine("Directory {0} was not found. Exporting FaceGen to {1} instead.", settings.FacegenOutputDirectory, outputDir);
                }
                else
                {
                    Console.WriteLine("Exporting FaceGen to {0}.", outputDir);
                }

                if (Directory.Exists(outputDir) == false)
                {
                    Directory.CreateDirectory(outputDir);
                }
            }

            foreach (var NPCdef in settings.NPCs)
            {
                NPCdef.CopyFrom.TryResolve<INpcGetter>(state.LinkCache, out var DonorNPCGetter);
                string DonorNPCDispStr = NPCdef.CopyFrom.FormKey.ToString();
                if (DonorNPCGetter != null)
                {
                    DonorNPCDispStr = DonorNPCGetter.Name + " | " + DonorNPCGetter.EditorID + " | " + DonorNPCGetter.FormKey.ToString();
                }
                else
                {
                    Console.WriteLine("Could not find donor NPC {0}. Skipping this transfer.", DonorNPCDispStr);
                    continue;
                }

                NPCdef.CopyTo.TryResolve<INpcGetter>(state.LinkCache, out var copyTo);
                string AcceptorNPCDispStr = NPCdef.CopyTo.FormKey.ToString();
                if (copyTo != null)
                {
                    AcceptorNPCDispStr = copyTo.Name + " | " + copyTo.EditorID + " | " + copyTo.FormKey.ToString();
                }
                else
                {
                    Console.WriteLine("Could not find acceptor NPC {0}. Skipping this transfer.", AcceptorNPCDispStr);
                    continue;
                }
                var AcceptorNPC = state.PatchMod.Npcs.GetOrAddAsOverride(copyTo);

                Console.WriteLine("Copying appearance of {0} to {1}", DonorNPCDispStr, AcceptorNPCDispStr);


                // HANDLE FACEGEN HERE
                string donorNifPath = state.DataFolderPath + "\\meshes\\actors\\character\\facegendata\\facegeom\\" + DonorNPCGetter.FormKey.ModKey.ToString() + "\\00" + DonorNPCGetter.FormKey.IDString() + ".nif";
                string acceptorNifPath = outputDir + "\\meshes\\actors\\character\\facegendata\\facegeom\\" + AcceptorNPC.FormKey.ModKey.ToString() + "\\00" + AcceptorNPC.FormKey.IDString() + ".nif";
                if (!File.Exists(donorNifPath))
                {
                    Console.WriteLine("The following Facegen .nif does not exist. If it is within a BSA, please extract it. Patching of this NPC will be skipped.\n{0}", donorNifPath);
                    continue;
                }

                string donorDdsPath = state.DataFolderPath + "\\textures\\actors\\character\\facegendata\\facetint\\" + DonorNPCGetter.FormKey.ModKey.ToString() + "\\00" + DonorNPCGetter.FormKey.IDString() + ".dds";
                string acceptorDdsPath = outputDir + "\\textures\\actors\\character\\facegendata\\facetint\\" + AcceptorNPC.FormKey.ModKey.ToString() + "\\00" + AcceptorNPC.FormKey.IDString() + ".dds";
                if (!File.Exists(donorDdsPath))
                {
                    Console.WriteLine("The following Facegen .dds does not exist. If it is within a BSA, please extract it. Patching of this NPC will be skipped.\n{0}", donorDdsPath);
                    continue;
                }

                // Backup NPC Facegen files if necessary
                if (File.Exists(acceptorNifPath) && NPCdef.BackUpFaceGen == true)
                {
                    string AcceptorNifBackupPath = state.ExtraSettingsDataPath + "\\BackupAssets\\" + AcceptorNPC.FormKey.ModKey.ToString() + "\\00" + AcceptorNPC.FormKey.IDString() + ".nif_bak_0";
                    backupFaceGen(AcceptorNifBackupPath, AcceptorNPC, state);
                }

                if (File.Exists(acceptorDdsPath) && NPCdef.BackUpFaceGen == true)
                {
                    string AcceptorDdsBackupPath = state.ExtraSettingsDataPath + "\\BackupAssets\\" + AcceptorNPC.FormKey.ModKey.ToString() + "\\00" + AcceptorNPC.FormKey.IDString() + ".dds_bak_0";
                    backupFaceGen(AcceptorDdsBackupPath, AcceptorNPC, state);
                }

                // Copy NPC Facegen Nif and Dds from the donor to acceptor NPC

                // first make the output paths if they don't exist
                Directory.CreateDirectory(outputDir + "\\meshes\\actors\\character\\facegendata\\facegeom\\" + AcceptorNPC.FormKey.ModKey.ToString());
                Directory.CreateDirectory(outputDir + "\\textures\\actors\\character\\facegendata\\facetint\\" + AcceptorNPC.FormKey.ModKey.ToString());
                // then copy the facegen to those paths
                File.Copy(donorNifPath, acceptorNifPath, true);
                File.Copy(donorDdsPath, acceptorDdsPath, true);
                // END FACEGEN

                //Race
                bool bSameRace = AcceptorNPC.Race.FormKey == DonorNPCGetter.Race.FormKey;
                bool bMainLeave = RaceChangeAction == RaceHandlingMode.Leave;
                bool bNPCLeave = bMainLeave; // temporarily change because Synthesis won't auto-set NACnpc racehandling mode
                //bool bNPCLeave = NPCdef.RaceChangeAction == NACnpc.RaceHandlingMode.Leave || (bMainLeave && NPCdef.RaceChangeAction == NACnpc.RaceHandlingMode.Default);

                if (!bSameRace)
                {
                    string donorRaceDispStr = DonorNPCGetter.Race.FormKey.ToString();
                    var donorRaceGetter = DonorNPCGetter.Race.TryResolve<IRaceGetter>(state.LinkCache);
                    if (donorRaceGetter != null)
                    {
                        donorRaceDispStr = donorRaceGetter.Name + " | " + donorRaceGetter.FormKey.ToString();
                    }

                    string acceptorRaceDispStr = AcceptorNPC.Race.FormKey.ToString();
                    var acceptorRaceGetter = AcceptorNPC.Race.TryResolve<IRaceGetter>(state.LinkCache);
                    if (acceptorRaceGetter != null)
                    {
                        acceptorRaceDispStr = acceptorRaceGetter.Name + " | " + acceptorRaceGetter.FormKey.ToString();
                    }

                    if (!bNPCLeave)
                    {
                        /* // temporarily change because Synthesis won't auto-set NACnpc racehandling mode
                        switch (NPCdef.RaceChangeAction)
                        {
                            case NACnpc.RaceHandlingMode.Default: RaceChangeAction = settings.RaceChangeAction; break;
                            case NACnpc.RaceHandlingMode.Change: RaceChangeAction = RaceHandlingMode.Change; break;
                            case NACnpc.RaceHandlingMode.Pseudocopy: RaceChangeAction = RaceHandlingMode.Pseudocopy; break;
                        }
                        */
                        switch (RaceChangeAction)
                        {
                            case RaceHandlingMode.Change:
                                Console.WriteLine("Warning: Changing race from {0} to {1}", acceptorRaceDispStr, donorRaceDispStr);
                                AcceptorNPC.Race.SetTo(DonorNPCGetter.Race.FormKey);
                                break;

                            case RaceHandlingMode.Pseudocopy:
                                var copiedRaceFormKey = PseudoCopyRace(DonorNPCGetter, AcceptorNPC, PseudoCopiedRaces, state);
                                string copiedRaceDispString = copiedRaceFormKey.ToString();
                                if (state.LinkCache.TryResolve<IRaceGetter>(copiedRaceFormKey, out var pseudoCopiedRaceGetter))
                                {
                                    copiedRaceDispString = pseudoCopiedRaceGetter.Name + " | " + pseudoCopiedRaceGetter.FormKey.ToString();
                                }

                                Console.WriteLine("Warning: Race has been pseudocopied from {0} to {1}", acceptorRaceDispStr, copiedRaceDispString);
                                AcceptorNPC.Race.SetTo(copiedRaceFormKey);
                                break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Warning: the donor NPC's race ({0}) is not the same as the acceptor NPC's race. The acceptor's race ({1}) will be kept.\nTHIS CAN CAUSE THE DARK FACEGEN BUG. You have been warned.", donorRaceDispStr, acceptorRaceDispStr);
                    }
                }
                

                //Head Texture
                AcceptorNPC.HeadTexture.SetTo(DonorNPCGetter.HeadTexture.FormKeyNullable);

                //Head Parts
                AcceptorNPC.HeadParts.Clear(); 
                foreach (var hp in DonorNPCGetter.HeadParts)
                {
                    AcceptorNPC.HeadParts.Add(hp);
                }

                //Face Morph
                if (AcceptorNPC.FaceMorph != null && DonorNPCGetter.FaceMorph != null)
                {
                    AcceptorNPC.FaceMorph.Clear();
                    AcceptorNPC.FaceMorph.DeepCopyIn(DonorNPCGetter.FaceMorph);
                }

                //Face Parts
                if (AcceptorNPC.FaceParts != null && DonorNPCGetter.FaceParts != null)
                {
                    AcceptorNPC.FaceParts.Clear();
                    AcceptorNPC.FaceParts.DeepCopyIn(DonorNPCGetter.FaceParts);
                }

                //Hair Color
                AcceptorNPC.HairColor.SetTo(DonorNPCGetter.HairColor.FormKeyNullable);
                
                //Texture Lighting
                AcceptorNPC.TextureLighting = DonorNPCGetter.TextureLighting;

                //Tint Layers
                AcceptorNPC.TintLayers.Clear();
                foreach (var tl in DonorNPCGetter.TintLayers)
                {
                    TintLayer newTintLayer = new TintLayer();
                    newTintLayer.DeepCopyIn(tl);
                    AcceptorNPC.TintLayers.Add(newTintLayer);
                }

                //Height and Weight
                AcceptorNPC.Height = DonorNPCGetter.Height;
                AcceptorNPC.Weight = DonorNPCGetter.Weight;

                //WNAM a.k.a Body
                if (NPCdef.CopyBody == true)
                {
                    AcceptorNPC.WornArmor.SetTo(DonorNPCGetter.WornArmor);

                    if (AcceptorNPC.Race.FormKey != DonorNPCGetter.Race.FormKey && (RaceChangeAction == RaceHandlingMode.Pseudocopy || RaceChangeAction == RaceHandlingMode.Leave)) // if there is a race change, adjust the worn armor to make sure the acceptor NPC's final race is valid for the armor and its armature
                    {
                        if (state.LinkCache.TryResolve<IArmorGetter>(AcceptorNPC.WornArmor.FormKey, out var WNAM))
                        {
                            foreach (var aa in WNAM.Armature)
                            {
                                if (state.LinkCache.TryResolve<IArmorAddonGetter>(aa.FormKey, out var ARMA) && (ARMA.Race.FormKey == DonorNPCGetter.Race.FormKey || ARMA.AdditionalRaces.Where(p => p.FormKey == DonorNPCGetter.Race.FormKey).Any())) // do not patch armature such as werewolf and vampire lord whose Race & Additional Races do not include the donor NPC's race
                                {
                                    var modARMA = state.PatchMod.ArmorAddons.GetOrAddAsOverride(ARMA);
                                    modARMA.AdditionalRaces.Add(AcceptorNPC.Race);
                                }
                            }
                        }                         
                    }
                }

                //Outfits
                if (NPCdef.CopyOutfit == true)
                {
                    AcceptorNPC.DefaultOutfit.SetTo(DonorNPCGetter.DefaultOutfit);
                    AcceptorNPC.SleepingOutfit.SetTo(DonorNPCGetter.SleepingOutfit);
                }

                // If necessary, add dependencies (headparts, worn armors, etc) to be merged into Synthesis.esp
                if (NPCdef.CopyResourcesToPlugin == true)
                {
                    foreach (var FL in DonorNPCGetter.ContainedFormLinks)
                    {
                        if (settings.PluginsExcludedFromMerge.Contains(FL.FormKey.ModKey) == false && PluginsToMerge.Contains(FL.FormKey.ModKey) == false && FL.FormKey.ModKey != state.PatchMod.ModKey)
                        {
                            PluginsToMerge.Add(FL.FormKey.ModKey);
                        }
                    }
                }
            }

            //remap dependencies
            foreach (var mk in PluginsToMerge)
            {
                Console.WriteLine("Remapping Dependencies from {0}.", mk.ToString());
                state.PatchMod.DuplicateFromOnlyReferenced(state.LinkCache, mk, out var _);
            }

            Console.WriteLine("\n======PLEASE DO NOT FORGET to package the output FaceGen meshes and textures and install as a mod======");
        }

        public static FormKey PseudoCopyRace(INpcGetter? DonorNPCGetter, Npc? AcceptorNPC, Dictionary<IFormLinkGetter<IRaceGetter>, Dictionary<IFormLink<IRaceGetter>, FormKey>> PseudoCopiedRaces, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (DonorNPCGetter == null || AcceptorNPC == null)
            {
                return new FormKey();
            }

            FormKey ToReturnFK = new FormKey();

            if (PseudoCopiedRaces.ContainsKey(DonorNPCGetter.Race) && PseudoCopiedRaces[DonorNPCGetter.Race].ContainsKey(AcceptorNPC.Race))
            {
                ToReturnFK = PseudoCopiedRaces[DonorNPCGetter.Race][AcceptorNPC.Race];
            }
            else
            {
                if (!state.LinkCache.TryResolve<IRaceGetter>(AcceptorNPC.Race.FormKey, out var acceptorRace))
                {
                    return ToReturnFK;
                }

                if (!state.LinkCache.TryResolve<IRaceGetter>(DonorNPCGetter.Race.FormKey, out var donorRace))
                {
                    return ToReturnFK;
                }

                var newRace = state.PatchMod.Races.AddNew();
                newRace.DeepCopyIn(acceptorRace);

                if (newRace != null)
                {
                    newRace.Flags &= ~Race.Flag.Playable;
                    
                    if (newRace.Eyes != null && donorRace.Eyes != null)
                    {
                        newRace.Eyes.Clear();
                        newRace.Eyes.AddRange(donorRace.Eyes);
                    }
                    
                    if (newRace.FaceFxPhonemes != null && donorRace.FaceFxPhonemes != null)
                    {
                        newRace.FaceFxPhonemes.Clear();
                        newRace.FaceFxPhonemes.DeepCopyIn(donorRace.FaceFxPhonemes);
                    }
                    
                    newRace.FacegenFaceClamp = donorRace.FacegenFaceClamp;

                    newRace.FacegenMainClamp = donorRace.FacegenMainClamp;
                    
                    if (newRace.Hairs != null && donorRace.Hairs != null)
                    {
                        newRace.Hairs.Clear();
                        newRace.Hairs.AddRange(donorRace.Hairs);
                    }

                    if (newRace.HeadData != null && donorRace.HeadData != null)
                    {
                        if (newRace.HeadData.Female != null && donorRace.HeadData.Female != null)
                        {
                            newRace.HeadData.Female.Clear();
                            newRace.HeadData.Female.DeepCopyIn(donorRace.HeadData.Female);
                        }
                        if (newRace.HeadData.Male != null && donorRace.HeadData.Male != null)
                        {
                            newRace.HeadData.Male.Clear();
                            newRace.HeadData.Male.DeepCopyIn(donorRace.HeadData.Male);
                        }
                    }

                    if (newRace.MorphRace != null && donorRace.MorphRace != null)
                    {
                        newRace.MorphRace.SetTo(donorRace.MorphRace);
                    }

                    if (newRace.SkeletalModel != null && donorRace.SkeletalModel != null)
                    {
                        if (newRace.SkeletalModel.Female != null && donorRace.SkeletalModel.Female != null)
                        {
                            newRace.SkeletalModel.Female.Clear();
                            newRace.SkeletalModel.Female.DeepCopyIn(donorRace.SkeletalModel.Female);
                        }
                        if (newRace.SkeletalModel.Male != null && donorRace.SkeletalModel.Male != null)
                        {
                            newRace.SkeletalModel.Male.Clear();
                            newRace.SkeletalModel.Male.DeepCopyIn(donorRace.SkeletalModel.Male);
                        }
                    }

                    if (newRace.Skin != null && donorRace.Skin != null)
                    {
                        newRace.Skin.SetTo(donorRace.Skin);
                    }        

                    // add to dictionary
                    if (PseudoCopiedRaces.ContainsKey(DonorNPCGetter.Race) == false)
                    {
                        PseudoCopiedRaces[DonorNPCGetter.Race] = new Dictionary<IFormLink<IRaceGetter>, FormKey>();
                    }

                    PseudoCopiedRaces[DonorNPCGetter.Race][AcceptorNPC.Race] = newRace.FormKey;
                    ToReturnFK = newRace.FormKey;

                    // patch armor addons to include this new race
                    foreach (var arma in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorAddonGetter>())
                    {
                        if (!state.LinkCache.TryResolve<IRaceGetter>(arma.Race.FormKey, out var armaRace))
                        {
                            continue;
                        }

                        if (arma.Race.Equals(acceptorRace) || arma.AdditionalRaces.Contains(acceptorRace))
                        {
                            var moddedArma = state.PatchMod.ArmorAddons.GetOrAddAsOverride(arma);
                            moddedArma.AdditionalRaces.Add(newRace);
                        }
                    }
                }
            }

            return ToReturnFK;
        }

        public static void backupFaceGen(string inputPath, Npc? NPCtoBackup, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (NPCtoBackup == null)
            {
                return;
            }

            string BackupPath = state.ExtraSettingsDataPath + "\\BackupAssets\\" + NPCtoBackup.FormKey.ModKey.ToString() + "\\00" + NPCtoBackup.FormKey.IDString() + ".nif_bak_0";
            int count = 0;
            while (File.Exists(BackupPath) == true)
            {
                count++;
                BackupPath.Remove(BackupPath.Length - 1, 1);
                BackupPath += count.ToString();
            }
            if (Directory.Exists(state.ExtraSettingsDataPath + "\\BackupAssets") == false)
            {
                Directory.CreateDirectory(state.ExtraSettingsDataPath + "\\BackupAssets");
            }

            if (Directory.Exists(state.ExtraSettingsDataPath + "\\BackupAssets\\" + NPCtoBackup.FormKey.ModKey.ToString()) == false)
            {
                Directory.CreateDirectory(state.ExtraSettingsDataPath + "\\BackupAssets\\" + NPCtoBackup.FormKey.ModKey.ToString());
            }

            File.Move(inputPath, BackupPath);
        }
    }
}
