//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.WeaponModding;
using Newtonsoft.Json;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
    public class ItemsWithDecals
    {
        // yes, there can be multiple items with same Id,
        // for example when you open item preview of weapon you already hold in hands,
        // or when hideout shooting range clones weapon (we pretend that they have the same Id)
        public Dictionary<int, ItemWithDecals> Items; // TODO iterating dict is probably not the best idea, but list in annoying
        public List<DecalInfo> DecalsInfo;
    }

    public class ItemWithDecals
    {
        public WeaponPrefab WeaponPrefab;
        public List<Decal> Decals;
    }

    public class DecalInfo
    {
        public const int CurrentSchemaVersion = 6;

        public int SchemaVersion;
        public long SaveTime;
        public string Name;
        public string Texture;
        public Vector4 TextureUV;
        public float TextureAngle;
        public Vector4 ColorHSVA;
        public string Mask;
        public Vector4 MaskUV;
        public float MaskAngle;
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale;
        public float MaxAngle;
        public bool IsVisible;
        public DecalMirrorMode MirrorMode;

        public DecalInfo GetCopy()
        {
            // this is enough for now
            return (DecalInfo)MemberwiseClone();
        }
    }

    public enum DecalMirrorMode : byte
    {
        Disabled,
        Enabled,
        EnabledNoFlip,
        MODES_COUNT,
    }

    public class DecalTextureData
    {
        public Texture2D Preview;
        public Vector2Int OriginalSize;
        public DecalTextureType Type;
        public DecalTextureFormat Format;
        public string FilePath;
        public bool Error; // this flag is needed so we dont try to load corrupted asset over and over again
    }

    public enum DecalTextureType
    {
        Camo,
        Sticker,
        Mask
    }

    public enum DecalTextureFormat
    {
        Unknown,
        PNG,
        Video,
    }

    public abstract class DecalTextureAsset
    {
        public bool IsLoaded;
        public int InstancesCount;
        public Dictionary<Decal, Action<Decal, Texture>> WaitingAfterLoad;
        public Texture Texture;

        public abstract void Release();
    }

    public class DecalTexturePNG : DecalTextureAsset
    {
        public override void Release()
        {
            UnityEngine.Object.Destroy(Texture);
        }
    }

    public class DecalTextureVideo : DecalTextureAsset
    {
        public VideoPlayer VideoPlayer;

        public override void Release()
        {
            VideoPlayer.Stop();
            UnityEngine.Object.Destroy(VideoPlayer);
            UnityEngine.Object.Destroy(Texture);
        }
    }

    public class TexturesDirectory
    {
        public bool IsClosed;
        public string Name;
        public string[] Textures;
        public TexturesDirectory[] Directories;
    }

    public class ClosedTexturesDirectories
    {
        public HashSet<string> CamosDirectory;
        public HashSet<string> StickersDirectory;
        public HashSet<string> MasksDirectory;
    }

    [BepInPlugin("7Bpencil.WeaponCamoAndStickers", "7Bpencil.WeaponCamoAndStickers", "1.5.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string DefaultCamoName = "builtin/camos/default.png";
        public const string DefaultStickerName = "builtin/stickers/default.png";
        public const string DefaultMaskName = "builtin/masks/default.png";
        public const string ErrorTextureFilePath = "builtin/error.png";
        public const string BuiltinDirectoryName = "builtin";
        public const int MaxVideoLoadTicks = 300; // most videos I tried took 5-10 ticks to load, so 300 should be enough (around 5 sec)

        public static Plugin Instance;

        public static ConfigEntry<bool> PlayVideoAudio;
        public static ConfigEntry<float> UIScale;
        public static ConfigEntry<KeyboardShortcut> MoveButton;
        public static ConfigEntry<KeyboardShortcut> RotateButton;
        public static ConfigEntry<KeyboardShortcut> ScaleButton;
        public static ConfigEntry<int> GoonsWeaponCamoSpawnChance;
        public static ConfigEntry<int> PMCWeaponCamoSpawnChance;
        public static ConfigEntry<int> OtherBossesWeaponCamoSpawnChance;

		public ManualLogSource LoggerInstance;

        private string TexturesDir;
        private string PreviewsDir;
        private string ItemsDir;
        private string PresetsDir;
        private string ClosedDirectoriesPath;
        private Shader DecalShader;
        private Texture2D ErrorTexture;
        private DecalTextureData ErrorTextureData;
        private CamoEditorResources CamoEditorResources;
        private Dictionary<string, DecalTextureData> DecalTextures;
        private Dictionary<string, DecalTextureAsset> DecalTextureAssets;
        private ClosedTexturesDirectories ClosedDirectories;
        private TexturesDirectory CamosDirectory;
        private TexturesDirectory StickersDirectory;
        private TexturesDirectory MasksDirectory;
        private string[] Camos;
        private string[] Stickers;
        private string[] Masks;

        private Dictionary<string, List<DecalInfo>> DecalPresets;
        private Dictionary<string, ItemsWithDecals> ItemsWithDecals;
        private Dictionary<string, string> Clones;
        private HashSet<string> ItemsWaitingForRandomCamo;
        private Dictionary<Camera, string> WeaponPreviewCameras;
        private HashSet<Camera> PlayerModelViewCameras;

        private DecalRenderer DecalRenderer;
        private Option<CamoEditor> CamoEditor;
        private bool IsCamoEditorWaitingForWeaponPreview;

        public bool IsFikaSupportEnabled;
        public bool IsFikaHeadless;
        public Option<bool> IsFikaServer;
        public Action<Dictionary<string, List<DecalInfo>>> OnBotWeaponCamoGenerated;
        public Dictionary<string, WeaponPrefab> WeaponsWaitingForRemoteCamo;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            PlayVideoAudio = Config.Bind<bool>("Main", "Video | Play Audio", false, "");
            PlayVideoAudio.SettingChanged += (_, _) => ChangeAudioOnAllVideos(PlayVideoAudio.Value);
            UIScale = Config.Bind<float>("Main", "Camo Editor | UI Scale", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.5f, 2f)));
            MoveButton = Config.Bind("Main", "Camo Editor | Keybinds | Move", new KeyboardShortcut(KeyCode.G), "");
            RotateButton = Config.Bind("Main", "Camo Editor | Keybinds | Rotate", new KeyboardShortcut(KeyCode.R), "");
            ScaleButton = Config.Bind("Main", "Camo Editor | Keybinds | Scale", new KeyboardShortcut(KeyCode.S), "");
            GoonsWeaponCamoSpawnChance = Config.Bind<int>("Main", "Raid | Camo Spawn Chance | Goons", 100, new ConfigDescription("", new AcceptableValueRange<int>(0, 100)));
            PMCWeaponCamoSpawnChance = Config.Bind<int>("Main", "Raid | Camo Spawn Chance | PMC", 33, new ConfigDescription("", new AcceptableValueRange<int>(0, 100)));
            OtherBossesWeaponCamoSpawnChance = Config.Bind<int>("Main", "Raid | Camo Spawn Chance | Other Bosses", 50, new ConfigDescription("", new AcceptableValueRange<int>(0, 100)));

            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            TexturesDir = Path.Combine(assemblyDir, "textures");
            PreviewsDir = Path.Combine(assemblyDir, "temp", "previews");
            ItemsDir = Path.Combine(assemblyDir, "items");
            PresetsDir = Path.Combine(assemblyDir, "presets");
            ClosedDirectoriesPath = Path.Combine(assemblyDir, "temp", "closed-directories.json");
			var bundlePath = Path.Combine(assemblyDir, "bundles", "weapon-camo-and-stickers");
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            DecalShader = bundle.LoadAsset<Shader>("Assets/WeaponCamoAndStickers/Shaders/DecalDynamic.shader");
            ErrorTexture = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Textures/missing.png");
            ErrorTextureData = new()
            {
                Preview = ErrorTexture,
                OriginalSize = new(ErrorTexture.width, ErrorTexture.height),
                Type = DecalTextureType.Camo,
                Format = DecalTextureFormat.PNG,
                FilePath = ErrorTextureFilePath,
                Error = true,
            };
            CamoEditorResources = new(bundle);
            DecalTextures = new();
            DecalTextureAssets = new();

            ClosedDirectories = LoadClosedTexturesDirectories(ClosedDirectoriesPath);
            (CamosDirectory, Camos) = LoadTexturesFromDirectory(DecalTextureType.Camo, "camos", ClosedDirectories.CamosDirectory, DefaultCamoName, DecalTextureFormat.PNG, Texture2D.whiteTexture);
            (StickersDirectory, Stickers) = LoadTexturesFromDirectory(DecalTextureType.Sticker, "stickers", ClosedDirectories.StickersDirectory, DefaultStickerName, DecalTextureFormat.PNG, Texture2D.whiteTexture);
            (MasksDirectory, Masks) = LoadTexturesFromDirectory(DecalTextureType.Mask, "masks", ClosedDirectories.MasksDirectory, DefaultMaskName, DecalTextureFormat.PNG, Texture2D.whiteTexture);

            DecalPresets = LoadDecalPresets(PresetsDir);
            ItemsWithDecals = LoadItemsWithDecals(ItemsDir);
            Clones = new();
            ItemsWaitingForRandomCamo = new();
            WeaponPreviewCameras = new();
            PlayerModelViewCameras = new();

            DecalRenderer = new(ItemsWithDecals, WeaponPreviewCameras, PlayerModelViewCameras);

            WeaponsWaitingForRemoteCamo = new();

            new Patch_WeaponPreview_Class3271_method_1().Enable();
            new Patch_WeaponPreview_Rotate().Enable();
            new Patch_WeaponPreview_Hide().Enable();
            new Patch_WeaponModdingScreen_Show().Enable();
            new Patch_WeaponModdingScreen_Close().Enable();
            new Patch_WeaponPrefab_InitHotObjects().Enable();
            new Patch_WeaponPrefab_ReturnToPool().Enable();
            new Patch_AssetPoolObject_OnDestroy().Enable();
            new Patch_GClass3380_smethod_2().Enable();
            new Patch_GClass2304_smethod_0().Enable();
            new Patch_PlayerModelView_method_0().Enable();
            new Patch_PlayerModelView_method_1().Enable();
            new Patch_PlayerBody_SetSkin().Enable();
            new Patch_BotCreatorClass_method_2().Enable();

            // TODO
            // maybe apply camo texture on top of diffuse texture?

            // TODO
            // hear me out: we can place 3D models as decorations on guns and equipment

            // TODO
            // figure out optimal settings for preview textures and full size textures
            // depending on will they be thrown away, written on disk, etc.

            // TODO
            // highlight currently selected texture

            // TODO
            // mark as favourite + favourite folder at the top

            // TODO
            // clean all orphan items

			// TODO
			// instancing

			// TODO
			// add tooltips on all UI elements
        }

        public ClosedTexturesDirectories LoadClosedTexturesDirectories(string filePath)
        {
            if (SafeIO.ReadAllText(filePath).Ok(out var json, out var e))
            {
                var result = JsonConvert.DeserializeObject<ClosedTexturesDirectories>(json);
                return result;
            }
            else
            {
                Logger.LogError($"Failed to load closed directories, rolling back to default config: {e}");
            }

            return new ClosedTexturesDirectories()
            {
                CamosDirectory = new(),
                StickersDirectory = new(),
                MasksDirectory = new(),
            };
        }

        public void SaveClosedTexturesDirectoriesToDisk(ClosedTexturesDirectories closedDirectories, string filePath)
        {
            closedDirectories.CamosDirectory.Clear();
            closedDirectories.StickersDirectory.Clear();
            closedDirectories.MasksDirectory.Clear();

            SaveClosedTexturesDirectories(CamosDirectory, closedDirectories.CamosDirectory);
            SaveClosedTexturesDirectories(StickersDirectory, closedDirectories.StickersDirectory);
            SaveClosedTexturesDirectories(MasksDirectory, closedDirectories.MasksDirectory);

            var json = JsonConvert.SerializeObject(closedDirectories, Formatting.Indented);
            SafeIO.WriteAllTextAsync(filePath, json);
        }

        public void SaveClosedTexturesDirectories(TexturesDirectory directory, HashSet<string> result)
        {
            if (directory.IsClosed)
            {
                result.Add(directory.Name);
            }
            foreach (var subDirectory in directory.Directories)
            {
                SaveClosedTexturesDirectories(subDirectory, result);
            }
        }

        public (TexturesDirectory, string[]) LoadTexturesFromDirectory(
            DecalTextureType textureType,
            string subfolder,
            HashSet<string> closedDirectories,
            string defaultTextureName,
            DecalTextureFormat defaultTextureFormat,
            Texture2D defaultTexture)
        {
            TexturesDirectory directory;

            var directoryPath = Path.Combine(TexturesDir, subfolder);
            if (Directory.Exists(directoryPath))
            {
                directory = WalkTextureDirectory(directoryPath, directoryPath, textureType, closedDirectories, 1);
            }
            else
            {
                var directoryName = GetDirectoryName(directoryPath, directoryPath);
                directory = new TexturesDirectory()
                {
                    IsClosed = closedDirectories.Contains(directoryName),
                    Name = directoryName,
                    Textures = [],
                    Directories = new TexturesDirectory[1] // reserve space for builtin folder
                };
            }

            AddBuiltinDirectory(defaultTexture, defaultTextureName, textureType, defaultTextureFormat, directory, closedDirectories);

            var totalTextures = CollectAllTexturesFromDirectory(directory);
            return (directory, totalTextures);
        }

        public string[] CollectAllTexturesFromDirectory(TexturesDirectory directory)
        {
            var totalTexturesCount = 0;
            GetTotalTexturesCountInDirectory(directory, ref totalTexturesCount);

            var totalTextures = new string[totalTexturesCount];
            var totalTextureIndex = 0;
            AddTexturesFromDirectory(directory, totalTextures, ref totalTextureIndex);

            return totalTextures;
        }

        public void GetTotalTexturesCountInDirectory(TexturesDirectory directory, ref int count)
        {
            count += directory.Textures.Length;
            foreach (var subDirectory in directory.Directories)
            {
                GetTotalTexturesCountInDirectory(subDirectory, ref count);
            }
        }

        public void AddTexturesFromDirectory(TexturesDirectory directory, string[] array, ref int index)
        {
            for (var i = 0; i < directory.Textures.Length; i++)
            {
                array[index + i] = directory.Textures[i];
            }
            index += directory.Textures.Length;

            foreach (var subDirectory in directory.Directories)
            {
                AddTexturesFromDirectory(subDirectory, array, ref index);
            }
        }

        public string GetDirectoryName(string directoryPath, string rootPath)
        {
            if (directoryPath == rootPath)
            {
                return "";
            }

            var directoryName = directoryPath
                .Replace(rootPath, "")
                .Remove(0, 1) // remove first slash
                .Replace(@"\", @"/"); // replace windows slashes with unix ones

            return directoryName;
        }

        public (string name, string extension) GetTextureName(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            var textureName = filePath
                .Replace(TexturesDir, "")
                .Remove(0, 1) // remove first slash
                .Replace(@"\", @"/"); // replace windows slashes with unix ones

            return (textureName, extension);
        }

        public TexturesDirectory WalkTextureDirectory(string directoryPath, string rootPath, DecalTextureType textureType, HashSet<string> closedDirectories, int suffix = 0)
        {
            // we check every file in directory even if its not a supported type,
            // this way user can see what files are broken (they will have error texture), and remove them (or not),
            // this also makes it easier to architect loading big assets asynchronously

            var filePaths = SafeIO.GetFiles(directoryPath);
            var textures = new string[filePaths.Length];
            for (var i = 0; i < filePaths.Length; i++)
            {
                var filePath = filePaths[i];
                var textureIndex = i;
                TryLoadTexture(filePath, textureType, textureIndex, textures);
            }

            var directoryPaths = SafeIO.GetDirectories(directoryPath);
            var directories = new TexturesDirectory[directoryPaths.Length + suffix]; // suffix to reserve space for builtin folder
            for (var i = 0; i < directoryPaths.Length; i++)
            {
                var subDirectoryPath = directoryPaths[i];
                directories[i] = WalkTextureDirectory(subDirectoryPath, rootPath, textureType, closedDirectories);
            }

            var directoryName = GetDirectoryName(directoryPath, rootPath);
            return new()
            {
                IsClosed = closedDirectories.Contains(directoryName),
                Name = directoryName,
                Textures = textures,
                Directories = directories
            };
        }

        public void AddBuiltinDirectory(
            Texture2D texture,
            string textureName,
            DecalTextureType textureType,
            DecalTextureFormat textureFormat,
            TexturesDirectory root,
            HashSet<string> closedDirectories)
        {
            var builtinDirectory = new TexturesDirectory()
            {
                IsClosed = closedDirectories.Contains(BuiltinDirectoryName),
                Name = BuiltinDirectoryName,
                Textures = [textureName],
                Directories = []
            };
            root.Directories[root.Directories.Length - 1] = builtinDirectory;
            AddTexture(texture, new(texture.width, texture.height), new()
            {
                FilePath = textureName,
                Name = textureName,
                Type = textureType,
                Format = textureFormat,
            });
            DecalTextureAssets.Add(textureName, new DecalTexturePNG()
            {
                IsLoaded = true,
                InstancesCount = 1,
                WaitingAfterLoad = null,
                Texture = texture,
            });
        }

        public void TryLoadTexture(
            string textureFilePath,
            DecalTextureType textureType,
            int textureIndex,
            string[] textures)
        {
            var (textureName, extension) = GetTextureName(textureFilePath);
            var textureFormat = GetTextureFormatFromExtension(extension);
            var param = new AddTexturePararms()
            {
                FilePath = textureFilePath,
                Name = textureName,
                Type = textureType,
                Format = textureFormat,
            };

            textures[textureIndex] = textureName;

            if (textureFormat == DecalTextureFormat.Unknown)
            {
                AddTextureError(param);
                return;
            }

            var previewFilePath = Path.Combine(PreviewsDir, textureName);
            if (File.Exists(previewFilePath))
            {
                try
                {
                    LoadPreviewFromDisk(previewFilePath, param);
                }
                catch
                {
                    AddTextureError(param);
                }
            }
            else
            {
                CreatePreviewAndStoreOnDisk(previewFilePath, param);
            }
        }

        public static DecalTextureFormat GetTextureFormatFromExtension(string extension)
        {
            if (EIC(extension, ".png"))
            {
                return DecalTextureFormat.PNG;
            }
            if (EIC(extension, ".mp4") || EIC(extension, ".webm"))
            {
                return DecalTextureFormat.Video;
            }

            return DecalTextureFormat.Unknown;
        }

        public static bool EIC(string current, string target)
        {
            return target.Equals(current, StringComparison.OrdinalIgnoreCase);
        }

        public void LoadPreviewFromDisk(string previewFilePath, AddTexturePararms param)
        {
            using var stream = File.Open(previewFilePath, FileMode.Open);
            using var reader = new BinaryReader(stream);

            var originalWidth = reader.ReadInt32();
            var originalHeight = reader.ReadInt32();
            var remaining = stream.Length - stream.Position;
            var previewBytes = reader.ReadBytes((int)remaining);

            // preview will look blurry with mip chain
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: false, createUninitialized: true);
            if (ImageConversion.LoadImage(preview, previewBytes, markNonReadable: true))
            {
                AddTexture(preview, new(originalWidth, originalHeight), param);
            }
            else
            {
                AddTextureError(param);
                Destroy(preview);
            }
        }

        public void CreatePreviewAndStoreOnDisk(string previewFilePath, AddTexturePararms param)
        {
            if (param.Format == DecalTextureFormat.PNG)
            {
                StartCoroutine(CreatePreviewAndStoreOnDisk_PNG(previewFilePath, param));
            }
            if (param.Format == DecalTextureFormat.Video)
            {
                StartCoroutine(CreatePreviewAndStoreOnDisk_Video(previewFilePath, param));
            }
        }

        public IEnumerator CreatePreviewAndStoreOnDisk_PNG(string previewFilePath, AddTexturePararms param)
        {
            using var uwr = UnityWebRequestTexture.GetTexture(param.FilePath, nonReadable: true);
            uwr.timeout = 5;

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                AddTextureError(param);
                yield break;
            }

            var texture = DownloadHandlerTexture.GetContent(uwr);
            var (preview, originalSize) = CreatePreviewAndStoreOnDisk_Texture(previewFilePath, texture);
            AddTexture(preview, originalSize, param);
            Destroy(texture);
        }

        private (Texture2D, Vector2Int) CreatePreviewAndStoreOnDisk_Texture(string previewFilePath, Texture texture)
        {
            const int previewMaxSize = 128;

            var textureSize = new Vector2Int(texture.width, texture.height);
            var textureMaxSize = Math.Max(textureSize.x, textureSize.y);
            var previewSize = (textureSize * previewMaxSize) / textureMaxSize;

            var previousActive = RenderTexture.active;
            var tmpRT = RenderTexture.GetTemporary(previewSize.x, previewSize.y, 0, RenderTextureFormat.ARGB32);

            Graphics.Blit(texture, tmpRT);

            RenderTexture.active = tmpRT;
            var preview = new Texture2D(previewSize.x, previewSize.y, TextureFormat.RGBA32, mipChain: false, linear: false, createUninitialized: false);
            preview.ReadPixels(new Rect(0, 0, previewSize.x, previewSize.y), 0, 0, false);
            preview.Apply();

            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(tmpRT);

            try
            {
                var previewFileInfo = new FileInfo(previewFilePath);
                Directory.CreateDirectory(previewFileInfo.Directory.FullName);
                using var stream = File.Open(previewFileInfo.FullName, FileMode.Create);
                using var writer = new BinaryWriter(stream);

                writer.Write(textureSize.x);
                writer.Write(textureSize.y);
                writer.Write(preview.EncodeToPNG());
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to save preview: {previewFilePath}, error: {e}");
            }

            return (preview, textureSize);
        }

        public IEnumerator CreatePreviewAndStoreOnDisk_Video(string previewFilePath, AddTexturePararms param)
        {
            var loadTicks = 0;
            var hitError = false;

            bool hitErrorOrTimeout()
            {
                return hitError || loadTicks >= MaxVideoLoadTicks;
            }

            var videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.errorReceived += (_, message) =>
            {
                Logger.LogError(message);
                hitError = true;
            };
            videoPlayer.audioOutputMode = GetVideoAudioOutputMode(false);
            videoPlayer.playOnAwake = false;
            videoPlayer.url = param.FilePath;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.Prepare();

            while (!videoPlayer.isPrepared && !hitErrorOrTimeout())
            {
                yield return null;
                loadTicks++;
            }

            if (hitErrorOrTimeout())
            {
                AddTextureError(param);
                videoPlayer.Stop();
                Destroy(videoPlayer);
                yield break;
            }

            var renderTexture = new RenderTexture((int)videoPlayer.width, (int)videoPlayer.height, 0);
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.Play();

            while (videoPlayer.frame <= 1 && !hitErrorOrTimeout())
            {
                yield return null;
                loadTicks++;
            }

            if (hitErrorOrTimeout())
            {
                AddTextureError(param);
                videoPlayer.Stop();
                Destroy(videoPlayer);
                Destroy(renderTexture);
                yield break;
            }

            videoPlayer.Pause();

            var (preview, originalSize) = CreatePreviewAndStoreOnDisk_Texture(previewFilePath, renderTexture);
            AddTexture(preview, originalSize, param);

            videoPlayer.Stop();
            Destroy(videoPlayer);
            Destroy(renderTexture);
        }

        public struct AddTexturePararms
        {
            public string FilePath;
            public string Name;
            public DecalTextureType Type;
            public DecalTextureFormat Format;
        }

        public void AddTexture(Texture2D preview, Vector2Int originalSize, AddTexturePararms param, bool error = false)
        {
            var textureData = new DecalTextureData()
            {
                Preview = preview,
                OriginalSize = originalSize,
                Type = param.Type,
                Format = param.Format,
                FilePath = param.FilePath,
                Error = error,
            };

            DecalTextures.Add(param.Name, textureData);
        }

        public void AddTextureError(AddTexturePararms param)
        {
            AddTexture(ErrorTexture, new(ErrorTexture.width, ErrorTexture.height), param, error: true);
            LogTexture(LogLevel.Error, "Failed to load texture", param.Name);
        }

        public Dictionary<string, List<DecalInfo>> LoadDecalPresets(string directoryPath)
        {
            var filePaths = SafeIO.GetFiles(directoryPath, "*.json");
            var result = new Dictionary<string, List<DecalInfo>>();

            foreach (var filePath in filePaths)
            {
                var presetName = Path.GetFileNameWithoutExtension(filePath);
                if (SafeIO.ReadAllText(filePath).Ok(out var json, out var e))
                {
                    var decalsInfo = JsonConvert.DeserializeObject<List<DecalInfo>>(json);
                    UpgradeOldVersionsOfDecalsInfo(decalsInfo);
                    result.Add(presetName, decalsInfo);
                }
                else
                {
                    Logger.LogError($"Failed to load preset: {presetName}, error: {e}");
                }
            }

            return result;
        }

        public Dictionary<string, ItemsWithDecals> LoadItemsWithDecals(string directoryPath)
        {
            var filePaths = SafeIO.GetFiles(directoryPath, "*.json");
            var result = new Dictionary<string, ItemsWithDecals>();

            foreach (var filePath in filePaths)
            {
                var itemId = Path.GetFileNameWithoutExtension(filePath);
                if (SafeIO.ReadAllText(filePath).Ok(out var json, out var e))
                {
                    var decalsInfo = JsonConvert.DeserializeObject<List<DecalInfo>>(json);
                    UpgradeOldVersionsOfDecalsInfo(decalsInfo);
                    var itemsWithDecals = new ItemsWithDecals()
                    {
                        Items = new(),
                        DecalsInfo = decalsInfo,
                    };

                    result.Add(itemId, itemsWithDecals);
                }
                else
                {
                    Logger.LogError($"Failed to load item: {itemId}, error: {e}");
                }
            }

            return result;
        }

        public static void UpgradeOldVersionsOfDecalsInfo(List<DecalInfo> decalsInfo)
        {
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var decalInfo in decalsInfo)
            {
                if (decalInfo.SchemaVersion == 0)
                {
                    decalInfo.SchemaVersion = 1;
                    // I forgot to set schema version, so now
                    // there are camos with schema version 0
                    // and non null names...
                    if (decalInfo.Name == null)
                    {
                        decalInfo.Name = "";
                    }
                }
                if (decalInfo.SchemaVersion == 1)
                {
                    decalInfo.SchemaVersion = 2;
                    decalInfo.TextureUV = UpgradeUV_from_1_to_2(decalInfo.TextureUV);
                    decalInfo.MaskUV = UpgradeUV_from_1_to_2(decalInfo.MaskUV);
                }
                if (decalInfo.SchemaVersion == 2)
                {
                    decalInfo.SchemaVersion = 3;
                    decalInfo.SaveTime = time; // this wont cause any issues, right?
                }
                if (decalInfo.SchemaVersion == 3)
                {
                    // here I realized that extension is useful
                    decalInfo.SchemaVersion = 4;
                    decalInfo.Texture += ".png";
                    decalInfo.Mask += ".png";
                }
                if (decalInfo.SchemaVersion == 4)
                {
                    decalInfo.SchemaVersion = 5;
                    decalInfo.IsVisible = true;
                }
                if (decalInfo.SchemaVersion == 5)
                {
                    decalInfo.SchemaVersion = 6;
                    decalInfo.MirrorMode = DecalMirrorMode.Disabled;
                }
            }
        }

        public static Vector4 UpgradeUV_from_1_to_2(Vector4 uv)
        {
            var size = new Vector2(uv.z, uv.w);
            var offset = 0.5f * (Vector2.one - UVTools.Divide(Vector2.one, size));
            var result = new Vector4(offset.x, offset.y, size.x, size.y);
            return result;
        }

        public void Update()
        {
            CheckCamoEditorKeybinds();
        }

        public void CheckCamoEditorKeybinds()
        {
            if (CamoEditor.Some(out var camoEditor) && camoEditor.CurrentlyEditedDecalIndex.HasValue)
            {
                if (Input.GetKeyDown(MoveButton.Value.MainKey))
                {
                    camoEditor.SetupTransformHandle(HandleType.Position);
                }
                else if (Input.GetKeyDown(RotateButton.Value.MainKey))
                {
                    camoEditor.SetupTransformHandle(HandleType.Rotation);
                }
                else if (Input.GetKeyDown(ScaleButton.Value.MainKey))
                {
                    camoEditor.SetupTransformHandle(HandleType.Scale);
                }
            }
        }

        public void LateUpdate()
        {
            if (CamoEditor.Some(out var camoEditor))
            {
                camoEditor.DrawDecalProjectionBox();
            }
        }

        public void OnGUI()
        {
            if (CamoEditor.Some(out var camoEditor))
            {
                camoEditor.DrawWindow();
            }
        }

        public (DecalInfo, Decal) GetDecal(string itemId, int instanceID, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];
            var decal = itemsWithDecals.Items[instanceID].Decals[decalIndex];
            return (decalInfo, decal);
        }

        public Option<List<DecalInfo>> GetDecalsInfo(string itemId)
        {
            if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                return new(itemsWithDecals.DecalsInfo);
            }

            return default;
        }

        public int GetDecalsCount(string itemId)
        {
            if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                return itemsWithDecals.DecalsInfo.Count;
            }

            return 0;
        }

        public int GetPresetsCount()
        {
            return DecalPresets.Count;
        }

        public Dictionary<string, List<DecalInfo>>.KeyCollection GetPresetNames()
        {
            return DecalPresets.Keys;
        }

        public DecalTextureData GetTextureData(string textureName)
        {
			if (DecalTextures.TryGetValue(textureName, out var textureData))
            {
                return textureData;
            }

            return ErrorTextureData;
        }

        public void AcquireDecalTextureAsset(Decal decal, string textureName, Action<Decal> beforeLoad, Action<Decal, Texture> afterLoad)
        {
            LogTexture(LogLevel.Info, "Increment", textureName);

            beforeLoad(decal);
            var textureData = GetTextureData(textureName);

            if (textureData.Error)
            {
                AcquireDecalTextureError(decal, afterLoad);
                return;
            }

            if (DecalTextureAssets.TryGetValue(textureData.FilePath, out var asset))
            {
                asset.InstancesCount++;
                if (asset.IsLoaded)
                {
                    afterLoad(decal, asset.Texture);
                    LogTexture(LogLevel.Info, "Load from cache", textureName);
                }
                else
                {
                    asset.WaitingAfterLoad.Add(decal, afterLoad);
                    LogTexture(LogLevel.Info, "Already loading", textureName);
                }
            }
            else
            {
                LogTexture(LogLevel.Info, "Start loading from disk", textureName);
                if (textureData.Format == DecalTextureFormat.PNG)
                {
                    StartCoroutine(LoadPNG(decal, textureName, textureData, afterLoad));
                }
                if (textureData.Format == DecalTextureFormat.Video)
                {
                    StartCoroutine(LoadVideo(decal, textureName, textureData, afterLoad));
                }
            }
        }

        public IEnumerator LoadPNG(Decal decal, string textureName, DecalTextureData textureData, Action<Decal, Texture> afterLoad)
        {
            if (!File.Exists(textureData.FilePath))
            {
                textureData.Error = true;
                AcquireDecalTextureError(decal, afterLoad);
                LogTexture(LogLevel.Error, "Failed to load from disk", textureName);
                yield break;
            }

            var asset = new DecalTexturePNG()
            {
                IsLoaded = false,
                InstancesCount = 1,
                WaitingAfterLoad = new() { { decal, afterLoad } },
                Texture = null,
            };
            DecalTextureAssets.Add(textureData.FilePath, asset);

            using var uwr = UnityWebRequestTexture.GetTexture(textureData.FilePath, nonReadable: true);
            uwr.timeout = 5;

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                textureData.Error = true;
                DecalTextureAssets.Remove(textureData.FilePath);
                ClearWaitingAfterLoadError(asset);
                LogTexture(LogLevel.Error, "Failed to load from disk", textureName);
                yield break;
            }

            var texture = DownloadHandlerTexture.GetContent(uwr);

            asset.Texture = texture;
            asset.IsLoaded = true;
            ClearWaitingAfterLoadSuccess(asset);

            // we finished loading, but player quickly closed window
            if (asset.InstancesCount == 0)
            {
                DecalTextureAssets.Remove(textureData.FilePath);
                asset.Release();
                LogTexture(LogLevel.Warning, "Finished loading, but no instances", textureName);
            }
            else
            {
                LogTexture(LogLevel.Info, "Finished loading from disk", textureName);
            }
        }

        public void ClearWaitingAfterLoadSuccess(DecalTextureAsset asset)
        {
            foreach (var (waitingDecal, waitingDecalAfterLoad) in asset.WaitingAfterLoad)
            {
                waitingDecalAfterLoad(waitingDecal, asset.Texture);
            }
            asset.WaitingAfterLoad.Clear();
            asset.WaitingAfterLoad = null;
        }

        public void ClearWaitingAfterLoadError(DecalTextureAsset asset)
        {
            foreach (var (waitingDecal, waitingDecalAfterLoad) in asset.WaitingAfterLoad)
            {
                AcquireDecalTextureError(waitingDecal, waitingDecalAfterLoad);
            }
            asset.WaitingAfterLoad.Clear();
            asset.WaitingAfterLoad = null;
        }

        public IEnumerator LoadVideo(Decal decal, string textureName, DecalTextureData textureData, Action<Decal, Texture> afterLoad)
        {
            if (!File.Exists(textureData.FilePath))
            {
                textureData.Error = true;
                AcquireDecalTextureError(decal, afterLoad);
                LogTexture(LogLevel.Error, "Failed to load from disk", textureName);
                yield break;
            }

            var asset = new DecalTextureVideo()
            {
                IsLoaded = false,
                InstancesCount = 1,
                WaitingAfterLoad = new() { { decal, afterLoad } },
                Texture = null,
                VideoPlayer = null,
            };
            DecalTextureAssets.Add(textureData.FilePath, asset);

            var loadTicks = 0;
            var hitError = false;

            bool hitErrorOrTimeout()
            {
                return hitError || loadTicks >= MaxVideoLoadTicks;
            }

            var videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.errorReceived += (_, message) =>
            {
                Logger.LogError(message);
                hitError = true;
            };
            videoPlayer.audioOutputMode = GetVideoAudioOutputMode(PlayVideoAudio.Value);
            videoPlayer.playOnAwake = false;
            videoPlayer.url = textureData.FilePath;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.Prepare();

            while (!videoPlayer.isPrepared && !hitErrorOrTimeout())
            {
                yield return null;
                loadTicks++;
            }

            if (hitErrorOrTimeout())
            {
                textureData.Error = true;
                DecalTextureAssets.Remove(textureData.FilePath);
                ClearWaitingAfterLoadError(asset);
                videoPlayer.Stop();
                Destroy(videoPlayer);
                yield break;
            }

            var renderTexture = new RenderTexture((int)videoPlayer.width, (int)videoPlayer.height, 0);
            renderTexture.wrapMode = TextureWrapMode.Repeat;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.Play();

            asset.Texture = renderTexture;
            asset.VideoPlayer = videoPlayer;
            asset.IsLoaded = true;
            ClearWaitingAfterLoadSuccess(asset);

            // we finished loading, but player quickly closed window
            if (asset.InstancesCount == 0)
            {
                DecalTextureAssets.Remove(textureData.FilePath);
                asset.Release();
                LogTexture(LogLevel.Warning, "Finished loading, but no instances", textureName);
            }
            else
            {
                LogTexture(LogLevel.Info, "Finished loading from disk", textureName);
            }
        }

        public void ChangeAudioOnAllVideos(bool isEnabled)
        {
            var mode = GetVideoAudioOutputMode(isEnabled);
            foreach (var asset in DecalTextureAssets.Values)
            {
                if (asset.IsLoaded && asset is DecalTextureVideo video)
                {
                    video.VideoPlayer.Stop();
                    video.VideoPlayer.audioOutputMode = mode;
                    video.VideoPlayer.Play();
                }
            }
        }

        // TODO potential optimization, weird behaviour tho: videoPlayer.EnableAudioTrack
        public VideoAudioOutputMode GetVideoAudioOutputMode(bool isEnabled)
        {
            return isEnabled ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None;
        }

        public void AcquireDecalTextureError(Decal decal, Action<Decal, Texture> afterLoad)
        {
            afterLoad(decal, ErrorTexture);
        }

        public void ReleaseDecalTextureAsset(Decal decal, string textureName)
        {
            LogTexture(LogLevel.Info, "Decrement", textureName);
            var textureData = GetTextureData(textureName);

            if (textureData.Error)
            {
                return;
            }

            if (DecalTextureAssets.TryGetValue(textureData.FilePath, out var asset))
            {
                asset.InstancesCount--;
                if (asset.IsLoaded)
                {
                    if (asset.InstancesCount <= 0)
                    {
                        DecalTextureAssets.Remove(textureData.FilePath);
                        asset.Release();
                        LogTexture(LogLevel.Info, "Release", textureName);
                    }
                }
                else
                {
                    asset.WaitingAfterLoad.Remove(decal);
                    LogTexture(LogLevel.Info, "Release still loading", textureName);
                }
            }
            else
            {
                LogTexture(LogLevel.Warning, "Tried to unload, but its already unloaded", textureName);
            }
        }

        public void LogPrefab(LogLevel level, string message, string itemId, int instanceID)
        {
            Logger.Log(level, $"[Prefab] {message}: {itemId} {instanceID}");
        }

        public void LogTexture(LogLevel level, string message, string textureName)
        {
#if LOG_TEXTURE
            Logger.Log(level, $"[Texture] {message}: {textureName}");
#endif
        }

        public TexturesDirectory GetTexturesDirectory(DecalTextureType texturesType)
        {
            return texturesType switch
            {
                DecalTextureType.Camo => CamosDirectory,
                DecalTextureType.Sticker => StickersDirectory,
                DecalTextureType.Mask => MasksDirectory,
                _ => throw new ArgumentException(),
            };
        }

        public void ChangeTexture(string itemId, int decalIndex, DecalInfo decalInfo, string textureName)
        {
            var oldTextureName = decalInfo.Texture;
            decalInfo.Texture = textureName;

            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                ReleaseDecalTextureAsset(decal, oldTextureName);
                AcquireDecalTextureAsset(decal, decalInfo.Texture, BeforeLoad_ChangeTexture, AfterLoad_ChangeTexture);
            });
        }

        public void ChangeMask(string itemId, int decalIndex, DecalInfo decalInfo, string maskName)
        {
            var oldMaskName = decalInfo.Mask;
            decalInfo.Mask = maskName;

            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                ReleaseDecalTextureAsset(decal, oldMaskName);
                AcquireDecalTextureAsset(decal, decalInfo.Mask, BeforeLoad_ChangeMask, AfterLoad_ChangeMask);
            });
        }

        public void ApplyColor(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeColor(decalInfo.ColorHSVA);
            });
        }

        public void ApplyMaxAngle(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeMaxAngle(decalInfo.MaxAngle);
            });
        }

        public void ApplyTextureUV(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeTextureUV(decalInfo.TextureUV);
            });
        }

        public void ApplyTextureAngle(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeTextureAngle(decalInfo.TextureAngle);
            });
        }

        public void ApplyMaskUV(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeMaskUV(decalInfo.MaskUV);
            });
        }

        public void ApplyMaskAngle(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeMaskAngle(decalInfo.MaskAngle);
            });
        }

        public void ApplyLocalPosition(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.DecalTransform.localPosition = decalInfo.LocalPosition;
            });
        }

        public void ApplyLocalEulerAngles(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.DecalTransform.localEulerAngles = decalInfo.LocalEulerAngles;
            });
        }

        public void ApplyLocalScale(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeLocalScale(decalInfo.LocalScale);
            });
        }

        public int Duplicate(string itemId, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];
            var decalInfoDuplicate = decalInfo.GetCopy();
            var newDecalIndex = decalIndex + 1;
            itemsWithDecals.DecalsInfo.Insert(newDecalIndex, decalInfoDuplicate);
            foreach (var itemWithDecals in itemsWithDecals.Items.Values)
            {
                var decal = CreateDecal(decalInfo, itemWithDecals.WeaponPrefab);
                itemWithDecals.Decals.Insert(newDecalIndex, decal);
            }

            return newDecalIndex;
        }

        public void Delete(string itemId, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];
            itemsWithDecals.DecalsInfo.RemoveAt(decalIndex);
            foreach (var itemWithDecals in itemsWithDecals.Items.Values)
            {
                var decal = itemWithDecals.Decals[decalIndex];
                itemWithDecals.Decals.RemoveAt(decalIndex);
                DestroyDecal(decal, decalInfo);
            }
        }

        public void SwitchMirrorMode(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.MirrorMode = (DecalMirrorMode)(((int)decalInfo.MirrorMode + 1) % (int)DecalMirrorMode.MODES_COUNT);
        }

        public void SwitchIsVisible(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.IsVisible = !decalInfo.IsVisible;
        }

        public void ApplyTextureAndMaskInfo(string itemId, int decalIndex, DecalInfo decalInfo, DecalInfo fromDecalInfo)
        {
            // TODO dont forget to update this when new texture data fields are added
            // TODO should we fix UV or scale? I dont know

            // match flip
            decalInfo.LocalScale = Vector3.Scale(decalInfo.LocalScale.Abs(), fromDecalInfo.LocalScale.Sign());
            ApplyLocalScale(itemId, decalIndex, decalInfo);

            ChangeTexture(itemId, decalIndex, decalInfo, fromDecalInfo.Texture);

            decalInfo.TextureUV = fromDecalInfo.TextureUV;
            ApplyTextureUV(itemId, decalIndex, decalInfo);

            decalInfo.TextureAngle = fromDecalInfo.TextureAngle;
            ApplyTextureAngle(itemId, decalIndex, decalInfo);

            decalInfo.ColorHSVA = fromDecalInfo.ColorHSVA;
            ApplyColor(itemId, decalIndex, decalInfo);

            ChangeMask(itemId, decalIndex, decalInfo, fromDecalInfo.Mask);

            decalInfo.MaskUV = fromDecalInfo.MaskUV;
            ApplyMaskUV(itemId, decalIndex, decalInfo);

            decalInfo.MaskAngle = fromDecalInfo.MaskAngle;
            ApplyMaskAngle(itemId, decalIndex, decalInfo);

            decalInfo.MaxAngle = fromDecalInfo.MaxAngle;
            ApplyMaxAngle(itemId, decalIndex, decalInfo);

            decalInfo.MirrorMode = fromDecalInfo.MirrorMode;
        }

        public void Swap(string itemId, int decalIndexA, int decalIndexB)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            var decalsInfo = itemsWithDecals.DecalsInfo;

            // TODO make it correctly wrap
            if (decalIndexA < 0 || decalIndexA > decalsInfo.Count - 1 ||
                decalIndexB < 0 || decalIndexB > decalsInfo.Count - 1)
            {
                return;
            }

            (decalsInfo[decalIndexA], decalsInfo[decalIndexB]) = (decalsInfo[decalIndexB], decalsInfo[decalIndexA]);
            foreach (var itemWithDecals in itemsWithDecals.Items.Values)
            {
                var decals = itemWithDecals.Decals;
                (decals[decalIndexA], decals[decalIndexB]) = (decals[decalIndexB], decals[decalIndexA]);
            }
        }

        private const float defaultDecalDepth = 0.04f;
        private const float defaultDecalSize = 0.2f;

        public (Vector3 localPosition, Vector3 localEulerAngles) GetStartPositionAndRotation(Transform weaponPreviewRotator, float previewPivotZ)
        {
            var rotatorY = weaponPreviewRotator.localEulerAngles.y;
            if (Math.Abs(rotatorY - 90) < 10)
            {
                // back
                return (new Vector3(0, -previewPivotZ, 0), new(0, 0, 0));
            }
            if (Math.Abs(rotatorY - 270) < 10)
            {
                // front
                return (new Vector3(0, -previewPivotZ, 0), new(0, 0, 180));
            }
            if (Math.Cos(rotatorY * Mathf.Deg2Rad) > 0)
            {
                // left
                return (new Vector3(-defaultDecalDepth, -previewPivotZ, 0), new(0, 0, 90));
            }
            else
            {
                // right
                return (new Vector3(defaultDecalDepth, -previewPivotZ, 0), new(0, 0, 270));
            }
        }

        public int AddNewDecal(string itemId, int instanceID, WeaponPrefab weaponPrefab, Transform weaponPreviewRotator, float previewPivotZ, Camera weaponPreviewCamera)
        {
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var (startLocalPosition, startLocalEulerAngles) = GetStartPositionAndRotation(weaponPreviewRotator, previewPivotZ);
            var decalInfo = new DecalInfo()
            {
                SchemaVersion = DecalInfo.CurrentSchemaVersion,
                SaveTime = time,
                Name = "",
                Texture = DefaultCamoName,
                TextureUV = new Vector4(0, 0, 1, 1),
                TextureAngle = 0,
                ColorHSVA = new Vector4(0, 0, 1, 1),
                Mask = DefaultMaskName,
                MaskUV = new Vector4(0, 0, 1, 1),
                MaskAngle = 0,
                LocalPosition = startLocalPosition,
                LocalEulerAngles = startLocalEulerAngles,
                LocalScale = new Vector3(defaultDecalSize, defaultDecalDepth, defaultDecalSize),
                MaxAngle = 0.4f,
                IsVisible = true,
                MirrorMode = DecalMirrorMode.Disabled,
            };

            if (ItemsWithDecals.ContainsKey(itemId))
            {
                var itemsWithDecals = ItemsWithDecals[itemId];
                itemsWithDecals.DecalsInfo.Add(decalInfo);
                foreach (var itemWithDecals in itemsWithDecals.Items.Values)
                {
                    var decal = CreateDecal(decalInfo, itemWithDecals.WeaponPrefab);
                    itemWithDecals.Decals.Add(decal);
                }

                return itemsWithDecals.DecalsInfo.Count - 1;
            }
            else
            {
                var decal = CreateDecal(decalInfo, weaponPrefab);
                var decals = new List<Decal>() { decal };
                var decalsInfo = new List<DecalInfo>() { decalInfo };
                var itemsWithDecals = new ItemsWithDecals()
                {
                    Items = new Dictionary<int, ItemWithDecals>()
                    {
                        {
                            instanceID,
                            new ItemWithDecals()
                            {
                                WeaponPrefab = weaponPrefab,
                                Decals = decals,
                            }
                        }
                    },
                    DecalsInfo = decalsInfo
                };

                ItemsWithDecals.Add(itemId, itemsWithDecals);
                WeaponPreviewCameras.Add(weaponPreviewCamera, itemId);

                return 0;
            }
        }

        // mirror around YZ plane
        public void MirrorLeftRight(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            var localRotation = decalInfo.LocalEulerAngles.ToQuaternion();
            MirrorLeftRight(ref decalInfo.LocalPosition, ref localRotation, ref decalInfo.LocalScale);
            decalInfo.LocalEulerAngles = localRotation.eulerAngles;

            ApplyLocalPosition(itemId, decalIndex, decalInfo);
            ApplyLocalEulerAngles(itemId, decalIndex, decalInfo);
            ApplyLocalScale(itemId, decalIndex, decalInfo);
        }

        public static void MirrorLeftRight(ref Vector3 localPosition, ref Quaternion localRotation, ref Vector3 localScale, bool flipHorizontally = true)
        {
            localPosition.ScaleX(-1);
            localRotation.x *= -1;
            localRotation.w *= -1;
            if (flipHorizontally)
            {
    			localScale.ScaleX(-1);
            }
        }

        public void FlipHorizontally(string itemId, int decalIndex, DecalInfo decalInfo)
        {
			FlipHorizontally(decalInfo);
            ApplyLocalScale(itemId, decalIndex, decalInfo);
        }

        public void FlipHorizontally(DecalInfo decalInfo)
        {
			decalInfo.LocalScale.ScaleX(-1);
        }

        public void FlipVertically(string itemId, int decalIndex, DecalInfo decalInfo)
        {
			decalInfo.LocalScale.ScaleZ(-1);
            ApplyLocalScale(itemId, decalIndex, decalInfo);
        }

        public void FlipDirection(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            var offset = Vector3.up * (decalInfo.LocalScale.y * 2 * -1);
            var rotation = decalInfo.LocalEulerAngles.ToQuaternion();
            var offsetRotated = rotation * offset;

            decalInfo.LocalPosition += offsetRotated;
            decalInfo.LocalEulerAngles.z += -180;

            ApplyLocalPosition(itemId, decalIndex, decalInfo);
            ApplyLocalEulerAngles(itemId, decalIndex, decalInfo);
        }

        public void RoundLocalEulerAnglesToDegree(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.LocalEulerAngles.x = MathF.Round(decalInfo.LocalEulerAngles.x);
            decalInfo.LocalEulerAngles.y = MathF.Round(decalInfo.LocalEulerAngles.y);
            decalInfo.LocalEulerAngles.z = MathF.Round(decalInfo.LocalEulerAngles.z);
            ApplyLocalEulerAngles(itemId, decalIndex, decalInfo);
        }

        public void RotateZ(string itemId, int decalIndex, DecalInfo decalInfo, float angle)
        {
            decalInfo.LocalEulerAngles.z += angle;
            ApplyLocalEulerAngles(itemId, decalIndex, decalInfo);
        }

        public void FixScale(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            // we keep decal width the same and change height to match texture aspect ratio
            var textureData = GetTextureData(decalInfo.Texture);
            var uvAspectRatio = decalInfo.TextureUV.z / decalInfo.TextureUV.w;
            var textureAspectRatio = textureData.OriginalSize.AspectRatio();
            var trueTextureAspectRatio = textureAspectRatio * uvAspectRatio;
            var signZ = Math.Sign(decalInfo.LocalScale.z);
            decalInfo.LocalScale.z = signZ * Math.Abs(decalInfo.LocalScale.x) / trueTextureAspectRatio;
            ApplyLocalScale(itemId, decalIndex, decalInfo);
        }

        public void ResetTextureUVOffset(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.TextureUV.x = 0;
            decalInfo.TextureUV.y = 0;
            ApplyTextureUV(itemId, decalIndex, decalInfo);
        }

        public void ResetTextureAngle(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.TextureAngle = 0;
            ApplyTextureAngle(itemId, decalIndex, decalInfo);
        }

        public void ResetTextureUVScale(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.TextureUV.z = 1;
            decalInfo.TextureUV.w = 1;
            ApplyTextureUV(itemId, decalIndex, decalInfo);
        }

        public void FixTextureUV(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            FixTextureUV(decalInfo);
            ApplyTextureUV(itemId, decalIndex, decalInfo);
        }

        public void FixTextureUV(DecalInfo decalInfo)
        {
            // we keep uv height and modify width to match it
            var textureData = GetTextureData(decalInfo.Texture);
            var textureAspectRatio = textureData.OriginalSize.AspectRatio();
            var decalAspectRatio = Math.Abs(decalInfo.LocalScale.x) / Math.Abs(decalInfo.LocalScale.z);
            var k = decalAspectRatio / textureAspectRatio;
            decalInfo.TextureUV.z = decalInfo.TextureUV.w * k;
        }

        public void ResetMaskUVOffset(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.MaskUV.x = 0;
            decalInfo.MaskUV.y = 0;
            ApplyMaskUV(itemId, decalIndex, decalInfo);
        }

        public void ResetMaskAngle(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.MaskAngle = 0;
            ApplyMaskAngle(itemId, decalIndex, decalInfo);
        }

        public void ResetMaskUVScale(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.MaskUV.z = 1;
            decalInfo.MaskUV.w = 1;
            ApplyMaskUV(itemId, decalIndex, decalInfo);
        }

        // notice that we modify decal on all items
        public void ModfiyDecalOnItems(string itemId, int decalIndex, Action<Decal> changeDecal)
        {
            foreach (var itemWithDecals in ItemsWithDecals[itemId].Items.Values)
            {
                var decal = itemWithDecals.Decals[decalIndex];
                changeDecal(decal);
            }
        }

        public void OnWeaponPrefabCreated(string itemId, WeaponPrefab weaponPrefab)
        {
            itemId = GetOriginalItemId(itemId);
            var instanceID = weaponPrefab.GetInstanceID();

            if (ItemsWaitingForRandomCamo.Remove(itemId))
            {
                GenerateRandomCamoForWeapon(itemId, weaponPrefab);
            }

            if (!ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                // true only if fika client,
                // only non fika client and fika host can decide if camo will be spawned,
                // in some cases client can spawn weapon earlier than
                // host sends info about its camo, so save weapon for future
                if (IsFikaSupportEnabled && IsFikaServer.Some(out var isFikaServer) && !isFikaServer)
                {
                    WeaponsWaitingForRemoteCamo.TryAdd(itemId, weaponPrefab);
                    LogPrefab(LogLevel.Info, "Created, cache for possible future camo", itemId, instanceID);
                    return;
                }
                else
                {
                    LogPrefab(LogLevel.Info, "Created, no decals", itemId, instanceID);
                }
                return;
            }

            if (itemsWithDecals.Items.ContainsKey(instanceID))
            {
                LogPrefab(LogLevel.Error, "Created, tried to init multiple times?", itemId, instanceID);
                return;
            }

            var decalsInfo = itemsWithDecals.DecalsInfo;
            var decals = new List<Decal>(decalsInfo.Count);
            foreach (var decalInfo in decalsInfo)
            {
                var decal = CreateDecal(decalInfo, weaponPrefab);
                decals.Add(decal);
            }

            var itemWithDecals = new ItemWithDecals()
            {
                WeaponPrefab = weaponPrefab,
                Decals = decals,
            };

            itemsWithDecals.Items.Add(instanceID, itemWithDecals);

            LogPrefab(LogLevel.Info, "Created, with decals", itemId, instanceID);
        }

		public Decal CreateDecal(DecalInfo decalInfo, WeaponPrefab weaponPrefab)
		{
            var decal = new GameObject("Decal", typeof(Decal)).GetComponent<Decal>();
            var root = GetWeaponRoot(weaponPrefab);
			decal.Init(DecalShader, root, decalInfo);
            AcquireDecalTextureAsset(decal, decalInfo.Texture, BeforeLoad_ChangeTexture, AfterLoad_ChangeTexture);
            AcquireDecalTextureAsset(decal, decalInfo.Mask, BeforeLoad_ChangeMask, AfterLoad_ChangeMask);
			return decal;
		}

        public void BeforeLoad_ChangeTexture(Decal decal)
        {
            // otherwise texture will flash white which is disturbing
            decal.ChangeTexture(Texture2D.blackTexture);
        }

        public void AfterLoad_ChangeTexture(Decal decal, Texture texture)
        {
            decal.ChangeTexture(texture);
        }

        public void BeforeLoad_ChangeMask(Decal decal)
        {
            // otherwise texture will flash white which is disturbing
            decal.ChangeMask(Texture2D.blackTexture);
        }

        public void AfterLoad_ChangeMask(Decal decal, Texture mask)
        {
            decal.ChangeMask(mask);
        }

		public static Transform GetWeaponRoot(WeaponPrefab weaponPrefab)
		{
			return weaponPrefab.Hierarchy.GetTransform(ECharacterWeaponBones.weapon);
		}

        // TODO add ability to select attachment point (ECharacterWeaponBones.weapon or EWeaponModType.mod_magazine)
        // public static Transform GetModTransform(WeaponPrefab weaponPrefab, EWeaponModType modType)
        // {
        //     return TransformHelperClass.FindTransformRecursive(weaponPrefab.Hierarchy.GetTransform(ECharacterWeaponBones.Weapon_root), modType.ToString()).GetChild(0).transform;
        // }

        public void OnWeaponPrefabDestroyed(string itemId, WeaponPrefab weaponPrefab)
        {
            itemId = GetOriginalItemId(itemId);
            if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                var instanceID = weaponPrefab.GetInstanceID();
                var decalsInfo = itemsWithDecals.DecalsInfo;
                if (itemsWithDecals.Items.Remove(instanceID, out var itemWithDecals))
                {
                    LogPrefab(LogLevel.Info, "Destroyed", itemId, instanceID);
                    var decals = itemWithDecals.Decals;
                    for (var i = 0; i < itemWithDecals.Decals.Count; i++)
                    {
                        // WeaponPrefab game object is not necessary destroyed,
                        // it can be returned back to pool and then used later
                        // with different id, so we have to destroy decals manually

                        var decal = decals[i];
                        var decalInfo = decalsInfo[i];
                        DestroyDecal(decal, decalInfo);
                    }
                    decals.Clear();
                }
            }
        }

        public void DestroyDecal(Decal decal, DecalInfo decalInfo)
        {
            ReleaseDecalTextureAsset(decal, decalInfo.Texture);
            ReleaseDecalTextureAsset(decal, decalInfo.Mask);

            if (decal)
            {
                Destroy(decal.gameObject);
            }
        }

        public void OnCloneItem(string originalId, string cloneId)
        {
            // when user tries weapon in hideout shooting range,
            // all his gear gets copied to new items to preserve
            // original durability/ammo count/etc,
            // so we have to clone decals ourselves
            if (ItemsWithDecals.ContainsKey(originalId))
            {
                if (Clones.TryAdd(cloneId, originalId))
                {
                    Logger.LogInfo($"OnCloneItem: original: {originalId}, clone: {cloneId}");
                }
                else
                {
                    Logger.LogWarning($"OnCloneItem: original: {originalId}, clone: {cloneId}, already added???");
                }
            }
        }

        public string GetOriginalItemId(string itemId)
        {
            if (Clones.TryGetValue(itemId, out var originalId))
            {
                return originalId;
            }

            return itemId;
        }

        public void WaitForWeaponPreview()
        {
			IsCamoEditorWaitingForWeaponPreview = true;
        }

        public void OnWeaponPreviewOpened(Camera weaponPreviewCamera, string itemId, WeaponPrefab weaponPrefab, Transform rotator, PreviewPivot previewPivot)
        {
            itemId = GetOriginalItemId(itemId);
			Logger.LogInfo($"OnWeaponPreviewOpened: {itemId}");
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                if (!WeaponPreviewCameras.TryAdd(weaponPreviewCamera, itemId))
                {
        			Logger.LogWarning($"OnWeaponPreviewOpened: {itemId}, already added weapon preview camera?");
                }
            }
			if (IsCamoEditorWaitingForWeaponPreview)
			{
				SetupCamoEditor(weaponPreviewCamera, itemId, weaponPrefab, rotator, previewPivot);
			}
        }

        public void OnWeaponPreviewClosed(Camera weaponPreviewCamera, string itemId)
        {
			Logger.LogInfo($"OnWeaponPreviewClosed: {itemId}");
            WeaponPreviewCameras.Remove(weaponPreviewCamera);
        }

		public void OnPlayerModelViewShown(Camera playerModelViewCamera)
        {
			Logger.LogInfo($"OnPlayerModelViewShown");
            PlayerModelViewCameras.Add(playerModelViewCamera);
        }

		public void OnPlayerModelViewClosed(Camera playerModelViewCamera)
        {
			Logger.LogInfo($"OnPlayerModelViewClosed");
            PlayerModelViewCameras.Remove(playerModelViewCamera);
        }

        public void SetupCamoEditor(Camera editorCamera, string itemId, WeaponPrefab weaponPrefab, Transform rotator, PreviewPivot previewPivot)
        {
            itemId = GetOriginalItemId(itemId);
            Logger.LogInfo($"SetupCamoEditor: {itemId}");
            IsCamoEditorWaitingForWeaponPreview = false;
            var instanceID = weaponPrefab.GetInstanceID();
            var runtimeGizmos = editorCamera.gameObject.AddComponent<RuntimeGizmos>();
            CamoEditor = new(new CamoEditor()
            {
                Plugin = this,
                CamoEditorResources = CamoEditorResources,
                Camera = editorCamera,
                RuntimeGizmos = runtimeGizmos,
                ItemId = itemId,
                InstanceID = instanceID,
                WeaponPrefab = weaponPrefab,
                WeaponPreviewRotator = rotator,
                PreviewPivotZ = previewPivot.pivotPosition.z,
                IsOpened = false,
                IsColorPickerOpened = false,
                CurrentPresetName = "",
                WindowRect = SevenBoldPencil.WeaponCamoAndStickers.CamoEditor.GetDefaultWindowRect()
            });
        }

		public bool CanWeaponPreviewRotate(string itemId)
        {
            if (CamoEditor.Some(out var camoEditor) &&
                camoEditor.ItemId == itemId &&
                camoEditor.CurrentlyEditedDecalIndex.HasValue &&
                camoEditor.TransformHandle)
            {
                // its annoying to tune decal placement
                // while gun is rotating on every mouse
                // movement, so disable rotation
                return !camoEditor.TransformHandle.IsDragging;
            }

            return true;
        }

        public bool CanHideCursor()
        {
            if (CamoEditor.Some(out var camoEditor))
            {
                // game hides cursor and resets it to the center,
                // when player drags in weapon modding screen, which
                // fucks up dragging transform handles and sliders,
                // so keep cursor visible
                return !camoEditor.IsOpened;
            }

            return true;
        }

        public void SwitchToRandomPreset(string itemId, int instanceID, WeaponPrefab weaponPrefab, Camera weaponPreviewCamera)
        {
            if (GenerateRandomCamo(weaponPrefab).Some(out var presetDecalsInfo))
            {
                SwitchToPreset(itemId, instanceID, weaponPrefab, weaponPreviewCamera, presetDecalsInfo);
            }
        }

        public void SwitchToPreset(string itemId, int instanceID, WeaponPrefab weaponPrefab, Camera weaponPreviewCamera, string presetName)
        {
            if (DecalPresets.TryGetValue(presetName, out var presetDecalsInfo))
            {
                SwitchToPreset(itemId, instanceID, weaponPrefab, weaponPreviewCamera, presetDecalsInfo);
            }
        }

        public void SwitchToPreset(string itemId, int instanceID, WeaponPrefab weaponPrefab, Camera weaponPreviewCamera, List<DecalInfo> presetDecalsInfo)
        {
            if (presetDecalsInfo.Count == 0)
            {
                return;
            }
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                var itemsWithDecals = ItemsWithDecals[itemId];

                var decalsInfo = itemsWithDecals.DecalsInfo;
                foreach (var itemWithDecals in itemsWithDecals.Items.Values)
                {
                    var decals = itemWithDecals.Decals;
                    for (var i = 0; i < decals.Count; i++)
                    {
                        var decal = decals[i];
                        var decalInfo = decalsInfo[i];
                        DestroyDecal(decal, decalInfo);
                    }
                    decals.Clear();
                }

                decalsInfo.Clear();
                CopyDecalsInfo(presetDecalsInfo, decalsInfo);

                foreach (var itemWithDecals in itemsWithDecals.Items.Values)
                {
                    var decals = itemWithDecals.Decals;
                    foreach (var decalInfo in decalsInfo)
                    {
                        var decal = CreateDecal(decalInfo, itemWithDecals.WeaponPrefab);
                        decals.Add(decal);
                    }
                }
            }
            else
            {
                var decalsInfo = CopyDecalsInfo(presetDecalsInfo);
                var decals = new List<Decal>(decalsInfo.Count);
                foreach (var decalInfo in decalsInfo)
                {
                    var decal = CreateDecal(decalInfo, weaponPrefab);
                    decals.Add(decal);
                }

                var itemsWithDecals = new ItemsWithDecals()
                {
                    Items = new Dictionary<int, ItemWithDecals>()
                    {
                        {
                            instanceID,
                            new ItemWithDecals()
                            {
                                WeaponPrefab = weaponPrefab,
                                Decals = decals,
                            }
                        }
                    },
                    DecalsInfo = decalsInfo
                };

                ItemsWithDecals.Add(itemId, itemsWithDecals);
                WeaponPreviewCameras.Add(weaponPreviewCamera, itemId);
            }
        }

        public void SaveDecalsIntoPreset(string itemId, string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                return;
            }
            if (!GetDecalsInfo(itemId).Some(out var decalsInfo))
            {
                return;
            }
            if (DecalPresets.TryGetValue(presetName, out var oldPresetDecalsInfo))
            {
                oldPresetDecalsInfo.Clear();
                CopyDecalsInfo(decalsInfo, oldPresetDecalsInfo);
                WritePresetToFile(presetName, oldPresetDecalsInfo);
            }
            else
            {
                var newPresetDecalsInfo = CopyDecalsInfo(decalsInfo);
                DecalPresets.Add(presetName, newPresetDecalsInfo);
                WritePresetToFile(presetName, newPresetDecalsInfo);
            }
        }

        public void CopyDecalsInfo(List<DecalInfo> source, List<DecalInfo> destination)
        {
            foreach (var decalInfo in source)
            {
                destination.Add(decalInfo.GetCopy());
            }
        }

        public List<DecalInfo> CopyDecalsInfo(List<DecalInfo> source)
        {
            var destination = new List<DecalInfo>(source.Count);
            CopyDecalsInfo(source, destination);
            return destination;
        }

        public void WritePresetToFile(string presetName, List<DecalInfo> preset)
        {
            var json = JsonConvert.SerializeObject(preset, Formatting.Indented);
            var filePath = GetPresetFilePath(presetName);
            SafeIO.WriteAllTextAsync(filePath, json);
        }

        public void DeletePreset(string presetName)
        {
            if (DecalPresets.Remove(presetName))
            {
                var filePath = GetPresetFilePath(presetName);
                SafeIO.DeleteFile(filePath);
            }
        }

        public string GetPresetFilePath(string presetName)
        {
            var fileName = $"{presetName}.json";
            var filePath = Path.Combine(PresetsDir, fileName);
            return filePath;
        }

        public void CloseCamoEditor()
        {
            IsCamoEditorWaitingForWeaponPreview = false;

			// CloseCamoEditor method can be called
			// even when editor is not intialized, this happens in cases:
			// 1) user can quickly tap Modify and hit Escape,
			// which means weapon preview won't be fully loaded,
			// 2) WeaponModdingScreen.Close is called even if user
			// entered customization window on trader guns

            if (!CamoEditor.Some(out var camoEditor))
            {
                Logger.LogWarning($"CloseCamoEditor: tried to close uninitialized decal editor");
                return;
            }

            camoEditor.ForceOnEndedDraggingHandle();

            var itemId = camoEditor.ItemId;
            if (GetDecalsInfo(itemId).Some(out var decalsInfo))
            {
                if (decalsInfo.Count == 0)
                {
                    ItemsWithDecals.Remove(itemId);
                    RemoveDecalsFile(itemId);
                    Logger.LogInfo($"CloseCamoEditor: {itemId} remove decals");
                }
                else
                {
                    var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    foreach (var decalInfo in decalsInfo)
                    {
                        decalInfo.SaveTime = time;
                    }

                    WriteDecalsToFile(itemId, decalsInfo);
                    Logger.LogInfo($"CloseCamoEditor: {itemId} rewrite decals");
                }
            }

            SaveClosedTexturesDirectoriesToDisk(ClosedDirectories, ClosedDirectoriesPath);

            camoEditor.Destroy();
            CamoEditor = default;
        }

        public void WriteDecalsToFile(string itemId, List<DecalInfo> decalsInfo)
        {
            var json = JsonConvert.SerializeObject(decalsInfo, Formatting.Indented);
            var filePath = GetItemFilePath(itemId);
            SafeIO.WriteAllTextAsync(filePath, json);
        }

        public void RemoveDecalsFile(string itemId)
        {
            var filePath = GetItemFilePath(itemId);
            SafeIO.DeleteFile(filePath);
        }

        public string GetItemFilePath(string itemId)
        {
            var fileName = $"{itemId}.json";
            var filePath = Path.Combine(ItemsDir, fileName);
            return filePath;
        }

        public Dictionary<string, List<DecalInfo>> SnapshotLocalDecals()
        {
            var snapshot = new Dictionary<string, List<DecalInfo>>();
    		if (!TarkovApplication.Exist(out var tarkovApplication))
            {
                return snapshot;
            }

            // copies all guns inside player equipment (on hands/sling/holster, inside backpack, rig, etc)
            var profile = tarkovApplication.Session.Profile;
            var equipmentItems = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment);

            foreach (var item in equipmentItems)
            {
                if (item is Weapon && ItemsWithDecals.TryGetValue(item.Id, out var itemsWithDecals))
                {
                    snapshot[item.Id] = CopyDecalsInfo(itemsWithDecals.DecalsInfo);
                }
            }

            return snapshot;
        }

        public void IngestRemoteDecals(Dictionary<string, List<DecalInfo>> remoteDecals)
        {
            foreach (var (itemId, decalsInfo) in remoteDecals)
            {
                IngestRemoteDecals(itemId, decalsInfo);
            }
        }

        public void IngestRemoteDecals(string itemId, List<DecalInfo> remoteDecalsInfo)
        {
            if (remoteDecalsInfo.Count == 0)
            {
                Logger.LogWarning($"IngestRemoteDecals: {itemId} has no decals, but was replicated?");
                return;
            }

            // TODO not sure if copying remoteDecalsInfo is necessary
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                // pick newer version
                var itemsWithDecals = ItemsWithDecals[itemId];
                var decalsInfo = itemsWithDecals.DecalsInfo;
                if (decalsInfo[0].SaveTime >= remoteDecalsInfo[0].SaveTime)
                {
                    Logger.LogInfo($"IngestRemoteDecals: {itemId}, mine is newer");
                    return;
                }

                decalsInfo.Clear();
                CopyDecalsInfo(remoteDecalsInfo, decalsInfo);
                WriteDecalsToFile(itemId, decalsInfo);
                Logger.LogInfo($"IngestRemoteDecals: {itemId}, his is newer, already spawned count: {itemsWithDecals.Items.Count}");
            }
            else
            {
                Logger.LogInfo($"IngestRemoteDecals: {itemId}, new");
                var decalsInfo = CopyDecalsInfo(remoteDecalsInfo);
                var itemsWithDecals = new ItemsWithDecals()
                {
                    Items = new(),
                    DecalsInfo = decalsInfo,
                };

                ItemsWithDecals.Add(itemId, itemsWithDecals);
                WriteDecalsToFile(itemId, decalsInfo);
            }

            if (WeaponsWaitingForRemoteCamo.Remove(itemId, out var weaponPrefab))
            {
                Logger.LogWarning($"WeaponsWaitingForRemoteCamo: {itemId}");
                OnWeaponPrefabCreated(itemId, weaponPrefab);
            }
        }

        public float GetCamoSpawnChanceFromBotRole(WildSpawnType botRole)
        {
            switch (botRole)
            {
                case WildSpawnType.bossKnight:
                case WildSpawnType.followerBigPipe:
                case WildSpawnType.followerBirdEye:
                    return GoonsWeaponCamoSpawnChance.Value;
                case WildSpawnType.pmcBEAR:
                case WildSpawnType.pmcUSEC:
                    return PMCWeaponCamoSpawnChance.Value;
            }

			if (BotSettingsRepoClass.IsBossOrFollower(botRole))
			{
                return OtherBossesWeaponCamoSpawnChance.Value;
			}

            // TODO we can give scavs dirty/rusty camos
            return 0;
        }

        public void QueueItemForRandomCamoGeneration(string itemId, float spawnChance)
        {
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                Logger.LogWarning($"Tried to generate camo for item that already has one: {itemId}");
                return;
            }

            // to generate adequate camo we need to know
            // dimensions of a weapon, the only reasonable
            // way to do it is WeaponPreview.GetBounds,
            // which requires spawned GameObject, so
            // we have to wait until weapon is constructed

            void queueItemForCamoGeneraion(string itemId)
            {
    			if (UnityEngine.Random.value <= spawnChance / 100f)
    			{
                    ItemsWaitingForRandomCamo.Add(itemId);
                    Logger.LogWarning($"Queue item for random camo: {itemId}");
    			}
            }

            if (IsFikaSupportEnabled)
            {
                if (IsFikaServer.Some(out var isFikaServer) && isFikaServer)
                {
                    queueItemForCamoGeneraion(itemId);
                }
            }
            else
            {
                queueItemForCamoGeneraion(itemId);
            }
        }

        public void GenerateRandomCamoForWeapon(string itemId, WeaponPrefab weaponPrefab)
        {
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                Logger.LogWarning($"Tried to generate camo for item that already has camo: {itemId}");
                return;
            }

            // this method gets called only on non fika clients or fika host, so we can omit checks
            if (!GenerateRandomCamo(weaponPrefab).Some(out var decalsInfo))
            {
                Logger.LogWarning($"Generated empty camo: {itemId}");
                return;
            }
            var itemsWithDecals = new ItemsWithDecals()
            {
                Items = new(),
                DecalsInfo = decalsInfo,
            };

            if (IsFikaSupportEnabled)
            {
                Logger.LogWarning($"Generate camo: {itemId}, fika host");
                OnBotWeaponCamoGenerated?.Invoke(new() {{ itemId, decalsInfo }});
                if (!IsFikaHeadless)
                {
                    ItemsWithDecals.Add(itemId, itemsWithDecals);
                    WriteDecalsToFile(itemId, decalsInfo);
                }
            }
            else
            {
                Logger.LogWarning($"Generate camo: {itemId}, no fika");
                ItemsWithDecals.Add(itemId, itemsWithDecals);
                WriteDecalsToFile(itemId, decalsInfo);
            }
        }

        public Option<List<DecalInfo>> GenerateRandomCamo(WeaponPrefab weaponPrefab)
        {
            if (!weaponPrefab.TryGetComponent<PreviewPivot>(out var previewPivot))
            {
                return default;
            }
            if (!GetRandomTexture(Camos).Some(out var camo))
            {
                return default;
            }
            if (!GetRandomTexture(Masks).Some(out var mask))
            {
                return default;
            }

            var (weaponCenter, weaponSize) = GetWeaponBounds(weaponPrefab);
            var positionY = weaponCenter.x;
            var positionZ = weaponCenter.y / 2f; // I have no idea why center.y gets multiplied by 2
            var size = new Vector3(weaponSize.x, defaultDecalDepth, weaponSize.y);
            var opacity = UnityEngine.Random.Range(0.6f, 1f);
            var left = GenerateRandomDecal(new(-defaultDecalDepth, positionY, positionZ), new(0, 0, 90), size, camo, mask, opacity);
            left.MirrorMode = DecalMirrorMode.Enabled;
            FixTextureUV(left);

            // TODO generate a couple of stickers

            return new([left]);
        }

        public (Vector3 center, Vector3 scale) GetWeaponBounds(WeaponPrefab weaponPrefab)
        {
            var weaponTransform = weaponPrefab.transform;
            var originalPosition = weaponTransform.position;
            var originalRotation = weaponTransform.rotation;

            weaponTransform.position = Vector3.zero;
            weaponTransform.eulerAngles = new(0, 270, 0);

            var weaponRoot = GetWeaponRoot(weaponPrefab);
            var weaponBounds = WeaponPreview.GetBounds(weaponRoot.gameObject);
            var weaponScale = weaponTransform.lossyScale.x; // scale is uniform, right?
            var center = weaponBounds.center / weaponScale;
            var size = weaponBounds.size / weaponScale;

            weaponTransform.position = originalPosition;
            weaponTransform.rotation = originalRotation;

            return (center, size);
        }

        private static readonly System.Random random = new();
        public Option<string> GetRandomTexture(string[] array)
        {
            // never give builtin pure white texture
            if (array.Length == 1)
            {
                return default;
            }

            var randomIndex = random.Next(array.Length - 1);
            var randomElement = array[randomIndex];
            return new(randomElement);
        }

        public DecalInfo GenerateRandomDecal(
            Vector3 startLocalPosition,
            Vector3 startLocalEulerAngles,
            Vector3 startLocalScale,
            string textureName,
            string maskName,
            float opacity)
        {
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return new DecalInfo()
            {
                SchemaVersion = DecalInfo.CurrentSchemaVersion,
                SaveTime = time,
                Name = "",
                Texture = textureName,
                TextureUV = new Vector4(0, 0, 1, 1),
                TextureAngle = 0,
                ColorHSVA = new Vector4(0, 0, 1, opacity),
                Mask = maskName,
                MaskUV = new Vector4(0, 0, 1, 1),
                MaskAngle = 0,
                LocalPosition = startLocalPosition,
                LocalEulerAngles = startLocalEulerAngles,
                LocalScale = startLocalScale,
                MaxAngle = 0.4f,
                IsVisible = true,
                MirrorMode = DecalMirrorMode.Disabled,
            };
        }
    }
}
