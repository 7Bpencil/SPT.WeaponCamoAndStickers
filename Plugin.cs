//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using DeferredDecals;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using EFT.UI.WeaponModding;
using Newtonsoft.Json;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Systems.Effects;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.Rendering;

namespace SevenBoldPencil.WeaponCamo
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
        public string Texture;
        public Vector4 ColorHSVA;
        public Vector4 UV;
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

    [BepInPlugin("7Bpencil.WeaponCamo", "7Bpencil.WeaponCamo", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string DefaultTextureName = "default";

        public static Plugin Instance;

		public ManualLogSource LoggerInstance;

        private DecalRenderer DecalRenderer;
        private Option<CamoEditor> CamoEditor;
        private CamoEditorResources CamoEditorResources;
        private bool IsCamoEditorWaitingForWeaponPreview;

        private string DecalTexturesDir;
        private string ItemsDir;
        private string PresetsDir;
        private Shader DecalShader;
        private List<string> LoadedDecalTexturesList;
        private Dictionary<string, Texture2D> LoadedDecalTextures;
        private Dictionary<string, ItemsWithDecals> ItemsWithDecals;
        private Dictionary<string, string> Clones;
        private Dictionary<Camera, string> WeaponPreviewCameras;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var userDir = Path.Combine(assemblyDir, "..", "..", "..", "SPT", "user", "mods", "7Bpencil.WeaponCamo");
            DecalTexturesDir = Path.Combine(userDir, "textures");
            ItemsDir = Path.Combine(userDir, "items");
            PresetsDir = Path.Combine(userDir, "presets");
			var bundlePath = Path.Combine(assemblyDir, "assets", "bundles", "weaponcamo");
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            DecalShader = bundle.LoadAsset<Shader>("Assets/WeaponCamo/Shaders/DecalDynamic.shader");
            (LoadedDecalTexturesList, LoadedDecalTextures) = LoadTexturesFromDirectory(DecalTexturesDir, bundle);
            CamoEditorResources = new(bundle);
            ItemsWithDecals = LoadItemsWithDecals(ItemsDir);
            Clones = new();
            WeaponPreviewCameras = new();

            DecalRenderer = new(ItemsWithDecals, WeaponPreviewCameras);

            new Patch_WeaponPreview_Class3271_method_1().Enable();
            new Patch_WeaponPreview_Rotate().Enable();
            new Patch_WeaponPreview_Hide().Enable();
            new Patch_WeaponModdingScreen_Show().Enable();
            new Patch_WeaponModdingScreen_Close().Enable();
            new Patch_WeaponPrefab_InitHotObjects().Enable();
            new Patch_WeaponPrefab_ReturnToPool().Enable();
            new Patch_AssetPoolObject_OnDestroy().Enable();
            new Patch_GClass3380_CloneItem().Enable();
            new Patch_GClass2304_smethod_0().Enable();

            // TODO
            // seems like decals are not drawing on gun (because of stencil?)
            // does it mean we can make decals that will only apply on gun (and not hands and env)?

            // TODO
            // maybe apply camo texture on top of diffuse texture?

            // TODO
            // we can save presets as jsons in assets/presets/ each preset separate file with name=preset name (with support for subfolders)
            // show presets per weapon template (Item.TemplateId?)

            // TODO
            // hear me out: we can place 3D models as decorations on guns and equipment!

            // TODO
            // are gifs possible?

            // TODO
            // move pivot point from center to face (in shaders + gizmo, not transform)

            // TODO
            // make keyboard shortcuts for move/rotate/scale

            // TODO
            // make buttons flip horizontally, resets etc

            // TODO
            // decals are not showing on some screens
        }

        // TODO
        // add support for adding/removing images on running game
        // add support for drawing sub folders as file tree
        public (List<string>, Dictionary<string, Texture2D>) LoadTexturesFromDirectory(string directoryPath, AssetBundle bundle)
        {
            var filePaths = Directory.GetFiles(directoryPath, "*", new EnumerationOptions()
            {
                RecurseSubdirectories = true
            });
            var resultList = new List<string>(filePaths.Length + 1);
            var resultDict = new Dictionary<string, Texture2D>();

            {
                resultList.Add(DefaultTextureName);
                resultDict.Add(DefaultTextureName, Texture2D.whiteTexture);
            }

            foreach (var filePath in filePaths)
            {
                var extension = Path.GetExtension(filePath);
                if (extension != ".png") // maybe we will support gif one day?
                {
                    continue;
                }

                var fileData = File.ReadAllBytes(filePath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, fileData))
                {
                    var name = filePath
                        .Replace(directoryPath, "")
                        .Replace(extension, "")
                        .Remove(0, 1) // remove first slash
                        .Replace(@"\", @"/"); // replace windows slashes with unix ones

                    resultList.Add(name);
                    resultDict.Add(name, texture);
                }
                else
                {
                    LoggerInstance.LogError($"Failed to load decal texture: {name}");
                }
            }

            return (resultList, resultDict);
        }

        public Dictionary<string, ItemsWithDecals> LoadItemsWithDecals(string directoryPath)
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
                var itemsWithDecals = new ItemsWithDecals()
                {
                    Items = new(),
                    DecalsInfo = decalsInfo,
                };

                result.Add(itemId, itemsWithDecals);
            }

            return result;
        }

        public void LateUpdate()
        {
            if (CamoEditor.Some(out var camoEditor) &&
                camoEditor.CurrentlyEditedDecalIndex.Some(out var currentlyEditedDecalIndex) &&
                camoEditor.RuntimeGizmos &&
                ItemsWithDecals.TryGetValue(camoEditor.ItemId, out var itemsWithDecals) &&
                itemsWithDecals.Items.TryGetValue(camoEditor.InstanceID, out var itemWithDecals))
            {
                var decal = itemWithDecals.Decals[currentlyEditedDecalIndex];
                if (decal)
                {
                    var decalTransform = decal.DecalTransform;
                    var position = decalTransform.position;
                    var scale = decalTransform.lossyScale;

                    camoEditor.RuntimeGizmos.Cubes.Add(new RuntimeGizmos.Cube()
                    {
                        Position = position,
                        Rotation = decalTransform.rotation,
                        Scale = scale,
                    });
                    camoEditor.RuntimeGizmos.Lines.Add(new RuntimeGizmos.Line()
                    {
                        Start = position,
                        End = position + decalTransform.up * scale.y,
                    });
                }
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

        public Texture2D GetTexture(string textureName)
        {
			// TODO if texture doesnt exist return pink texture with ERROR text
			if (LoadedDecalTextures.TryGetValue(textureName, out var texture))
            {
                return texture;
            }

			return Texture2D.whiteTexture;
        }

        public int GetTexturesCount()
        {
            return LoadedDecalTexturesList.Count;
        }

        public string GetTextureName(int textureIndex)
        {
            return LoadedDecalTexturesList[textureIndex];
        }

        public void ApplyTexture(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            var texture = GetTexture(decalInfo.Texture);
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeTexture(texture);
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

        public void ApplyUV(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            ModfiyDecalOnItems(itemId, decalIndex, decal =>
            {
                decal.ChangeUV(decalInfo.UV);
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
            foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
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
            foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
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

            if (decalIndexA < 0 || decalIndexA > decalsInfo.Count - 1 ||
                decalIndexB < 0 || decalIndexB > decalsInfo.Count - 1)
            {
                return;
            }

            (decalsInfo[decalIndexA], decalsInfo[decalIndexB]) = (decalsInfo[decalIndexB], decalsInfo[decalIndexA]);
            foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
            {
                var decals = itemWithDecals.Decals;
                (decals[decalIndexA], decals[decalIndexB]) = (decals[decalIndexB], decals[decalIndexA]);
            }
        }

        public int AddNewDecal(string itemId, int instanceID, Transform decalsRoot, Camera weaponPreviewCamera)
        {
            var decalInfo = new DecalInfo()
            {
                Texture = DefaultTextureName,
                ColorHSVA = new Vector4(0, 0, 1, 1),
                UV = new Vector4(0, 0, 1, 1),
                LocalPosition = typicalRifleCenter,
                LocalEulerAngles = Decal.LeftSideDecalRotation,
                LocalScale = new Vector3(defaultDecalSize, defaultDecalDepth, defaultDecalSize),
                MaxAngle = 0.8f,
            };

            if (ItemsWithDecals.ContainsKey(itemId))
            {
                var itemsWithDecals = ItemsWithDecals[itemId];
                itemsWithDecals.DecalsInfo.Add(decalInfo);
                foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
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
                var itemsWithDecals = new ItemsWithDecals() {
                    Items = new Dictionary<int, ItemWithDecals>() {
                        {
                            instanceID,
                            new ItemWithDecals() {
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

        public void RoundLocalEulerAnglesToDegree(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            decalInfo.LocalEulerAngles.x = MathF.Round(decalInfo.LocalEulerAngles.x);
            decalInfo.LocalEulerAngles.y = MathF.Round(decalInfo.LocalEulerAngles.y);
            decalInfo.LocalEulerAngles.z = MathF.Round(decalInfo.LocalEulerAngles.z);
            ApplyLocalEulerAngles(itemId, decalIndex, decalInfo);
        }

        public void FixAspectRatio(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            // we keep decal width the same and change height to match texture aspect ratio
            var texture = GetTexture(decalInfo.Texture);
            var textureInverseAspectRatio = texture.height / (float)texture.width;
            decalInfo.LocalScale.z = decalInfo.LocalScale.x * textureInverseAspectRatio;
            ApplyLocalScale(itemId, decalIndex, decalInfo);
        }

        public void FixUVAspectRatio(string itemId, int decalIndex, DecalInfo decalInfo)
        {
            // we keep uv height and modify width to match it
            var aspectRatio = decalInfo.LocalScale.x / decalInfo.LocalScale.z;
            decalInfo.UV.z = decalInfo.UV.w * aspectRatio;
            ApplyUV(itemId, decalIndex, decalInfo);
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

        private readonly Vector3 typicalRifleCenter = new Vector3(0f, -0.35f, -0.003f);
        private readonly float defaultDecalSize = 0.2f;
        private readonly float defaultDecalDepth = 0.1f;

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
            var decalTexture = GetTexture(decalInfo.Texture);
			decal.Init(DecalShader);
			decal.Set(decalInfo, root, decalTexture);
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
            // but we have to clone decals ourselves
            if (ItemsWithDecals.ContainsKey(originalId))
            {
                Logger.LogInfo($"OnCloneItem: original: {originalId}, clone: {cloneId}");
                Clones.Add(cloneId, originalId);
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

        public void OnWeaponPreviewOpened(Camera weaponPreviewCamera, string itemId, WeaponPrefab weaponPrefab)
        {
			Logger.LogWarning($"OnWeaponPreviewOpened: {itemId}");
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                WeaponPreviewCameras.Add(weaponPreviewCamera, itemId);
            }
			if (IsCamoEditorWaitingForWeaponPreview)
			{
				SetupCamoEditor(weaponPreviewCamera, itemId, weaponPrefab);
			}
        }

        public void OnWeaponPreviewClosed(Camera weaponPreviewCamera, string itemId)
        {
			Logger.LogWarning($"OnWeaponPreviewClosed: {itemId}");
            WeaponPreviewCameras.Remove(weaponPreviewCamera);
        }

        public void SetupCamoEditor(Camera editorCamera, string itemId, WeaponPrefab weaponPrefab)
        {
            itemId = GetOriginalItemId(itemId);
            LoggerInstance.LogInfo($"SetupCamoEditor: {itemId}");
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
                IsOpened = false,
                IsColorPickerOpened = false,
                WindowRect = SevenBoldPencil.WeaponCamo.CamoEditor.GetDefaultWindowRect()
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
                LoggerInstance.LogWarning($"CloseCamoEditor: tried to close uninitialized decal editor");
                return;
            }

            var itemId = camoEditor.ItemId;
            if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                var decalsInfo = itemsWithDecals.DecalsInfo;
                if (decalsInfo.Count == 0)
                {
                    ItemsWithDecals.Remove(itemId);
                    RemoveDecalsFile(itemId);
                    LoggerInstance.LogInfo($"CloseCamoEditor: {itemId} remove decals");
                }
                else
                {
                    WriteDecalsToFile(itemId, decalsInfo);
                    LoggerInstance.LogInfo($"CloseCamoEditor: {itemId} rewrite decals");
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
