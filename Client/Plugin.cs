//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Newtonsoft.Json;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using RuntimeHandle;
using UnityEngine;

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
        public Transform DecalsRoot;
        public List<Decal> Decals;
    }

    public class DecalInfo
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion;
        public string Name;
        public string Texture;
        public Vector4 TextureUV;
        public Vector4 ColorHSVA;
        public string Mask;
        public Vector4 MaskUV;
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale;
        public float MaxAngle;

        public DecalInfo GetCopy()
        {
            // this is enough for now
            return (DecalInfo)MemberwiseClone();
        }
    }

    public struct DecalTextureData
    {
        public Texture2D Texture;
        public DecalTextureType Type;
    }

    public enum DecalTextureType
    {
        Camo,
        Sticker,
        Mask
    }

    [BepInPlugin("7Bpencil.WeaponCamoAndStickers", "7Bpencil.WeaponCamoAndStickers", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public const string DefaultCamoName = "builtin/camos/default";
        public const string DefaultStickerName = "builtin/stickers/default";
        public const string DefaultMaskName = "builtin/masks/default";

        public static Plugin Instance;

        public static ConfigEntry<KeyboardShortcut> MoveButton;
        public static ConfigEntry<KeyboardShortcut> RotateButton;
        public static ConfigEntry<KeyboardShortcut> ScaleButton;

		public ManualLogSource LoggerInstance;

        private string DecalTexturesDir;
        private string ItemsDir;
        private string PresetsDir;
        private Shader DecalShader;
        private Texture2D MissingTexture;
        private CamoEditorResources CamoEditorResources;
        private Dictionary<string, DecalTextureData> DecalTextures;
        private List<string> CamosList;
        private List<string> StickersList;
        private List<string> MasksList;

        private Dictionary<string, List<DecalInfo>> DecalPresets;
        private Dictionary<string, ItemsWithDecals> ItemsWithDecals;
        private Dictionary<string, string> Clones;
        private Dictionary<Camera, string> WeaponPreviewCameras;
        private HashSet<Camera> PlayerModelViewCameras;

        private DecalRenderer DecalRenderer;
        private Option<CamoEditor> CamoEditor;
        private bool IsCamoEditorWaitingForWeaponPreview;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            MoveButton = Config.Bind("Main", "Camo Editor | Hotkeys | Move", new KeyboardShortcut(KeyCode.G), "");
            RotateButton = Config.Bind("Main", "Camo Editor | Hotkeys | Rotate", new KeyboardShortcut(KeyCode.R), "");
            ScaleButton = Config.Bind("Main", "Camo Editor | Hotkeys | Scale", new KeyboardShortcut(KeyCode.S), "");

            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DecalTexturesDir = Path.Combine(assemblyDir, "textures");
            ItemsDir = Path.Combine(assemblyDir, "items");
            PresetsDir = Path.Combine(assemblyDir, "presets");
			var bundlePath = Path.Combine(assemblyDir, "bundles", "weapon-camo-and-stickers");
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            DecalShader = bundle.LoadAsset<Shader>("Assets/WeaponCamoAndStickers/Shaders/DecalDynamic.shader");
            MissingTexture = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Textures/missing.png");
            CamoEditorResources = new(bundle);
            DecalTextures = new();
            CamosList = LoadTexturesFromDirectory(DecalTextureType.Camo, DecalTexturesDir, "camos", bundle, DecalTextures, DefaultCamoName, Texture2D.whiteTexture);
            StickersList = LoadTexturesFromDirectory(DecalTextureType.Sticker, DecalTexturesDir, "stickers", bundle, DecalTextures, DefaultStickerName, Texture2D.whiteTexture);
            MasksList = LoadTexturesFromDirectory(DecalTextureType.Mask, DecalTexturesDir, "masks", bundle, DecalTextures, DefaultMaskName, Texture2D.whiteTexture);

            DecalPresets = LoadDecalPresets(PresetsDir);
            ItemsWithDecals = LoadItemsWithDecals(ItemsDir);
            Clones = new();
            WeaponPreviewCameras = new();
            PlayerModelViewCameras = new();

            DecalRenderer = new(ItemsWithDecals, WeaponPreviewCameras, PlayerModelViewCameras);

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

            // TODO
            // maybe apply camo texture on top of diffuse texture?

            // TODO
            // hear me out: we can place 3D models as decorations on guns and equipment

            // TODO
            // are gifs possible?
        }

        public List<string> LoadTexturesFromDirectory(
            DecalTextureType decalTextureType,
            string rootDirectoryPath,
            string subfolder,
            AssetBundle bundle,
            Dictionary<string, DecalTextureData> resultDict,
            string defaultTextureName,
            Texture2D defaultTexture)
        {
            List<string> resultList;

            var directoryPath = Path.Combine(rootDirectoryPath, subfolder);
            if (!Directory.Exists(directoryPath))
            {
                resultList = new List<string>(1);
                AddTexture(defaultTextureName, defaultTexture, decalTextureType, resultList, resultDict);
                return resultList;
            }

            var filePaths = Directory.GetFiles(directoryPath, "*.png", new EnumerationOptions() { RecurseSubdirectories = true });
            resultList = new List<string>(filePaths.Length + 1);
            AddTexture(defaultTextureName, defaultTexture, decalTextureType, resultList, resultDict);

            foreach (var filePath in filePaths)
            {
                var extension = Path.GetExtension(filePath);
                var fileData = File.ReadAllBytes(filePath);
                var texture = new Texture2D(2, 2);

                if (ImageConversion.LoadImage(texture, fileData))
                {
                    var name = filePath
                        .Replace(rootDirectoryPath, "")
                        .Replace(extension, "")
                        .Remove(0, 1) // remove first slash
                        .Replace(@"\", @"/"); // replace windows slashes with unix ones

                    AddTexture(name, texture, decalTextureType, resultList, resultDict);
                }
                else
                {
                    Logger.LogError($"Failed to load decal texture: {filePath}");
                }
            }

            return resultList;
        }

        public static void AddTexture(string textureName, Texture2D texture, DecalTextureType type, List<string> resultList, Dictionary<string, DecalTextureData> resultDict)
        {
            resultList.Add(textureName);
            resultDict.Add(textureName, new DecalTextureData()
            {
                Texture = texture,
                Type = type
            });
        }

        public static Dictionary<string, List<DecalInfo>> LoadDecalPresets(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return new();
            }

            var filePaths = Directory.GetFiles(directoryPath, "*.json");
            var result = new Dictionary<string, List<DecalInfo>>();

            foreach (var filePath in filePaths)
            {
                var presetName = Path.GetFileNameWithoutExtension(filePath);
                var json = File.ReadAllText(filePath);
                var decalsInfo = JsonConvert.DeserializeObject<List<DecalInfo>>(json);
                UpgradeOldVersionsOfDecalsInfo(decalsInfo);

                result.Add(presetName, decalsInfo);
            }

            return result;
        }

        public static Dictionary<string, ItemsWithDecals> LoadItemsWithDecals(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return new();
            }

            var filePaths = Directory.GetFiles(directoryPath, "*.json");
            var result = new Dictionary<string, ItemsWithDecals>();

            foreach (var filePath in filePaths)
            {
                var itemId = Path.GetFileNameWithoutExtension(filePath);
                var json = File.ReadAllText(filePath);
                var decalsInfo = JsonConvert.DeserializeObject<List<DecalInfo>>(json);
                UpgradeOldVersionsOfDecalsInfo(decalsInfo);
                var itemsWithDecals = new ItemsWithDecals()
                {
                    Items = new(),
                    DecalsInfo = decalsInfo,
                };

                result.Add(itemId, itemsWithDecals);
            }

            return result;
        }

        public static void UpgradeOldVersionsOfDecalsInfo(List<DecalInfo> decalsInfo)
        {
            foreach (var decalInfo in decalsInfo)
            {
                if (decalInfo.SchemaVersion == 0)
                {
                    decalInfo.Name = "";
                }
            }
        }

        public void Update()
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
            return new DecalTextureData()
            {
                Texture = MissingTexture,
                Type = DecalTextureType.Camo
            };
        }

        public int GetTexturesCount(DecalTextureType texturesType)
        {
            return texturesType switch
            {
                DecalTextureType.Camo => CamosList.Count,
                DecalTextureType.Sticker => StickersList.Count,
                DecalTextureType.Mask => MasksList.Count,
                _ => throw new ArgumentException(),
            };
        }

        public string GetTextureName(DecalTextureType texturesType, int textureIndex)
        {
            return texturesType switch
            {
                DecalTextureType.Camo => CamosList[textureIndex],
                DecalTextureType.Sticker => StickersList[textureIndex],
                DecalTextureType.Mask => MasksList[textureIndex],
                _ => throw new ArgumentException(),
            };
        }

        public void ApplyTexture(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            var textureData = GetTextureData(decalInfo.Texture);
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeTexture(textureData.Texture);
            });
        }

        public void ApplyMask(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            var maskData = GetTextureData(decalInfo.Mask);
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeMask(maskData.Texture);
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

        public void ApplyMaskUV(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeMaskUV(decalInfo.MaskUV);
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
                decal.DecalTransform.localScale = decalInfo.LocalScale;
            });
        }

        public int Duplicate(string itemId, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];
            var decalInfoDuplicate = decalInfo.GetCopy();
            itemsWithDecals.DecalsInfo.Insert(decalIndex, decalInfoDuplicate);
            foreach (var itemWithDecals in itemsWithDecals.Items.Values)
            {
                var decal = CreateDecal(decalInfo, itemWithDecals.DecalsRoot);
                itemWithDecals.Decals.Insert(decalIndex, decal);
            }

            return decalIndex + 1;
        }

        public void Delete(string itemId, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            itemsWithDecals.DecalsInfo.RemoveAt(decalIndex);
            foreach (var itemWithDecals in itemsWithDecals.Items.Values)
            {
                var decal = itemWithDecals.Decals[decalIndex];
                itemWithDecals.Decals.RemoveAt(decalIndex);
                Destroy(decal.gameObject);
            }
        }

        public void Swap(string itemId, int decalIndexA, int decalIndexB)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            var decalsInfo = itemsWithDecals.DecalsInfo;

            // loop indices
            decalIndexA = (decalsInfo.Count + decalIndexA) % decalsInfo.Count;
            decalIndexB = (decalsInfo.Count + decalIndexB) % decalsInfo.Count;

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

        public int AddNewDecal(string itemId, int instanceID, Transform decalsRoot, Transform weaponPreviewRotator, float previewPivotZ, Camera weaponPreviewCamera)
        {
            var (startLocalPosition, startLocalEulerAngles) = GetStartPositionAndRotation(weaponPreviewRotator, previewPivotZ);
            var decalInfo = new DecalInfo()
            {
                SchemaVersion = DecalInfo.CurrentSchemaVersion,
                Name = "",
                Texture = DefaultCamoName,
                TextureUV = new Vector4(0, 0, 1, 1),
                ColorHSVA = new Vector4(0, 0, 1, 1),
                Mask = DefaultMaskName,
                MaskUV = new Vector4(0, 0, 1, 1),
                LocalPosition = startLocalPosition,
                LocalEulerAngles = startLocalEulerAngles,
                LocalScale = new Vector3(defaultDecalSize, defaultDecalDepth, defaultDecalSize),
                MaxAngle = 0.5f,
            };

            if (ItemsWithDecals.ContainsKey(itemId))
            {
                var itemsWithDecals = ItemsWithDecals[itemId];
                itemsWithDecals.DecalsInfo.Add(decalInfo);
                foreach (var itemWithDecals in itemsWithDecals.Items.Values)
                {
                    var decal = CreateDecal(decalInfo, itemWithDecals.DecalsRoot);
                    itemWithDecals.Decals.Add(decal);
                }

                return itemsWithDecals.DecalsInfo.Count - 1;
            }
            else
            {
                var decal = CreateDecal(decalInfo, decalsRoot);
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
                                DecalsRoot = decalsRoot,
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

        // this works only left/right,
        // to make it work in more cases would require defining
        // proper mirror plane, which is obvious for left/right,
        // but not so obvious in other cases
        public void FlipSideLeftRight(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.LocalPosition.x *= -1f;
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
            var texture = textureData.Texture;
            var uvAspectRatio = decalInfo.TextureUV.z / decalInfo.TextureUV.w;
            var textureAspectRatio = texture.width / (float)texture.height;
            var trueTextureAspectRatio = textureAspectRatio * uvAspectRatio;
            decalInfo.LocalScale.z = decalInfo.LocalScale.x / trueTextureAspectRatio;
            ApplyLocalScale(itemId, decalIndex, decalInfo);
        }

        public void FixUV(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            // we keep uv height and modify width to match it
            var textureData = GetTextureData(decalInfo.Texture);
            var texture = textureData.Texture;
            var textureAspectRatio = texture.width / (float)texture.height;
            var decalAspectRatio = decalInfo.LocalScale.x / decalInfo.LocalScale.z;
            var k = decalAspectRatio / textureAspectRatio;
            decalInfo.TextureUV.z = decalInfo.TextureUV.w * k;
            ApplyTextureUV(itemId, decalIndex, decalInfo);
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
            if (!ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                Logger.LogInfo($"OnWeaponPrefabCreated: {itemId}, no decals info");
                return;
            }

            var instanceID = weaponPrefab.GetInstanceID();
            if (itemsWithDecals.Items.ContainsKey(instanceID))
            {
                Logger.LogError($"OnWeaponPrefabCreated: {itemId}, tried to init multiple times?");
                return;
            }

            var decalsRoot = GetWeaponRoot(weaponPrefab);
            var decalsInfo = itemsWithDecals.DecalsInfo;
            var decals = new List<Decal>(decalsInfo.Count);
            foreach (var decalInfo in decalsInfo)
            {
                var decal = CreateDecal(decalInfo, decalsRoot);
                decals.Add(decal);
            }

            var itemWithDecals = new ItemWithDecals()
            {
                DecalsRoot = decalsRoot,
                Decals = decals,
            };

            itemsWithDecals.Items.Add(instanceID, itemWithDecals);

            Logger.LogInfo($"OnWeaponPrefabCreated: {itemId}, success");
        }

		public Decal CreateDecal(DecalInfo decalInfo, Transform root)
		{
            var decal = new GameObject("Decal", typeof(Decal)).GetComponent<Decal>();
            var decalTextureData = GetTextureData(decalInfo.Texture);
            var decalMaskData = GetTextureData(decalInfo.Mask);
			decal.Init(DecalShader);
			decal.Set(decalInfo, root, decalTextureData.Texture, decalMaskData.Texture);
			return decal;
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
                if (itemsWithDecals.Items.Remove(instanceID, out var itemWithDecals))
                {
        			Logger.LogInfo($"OnWeaponPrefabDestroyed: {itemId}, {instanceID}");
                    foreach (var decal in itemWithDecals.Decals)
                    {
                        if (decal)
                        {
                            Destroy(decal.gameObject);
                        }
                    }
                }
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
			Logger.LogInfo($"OnWeaponPreviewOpened: {itemId}");
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                WeaponPreviewCameras.Add(weaponPreviewCamera, itemId);
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
            var decalsRoot = GetWeaponRoot(weaponPrefab);
            var runtimeGizmos = editorCamera.gameObject.AddComponent<RuntimeGizmos>();
            CamoEditor = new(new CamoEditor()
            {
                Plugin = this,
                CamoEditorResources = CamoEditorResources,
                Camera = editorCamera,
                RuntimeGizmos = runtimeGizmos,
                ItemId = itemId,
                InstanceID = instanceID,
                DecalsRoot = decalsRoot,
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

        public void SwitchToPreset(string itemId, int instanceID, Transform decalsRoot, Camera weaponPreviewCamera, string presetName)
        {
            if (!DecalPresets.TryGetValue(presetName, out var presetDecalsInfo))
            {
                return;
            }
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                var itemsWithDecals = ItemsWithDecals[itemId];

                var decalsInfo = itemsWithDecals.DecalsInfo;
                decalsInfo.Clear();
                CopyDecalsInfo(presetDecalsInfo, decalsInfo);

                foreach (var itemWithDecals in itemsWithDecals.Items.Values)
                {
                    var decals = itemWithDecals.Decals;
                    foreach (var decal in decals)
                    {
                        GameObject.Destroy(decal.gameObject);
                    }
                    decals.Clear();

                    foreach (var decalInfo in decalsInfo)
                    {
                        var decal = CreateDecal(decalInfo, itemWithDecals.DecalsRoot);
                        decals.Add(decal);
                    }
                }
            }
            else
            {
                var decalsInfo = new List<DecalInfo>(presetDecalsInfo.Count);
                CopyDecalsInfo(presetDecalsInfo, decalsInfo);

                var decals = new List<Decal>(presetDecalsInfo.Count);
                foreach (var decalInfo in decalsInfo)
                {
                    var decal = CreateDecal(decalInfo, decalsRoot);
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
                                DecalsRoot = decalsRoot,
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
                var newPresetDecalsInfo = new List<DecalInfo>(decalsInfo.Count);
                CopyDecalsInfo(decalsInfo, newPresetDecalsInfo);
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

        public void WritePresetToFile(string presetName, List<DecalInfo> preset)
        {
            var fileInfo = GetPresetFileInfo(presetName);
            var json = JsonConvert.SerializeObject(preset, Formatting.Indented);
            Directory.CreateDirectory(fileInfo.Directory.FullName);
            File.WriteAllText(fileInfo.FullName, json);
        }

        public void DeletePreset(string presetName)
        {
            if (DecalPresets.Remove(presetName))
            {
                var fileInfo = GetPresetFileInfo(presetName);
                File.Delete(fileInfo.FullName);
            }
        }

        public FileInfo GetPresetFileInfo(string presetName)
        {
            var fileName = $"{presetName}.json";
            var filePath = Path.Combine(PresetsDir, fileName);
            return new FileInfo(filePath);
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
                    WriteDecalsToFile(itemId, decalsInfo);
                    Logger.LogInfo($"CloseCamoEditor: {itemId} rewrite decals");
                }
            }

            camoEditor.Destroy();
            CamoEditor = default;
        }

        public void WriteDecalsToFile(string itemId, List<DecalInfo> decalsInfo)
        {
            var fileInfo = GetItemFileInfo(itemId);
            var json = JsonConvert.SerializeObject(decalsInfo, Formatting.Indented);
            Directory.CreateDirectory(fileInfo.Directory.FullName);
            File.WriteAllText(fileInfo.FullName, json);
        }

        public void RemoveDecalsFile(string itemId)
        {
            var fileInfo = GetItemFileInfo(itemId);
            File.Delete(fileInfo.FullName);
        }

        public FileInfo GetItemFileInfo(string itemId)
        {
            var fileName = $"{itemId}.json";
            var filePath = Path.Combine(ItemsDir, fileName);
            return new FileInfo(filePath);
        }
    }
}
