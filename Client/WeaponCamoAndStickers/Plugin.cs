//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using Diz.Skinning;
using EFT;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using EFT.Visual;
using EFT.UI.WeaponModding;
using Newtonsoft.Json;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using SystemObject = System.Object;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
    public readonly record struct ItemData(string Id, ItemType Type);

    public readonly record struct DressData(string Id, int InstanceID);

    public enum ItemType
    {
        Weapon,
        Knife,
        Headwear,
        FaceCover,
        Container,
        Armor,
        Vest,
        Backpack,
    }

    public interface IDecalsHost
    {
        public Transform GetDecalRoot(string bone);
    }

    public class SimpleDecalsHost : IDecalsHost
    {
        public readonly Transform DecalsRoot;

        public SimpleDecalsHost(Transform decalsRoot)
        {
            DecalsRoot = decalsRoot;
        }

        public Transform GetDecalRoot(string bone)
        {
            return DecalsRoot;
        }
    }

    public class SkinnedDecalsHost : IDecalsHost
    {
        public readonly Transform DefaultDecalsRoot;
        public readonly Skeleton Skeleton;

        public SkinnedDecalsHost(Transform defaultDecalsRoot, Skeleton skeleton)
        {
            DefaultDecalsRoot = defaultDecalsRoot;
            Skeleton = skeleton;
        }

        public Transform GetDecalRoot(string bone)
        {
		    if (string.IsNullOrWhiteSpace(bone))
            {
                return DefaultDecalsRoot;
            }
			if (Skeleton.Bones.TryGetValue(bone, out var boneTransform))
            {
                return boneTransform;
            }

            return DefaultDecalsRoot;
        }
    }

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
        public IDecalsHost DecalsHost;
        public List<Decal> Decals;
    }

    public class DecalInfo
    {
        public const int CurrentSchemaVersion = 9;

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
        public string Bone;
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale;
        public float MaxAngle;
        public bool IsVisible;
        public DecalMirrorMode MirrorMode;
        public DecalPaintMode PaintMode;
        public byte StencilType;

        // this is enough for now
        public DecalInfo GetCopy() => (DecalInfo)MemberwiseClone();
    }

    public enum DecalPaintMode : byte
    {
        Paint,
        Erase,
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
        public Dictionary<SystemObject, Action<SystemObject, Texture>> WaitingAfterLoad;
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

    [BepInPlugin("7Bpencil.WeaponCamoAndStickers", "7Bpencil.WeaponCamoAndStickers", "1.18.0")]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
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
        public static ConfigEntry<int> ScavsWeaponCamoSpawnChance;

		public ManualLogSource LoggerInstance;

        private string TexturesDir;
        private string PreviewsDir;
        private string ItemsDir;
        private string PresetsDir;
        private string BotPresetsDir;
        private string ClosedDirectoriesPath;
        private string FavouriteTexturesPath;
        private Shader DecalShader;
        private Shader CombineTexturesShader; // used by MaterialEditor
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
        private HashSet<string> FavouriteTextures;

        private Dictionary<string, List<DecalInfo>> DecalPresets;
        private Dictionary<int, Dictionary<string, List<List<DecalInfo>>>> BotDecalPresets;
        private Dictionary<string, ItemsWithDecals> ItemsWithDecals;
        private Dictionary<string, string> Clones;
        private Dictionary<ResourceKey, ItemData> ResourceKeyToItem;
        private Dictionary<int, string> InstanceIdToItemId;
        private Dictionary<int, string> DressesWaitingToBeSkinned;
        // private HashSet<string> WeaponsWaitingForRandomCamo;
        private Dictionary<Camera, HashSet<string>> DecalCameras;

        private DecalRenderer DecalRenderer;
        private Option<CamoEditor> CamoEditor;
        private Option<CamoEditorError> CamoEditorError;
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
            ScavsWeaponCamoSpawnChance = Config.Bind<int>("Main", "Raid | Camo Spawn Chance | Scavs", 0, new ConfigDescription("", new AcceptableValueRange<int>(0, 100)));

            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            TexturesDir = Path.Combine(assemblyDir, "textures");
            PreviewsDir = Path.Combine(assemblyDir, "temp", "previews");
            ItemsDir = Path.Combine(assemblyDir, "items");
            PresetsDir = Path.Combine(assemblyDir, "presets");
            BotPresetsDir = Path.Combine(assemblyDir, "bot-presets");
            ClosedDirectoriesPath = Path.Combine(assemblyDir, "temp", "closed-directories.json");
            FavouriteTexturesPath = Path.Combine(assemblyDir, "temp", "favourite-textures.json");

			var bundlePath = Path.Combine(assemblyDir, "bundles", "weapon-camo-and-stickers");
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            DecalShader = bundle.LoadAsset<Shader>("Assets/WeaponCamoAndStickers/Shaders/DecalDynamic.shader");
            CombineTexturesShader = bundle.LoadAsset<Shader>("Assets/WeaponCamoAndStickers/Shaders/CombineTextures.shader");
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
            bundle.UnloadAsync(false);

            DecalTextures = new();
            DecalTextureAssets = new();

            ClosedDirectories = LoadClosedTexturesDirectories();
            (CamosDirectory, Camos) = LoadTexturesFromDirectory(DecalTextureType.Camo, "camos", ClosedDirectories.CamosDirectory, DefaultCamoName, DecalTextureFormat.PNG, Texture2D.whiteTexture);
            (StickersDirectory, Stickers) = LoadTexturesFromDirectory(DecalTextureType.Sticker, "stickers", ClosedDirectories.StickersDirectory, DefaultStickerName, DecalTextureFormat.PNG, Texture2D.whiteTexture);
            (MasksDirectory, Masks) = LoadTexturesFromDirectory(DecalTextureType.Mask, "masks", ClosedDirectories.MasksDirectory, DefaultMaskName, DecalTextureFormat.PNG, Texture2D.whiteTexture);
            FavouriteTextures = LoadFavouriteTextures();

            DecalPresets = LoadDecalPresets();
            BotDecalPresets = LoadBotDecalPresets();
            ItemsWithDecals = LoadItemsWithDecals();
            Clones = new();
            ResourceKeyToItem = new();
            InstanceIdToItemId = new();
            DressesWaitingToBeSkinned = new();
            // WeaponsWaitingForRandomCamo = new();
            DecalCameras = new();

            DecalRenderer = new(ItemsWithDecals, InstanceIdToItemId, DecalCameras);

            WeaponsWaitingForRemoteCamo = new();

            new Patch_WeaponModdingScreen_CreateModSlotViews().Enable();
            new Patch_WeaponPreview_CG_SetupItemPreview_method_1().Enable();
            new Patch_WeaponPreview_Rotate().Enable();
            new Patch_ScrollTrigger_OnScroll().Enable();
            new Patch_WeaponPreview_Hide().Enable();
            new Patch_ItemUiContext_GetItemContextInteractions().Enable();
            new Patch_InteractionButtonsContainer_CreateDynamicContextButton().Enable();
            new Patch_WeaponModdingScreen_Close().Enable();
            new Patch_ObjectsFactory_CreateItemAsync().Enable();
            new Patch_ObjectsFactory_PopOrCreate().Enable();
            new Patch_WeaponPrefab_InitHotObjects().Enable();
            new Patch_PlayerBody_SlotView_CreateAndParent().Enable();
            new Patch_AssetPoolObject_ReturnToPool().Enable();
            new Patch_AssetPoolObject_OnDestroy().Enable();
            new Patch_PlayerBody_SetSkin_CreateItem().Enable();
            new Patch_LoddedSkin_Unskin().Enable();
            new Patch_ItemExtensions_CloneItemInternal().Enable();
            new Patch_ClientApplicationInitOperation_CursorVisibilityChangedHandler().Enable();
            new Patch_PlayerModelView_OnLoadingCompleted().Enable();
            new Patch_PlayerModelView_Destroy().Enable();
            new Patch_PlayerBody_SetSkin().Enable();
            new Patch_BotCreatorClient_CreateBot().Enable();
            new Patch_LocalPlayer_Create().Enable();
            new Patch_ItemIconCreator_GetItemIcon().Enable();
            new Patch_IconsHash_GetItemHash().Enable();

            TryEnableFikaSupport(assemblyDir);

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
			// instancing: not really possible in Unity on D3D11 (no bindless textures)

			// TODO
			// add tooltips on all UI elements
        }

        public void TryEnableFikaSupport(string mainAssemblyDir)
        {
            if (!Chainloader.PluginInfos.ContainsKey("com.fika.core"))
            {
                return;
            }

            var fikaAssemblyPath = Path.Combine(mainAssemblyDir, "7Bpencil.WeaponCamoAndStickers.Fika.dll");
            if (!File.Exists(fikaAssemblyPath))
            {
                return;
            }

            var fikaAssembly = Assembly.LoadFrom(fikaAssemblyPath);
            var fikaPluginType = fikaAssembly.GetType("SevenBoldPencil.WeaponCamoAndStickers.Fika.Plugin");
            var fikaPluginAwake = fikaPluginType.GetMethod("Awake");
            var fikaPlugin = Activator.CreateInstance(fikaPluginType);
            fikaPluginAwake.Invoke(fikaPlugin, null);
        }

        public ClosedTexturesDirectories LoadClosedTexturesDirectories()
        {
            if (SafeIO.ReadAllText(ClosedDirectoriesPath).Ok(out var json, out var e))
            {
                var result = JsonConvert.DeserializeObject<ClosedTexturesDirectories>(json);
                return result;
            }
            else
            {
                Logger.Log(LogLevel.Error, "ClosedDirectories", "Failed to load from disk, rolling back to default", e);
            }

            return new ClosedTexturesDirectories()
            {
                CamosDirectory = new(),
                StickersDirectory = new(),
                MasksDirectory = new(),
            };
        }

        public void SaveClosedTexturesDirectoriesToDisk()
        {
            ClosedDirectories.CamosDirectory.Clear();
            ClosedDirectories.StickersDirectory.Clear();
            ClosedDirectories.MasksDirectory.Clear();

            SaveClosedTexturesDirectories(CamosDirectory, ClosedDirectories.CamosDirectory);
            SaveClosedTexturesDirectories(StickersDirectory, ClosedDirectories.StickersDirectory);
            SaveClosedTexturesDirectories(MasksDirectory, ClosedDirectories.MasksDirectory);

            var json = JsonConvert.SerializeObject(ClosedDirectories, Formatting.Indented);
            SafeIO.WriteAllTextAsync(ClosedDirectoriesPath, json);
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
                Logger.Log(LogLevel.Error, "Texture", "Failed to save preview", previewFilePath, e);
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
                Logger.Log(LogLevel.Error, "Texture", "Video error", message);
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
            Logger.Log(LogLevel.Error, "Texture", "Failed to load texture", param.Name);
        }

        public HashSet<string> LoadFavouriteTextures()
        {
            if (SafeIO.ReadAllText(FavouriteTexturesPath).Ok(out var json, out var e))
            {
                var result = JsonConvert.DeserializeObject<HashSet<string>>(json);
                return result;
            }
            else
            {
                Logger.Log(LogLevel.Error, "FavouriteTextures", "Failed to load from disk, rolling back to default", e);
            }

            return new();
        }

        public void SaveFavouriteTexturesToDisk()
        {
            var json = JsonConvert.SerializeObject(FavouriteTextures, Formatting.Indented);
            SafeIO.WriteAllTextAsync(FavouriteTexturesPath, json);
        }

        public Dictionary<string, List<DecalInfo>> LoadDecalPresets()
        {
            var filePaths = SafeIO.GetFiles(PresetsDir, "*.json");
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
                    Logger.Log(LogLevel.Error, "Preset", "Failed to load from disk", presetName, e);
                }
            }

            return result;
        }

        public Dictionary<int, Dictionary<string, List<List<DecalInfo>>>> LoadBotDecalPresets()
        {
            var botDirectoryPaths = SafeIO.GetDirectories(BotPresetsDir);
            var bots = new Dictionary<int, Dictionary<string, List<List<DecalInfo>>>>(botDirectoryPaths.Length);
            foreach (var botDirectoryPath in botDirectoryPaths)
            {
                var botDirectoryName = Path.GetFileName(botDirectoryPath);

                Logger.LogError(botDirectoryPath);
                Logger.LogError(botDirectoryName);

                if (!int.TryParse(botDirectoryName, out var botType))
                {
                    continue;
                }

                var itemDirectoryPaths = SafeIO.GetDirectories(botDirectoryPath);
                var items = new Dictionary<string, List<List<DecalInfo>>>(itemDirectoryPaths.Length);

                foreach (var itemDirectoryPath in itemDirectoryPaths)
                {
                    var itemTemplateId = Path.GetFileName(itemDirectoryPath);
                    Logger.LogError(itemTemplateId);

                    var presetsPath = Path.Combine(itemDirectoryPath, "presets");
                    var presetPaths = SafeIO.GetFiles(presetsPath, "*.json");
                    var presets = new List<List<DecalInfo>>(presetPaths.Length);

                    foreach (var presetPath in presetPaths)
                    {
                        Logger.LogError(presetPath);
                        if (SafeIO.ReadAllText(presetPath).Ok(out var json, out var e))
                        {
                            var decalsInfo = JsonConvert.DeserializeObject<List<DecalInfo>>(json);
                            UpgradeOldVersionsOfDecalsInfo(decalsInfo);
                            presets.Add(decalsInfo);
                            Logger.LogError("added");
                        }
                        else
                        {
                            Logger.Log(LogLevel.Error, "BotPreset", "Failed to load from disk", presetPath, e);
                        }
                    }

                    if (presets.Count > 0)
                    {
                        items.Add(itemTemplateId, presets);
                    }
                }

                if (items.Count > 0)
                {
                    bots.Add(botType, items);
                }
            }

            return bots;
        }

        public Dictionary<string, ItemsWithDecals> LoadItemsWithDecals()
        {
            var filePaths = SafeIO.GetFiles(ItemsDir, "*.json");
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
                    Logger.Log(LogLevel.Error, "Item", "Failed to load from disk", itemId, e);
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
                if (decalInfo.SchemaVersion == 6)
                {
                    decalInfo.SchemaVersion = 7;
                    decalInfo.PaintMode = DecalPaintMode.Paint;
                }
                if (decalInfo.SchemaVersion == 7)
                {
                    decalInfo.SchemaVersion = 8;
                    decalInfo.StencilType = 2;
                }
                if (decalInfo.SchemaVersion == 8)
                {
                    decalInfo.SchemaVersion = 9;
                    decalInfo.Bone = "";
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
            if (CamoEditor.Some(out var camoEditor))
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
            if (CamoEditorError.Some(out var camoEditorError))
            {
                camoEditorError.DrawWindow();
            }
        }

        // TODO this sometimes panics, no idea why
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

        public void AcquireDecalTextureAsset(SystemObject key, string textureName, Action<SystemObject, Texture> beforeLoad, Action<SystemObject, Texture> afterLoad)
        {
            Logger.Log(LogLevel.Info, "Texture", "Increment", textureName);

            var textureData = GetTextureData(textureName);

            // set low res version immediately, otherwise texture will flash white which is disturbing
            beforeLoad(key, textureData.Preview);

            if (textureData.Error)
            {
                AcquireDecalTextureError(key, afterLoad);
                return;
            }

            if (DecalTextureAssets.TryGetValue(textureData.FilePath, out var asset))
            {
                asset.InstancesCount++;
                if (asset.IsLoaded)
                {
                    afterLoad(key, asset.Texture);
                    Logger.Log(LogLevel.Info, "Texture", "Load from cache", textureName);
                }
                else
                {
                    asset.WaitingAfterLoad.Add(key, afterLoad);
                    Logger.Log(LogLevel.Info, "Texture", "Already loading", textureName);
                }
            }
            else
            {
                Logger.Log(LogLevel.Info, "Texture", "Start loading from disk", textureName);
                if (textureData.Format == DecalTextureFormat.PNG)
                {
                    StartCoroutine(LoadPNG(key, textureName, textureData, afterLoad));
                }
                if (textureData.Format == DecalTextureFormat.Video)
                {
                    StartCoroutine(LoadVideo(key, textureName, textureData, afterLoad));
                }
            }
        }

        public IEnumerator LoadPNG(SystemObject key, string textureName, DecalTextureData textureData, Action<SystemObject, Texture> afterLoad)
        {
            if (!File.Exists(textureData.FilePath))
            {
                textureData.Error = true;
                AcquireDecalTextureError(key, afterLoad);
                Logger.Log(LogLevel.Error, "Texture", "Failed to load from disk", textureName);
                yield break;
            }

            var asset = new DecalTexturePNG()
            {
                IsLoaded = false,
                InstancesCount = 1,
                WaitingAfterLoad = new() { { key, afterLoad } },
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
                Logger.Log(LogLevel.Error, "Texture", "Failed to load from disk", textureName);
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
                Logger.Log(LogLevel.Warning, "Texture", "Finished loading, but no instances", textureName);
            }
            else
            {
                Logger.Log(LogLevel.Info, "Texture", "Finished loading from disk", textureName);
            }
        }

        public void ClearWaitingAfterLoadSuccess(DecalTextureAsset asset)
        {
            foreach (var (waitingKey, waitingDecalAfterLoad) in asset.WaitingAfterLoad)
            {
                waitingDecalAfterLoad(waitingKey, asset.Texture);
            }
            asset.WaitingAfterLoad.Clear();
            asset.WaitingAfterLoad = null;
        }

        public void ClearWaitingAfterLoadError(DecalTextureAsset asset)
        {
            foreach (var (waitingKey, waitingDecalAfterLoad) in asset.WaitingAfterLoad)
            {
                AcquireDecalTextureError(waitingKey, waitingDecalAfterLoad);
            }
            asset.WaitingAfterLoad.Clear();
            asset.WaitingAfterLoad = null;
        }

        public IEnumerator LoadVideo(SystemObject key, string textureName, DecalTextureData textureData, Action<SystemObject, Texture> afterLoad)
        {
            if (!File.Exists(textureData.FilePath))
            {
                textureData.Error = true;
                AcquireDecalTextureError(key, afterLoad);
                Logger.Log(LogLevel.Error, "Texture", "Failed to load from disk", textureName);
                yield break;
            }

            var asset = new DecalTextureVideo()
            {
                IsLoaded = false,
                InstancesCount = 1,
                WaitingAfterLoad = new() { { key, afterLoad } },
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
                Logger.Log(LogLevel.Error, "Texture", "Video error", message);
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
                Logger.Log(LogLevel.Warning, "Texture", "Finished loading, but no instances", textureName);
            }
            else
            {
                Logger.Log(LogLevel.Info, "Texture", "Finished loading from disk", textureName);
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

        public void AcquireDecalTextureError(SystemObject key, Action<SystemObject, Texture> afterLoad)
        {
            afterLoad(key, ErrorTexture);
        }

        public void ReleaseDecalTextureAsset(SystemObject key, string textureName)
        {
            Logger.Log(LogLevel.Info, "Texture", "Decrement", textureName);
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
                        Logger.Log(LogLevel.Info, "Texture", "Release", textureName);
                    }
                }
                else
                {
                    asset.WaitingAfterLoad.Remove(key);
                    Logger.Log(LogLevel.Info, "Texture", "Release still loading", textureName);
                }
            }
            else
            {
                Logger.Log(LogLevel.Warning, "Texture", "Tried to unload, but its already unloaded", textureName);
            }
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
            ApplyTexture(itemId, decalIndex, oldTextureName);
        }

        public void ApplyTexture(string itemId, int decalIndex, string oldTextureName)
        {
            ModifyDecalOnItems(itemId, decalIndex, (decal, decalInfo) =>
            {
                ReleaseDecalTextureAsset(decal, oldTextureName);
                AcquireDecalTextureAsset(decal, decalInfo.Texture, DecalChangeTexture, DecalChangeTexture);
            });
        }

        public void ChangeMask(string itemId, int decalIndex, DecalInfo decalInfo, string maskName)
        {
            var oldMaskName = decalInfo.Mask;
            decalInfo.Mask = maskName;
            ApplyMask(itemId, decalIndex, oldMaskName);
        }

        public void ApplyMask(string itemId, int decalIndex, string oldMaskName)
        {
            ModifyDecalOnItems(itemId, decalIndex, (decal, decalInfo) =>
            {
                ReleaseDecalTextureAsset(decal, oldMaskName);
                AcquireDecalTextureAsset(decal, decalInfo.Mask, DecalChangeMask, DecalChangeMask);
            });
        }

        public void ApplyColor(string itemId, int decalIndex)
        {
            ModifyDecalOnItems(itemId, decalIndex, static (decal, decalInfo) =>
            {
                decal.ChangeColor(decalInfo.ColorHSVA);
            });
        }

        public void ApplyMaxAngle(string itemId, int decalIndex)
        {
            ModifyDecalOnItems(itemId, decalIndex, static (decal, decalInfo) =>
            {
                decal.ChangeMaxAngle(decalInfo.MaxAngle);
            });
        }

        public void ApplyTextureUV(string itemId, int decalIndex)
        {
            ModifyDecalOnItems(itemId, decalIndex, static (decal, decalInfo) =>
            {
                decal.ChangeTextureUV(decalInfo.TextureUV);
            });
        }

        public void ApplyTextureAngle(string itemId, int decalIndex)
        {
            ModifyDecalOnItems(itemId, decalIndex, static (decal, decalInfo) =>
            {
                decal.ChangeTextureAngle(decalInfo.TextureAngle);
            });
        }

        public void ApplyMaskUV(string itemId, int decalIndex)
        {
            ModifyDecalOnItems(itemId, decalIndex, static (decal, decalInfo) =>
            {
                decal.ChangeMaskUV(decalInfo.MaskUV);
            });
        }

        public void ApplyMaskAngle(string itemId, int decalIndex)
        {
            ModifyDecalOnItems(itemId, decalIndex, static (decal, decalInfo) =>
            {
                decal.ChangeMaskAngle(decalInfo.MaskAngle);
            });
        }

		public void ApplyBone(string itemId, int decalIndex)
        {
            ModifyDecalOnItems(itemId, decalIndex, static (decalsHost, decal, decalInfo) =>
            {
				var bone = decalsHost.GetDecalRoot(decalInfo.Bone);
                decal.ChangeRoot(bone);
            });
        }

        public void ApplyLocalPosition(string itemId, int decalIndex)
        {
            ModifyDecalOnItems(itemId, decalIndex, static (decal, decalInfo) =>
            {
                decal.DecalTransform.localPosition = decalInfo.LocalPosition;
            });
        }

        public void ApplyLocalEulerAngles(string itemId, int decalIndex)
        {
            ModifyDecalOnItems(itemId, decalIndex, static (decal, decalInfo) =>
            {
                decal.DecalTransform.localEulerAngles = decalInfo.LocalEulerAngles;
            });
        }

        public void ApplyLocalScale(string itemId, int decalIndex)
        {
            ModifyDecalOnItems(itemId, decalIndex, static (decal, decalInfo) =>
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
            SpawnNewDecalOnItems(itemId, newDecalIndex, decalInfoDuplicate);
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

        public void ApplyPaintInfo(string itemId, int decalIndex, DecalInfo decalInfo, DecalInfo fromDecalInfo)
        {
            // why exactly those? ask @Bandoot, he knows better,
            // as I remember, he got tired of copy-pasting textures between multiple attachments

            // match flip
            decalInfo.LocalScale = Vector3.Scale(decalInfo.LocalScale.Abs(), fromDecalInfo.LocalScale.Sign());
            ApplyLocalScale(itemId, decalIndex);

            ChangeTexture(itemId, decalIndex, decalInfo, fromDecalInfo.Texture);

            decalInfo.ColorHSVA = fromDecalInfo.ColorHSVA;
            ApplyColor(itemId, decalIndex);

            decalInfo.MaxAngle = fromDecalInfo.MaxAngle;
            ApplyMaxAngle(itemId, decalIndex);

            decalInfo.MirrorMode = fromDecalInfo.MirrorMode;
        }

        public void ApplyEraserInfo(string itemId, int decalIndex, DecalInfo decalInfo, DecalInfo fromDecalInfo)
        {
            // why exactly those? ask @Bandoot, he knows better,
            // as I remember, he got tired of copy-pasting mag eraser between presets

            decalInfo.Name = fromDecalInfo.Name;

            ChangeMask(itemId, decalIndex, decalInfo, fromDecalInfo.Mask);

            decalInfo.MaskUV = fromDecalInfo.MaskUV;
            ApplyMaskUV(itemId, decalIndex);

            decalInfo.MaskAngle = fromDecalInfo.MaskAngle;
            ApplyMaskAngle(itemId, decalIndex);

            decalInfo.LocalPosition = fromDecalInfo.LocalPosition;
            decalInfo.LocalEulerAngles = fromDecalInfo.LocalEulerAngles;
            decalInfo.LocalScale = fromDecalInfo.LocalScale;

            ApplyLocalPosition(itemId, decalIndex);
            ApplyLocalEulerAngles(itemId, decalIndex);
            ApplyLocalScale(itemId, decalIndex);

            decalInfo.MaxAngle = fromDecalInfo.MaxAngle;
            ApplyMaxAngle(itemId, decalIndex);

            decalInfo.MirrorMode = fromDecalInfo.MirrorMode;
        }

        public void Move(string itemId, int decalIndexOld, int decalIndexNew)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            var decalsInfo = itemsWithDecals.DecalsInfo;

            decalIndexNew = (decalsInfo.Count + decalIndexNew) % decalsInfo.Count;

            // you would think that this can be done with cheap swap operation,
            // but swap doesnt work correctly when we move first element up or last element down

            var decalInfo = decalsInfo[decalIndexOld];
            decalsInfo.RemoveAt(decalIndexOld);
            decalsInfo.Insert(decalIndexNew, decalInfo);
            foreach (var itemWithDecals in itemsWithDecals.Items.Values)
            {
                var decals = itemWithDecals.Decals;
                var decal = decals[decalIndexOld];
                decals.RemoveAt(decalIndexOld);
                decals.Insert(decalIndexNew, decal);
            }
        }

        private const float defaultDecalDepth = 0.04f;
        private const float defaultDecalSize = 0.2f;

        public readonly record struct StartDecalTransform(Vector3 LocalPosition, Vector3 LocalEulerAngles, Vector3 LocalScale);

        public static StartDecalTransform GetStartDecalTransform(ItemType itemType, Transform weaponPreviewRotator, Vector3 previewPivot)
        {
            if (itemType == ItemType.Weapon)
            {
                return GetStartDecalTransform_Weapon(weaponPreviewRotator, previewPivot);
            }
            if (itemType == ItemType.Knife)
            {
                return GetStartDecalTransform_Knife(weaponPreviewRotator);
            }
            if (itemType == ItemType.Headwear)
            {
                return GetStartDecalTransform_Headwear(weaponPreviewRotator, previewPivot);
            }
            if (itemType == ItemType.FaceCover)
            {
                return GetStartDecalTransform_Headwear(weaponPreviewRotator, previewPivot);
            }

            return GetStartDecalTransform_Other(weaponPreviewRotator, previewPivot);
        }

        public static StartDecalTransform GetStartDecalTransform_Weapon(Transform weaponPreviewRotator, Vector3 previewPivot)
        {
            var weaponLocalScale = new Vector3(defaultDecalSize, defaultDecalDepth, defaultDecalSize);
            var rotatorY = weaponPreviewRotator.localEulerAngles.y;
            if (Math.Abs(rotatorY - 90) < 10)
            {
                // back
                return new(new(0, -previewPivot.z, 0), new(0, 0, 0), weaponLocalScale);
            }
            if (Math.Abs(rotatorY - 270) < 10)
            {
                // front
                return new(new(0, -previewPivot.z, 0), new(0, 0, 180), weaponLocalScale);
            }
            if (Math.Cos(rotatorY * Mathf.Deg2Rad) > 0)
            {
                // left
                return new(new(-defaultDecalDepth, -previewPivot.z, 0), new(0, 0, 90), weaponLocalScale);
            }
            else
            {
                // right
                return new(new(defaultDecalDepth, -previewPivot.z, 0), new(0, 0, 270), weaponLocalScale);
            }
        }

        public static StartDecalTransform GetStartDecalTransform_Knife(Transform weaponPreviewRotator)
        {
            // knives are weirdly angled, so back and front are not accurate and confusing, so provide only left and right

            var weaponLocalScale = new Vector3(defaultDecalSize, defaultDecalDepth, defaultDecalSize);
            var rotatorY = weaponPreviewRotator.localEulerAngles.y;
            if (Math.Cos(rotatorY * Mathf.Deg2Rad) > 0)
            {
                // left
                return new(new(-defaultDecalDepth, 0, 0), new(0, 0, 90), weaponLocalScale);
            }
            else
            {
                // right
                return new(new(defaultDecalDepth, 0, 0), new(0, 0, 270), weaponLocalScale);
            }
        }

        public static StartDecalTransform GetStartDecalTransform_Headwear(Transform weaponPreviewRotator, Vector3 previewPivot)
        {
            const float helmetDecalDepth = 0.1f;
            var helmetLocalScale = new Vector3(defaultDecalSize, helmetDecalDepth, defaultDecalSize);
            var rotatorY = weaponPreviewRotator.localEulerAngles.y;
            if (Math.Abs(rotatorY - 90) < 10)
            {
                // back
                return new(new(0, previewPivot.y, -0.12f), new(270, 0, 0), helmetLocalScale);
            }
            if (Math.Abs(rotatorY - 270) < 10)
            {
                // front
                return new(new(0, previewPivot.y, 0.18f), new(270, 180, 0), helmetLocalScale);
            }
            if (Math.Cos(rotatorY * Mathf.Deg2Rad) > 0)
            {
                // left
                return new(new(-0.135f, previewPivot.y, previewPivot.z), new(270, 90, 0), helmetLocalScale);
            }
            else
            {
                // right
                return new(new(0.135f, previewPivot.y, previewPivot.z), new(270, 270, 0), helmetLocalScale);
            }
        }

        public static StartDecalTransform GetStartDecalTransform_Other(Transform weaponPreviewRotator, Vector3 previewPivot)
        {
            // TODO get bounds to place decal on correct item face and not inside object?
            var weaponLocalScale = new Vector3(defaultDecalSize, defaultDecalDepth, defaultDecalSize);
            return new(previewPivot, Vector3.zero, weaponLocalScale);
        }

        public DecalInfo GetNewEraserDecalInfo(ItemType itemType, Transform weaponPreviewRotator, Vector3 previewPivot, byte stencilType)
        {
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var startTransform = GetStartDecalTransform(itemType, weaponPreviewRotator, previewPivot);
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
                Bone = "",
                LocalPosition = startTransform.LocalPosition,
                LocalEulerAngles = startTransform.LocalEulerAngles,
                LocalScale = startTransform.LocalScale,
                MaxAngle = 0.4f,
                IsVisible = true,
                MirrorMode = DecalMirrorMode.Disabled,
                PaintMode = DecalPaintMode.Erase,
                StencilType = stencilType,
            };

            return decalInfo;
        }

        public int AddNewEraserDecal(string itemId, int instanceID, DecalInfo decalInfo, IDecalsHost decalsHost, Camera weaponPreviewCamera)
        {
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                SpawnNewDecalOnItems(itemId, 0, decalInfo);
            }
            else
            {
                CreateNewItemsWithDecals(itemId, instanceID, decalsHost, weaponPreviewCamera, decalInfo);
            }

            return 0;
        }

        public DecalInfo GetNewPaintDecalInfo(ItemType itemType, Transform weaponPreviewRotator, Vector3 previewPivot, byte stencilType)
        {
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var startTransform = GetStartDecalTransform(itemType, weaponPreviewRotator, previewPivot);
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
                Bone = "",
                LocalPosition = startTransform.LocalPosition,
                LocalEulerAngles = startTransform.LocalEulerAngles,
                LocalScale = startTransform.LocalScale,
                MaxAngle = 0.4f,
                IsVisible = true,
                MirrorMode = DecalMirrorMode.Disabled,
                PaintMode = DecalPaintMode.Paint,
                StencilType = stencilType,
            };

            return decalInfo;
        }

        public int AddNewPaintDecal(string itemId, int instanceID, DecalInfo decalInfo, IDecalsHost decalsHost, Camera weaponPreviewCamera)
        {
            if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                var decalIndex = itemsWithDecals.DecalsInfo.Count;
                SpawnNewDecalOnItems(itemId, decalIndex, decalInfo);
                return decalIndex;
            }
            else
            {
                CreateNewItemsWithDecals(itemId, instanceID, decalsHost, weaponPreviewCamera, decalInfo);
                return 0;
            }
        }

        public void SpawnNewDecalOnItems(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            itemsWithDecals.DecalsInfo.Insert(decalIndex, decalInfo);
            foreach (var itemWithDecals in itemsWithDecals.Items.Values)
            {
                var decal = CreateDecal(decalInfo, itemWithDecals.DecalsHost);
                itemWithDecals.Decals.Insert(decalIndex, decal);
            }
        }

        public void CreateNewItemsWithDecals(string itemId, int instanceID, IDecalsHost decalsHost, Camera weaponPreviewCamera, DecalInfo decalInfo)
        {
            var decal = CreateDecal(decalInfo, decalsHost);
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
                            DecalsHost = decalsHost,
                            Decals = decals,
                        }
                    }
                },
                DecalsInfo = decalsInfo
            };

            ItemsWithDecals.Add(itemId, itemsWithDecals);
            InstanceIdToItemId.Add(instanceID, itemId);
            AddItemToDecalCamera(weaponPreviewCamera, itemId);
        }

        public void AddItemToDecalCamera(Camera camera, string itemId)
        {
            if (DecalCameras.TryGetValue(camera, out var items))
            {
                items.Add(itemId);
            }
            else
            {
                DecalCameras.Add(camera, new() { itemId });
            }
        }

        // mirror around YZ plane
        public void MirrorLeftRight(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            var localRotation = decalInfo.LocalEulerAngles.ToQuaternion();
            MirrorLeftRight(ref decalInfo.LocalPosition, ref localRotation, ref decalInfo.LocalScale);
            decalInfo.LocalEulerAngles = localRotation.eulerAngles;

            ApplyLocalPosition(itemId, decalIndex);
            ApplyLocalEulerAngles(itemId, decalIndex);
            ApplyLocalScale(itemId, decalIndex);
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
            ApplyLocalScale(itemId, decalIndex);
        }

        public void FlipHorizontally(DecalInfo decalInfo)
        {
			decalInfo.LocalScale.ScaleX(-1);
        }

        public void FlipVertically(string itemId, int decalIndex, DecalInfo decalInfo)
        {
			decalInfo.LocalScale.ScaleZ(-1);
            ApplyLocalScale(itemId, decalIndex);
        }

        public void FlipDirection(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            var offset = Vector3.up * (decalInfo.LocalScale.y * 2 * -1);
            var rotation = decalInfo.LocalEulerAngles.ToQuaternion();
            var offsetRotated = rotation * offset;

            decalInfo.LocalPosition += offsetRotated;
            decalInfo.LocalEulerAngles.z += -180;

            ApplyLocalPosition(itemId, decalIndex);
            ApplyLocalEulerAngles(itemId, decalIndex);
        }

        public void RoundLocalEulerAnglesToDegree(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.LocalEulerAngles.x = MathF.Round(decalInfo.LocalEulerAngles.x);
            decalInfo.LocalEulerAngles.y = MathF.Round(decalInfo.LocalEulerAngles.y);
            decalInfo.LocalEulerAngles.z = MathF.Round(decalInfo.LocalEulerAngles.z);
            ApplyLocalEulerAngles(itemId, decalIndex);
        }

        public void RotateZ(string itemId, int decalIndex, DecalInfo decalInfo, float angle)
        {
            decalInfo.LocalEulerAngles.z += angle;
            ApplyLocalEulerAngles(itemId, decalIndex);
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
            ApplyLocalScale(itemId, decalIndex);
        }

        public void ResetTextureUVOffset(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.TextureUV.x = 0;
            decalInfo.TextureUV.y = 0;
            ApplyTextureUV(itemId, decalIndex);
        }

        public void ResetTextureAngle(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.TextureAngle = 0;
            ApplyTextureAngle(itemId, decalIndex);
        }

        public void ResetTextureUVScale(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.TextureUV.z = 1;
            decalInfo.TextureUV.w = 1;
            ApplyTextureUV(itemId, decalIndex);
        }

        public void FixTextureUV(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            FixTextureUV(decalInfo);
            ApplyTextureUV(itemId, decalIndex);
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
            ApplyMaskUV(itemId, decalIndex);
        }

        public void ResetMaskAngle(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.MaskAngle = 0;
            ApplyMaskAngle(itemId, decalIndex);
        }

        public void ResetMaskUVScale(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.MaskUV.z = 1;
            decalInfo.MaskUV.w = 1;
            ApplyMaskUV(itemId, decalIndex);
        }

        // notice that we modify decal on all items
        public void ModifyDecalOnItems(string itemId, int decalIndex, Action<Decal, DecalInfo> changeDecal)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];
            foreach (var itemWithDecals in itemsWithDecals.Items.Values)
            {
                var decal = itemWithDecals.Decals[decalIndex];
                changeDecal(decal, decalInfo);
            }
        }

        // notice that we modify decal on all items
        public void ModifyDecalOnItems(string itemId, int decalIndex, Action<IDecalsHost, Decal, DecalInfo> changeDecal)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];
            foreach (var itemWithDecals in itemsWithDecals.Items.Values)
            {
                var decal = itemWithDecals.Decals[decalIndex];
                changeDecal(itemWithDecals.DecalsHost, decal, decalInfo);
            }
        }

        public void ToggleFavouriteTexture(string textureName)
        {
            FavouriteTextures.Toggle(textureName);
        }

        public bool IsFavouriteTexture(string textureName)
        {
            return FavouriteTextures.Contains(textureName);
        }

        public void OnCreateItemAsync(Item item)
        {
            if (!GetItemType(item).Some(out var itemType))
            {
                return;
            }

            var itemId = GetOriginalItemId(item.Id);
            if (!ItemsWithDecals.ContainsKey(itemId))
            {
                return;
            }

            if (ResourceKeyToItem.TryGetValue(item.Prefab, out var existingItem))
            {
                if (existingItem.Id == itemId)
                {
                    Logger.Log(LogLevel.Info, "Item", "Potential warning, already loading (ignore if happened on weapon reload)", itemId, item.Prefab.path);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Item", "Collision", itemId, existingItem.Id, item.Prefab.path);
                }
            }
            else
            {
                ResourceKeyToItem.Add(item.Prefab, new(itemId, itemType));
                Logger.Log(LogLevel.Info, "Item", "Loading", itemId, item.Prefab.path);
            }
        }

        public void OnCreatedItemGameObject(ResourceKey itemPrefab, GameObject itemGameObject)
        {
            if (ResourceKeyToItem.Remove(itemPrefab, out var item))
            {
                if (itemGameObject.TryGetComponent<AssetPoolObject>(out var assetPoolObject))
                {
                    if (assetPoolObject is WeaponPrefab weaponPrefab)
                    {
                        OnDecalsHostCreated_Weapon(item.Id, item.Type, weaponPrefab);
                    }
                    else
                    {
                        OnDecalsHostCreated_Item(item.Id, item.Type, assetPoolObject);
                    }
                }
            }
        }

        public void OnDecalsHostCreated_Weapon(string itemId, ItemType itemType, WeaponPrefab weaponPrefab)
        {
            itemId = GetOriginalItemId(itemId);
            var instanceID = weaponPrefab.gameObject.GetInstanceID();

            // if (WeaponsWaitingForRandomCamo.Remove(itemId))
            // {
            //     GenerateAndRecordRandomCamoForWeapon(itemId, weaponPrefab);
            // }

            if (!ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                // true only if fika client,
                // only non fika client and fika host can decide if camo will be spawned,
                // in some cases client can spawn weapon earlier than
                // host sends info about its camo, so save weapon for future
                if (IsFikaSupportEnabled && IsFikaServer.Some(out var isFikaServer) && !isFikaServer)
                {
                    WeaponsWaitingForRemoteCamo.TryAdd(itemId, weaponPrefab);
                    Logger.Log(LogLevel.Info, "Item", "Created, cache for possible future camo", itemId, instanceID);
                    return;
                }
                else
                {
                    Logger.Log(LogLevel.Info, "Item", "Created, no decals", itemId, instanceID);
                    return;
                }
            }

            if (itemsWithDecals.Items.ContainsKey(instanceID))
            {
                Logger.Log(LogLevel.Info, "Item", "Potential error. Created, tried to init multiple times (ignore if happened on weapon reload)", itemId, instanceID);
                return;
            }

            if (weaponPrefab.TryGetComponent<DressItem>(out _))
            {
                // I doubt there are skinned mesh weapons, even if there are, lets not support them
                return;
            }

            var decalsRoot = GetDecalsRoot(itemType, weaponPrefab);
            var decalsHost = new SimpleDecalsHost(decalsRoot);
            OnDecalsHostCreated(itemId, instanceID, decalsHost, itemsWithDecals);
        }

        public void OnDecalsHostCreated_Item(string itemId, ItemType itemType, AssetPoolObject assetPoolObject)
        {
            itemId = GetOriginalItemId(itemId);
            var instanceID = assetPoolObject.gameObject.GetInstanceID();

            if (!ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                Logger.Log(LogLevel.Info, "Item", "Created, no decals", itemId, instanceID);
                return;
            }

            if (itemsWithDecals.Items.ContainsKey(instanceID))
            {
                Logger.Log(LogLevel.Info, "Item", "Potential error. Created, tried to init multiple times (ignore if happened on weapon reload)", itemId, instanceID);
                return;
            }

            if (assetPoolObject.TryGetComponent<DressItem>(out _))
            {
                // we support some skinned mesh items, but they have to be dressed on PlayerBody
                // to spawn decals properly, at this moment there is only static loot model,
                // so keep record of this item for future when it gets put on character
                // or gets destroyed (in case it was on floor as loose loot all this time)

                DressesWaitingToBeSkinned.Add(instanceID, itemId);
                Logger.Log(LogLevel.Info, "Dress", "Record for future", itemId, instanceID);
                return;
            }

            var decalsRoot = GetDecalsRoot(itemType, assetPoolObject);
            var decalsHost = new SimpleDecalsHost(decalsRoot);
            OnDecalsHostCreated(itemId, instanceID, decalsHost, itemsWithDecals);
        }

        public void OnDecalsHostCreated(string itemId, int instanceID, IDecalsHost decalsHost, ItemsWithDecals itemsWithDecals)
        {
            var decalsInfo = itemsWithDecals.DecalsInfo;
            var decals = new List<Decal>(decalsInfo.Count);
            foreach (var decalInfo in decalsInfo)
            {
                var decal = CreateDecal(decalInfo, decalsHost);
                decals.Add(decal);
            }

            var itemWithDecals = new ItemWithDecals()
            {
                DecalsHost = decalsHost,
                Decals = decals,
            };

            itemsWithDecals.Items.Add(instanceID, itemWithDecals);
            InstanceIdToItemId.Add(instanceID, itemId);

            Logger.Log(LogLevel.Info, "Item", "Created, with decals", itemId, instanceID);
        }

		public Decal CreateDecal(DecalInfo decalInfo, IDecalsHost decalsHost)
		{
            var decal = new GameObject("Decal", typeof(Decal)).GetComponent<Decal>();
            var decalRoot = decalsHost.GetDecalRoot(decalInfo.Bone);
			decal.Init(decalInfo, decalRoot, DecalShader);
            AcquireDecalTextureAsset(decal, decalInfo.Texture, DecalChangeTexture, DecalChangeTexture);
            AcquireDecalTextureAsset(decal, decalInfo.Mask, DecalChangeMask, DecalChangeMask);
			return decal;
		}

        public void DecalChangeTexture(SystemObject key, Texture texture)
        {
            if (key is Decal decal)
            {
                decal.ChangeTexture(texture);
            }
        }

        public void DecalChangeMask(SystemObject key, Texture mask)
        {
            if (key is Decal decal)
            {
                decal.ChangeMask(mask);
            }
        }

		public static Transform GetDecalsRoot(ItemType itemType, AssetPoolObject assetPoolObject)
		{
            if (assetPoolObject is WeaponPrefab weaponPrefab)
            {
                var weaponRoot = GetWeaponRoot(weaponPrefab);
                if (itemType == ItemType.Weapon)
                {
                    return weaponRoot;
                }
                if (itemType == ItemType.Knife)
                {
                    var knifeRoot = weaponRoot.Find("bone_mele");
                    if (knifeRoot)
                    {
                        return knifeRoot;
                    }
                }
            }

            return assetPoolObject.transform;
		}

		public static Transform GetWeaponRoot(WeaponPrefab weaponPrefab)
		{
			return weaponPrefab.Hierarchy.GetTransform(ECharacterWeaponBones.weapon);
		}

        // TODO add ability to select attachment point (ECharacterWeaponBones.weapon or EWeaponModType.mod_magazine)
        // public static Transform GetModTransform(WeaponPrefab weaponPrefab, EWeaponModType modType)
        // {
        //     return TransformTools.FindTransformRecursive(weaponPrefab.Hierarchy.GetTransform(ECharacterWeaponBones.Weapon_root), modType.ToString()).GetChild(0).transform;
        // }

        public void OnSkinCreated(string profileId, string skinId, LoddedSkin skin, Skeleton skeleton)
        {
    		// opposite to other items skinId is the same for all players,
    		// we could make separate system for such type of items,
    		// but its simpler to just prepend it by player id

    		var itemId = profileId + skinId;
            if (!ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
			{
				return;
			}

            var instanceID = skin.gameObject.GetInstanceID();
            if (itemsWithDecals.Items.ContainsKey(instanceID))
            {
    			Logger.Log(LogLevel.Error, "Skin", "Already created", itemId, instanceID);
                return;
            }

			var decalsHost = new SkinnedDecalsHost(skin.transform, skeleton);
			OnDecalsHostCreated(itemId, instanceID, decalsHost, itemsWithDecals);
        }

        public void OnSkinDestroyed(LoddedSkin skin)
        {
            var instanceID = skin.gameObject.GetInstanceID();
			OnItemDestroyed(instanceID);
        }

        public void OnEquippedInSlot(PlayerBody playerBody, GameObject gameObject)
        {
            var instanceID = gameObject.GetInstanceID();

            if (DressesWaitingToBeSkinned.Remove(instanceID, out var itemId) &&
                ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
				var decalsHost = new SkinnedDecalsHost(gameObject.transform, playerBody.SkeletonRootJoint);
				OnDecalsHostCreated(itemId, instanceID, decalsHost, itemsWithDecals);
                Logger.Log(LogLevel.Info, "Dress", "Equipped", itemId, instanceID);
            }
        }

        public void OnItemDestroyed(AssetPoolObject assetPoolObject)
        {
            var instanceID = assetPoolObject.gameObject.GetInstanceID();
            OnItemDestroyed(instanceID);
        }

        public void OnItemDestroyed(int instanceID)
        {
            if (!InstanceIdToItemId.Remove(instanceID, out var itemId))
            {
                if (DressesWaitingToBeSkinned.Remove(instanceID))
                {
                    // skinned meshes that didn't had the chance to be dressed don't get recorded
                    // in InstanceIdToItemId, but are still listed in DressesWaitingToBeSkinned

                    Logger.Log(LogLevel.Info, "Dress", "Destroyed not equipped", instanceID);
                }
                return;
            }

            itemId = GetOriginalItemId(itemId);
            if (!ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                return;
            }

            if (!itemsWithDecals.Items.Remove(instanceID, out var itemWithDecals))
            {
                return;
            }

            Logger.Log(LogLevel.Info, "Item", "Destroyed", itemId, instanceID);

            var decals = itemWithDecals.Decals;
            var decalsInfo = itemsWithDecals.DecalsInfo;
            for (var i = 0; i < itemWithDecals.Decals.Count; i++)
            {
                // Host game object is not necessary destroyed,
                // it can be returned back to pool and then used later
                // with different id, so we have to destroy decals manually

                var decal = decals[i];
                var decalInfo = decalsInfo[i];
                DestroyDecal(decal, decalInfo);
            }
            decals.Clear();
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

        // TODO I forget to clean clone dict in OnItemDestroy...
        public void OnCloneItem(string originalId, string cloneId)
        {
            // when user tries weapon in hideout shooting range,
            // all his gear gets copied to new items to preserve
            // original durability/ammo count/etc,
            // so we have to clone decals ourselves
            if (ItemsWithDecals.ContainsKey(originalId))
            {
                if (originalId == cloneId)
                {
                    // yes, it does happen a lot, no idea why
                    Logger.Log(LogLevel.Warning, "Clone", "Same Id", originalId);
                    return;
                }
                if (Clones.TryAdd(cloneId, originalId))
                {
                    Logger.Log(LogLevel.Info, "Clone", "Added", originalId, cloneId);
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "Clone", "Already added", originalId, cloneId);
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

        public bool IsWaitingForWeaponPreview()
        {
            return IsCamoEditorWaitingForWeaponPreview;
        }

        public void OnWeaponPreviewOpened(Item item, AssetPoolObject assetPoolObject, Transform rotator, PreviewPivot previewPivot, Camera weaponPreviewCamera)
        {
            var itemId = GetOriginalItemId(item.Id);
			Logger.Log(LogLevel.Info, "WeaponPreview", "Opened", itemId);
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                AddItemToDecalCamera(weaponPreviewCamera, itemId);
            }
			if (IsCamoEditorWaitingForWeaponPreview)
			{
				SetupCamoEditor(item, assetPoolObject, rotator, previewPivot, weaponPreviewCamera);
			}
        }

        public void OnWeaponPreviewClosed(string itemId, Camera weaponPreviewCamera)
        {
			Logger.Log(LogLevel.Info, "WeaponPreview", "Closed", itemId);
            DecalCameras.Remove(weaponPreviewCamera);
        }

        public void BeforeInventoryIconRecorded(string itemId, Camera inventoryIconCamera)
        {
            itemId = GetOriginalItemId(itemId);
			Logger.Log(LogLevel.Info, "InventoryIcon", "Before recorded", itemId);
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                AddItemToDecalCamera(inventoryIconCamera, itemId);
            }
        }

        public void AfterInventoryIconRecorded(string itemId, Camera inventoryIconCamera)
        {
			Logger.Log(LogLevel.Info, "InventoryIcon", "After recorded", itemId);
            DecalCameras.Remove(inventoryIconCamera);
        }

		public void OnPlayerModelViewShown(Profile profile, Camera playerModelViewCamera)
        {
			Logger.Log(LogLevel.Info, "PlayerModelView", "Shown");

            var equipmentItems = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment);

            // gets all items with decals
            foreach (var item in equipmentItems)
            {
                if (ItemsWithDecals.ContainsKey(item.Id))
                {
                    AddItemToDecalCamera(playerModelViewCamera, item.Id);
                }
            }

            // gets all clothes with decals
            var profileId = profile.Id;
            foreach (var skinId in profile.Customization.Values)
            {
                var itemId = profileId + skinId;
                if (ItemsWithDecals.ContainsKey(itemId))
                {
                    AddItemToDecalCamera(playerModelViewCamera, itemId);
                }
            }
        }

		public void OnPlayerModelViewClosed(Camera playerModelViewCamera)
        {
			Logger.Log(LogLevel.Info, "PlayerModelView", "Closed");
            DecalCameras.Remove(playerModelViewCamera);
        }

        public void SetupCamoEditor(Item item, AssetPoolObject assetPoolObject, Transform rotator, PreviewPivot previewPivot, Camera editorCamera)
        {
            IsCamoEditorWaitingForWeaponPreview = false;

            if (GetItemType(item).Some(out var itemType) && !assetPoolObject.TryGetComponent<DressItem>(out _))
            {
                var runtimeGizmos = editorCamera.gameObject.AddComponent<RuntimeGizmos>();
                var itemId = GetOriginalItemId(item.Id);
                var instanceID = assetPoolObject.gameObject.GetInstanceID();
                var decalsRoot = GetDecalsRoot(itemType, assetPoolObject);
                var decalsHost = new SimpleDecalsHost(decalsRoot);
                var stencilType = GetItemStencilType(itemType);

                CamoEditor = new(new CamoEditor()
                {
                    Plugin = this,
                    CamoEditorResources = CamoEditorResources,
                    Camera = editorCamera,
                    RuntimeGizmos = runtimeGizmos,
                    ItemId = itemId,
                    InstanceID = instanceID,
                    ItemType = itemType,
                    AssetPoolObject = assetPoolObject,
                    DecalsHost = decalsHost,
                    WeaponPreviewRotator = rotator,
                    PreviewPivot = previewPivot.pivotPosition,
                    StencilType = stencilType,
                });

    			Logger.Log(LogLevel.Info, "CamoEditor", "Setup", itemId, itemType);
            }
            else
            {
                // unsupported item type or skeletal mesh
                CamoEditorError = new(new CamoEditorError()
                {
                    Plugin = this,
                    CamoEditorResources = CamoEditorResources,
                    ErrorMessage = "You can put stickers on this item on Overall screen, or change material to apply camo",
                });

    			Logger.Log(LogLevel.Info, "CamoEditorError", "Setup");
            }
        }

        public static byte GetItemStencilType(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Weapon => 2,
                ItemType.Knife => 2,
                ItemType.Headwear => 1,
                ItemType.FaceCover => 1,
                ItemType.Container => 0,
                ItemType.Armor => 1,
                ItemType.Vest => 1,
                ItemType.Backpack => 1,
                _ => throw new ArgumentException($"Unknown item type: {itemType}"),
            };
        }

        // its annoying to drag sliders
        // or tune decal placement
        // while gun is rotating on every mouse
        // movement, so disable rotation
		public bool CanWeaponPreviewRotate()
        {
            if (CamoEditor.Some(out var camoEditor))
            {
                if (GUIUtility.hotControl != 0)
                {
                    return false;
                }
                if (camoEditor.TransformHandle &&
                    camoEditor.TransformHandle.IsDragging)
                {
                    return false;
                }
            }
            if (CamoEditorError.Some(out var camoEditorError))
            {
                if (GUIUtility.hotControl != 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool CanScroll()
        {
            if (CamoEditor.Some(out var camoEditor))
            {
                if (WeaponCamoAndStickers.CamoEditor.WindowRectContainsMouse(camoEditor.WindowRect))
                {
                    return false;
                }
            }
            if (CamoEditorError.Some(out var camoEditorError))
            {
                if (WeaponCamoAndStickers.CamoEditor.WindowRectContainsMouse(camoEditorError.WindowRect))
                {
                    return false;
                }
            }

            return true;
        }

        // game hides cursor and resets it to the center,
        // when player drags in weapon modding screen, which
        // fucks up dragging transform handles and sliders,
        // so keep cursor visible
        public bool CanHideCursor()
        {
            return !CamoEditor.HasValue && !CamoEditorError.HasValue;
        }

        public void SwitchToRandomPreset(string itemId, int instanceID, AssetPoolObject assetPoolObject, IDecalsHost decalsHost, Camera weaponPreviewCamera)
        {
            if (assetPoolObject is WeaponPrefab weaponPrefab && GenerateRandomCamoForWeapon(weaponPrefab).Some(out var presetDecalsInfo))
            {
                SwitchToPreset(itemId, instanceID, decalsHost, weaponPreviewCamera, presetDecalsInfo);
            }
        }

        public void SwitchToPreset(string itemId, int instanceID, IDecalsHost decalsHost, Camera weaponPreviewCamera, string presetName)
        {
            if (DecalPresets.TryGetValue(presetName, out var presetDecalsInfo))
            {
                SwitchToPreset(itemId, instanceID, decalsHost, weaponPreviewCamera, presetDecalsInfo);
            }
        }

        public void SwitchToPreset(string itemId, int instanceID, IDecalsHost decalsHost, Camera weaponPreviewCamera, List<DecalInfo> presetDecalsInfo)
        {
            if (presetDecalsInfo.Count == 0)
            {
                return;
            }
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                var itemsWithDecals = ItemsWithDecals[itemId];

                // technically we dont need to destroy existing decals,
                // we can simply add ones from preset to the end of the list,
                // but its easier to check presets when they are applied
                // cleanly and look exactly how preset author intended

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
                        var decal = CreateDecal(decalInfo, itemWithDecals.DecalsHost);
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
                    var decal = CreateDecal(decalInfo, decalsHost);
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
                                DecalsHost = decalsHost,
                                Decals = decals,
                            }
                        }
                    },
                    DecalsInfo = decalsInfo
                };

                ItemsWithDecals.Add(itemId, itemsWithDecals);
                InstanceIdToItemId.Add(instanceID, itemId);
                AddItemToDecalCamera(weaponPreviewCamera, itemId);
            }
        }

        public void SaveDecalsIntoPreset(string itemId, string presetName)
        {
            if (!SafeIO.IsValidFileName(presetName))
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

			if (CamoEditorError.Some(out var camoEditorError))
            {
                CamoEditorError = default;
                return;
            }

            if (!CamoEditor.Some(out var camoEditor))
            {
                Logger.Log(LogLevel.Info, "CamoEditor", "Potential warning. Tried to close uninitialized decal editor");
                return;
            }

            camoEditor.ForceOnEndedDraggingHandle();

            SaveOrDeleteItemDecals(camoEditor.ItemId, camoEditor.InstanceID);

            SaveClosedTexturesDirectoriesToDisk();
            SaveFavouriteTexturesToDisk();

            camoEditor.Destroy();
            CamoEditor = default;
        }

        public void SaveOrDeleteItemDecals(string itemId, int instanceID)
        {
            if (GetDecalsInfo(itemId).Some(out var decalsInfo))
            {
                if (decalsInfo.Count == 0)
                {
                    ItemsWithDecals.Remove(itemId);
                    InstanceIdToItemId.Remove(instanceID);
                    RemoveDecalsFile(itemId);
                    Logger.Log(LogLevel.Info, "CamoEditor", "Remove decals", itemId);
                }
                else
                {
                    var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    foreach (var decalInfo in decalsInfo)
                    {
                        decalInfo.SaveTime = time;
                    }

                    WriteDecalsToFile(itemId, decalsInfo);
                    Logger.Log(LogLevel.Info, "CamoEditor", "Rewrite decals", itemId);
                }
            }
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

            // copies all painted items inside player equipment (on hands/sling/holster, inside backpack, rig, etc)
            var profile = tarkovApplication.Session.Profile;
            var equipmentItems = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment);

            foreach (var item in equipmentItems)
            {
                if (CanItemHaveDecals(item) && ItemsWithDecals.TryGetValue(item.Id, out var itemsWithDecals))
                {
                    snapshot[item.Id] = CopyDecalsInfo(itemsWithDecals.DecalsInfo);
                }
            }

            // copies all painted clothes
            var profileId = profile.Id;
            foreach (var skinId in profile.Customization.Values)
            {
                var itemId = profileId + skinId;
                if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
                {
                    snapshot[itemId] = CopyDecalsInfo(itemsWithDecals.DecalsInfo);
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
                Logger.Log(LogLevel.Warning, "RemoteDecals", "Has no decals, but was replicated", itemId);
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
                    Logger.Log(LogLevel.Info, "RemoteDecals", "Mine is newer", itemId);
                    return;
                }

                decalsInfo.Clear();
                CopyDecalsInfo(remoteDecalsInfo, decalsInfo);
                WriteDecalsToFile(itemId, decalsInfo);
                Logger.Log(LogLevel.Info, "RemoteDecals", "His is newer", itemId, itemsWithDecals.Items.Count);
            }
            else
            {
                var decalsInfo = CopyDecalsInfo(remoteDecalsInfo);
                var itemsWithDecals = new ItemsWithDecals()
                {
                    Items = new(),
                    DecalsInfo = decalsInfo,
                };

                ItemsWithDecals.Add(itemId, itemsWithDecals);
                WriteDecalsToFile(itemId, decalsInfo);
                Logger.Log(LogLevel.Info, "RemoteDecals", "New", itemId);
            }

            if (WeaponsWaitingForRemoteCamo.Remove(itemId, out var weaponPrefab))
            {
                Logger.Log(LogLevel.Info, "RemoteDecals", "Was waiting for remote", itemId);
                OnDecalsHostCreated_Weapon(itemId, ItemType.Weapon, weaponPrefab);
            }
        }

        public float GetCamoSpawnChanceFromBotRole(WildSpawnType botRole)
        {
            switch (botRole)
            {
                case WildSpawnType.marksman:
                case WildSpawnType.assault:
                case WildSpawnType.assaultGroup:
                    return ScavsWeaponCamoSpawnChance.Value;
                case WildSpawnType.bossKnight:
                case WildSpawnType.followerBigPipe:
                case WildSpawnType.followerBirdEye:
                    return GoonsWeaponCamoSpawnChance.Value;
                case WildSpawnType.pmcBEAR:
                case WildSpawnType.pmcUSEC:
                    return PMCWeaponCamoSpawnChance.Value;
            }

			if (WildSpawnTypeExtension.IsBossOrFollower(botRole))
			{
                return OtherBossesWeaponCamoSpawnChance.Value;
			}

            return 0;
        }

        public void QueueWeaponForRandomCamoGeneration(WildSpawnType botRole, string itemId, string templateId)
        {
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                Logger.Log(LogLevel.Warning, "RandomCamo", "Tried to queue weapon for camo generation, but weapon already has one", itemId);
                return;
            }
            if (!BotDecalPresets.TryGetValue((int)botRole, out var botItems))
            {
                return;
            }
            if (!botItems.TryGetValue(templateId, out var itemPresets))
            {
                return;
            }

            var decalsInfo = itemPresets[UnityEngine.Random.Range(0, itemPresets.Count)];
            // TODO should we make a copy?
            var itemsWithDecals = new ItemsWithDecals()
            {
                Items = new(),
                DecalsInfo = decalsInfo,
            };
            ItemsWithDecals.Add(itemId, itemsWithDecals);
            WriteDecalsToFile(itemId, decalsInfo);

            Logger.Log(LogLevel.Info, "RandomCamo", "Queue item", itemId);
        }

        public void GenerateAndRecordRandomCamoForWeapon(string itemId, WeaponPrefab weaponPrefab)
        {
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                Logger.Log(LogLevel.Warning, "RandomCamo", "Tried to generate camo for item that already has one", itemId);
                return;
            }

            // this method gets called only on non fika clients or fika host, so we can omit checks
            if (!GenerateRandomCamoForWeapon(weaponPrefab).Some(out var decalsInfo))
            {
                Logger.Log(LogLevel.Warning, "RandomCamo", "Generated empty camo", itemId);
                return;
            }
            var itemsWithDecals = new ItemsWithDecals()
            {
                Items = new(),
                DecalsInfo = decalsInfo,
            };

            if (IsFikaSupportEnabled)
            {
                Logger.Log(LogLevel.Info, "RandomCamo", "Generated camo, fika host", itemId);
                OnBotWeaponCamoGenerated?.Invoke(new() {{ itemId, decalsInfo }});
                if (!IsFikaHeadless)
                {
                    ItemsWithDecals.Add(itemId, itemsWithDecals);
                    WriteDecalsToFile(itemId, decalsInfo);
                }
            }
            else
            {
                Logger.Log(LogLevel.Info, "RandomCamo", "Generated camo, no fika", itemId);
                ItemsWithDecals.Add(itemId, itemsWithDecals);
                WriteDecalsToFile(itemId, decalsInfo);
            }
        }

        public Option<List<DecalInfo>> GenerateRandomCamoForWeapon(WeaponPrefab weaponPrefab)
        {
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
                Bone = "",
                LocalPosition = startLocalPosition,
                LocalEulerAngles = startLocalEulerAngles,
                LocalScale = startLocalScale,
                MaxAngle = 0.4f,
                IsVisible = true,
                MirrorMode = DecalMirrorMode.Disabled,
                PaintMode = DecalPaintMode.Paint,
                StencilType = 2,
            };
        }

        public static bool CanItemHaveDecals(Item item)
        {
            return GetItemType(item).HasValue;
        }

        public static Option<ItemType> GetItemType(Item item)
        {
            return item switch
            {
                Weapon => new(ItemType.Weapon),
                Knife => new(ItemType.Knife),
                Headwear => new(ItemType.Headwear),
                FaceCover => new(ItemType.FaceCover),
                SimpleContainer => new(ItemType.Container),
                MobContainer => new(ItemType.Container),
                Armor => new(ItemType.Armor),
                Vest => new(ItemType.Vest),
                Backpack => new(ItemType.Backpack),
                _ => default,
            };
        }

    }
}
