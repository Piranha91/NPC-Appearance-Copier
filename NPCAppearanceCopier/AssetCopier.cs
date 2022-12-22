using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using NPCAppearanceCopier.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPCAppearanceCopier
{
    public class AssetCopier
    {
        public static void copyAssets(INpcGetter npc, NACsettings settings, string currentModDirectory, NACnpc perNPCsetting, bool isTemplated, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, FileOperationLog fileCopyOperations)
        {
            HashSet<string> meshes = new HashSet<string>();
            HashSet<string> textures = new HashSet<string>();

            var context = state.LoadOrder.PriorityOrder.Npc().WinningContextOverrides().Where(x => x.Record.FormKey.Equals(npc.FormKey)).FirstOrDefault();
            if(context == null)
            {
                throw new Exception("Could not find context for NPC " + npc.FormKey);
            }

            var winningMod = context.ModKey;

            //FaceGen (not needed; handled by parent function)
            
            if (isTemplated == false)
            {
                var FaceGenSubPaths = getFaceGenSubPathStrings(npc.FormKey);
                meshes.Add(FaceGenSubPaths.Item1);
                textures.Add(FaceGenSubPaths.Item2);
            }
            if (perNPCsetting.CopyResourceFiles)
            {
                getAssetsReferencedByplugin(npc, settings, meshes, textures, state);
            }

            //extract needed files from BSA
            HashSet<string> extractedMeshFiles = new HashSet<string>();
            HashSet<string> extractedTexFiles = new HashSet<string>();
            if (settings.HandleBSAFiles) // if settings.CopyExtraAssets is disabled, then only FaceGen will be extracted here (if in BSA)
            {
                unpackAssetsFromBSA(meshes, textures, extractedMeshFiles, extractedTexFiles, winningMod, currentModDirectory, settings.FacegenOutputDirectory, fileCopyOperations); // meshes and textures are edited in this function - found & extracted files are removed from the HashSets. Only loose files remain
            } // end BSA handling for extra assets found in plugin


            if (perNPCsetting.CopyResourceFiles && perNPCsetting.FindExtraTexturesInNifs)
            {
                HashSet<string> alreadyHandledTextures = new HashSet<string>(textures, StringComparer.OrdinalIgnoreCase); // ignored these if found in nif because they have already been processed
                alreadyHandledTextures.UnionWith(extractedTexFiles); // will simply be empty if settings.HandleBSAFiles_Patching == false

                HashSet<string> extraTexturesFromNif = new HashSet<string>();
                getExtraTexturesFromNif(meshes, currentModDirectory, extraTexturesFromNif, alreadyHandledTextures); //traverse nifs for extra textures (loose nifs - in mod folder)

                if (settings.HandleBSAFiles)
                {
                    getExtraTexturesFromNif(extractedMeshFiles, settings.FacegenOutputDirectory, extraTexturesFromNif, alreadyHandledTextures); //traverse nifs for extra textures (BSA extracted nifs - in output folder)

                    // extract these additional textures from BSA if possible
                    HashSet<string> ExtractedExtraTextures = new HashSet<string>();
                    unpackAssetsFromBSA(new HashSet<string>(), extraTexturesFromNif, new HashSet<string>(), ExtractedExtraTextures, winningMod, currentModDirectory, settings.FacegenOutputDirectory, fileCopyOperations); // if any additional textures live the BSA, unpack them

                    // remove BSA-unpacked textures from the additional texture list
                    foreach (string s in ExtractedExtraTextures)
                    {
                        extraTexturesFromNif.Remove(s);
                    }
                }
                // copy extra texture list to output texture list
                foreach (string s in extraTexturesFromNif)
                {
                    textures.Add(s);
                }

            }

            // copy loose files
            var warningsToSuppressList = settings.warningsToSuppress.Where(w => w.Plugin.Equals(winningMod.ToString(), StringComparison.OrdinalIgnoreCase));
            var warningsToSuppress = new HashSet<string>(settings.warningsToSuppress_Global.Paths);
            if (warningsToSuppressList.Any()) { warningsToSuppress = warningsToSuppressList.First().Paths; }

            copyAssetFiles(settings, currentModDirectory, meshes, new HashSet<string>(), "Meshes", warningsToSuppress, state.DataFolderPath, fileCopyOperations);
            copyAssetFiles(settings, currentModDirectory, textures, new HashSet<string>(), "Textures", warningsToSuppress, state.DataFolderPath, fileCopyOperations);
        }

        public static void getExtraTexturesFromNif(HashSet<string> NifPaths, string NifDirectory, HashSet<string> outputTextures, HashSet<string> ignoredTextures)
        {
            foreach (var nifpath in NifPaths)
            {
                string fullPath = Path.Combine(NifDirectory, "meshes", nifpath);
                if (Path.GetExtension(nifpath) == ".nif" && File.Exists(fullPath))
                {
                    var nifTextures = NifHandler.getExtraTexturesFromNif(fullPath);
                    foreach (var t in nifTextures)
                    {
                        if (outputTextures.Contains(t) == false && ignoredTextures.Contains(t) == false)
                        {
                            outputTextures.Add(t);
                        }
                    }
                }
            }
        }

        public static void unpackAssetsFromBSA(HashSet<string> MeshesToExtract, HashSet<string> TexturesToExtract, HashSet<string> extractedMeshes, HashSet<string> ExtractedTextures, ModKey currentModKey, string currentModDirectory, string outputParentDir, FileOperationLog FileCopyOperations)
        {
            var BSAreaders = BSAHandler.openBSAArchiveReaders(currentModDirectory, currentModKey);
            foreach (string subPath in MeshesToExtract)
            {
                string meshPath = Path.Combine("meshes", subPath);
                foreach (var reader in BSAreaders)
                {
                    if (reader.Reader != null && BSAHandler.TryGetFile(meshPath, reader.Reader, out var file) && file != null)
                    {
                        extractedMeshes.Add(subPath);
                        string destFile = Path.Combine(outputParentDir, meshPath);
                        BSAHandler.extractFileFromBSA(file, destFile);
                        FileCopyOperations.CopyLog.Add(new FileCopyLog() { BSApath = reader.FilePath.Path, Source = meshPath, Destination = destFile });
                        break;
                    }
                }
            }

            foreach (string subPath in TexturesToExtract)
            {
                string texPath = Path.Combine("textures", subPath);
                foreach (var reader in BSAreaders)
                {
                    if (reader.Reader != null && BSAHandler.TryGetFile(texPath, reader.Reader, out var file) && file != null)
                    {
                        ExtractedTextures.Add(subPath);
                        string destFile = Path.Combine(outputParentDir, texPath);
                        BSAHandler.extractFileFromBSA(file, destFile);
                        FileCopyOperations.CopyLog.Add(new FileCopyLog() { BSApath = reader.FilePath.Path, Source = texPath, Destination = destFile });
                        break;
                    }
                }
            }

            // remove extracted files from list, which should now only contain loose files
            foreach (string s in extractedMeshes)
            {
                MeshesToExtract.Remove(s);
            }
            foreach (String s in ExtractedTextures)
            {
                TexturesToExtract.Remove(s);
            }
        }

        public static void getAssetsReferencedByplugin(INpcGetter npc, NACsettings settings, HashSet<string> meshes, HashSet<string> textures, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            //headparts
            foreach (var hp in npc.HeadParts)
            {
                if (!settings.PluginsExcludedFromMerge.Contains(hp.FormKey.ModKey))
                {
                    getHeadPartAssetPaths(hp, textures, meshes, settings.PluginsExcludedFromMerge, state);
                }
            }

            // armor and armature
            if (npc.WornArmor != null && state.LinkCache.TryResolve<IArmorGetter>(npc.WornArmor.FormKey, out var wnamGetter) && wnamGetter.Armature != null)
            {
                foreach (var aa in wnamGetter.Armature)
                {
                    if (!settings.PluginsExcludedFromMerge.Contains(aa.FormKey.ModKey))
                    {
                        {
                            getARMAAssetPaths(aa, textures, meshes, settings.PluginsExcludedFromMerge, state);
                        }
                    }
                }
            }
        }

        public static (string, string) getFaceGenSubPathStrings(FormKey npcFormKey)
        {
            string meshPath = "actors\\character\\facegendata\\facegeom\\" + npcFormKey.ModKey.ToString() + "\\00" + npcFormKey.IDString() + ".nif";
            string texPath = "actors\\character\\facegendata\\facetint\\" + npcFormKey.ModKey.ToString() + "\\00" + npcFormKey.IDString() + ".dds";
            return (meshPath, texPath);
        }

        public static void copyAssetFiles(NACsettings settings, string dataPath, HashSet<string> assetPathList, HashSet<string> ExtraDataDirectories, string type, HashSet<string> warningsToSuppress, string gameDataFolder, FileOperationLog FileCopyOperations)
        {
            string outputPrepend = Path.Combine(settings.FacegenOutputDirectory, type);
            if (Directory.Exists(outputPrepend) == false)
            {
                Directory.CreateDirectory(outputPrepend);
            }

            foreach (string s in assetPathList)
            {
                if (!isIgnored(s, settings.pathsToIgnore))
                {
                    string currentPath = Path.Join(dataPath, type, s);

                    bool bFileExists = false;
                    // check if file exists at primary path
                    if (File.Exists(currentPath))
                    {
                        bFileExists = true;
                    }
                    else
                    {
                        // check if file exists in the specified extra data paths
                        foreach (string extraDir in ExtraDataDirectories)
                        {
                            currentPath = Path.Join(extraDir, type, s);
                            if (File.Exists(currentPath))
                            {
                                bFileExists = true;
                                break;
                            }
                        }
                        if (bFileExists == false && s.IndexOf("actors\\character\\facegendata\\facetint") != 0 && s.IndexOf("actors\\character\\facegendata\\facegeom") != 0) // if enabled & asset is not FaceGen, look for it in global data path.
                        {
                            currentPath = Path.Join(gameDataFolder, type, s);
                            if (File.Exists(currentPath))
                            {
                                bFileExists = true;
                            }
                        }
                    }

                    if (bFileExists == false)
                    {
                        bool suppressMeSpecifically = settings.SuppressKnownMissingFileWarnings && (warningsToSuppress.Any(s => s.Equals(s, StringComparison.OrdinalIgnoreCase)));
                        bool isTriFile = getExtensionOfMissingFile(s) == ".tri";
                        bool suppressThis = settings.SuppressAllMissingFileWarnings || suppressMeSpecifically || isTriFile;

                        if (!suppressThis) // nested if statement intentional; otherwise a suppressed warning goes into the else block despite the target file not existing
                        {
                            if (settings.AbortIfMissingExtraAssets)
                            {
                                if (ExtraDataDirectories.Count == 0)
                                {
                                    throw new Exception("Extra Asset " + currentPath + " was not found.");
                                }
                                else
                                {
                                    throw new Exception("Extra Asset " + s + " was not found in " + dataPath + " or any Extra Data Directories.");
                                }
                            }
                            else
                            {
                                if (ExtraDataDirectories.Count == 0)
                                {
                                    Console.WriteLine("Warning: Extra Asset " + currentPath + " was not found.");
                                }
                                else
                                {
                                    Console.WriteLine("Warning: Extra Asset " + s + " was not found in " + dataPath + " or any Extra Data Directories.");
                                }
                            }
                        }
                    }
                    else
                    {
                        string destPath = Path.Join(outputPrepend, s);

                        FileInfo fileInfo = new FileInfo(destPath);
                        if (fileInfo != null && fileInfo.Directory != null && !fileInfo.Directory.Exists)
                        {
                            Directory.CreateDirectory(fileInfo.Directory.FullName);
                        }

                        File.Copy(currentPath, destPath, true);
                        FileCopyOperations.CopyLog.Add(new FileCopyLog() { Source = currentPath, Destination = destPath, BSApath = "" });
                    }
                }
            }
        }

        public static string getExtensionOfMissingFile(string input)
        {
            if (input == "") { return ""; }

            input = input.ToLower();
            var split = input.Split('.');
            return "." + split[split.Length - 1];
        }

        public static bool isIgnored(string s, HashSet<string> toIgnore)
        {
            string l = s.ToLower();
            foreach (string ig in toIgnore)
            {
                if (ig.ToLower() == l)
                {
                    return true;
                }
            }
            return false;
        }

        public static void getARMAAssetPaths(IFormLinkGetter<IArmorAddonGetter> aa, HashSet<string> texturePaths, HashSet<string> meshPaths, HashSet<ModKey> PluginsExcludedFromMerge, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!state.LinkCache.TryResolve<IArmorAddonGetter>(aa.FormKey, out var aaGetter))
            {
                return;
            }

            if (aaGetter.WorldModel != null && aaGetter.WorldModel.Male != null && aaGetter.WorldModel.Male.File != null)
            {
                meshPaths.Add(aaGetter.WorldModel.Male.File);
            }
            if (aaGetter.WorldModel != null && aaGetter.WorldModel.Female != null && aaGetter.WorldModel.Female.File != null)
            {
                meshPaths.Add(aaGetter.WorldModel.Female.File);
            }

            if (aaGetter.SkinTexture != null && aaGetter.SkinTexture.Male != null && !PluginsExcludedFromMerge.Contains(aaGetter.SkinTexture.Male.FormKey.ModKey) && state.LinkCache.TryResolve<ITextureSetGetter>(aaGetter.SkinTexture.Male.FormKey, out var mSkinTxst))
            {
                getTextureSetPaths(mSkinTxst, texturePaths);
            }
            if (aaGetter.SkinTexture != null && aaGetter.SkinTexture.Female != null && !PluginsExcludedFromMerge.Contains(aaGetter.SkinTexture.Female.FormKey.ModKey) && state.LinkCache.TryResolve<ITextureSetGetter>(aaGetter.SkinTexture.Female.FormKey, out var fSkinTxst))
            {
                getTextureSetPaths(fSkinTxst, texturePaths);
            }
        }

        public static void getHeadPartAssetPaths(IFormLinkGetter<IHeadPartGetter> hp, HashSet<string> texturePaths, HashSet<string> meshPaths, HashSet<ModKey> PluginsExcludedFromMerge, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!state.LinkCache.TryResolve<IHeadPartGetter>(hp.FormKey, out var hpGetter))
            {
                return;
            }

            if (hpGetter.Model != null && hpGetter.Model.File != null)
            {
                meshPaths.Add(hpGetter.Model.File);
            }

            if (hpGetter.Parts != null)
            {
                foreach (var part in hpGetter.Parts)
                {
                    if (part.FileName != null)
                    {
                        meshPaths.Add(part.FileName);
                    }
                }
            }

            if (hpGetter.TextureSet != null && state.LinkCache.TryResolve<ITextureSetGetter>(hpGetter.TextureSet.FormKey, out var hTxst))
            {
                getTextureSetPaths(hTxst, texturePaths);
            }

            if (hpGetter.ExtraParts != null)
            {
                foreach (var EP in hpGetter.ExtraParts)
                {
                    if (!PluginsExcludedFromMerge.Contains(EP.FormKey.ModKey))
                    {
                        getHeadPartAssetPaths(EP, texturePaths, meshPaths, PluginsExcludedFromMerge, state);
                    }
                }
            }
        }

        public static void getTextureSetPaths(ITextureSetGetter Txst, HashSet<string> texturePaths)
        {
            if (Txst.Diffuse != null)
            {
                texturePaths.Add(Txst.Diffuse);
            }
            if (Txst.NormalOrGloss != null)
            {
                texturePaths.Add(Txst.NormalOrGloss);
            }
            if (Txst.BacklightMaskOrSpecular != null)
            {
                texturePaths.Add(Txst.BacklightMaskOrSpecular);
            }
            if (Txst.Environment != null)
            {
                texturePaths.Add(Txst.Environment);
            }
            if (Txst.EnvironmentMaskOrSubsurfaceTint != null)
            {
                texturePaths.Add(Txst.EnvironmentMaskOrSubsurfaceTint);
            }
            if (Txst.GlowOrDetailMap != null)
            {
                texturePaths.Add(Txst.GlowOrDetailMap);
            }
        }

        public class FileOperationLog
        {
            public FileOperationLog()
            {
                CopyLog = new List<FileCopyLog>();
                Misc = new List<string>();
            }
            public List<FileCopyLog> CopyLog { get; set; }
            public List<string> Misc { get; set; }
        }

        public class FileCopyLog
        {
            public FileCopyLog()
            {
                Source = "";
                Destination = "";
                BSApath = "";
            }
            public string Source { get; set; }
            public string Destination { get; set; }
            public string BSApath { get; set; }
        }
    }
}
