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
using EFT;
using EFT.AssetsManager;
using EFT.InventoryLogic;
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

// TODO add reset button right to every changed field

using BigPlugin = SevenBoldPencil.WeaponCamoAndStickers.Plugin;
using CamoEditorResources = SevenBoldPencil.WeaponCamoAndStickers.CamoEditorResources;
using DecalTextureType = SevenBoldPencil.WeaponCamoAndStickers.DecalTextureType;
using SystemObject = System.Object;

namespace SevenBoldPencil.MaterialEditor
{
    public class ItemsWithMaterials
    {
        // yes, there can be multiple items with same Id,
        // for example when you open item preview of weapon you already hold in hands,
        // or when hideout shooting range clones weapon (we pretend that they have the same Id)
        public Dictionary<int, ItemWithMaterials> Items; // TODO iterating dict is probably not the best idea, but list in annoying
        public MaterialsInfo MaterialsInfo;
    }

    public class ItemWithMaterials
    {
        public Dictionary<string, TargetMaterial> Materials;
    }

    public class TargetMaterial
    {
        public MaterialPropertyBlock PropertyBlock;
        public List<(Renderer, int)> Renderers;
    }

    public class MaterialsInfo
    {
        public const int CurrentSchemaVersion = 0;

        public int SchemaVersion;
        public long SaveTime;
        public Dictionary<string, MaterialInfo> Materials;
    }

    public class MaterialInfo
    {
        public string Texture;
        public Vector4 TextureUV;
        public Vector3 ColorHSV;
		public float Glossness;
		public float Specularness;

        public MaterialInfo GetCopy() => (MaterialInfo)MemberwiseClone();
    }

    public class MaterialPreset
    {
        public int SchemaVersion;
        public MaterialInfo Material;
    }

    [BepInPlugin("7Bpencil.MaterialEditor", "7Bpencil.MaterialEditor", "1.1.0")]
    [BepInDependency("7Bpencil.WeaponCamoAndStickers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static readonly int _MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int _MainTex_ST = Shader.PropertyToID("_MainTex_ST");
        public static readonly int _Color = Shader.PropertyToID("_Color");
        public static readonly int _Glossness = Shader.PropertyToID("_Specularness"); // yes, its swapped in the BSG shader
        public static readonly int _Specularness = Shader.PropertyToID("_Glossness");
        public static readonly int _BumpTiling = Shader.PropertyToID("_BumpTiling"); // this is used to tile main texture without tiling normals

        public static Plugin Instance;

		public ManualLogSource LoggerInstance;

        private string MaterialPresetsDir;
        private string ItemsDir;
        private CamoEditorResources CamoEditorResources;

        private Dictionary<string, MaterialPreset> MaterialPresets;
        private Dictionary<string, ItemsWithMaterials> ItemsWithMaterials;
        private HashSet<Renderer> PatchedRenderers;
        private Dictionary<string, string> Clones;
        private Dictionary<ResourceKey, string> ResourceKeyToItem;
        private Dictionary<int, string> InstanceIdToItemId;

        private Option<CamoEditor> CamoEditor;
        private bool IsCamoEditorWaitingForWeaponPreview;

        public bool IsFikaSupportEnabled;
        public bool IsFikaHeadless;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            MaterialPresetsDir = Path.Combine(assemblyDir, "presets-materials");
            ItemsDir = Path.Combine(assemblyDir, "items-materials");
            CamoEditorResources = new TypedFieldInfo<BigPlugin, CamoEditorResources>("CamoEditorResources").Get(BigPlugin.Instance);

            MaterialPresets = LoadMaterialPresets();
            ItemsWithMaterials = LoadItemsWithMaterials();
            PatchedRenderers = new();
            Clones = new();
            ResourceKeyToItem = new();
            InstanceIdToItemId = new();

            new Patch_PoolManagerClass_CreateItemAsync().Enable();
            new Patch_PoolManagerClass_method_2().Enable();
            new Patch_AssetPoolObject_ReturnToPool().Enable();
            new Patch_AssetPoolObject_OnDestroy().Enable();
            new Patch_ItemUiContext_GetItemContextInteractions().Enable();
            new Patch_WeaponModdingScreen_method_6().Enable();
            new Patch_GClass2304_smethod_0().Enable();
            new Patch_WeaponPreview_Class3271_method_1().Enable();
            new Patch_WeaponModdingScreen_Close().Enable();
            new Patch_GClass3380_smethod_2().Enable();
            new Patch_GClass928_GetItemHash().Enable();
            new Patch_GClass928_smethod_1().Enable();
            new Patch_HotObject_SetTemperatureToRenderer().Enable();
            new Patch_RainCondensator_OnEnable().Enable();
            new Patch_RainCondensator_UpdateValues().Enable();
            new Patch_RainCondensator_OnDisable().Enable();

            TryEnableFikaSupport(assemblyDir);

            // TODO
            // should we color only item itself, or all subitems too?
        }

        public void TryEnableFikaSupport(string mainAssemblyDir)
        {
            if (!Chainloader.PluginInfos.ContainsKey("com.fika.core"))
            {
                return;
            }

            var fikaAssemblyPath = Path.Combine(mainAssemblyDir, "7Bpencil.MaterialEditor.Fika.dll");
            if (!File.Exists(fikaAssemblyPath))
            {
                return;
            }

            var fikaAssembly = Assembly.LoadFrom(fikaAssemblyPath);
            var fikaPluginType = fikaAssembly.GetType("SevenBoldPencil.MaterialEditor.Fika.Plugin");
            var fikaPluginAwake = fikaPluginType.GetMethod("Awake");
            var fikaPlugin = Activator.CreateInstance(fikaPluginType);
            fikaPluginAwake.Invoke(fikaPlugin, null);
        }

        public Dictionary<string, MaterialPreset> LoadMaterialPresets()
        {
            var filePaths = SafeIO.GetFiles(MaterialPresetsDir, "*.json");
            var result = new Dictionary<string, MaterialPreset>();

            foreach (var filePath in filePaths)
            {
                var presetName = Path.GetFileNameWithoutExtension(filePath);
                if (SafeIO.ReadAllText(filePath).Ok(out var json, out var e))
                {
                    var preset = JsonConvert.DeserializeObject<MaterialPreset>(json);
                    result.Add(presetName, preset);
                }
                else
                {
                    Logger.LogError($"Failed to load preset: {presetName}, error: {e}");
                }
            }

            return result;
        }

        public Dictionary<string, ItemsWithMaterials> LoadItemsWithMaterials()
        {
            var filePaths = SafeIO.GetFiles(ItemsDir, "*.json");
            var result = new Dictionary<string, ItemsWithMaterials>();

            foreach (var filePath in filePaths)
            {
                var itemId = Path.GetFileNameWithoutExtension(filePath);
                if (SafeIO.ReadAllText(filePath).Ok(out var json, out var e))
                {
                    var materialsInfo = JsonConvert.DeserializeObject<MaterialsInfo>(json);
                    var itemsWithMaterials = new ItemsWithMaterials()
                    {
                        Items = new(),
                        MaterialsInfo = materialsInfo,
                    };

                    result.Add(itemId, itemsWithMaterials);
                }
                else
                {
                    Logger.LogError($"Failed to load item: {itemId}, error: {e}");
                }
            }

            return result;
        }

        public void OnCreateItemAsync(Item item)
        {
            var itemId = GetOriginalItemId(item.Id);
            if (!ItemsWithMaterials.ContainsKey(itemId))
            {
                return;
            }
            if (ResourceKeyToItem.TryGetValue(item.Prefab, out var existingItemId))
            {
                if (existingItemId == itemId)
                {
                    // yes, this does happen, for instance when player reloads his weapon (why?)
                    Logger.LogWarning($"OnCreateItemAsync: {itemId} | {item.Prefab.path}, already loading?");
                }
                else
                {
                    Logger.LogError($"OnCreateItemAsync: {itemId} | {item.Prefab.path}, collision with {existingItemId}!");
                }
            }
            else
            {
                ResourceKeyToItem.Add(item.Prefab, itemId);
                Logger.LogWarning($"OnCreateItemAsync: {itemId} | {item.Prefab.path}");
            }
        }

        public void OnCreatedItemGameObject(ResourceKey itemPrefab, GameObject itemGameObject)
        {
            if (ResourceKeyToItem.Remove(itemPrefab, out var itemId))
            {
                if (ItemsWithMaterials.TryGetValue(itemId, out var itemsWithMaterials))
                {
                    var instanceID = itemGameObject.GetInstanceID();
                    if (itemsWithMaterials.Items.ContainsKey(instanceID))
                    {
            			Logger.LogError($"OnCreatedItemGameObject: {itemId} | {itemPrefab.path} | {instanceID}, already added?");
                        return;
                    }
                    if (itemGameObject.TryGetComponent<AssetPoolObject>(out var assetPoolObject))
                    {
                        var itemWithMaterials = BuildItemOverrides(assetPoolObject);
                        PatchItem(itemWithMaterials, itemsWithMaterials.MaterialsInfo);
                        itemsWithMaterials.Items.Add(instanceID, itemWithMaterials);
                        InstanceIdToItemId.Add(instanceID, itemId);
            			Logger.LogWarning($"OnCreatedItemGameObject: {itemId} | {itemPrefab.path} | {instanceID}");
                    }
                    else
                    {
            			Logger.LogError($"OnCreatedItemGameObject: {itemId} | {itemPrefab.path} | {instanceID}, no AssetPoolObject?");
                    }
                }
            }
        }

        public ItemWithMaterials BuildItemOverrides(AssetPoolObject assetPoolObject)
        {
            var targetMaterials = new Dictionary<string, TargetMaterial>();
            foreach (var renderer in assetPoolObject.Renderers)
            {
                BuildRendererOverrides(renderer, targetMaterials);
            }
            return new()
            {
                Materials = targetMaterials,
            };
        }

        public void BuildRendererOverrides(Renderer renderer, Dictionary<string, TargetMaterial> targetMaterials)
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
    			if (IsSupportedShader(material))
                {
                    var materialName = GetMaterialName(material);
                    var pair = (renderer, i);

                    if (targetMaterials.TryGetValue(materialName, out var targetMaterial))
                    {
                        targetMaterial.Renderers.Add(pair);
                    }
                    else
                    {
                        targetMaterials.Add(materialName, new TargetMaterial()
                        {
                            PropertyBlock = new MaterialPropertyBlock(),
                            Renderers = new() { pair },
                        });
                    }
                }
            }
        }

        public string GetMaterialName(Material material)
        {
            return material.name
                .Replace("_LOD0", "")
                .Replace("_LOD1", "")
                .Replace(" (Instance)", ""); // in some cases BSG fucks it up and items get unique materials...
        }

        public bool IsSupportedShader(Material material)
        {
            if (!material)
            {
                Logger.LogWarning("this item has null material!");
                return false;
            }

            // TODO I noticed LOD1 have p0/Reflective/Specular shader, so we skip LOD1 entirely, not good, but it has different properties...
            // TODO how to support other shaders? switch with predetermined list in enum
            var materialShaderName = material.shader.name;
			if (materialShaderName == "p0/Reflective/Bumped Specular SMap" ||
                materialShaderName == "p0/Reflective/Bumped Specular SMap_Decal")
            {
                return true;
            }

            return false;
        }

        public Dictionary<string, MaterialInfo> GetOriginalMaterials(AssetPoolObject assetPoolObject)
        {
            var originals = new Dictionary<string, MaterialInfo>();
            foreach (var renderer in assetPoolObject.Renderers)
            {
                GetOriginalMaterials(renderer, originals);
            }
            return originals;
        }

        public void GetOriginalMaterials(Renderer renderer, Dictionary<string, MaterialInfo> originalMaterials)
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
    			if (IsSupportedShader(material))
                {
                    var materialName = GetMaterialName(material);
                    if (!originalMaterials.ContainsKey(materialName))
                    {
                        originalMaterials.Add(materialName, GetMaterialInfo(material));
                    }
                }
            }
        }

        public MaterialInfo GetMaterialInfo(Material material)
        {
            return new MaterialInfo()
            {
                Texture = "",
                TextureUV = material.GetVector(_MainTex_ST),
                ColorHSV = material.GetColor(_Color).RGBAtoHSV(),
                Glossness = material.GetFloat(_Glossness),
                Specularness = material.GetFloat(_Specularness),
            };
        }

        public void PatchItem(ItemWithMaterials item, MaterialsInfo materialsInfo)
        {
            foreach (var (materialName, materialInfo) in materialsInfo.Materials)
            {
                if (item.Materials.TryGetValue(materialName, out var targetMaterial))
                {
                    ApplyAllOverrides(targetMaterial, materialInfo);
                    Logger.LogWarning($"Patch: {materialName} | {targetMaterial.Renderers.Count}");
                }
                else
                {
                    Logger.LogError($"Patch: {materialName}, failure");
                }
            }
        }

        public void OnItemDestroyed(AssetPoolObject assetPoolObject)
        {
            var instanceID = assetPoolObject.gameObject.GetInstanceID();
            if (!InstanceIdToItemId.Remove(instanceID, out var itemId))
            {
                return;
            }

            if (!ItemsWithMaterials.TryGetValue(itemId, out var itemsWithMaterials))
            {
    			Logger.LogError($"OnItemDestroyed: {itemId} | {instanceID}, not registered item?");
                return;
            }

            if (!itemsWithMaterials.Items.Remove(instanceID, out var itemWithMaterials))
            {
    			Logger.LogError($"OnItemDestroyed: {itemId} | {instanceID}, not registered clone?");
                return;
            }

            foreach (var (materialName, materialInfo) in itemsWithMaterials.MaterialsInfo.Materials)
            {
                ResetMaterial(itemWithMaterials, materialName, materialInfo);
            }

			Logger.LogWarning($"OnItemDestroyed: {itemId} | {instanceID}");
        }

        public bool IsPatchedRenderer(Renderer renderer)
        {
            return PatchedRenderers.Contains(renderer);
        }

        public void OnWeaponPreviewOpened(Item item, AssetPoolObject assetPoolObject)
        {
            var itemId = GetOriginalItemId(item.Id);
			Logger.LogInfo($"OnWeaponPreviewOpened: {itemId}");
			if (IsCamoEditorWaitingForWeaponPreview)
			{
				SetupCamoEditor(item, assetPoolObject);
			}
        }

        public void SetupCamoEditor(Item item, AssetPoolObject assetPoolObject)
        {
            var items = GetOrBuildItemWithAllItSlots(item, assetPoolObject);
            CamoEditor = new(new CamoEditor()
            {
                Plugin = this,
                BigPlugin = BigPlugin.Instance,
                CamoEditorResources = CamoEditorResources,
                Items = items,
                IsOpened = false,
                CurrentPresetName = "",
                IsCurrentPresetNameValid = false,
                CurrentlyEditedOverride = default,
                LinkedOverrides = new(),
                IsColorPickerOpened = false,
                DecalTypeMenu = DecalTextureType.Camo,
                WindowRect = SevenBoldPencil.MaterialEditor.CamoEditor.GetDefaultWindowRect()
            });
        }

        public List<CamoEditorItem> GetOrBuildItemWithAllItSlots(Item item, AssetPoolObject assetPoolObject)
        {
            List<CamoEditorItem> result;

            if (assetPoolObject.ContainerCollectionView != null)
            {
                var containerBones = assetPoolObject.ContainerCollectionView.ContainerBones;
                result = new(containerBones.Count + 1);
                result.Add(GetOrBuildItem(item, assetPoolObject));
                foreach (var (container, containerData) in containerBones)
                {
                    // empty slots or slots with invisible items have nulls (soft armor, helmet plates, etc)
                    if (containerData.Item == null)
                    {
                        continue;
                    }
                    if (!containerData.ItemView)
                    {
                        continue;
                    }
                    if (containerData.ItemView.TryGetComponent<AssetPoolObject>(out var subItemAssetPoolObject))
                    {
                        result.Add(GetOrBuildItem(containerData.Item, subItemAssetPoolObject));
                    }
                }
            }
            else
            {
                result = new(1);
                result.Add(GetOrBuildItem(item, assetPoolObject));
            }

            return result;
        }

        public CamoEditorItem GetOrBuildItem(Item item, AssetPoolObject assetPoolObject)
        {
            var itemId = GetOriginalItemId(item.Id);
            var instanceID = assetPoolObject.gameObject.GetInstanceID();
            var itemWithMaterials = GetOrBuildItemWithMaterials(itemId, instanceID, assetPoolObject);
            var originalMaterials = GetOriginalMaterials(assetPoolObject);

            Logger.LogInfo($"SetupCamoEditor: {itemId}");

            return new()
            {
                Name = GClass2348.Localized(item.Name),
                ItemId = itemId,
                InstanceID = instanceID,
                ItemWithMaterials = itemWithMaterials,
                OriginalMaterials = originalMaterials,
            };
        }

        public ItemWithMaterials GetOrBuildItemWithMaterials(string itemId, int instanceID, AssetPoolObject assetPoolObject)
        {
            if (ItemsWithMaterials.TryGetValue(itemId, out var itemsWithMaterials) &&
                itemsWithMaterials.Items.TryGetValue(instanceID, out var itemWithMaterials))
            {
                return itemWithMaterials;
            }

            return BuildItemOverrides(assetPoolObject);
        }

        public void WaitForWeaponPreview()
        {
			IsCamoEditorWaitingForWeaponPreview = true;
        }

        public bool IsWaitingForWeaponPreview()
        {
            return IsCamoEditorWaitingForWeaponPreview;
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
                Logger.LogWarning($"CloseCamoEditor: tried to close uninitialized decal editor");
                return;
            }

            foreach (var item in camoEditor.Items)
            {
                if (GetMaterialsInfo(item.ItemId).Some(out var materialsInfo))
                {
                    if (materialsInfo.Materials.Count == 0)
                    {
                        ItemsWithMaterials.Remove(item.ItemId);
                        InstanceIdToItemId.Remove(item.InstanceID);
                        RemoveMaterialsFile(item.ItemId);
                        Logger.LogInfo($"CloseCamoEditor: {item.ItemId} remove materials");
                    }
                    else
                    {
                        materialsInfo.SaveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        WriteMaterialsToFile(item.ItemId, materialsInfo);
                        Logger.LogInfo($"CloseCamoEditor: {item.ItemId} rewrite materials");
                    }
                }
            }

            BigPlugin.Instance.SaveClosedTexturesDirectoriesToDisk();
            BigPlugin.Instance.SaveFavouriteTexturesToDisk();

            camoEditor.Destroy();
            CamoEditor = default;
        }

        public void WriteMaterialsToFile(string itemId, MaterialsInfo materialsInfo)
        {
            var json = JsonConvert.SerializeObject(materialsInfo, Formatting.Indented);
            var filePath = GetItemFilePath(itemId);
            SafeIO.WriteAllTextAsync(filePath, json);
        }

        public void RemoveMaterialsFile(string itemId)
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

        public void WriteMaterialPresetToFile(string presetName, MaterialPreset preset)
        {
            var json = JsonConvert.SerializeObject(preset, Formatting.Indented);
            var filePath = GetMaterialPresetFilePath(presetName);
            SafeIO.WriteAllTextAsync(filePath, json);
        }

        public void DeleteMaterialPreset(string presetName)
        {
            if (MaterialPresets.Remove(presetName))
            {
                var filePath = GetMaterialPresetFilePath(presetName);
                SafeIO.DeleteFile(filePath);
            }
        }

        public string GetMaterialPresetFilePath(string presetName)
        {
            var fileName = $"{presetName}.json";
            var filePath = Path.Combine(MaterialPresetsDir, fileName);
            return filePath;
        }

        public void OnGUI()
        {
            if (CamoEditor.Some(out var camoEditor))
            {
                camoEditor.DrawWindow();
            }
        }

        public Option<MaterialsInfo> GetMaterialsInfo(string itemId)
        {
            if (ItemsWithMaterials.TryGetValue(itemId, out var itemsWithMaterials))
            {
                return new(itemsWithMaterials.MaterialsInfo);
            }

            return default;
        }

        public Option<MaterialInfo> GetMaterialInfo(string itemId, string materialName)
        {
            if (!ItemsWithMaterials.TryGetValue(itemId, out var itemsWithMaterials))
            {
                return default;
            }
            if (!itemsWithMaterials.MaterialsInfo.Materials.TryGetValue(materialName, out var materialInfo))
            {
                return default;
            }

            return new(materialInfo);
        }

        public int GetMaterialPresetsCount()
        {
            return MaterialPresets.Count;
        }

        public Dictionary<string, MaterialPreset>.KeyCollection GetMaterialPresetNames()
        {
            return MaterialPresets.Keys;
        }

        public void OverrideMaterial(ItemWithMaterials itemWithMaterials, Dictionary<string, MaterialInfo> originalMaterials, string itemId, int instanceID, string materialName)
        {
            if (!originalMaterials.TryGetValue(materialName, out var originalMaterial))
            {
                Logger.LogError($"OverrideMaterial: {itemId} {instanceID} {materialName}, no original material?");
                return;
            }

            if (ItemsWithMaterials.ContainsKey(itemId))
            {
                var itemsWithMaterials = ItemsWithMaterials[itemId];
                var materials = itemsWithMaterials.MaterialsInfo.Materials;
                if (materials.ContainsKey(materialName))
                {
                    Logger.LogWarning($"OverrideMaterial: {itemId} {instanceID} {materialName}, already overriden");
                    return;
                }

                materials.Add(materialName, originalMaterial.GetCopy());
            }
            else
            {
                var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var itemsWithMaterials = new ItemsWithMaterials()
                {
                    Items = new() { { instanceID, itemWithMaterials } },
                    MaterialsInfo = new()
                    {
                        SchemaVersion = MaterialsInfo.CurrentSchemaVersion,
                        SaveTime = time,
                        Materials = new() { { materialName, originalMaterial.GetCopy() } }
                    }
                };

                ItemsWithMaterials.Add(itemId, itemsWithMaterials);
                InstanceIdToItemId.Add(instanceID, itemId);
            }
        }

        public void ResetMaterial(string itemId, string materialName)
        {
            if (!ItemsWithMaterials.TryGetValue(itemId, out var itemsWithMaterials))
            {
                return;
            }
            if (!itemsWithMaterials.MaterialsInfo.Materials.Remove(materialName, out var materialInfo))
            {
                return;
            }
            foreach (var itemWithMaterials in itemsWithMaterials.Items.Values)
            {
                ResetMaterial(itemWithMaterials, materialName, materialInfo);
            }
        }

        public void ResetMaterial(ItemWithMaterials itemWithMaterials, string materialName, MaterialInfo materialInfo)
        {
            if (itemWithMaterials.Materials.TryGetValue(materialName, out var targetMaterial))
            {
                ResetMaterial(targetMaterial, materialInfo);
            }
        }

        public void ResetMaterial(TargetMaterial targetMaterial, MaterialInfo materialInfo)
        {
            targetMaterial.PropertyBlock.Clear();
            if (!string.IsNullOrWhiteSpace(materialInfo.Texture))
            {
                BigPlugin.Instance.ReleaseDecalTextureAsset(targetMaterial, materialInfo.Texture);
            }
            foreach (var (renderer, index) in targetMaterial.Renderers)
            {
                renderer.SetPropertyBlock(null, index);
                PatchedRenderers.Remove(renderer);
            }
        }

        public void ApplyAllOverrides(TargetMaterial targetMaterial, MaterialInfo materialInfo)
        {
            var propertyBlock = targetMaterial.PropertyBlock;

            propertyBlock.SetColor(_Color, materialInfo.ColorHSV.HSVtoRGBA());
            propertyBlock.SetFloat(_Glossness, materialInfo.Glossness);
            propertyBlock.SetFloat(_Specularness, materialInfo.Specularness);
            propertyBlock.SetVector(_MainTex_ST, materialInfo.TextureUV);
            propertyBlock.SetFloat(_BumpTiling, 1f / materialInfo.TextureUV.x);
            ApplyPropertyBlock(targetMaterial);

            if (!string.IsNullOrWhiteSpace(materialInfo.Texture))
            {
                BigPlugin.Instance.AcquireDecalTextureAsset(targetMaterial, materialInfo.Texture, MaterialChangeTexture, MaterialChangeTexture);
            }
        }

        public void ApplyPropertyBlock(TargetMaterial targetMaterial)
        {
            var propertyBlock = targetMaterial.PropertyBlock;
            foreach (var (renderer, index) in targetMaterial.Renderers)
            {
                renderer.SetPropertyBlock(propertyBlock, index);
                PatchedRenderers.Add(renderer);
            }
        }

        public void ChangeColor(string itemId, string materialName, Vector3 colorHSV)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.ColorHSV = colorHSV,
                static (propertyBlock, materialInfo) => propertyBlock.SetColor(_Color, materialInfo.ColorHSV.HSVtoRGBA())
            );
        }

        public void ChangeGlossness(string itemId, string materialName, float glossness)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.Glossness = glossness,
                static (propertyBlock, materialInfo) => propertyBlock.SetFloat(_Glossness, materialInfo.Glossness)
            );
        }

        public void ChangeSpecularness(string itemId, string materialName, float specularness)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.Specularness = specularness,
                static (propertyBlock, materialInfo) => propertyBlock.SetFloat(_Specularness, materialInfo.Specularness)
            );
        }

        public void ChangeTextureUV(string itemId, string materialName, Vector4 textureUV)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.TextureUV = textureUV,
                static (propertyBlock, materialInfo) =>
                {
                    propertyBlock.SetVector(_MainTex_ST, materialInfo.TextureUV);
                    propertyBlock.SetFloat(_BumpTiling, 1f / materialInfo.TextureUV.x);
                }
            );
        }

        // notice that we modify material on all items
        public void ModifyMaterialOnItems(
            string itemId, string materialName,
            Action<MaterialInfo> changeMaterial,
            Action<MaterialPropertyBlock, MaterialInfo> changePropertyBlock)
        {
            var itemsWithMaterials = ItemsWithMaterials[itemId];
            var materialInfo = itemsWithMaterials.MaterialsInfo.Materials[materialName];
            changeMaterial(materialInfo);
            foreach (var itemWithMaterials in itemsWithMaterials.Items.Values)
            {
                var targetMaterial = itemWithMaterials.Materials[materialName];
                changePropertyBlock(targetMaterial.PropertyBlock, materialInfo);
                ApplyPropertyBlock(targetMaterial);
            }
        }

        // textures as always require special handling
        public void ChangeTexture(string itemId, string materialName, string textureName)
        {
            var itemsWithMaterials = ItemsWithMaterials[itemId];
            var materialInfo = itemsWithMaterials.MaterialsInfo.Materials[materialName];

            var oldTextureName = materialInfo.Texture;
            materialInfo.Texture = textureName;

            foreach (var itemWithMaterials in itemsWithMaterials.Items.Values)
            {
                var targetMaterial = itemWithMaterials.Materials[materialName];
                if (!string.IsNullOrWhiteSpace(oldTextureName))
                {
                    BigPlugin.Instance.ReleaseDecalTextureAsset(targetMaterial, oldTextureName);
                }
                if (!string.IsNullOrWhiteSpace(materialInfo.Texture))
                {
                    BigPlugin.Instance.AcquireDecalTextureAsset(targetMaterial, materialInfo.Texture, MaterialChangeTexture, MaterialChangeTexture);
                }
            }
        }

        public void MaterialChangeTexture(SystemObject key, Texture texture)
        {
            if (key is TargetMaterial targetMaterial)
            {
                targetMaterial.PropertyBlock.SetTexture(_MainTex, texture);
                ApplyPropertyBlock(targetMaterial);
            }
        }

        // TODO I forget to clean clone dict in OnItemDestroy...
        public void OnCloneItem(string originalId, string cloneId)
        {
            // when user tries weapon in hideout shooting range,
            // all his gear gets copied to new items to preserve
            // original durability/ammo count/etc,
            // so we have to clone decals ourselves
            if (ItemsWithMaterials.ContainsKey(originalId))
            {
                if (originalId == cloneId)
                {
                    // yes, it does happen a lot, no idea why
                    Logger.LogWarning($"OneCloneItem: {originalId} same id");
                    return;
                }
                if (Clones.TryAdd(cloneId, originalId))
                {
                    Logger.LogInfo($"OnCloneItem: original: {originalId}, clone: {cloneId}, clones recorded: {Clones.Count}");
                }
                else
                {
                    Logger.LogError($"OnCloneItem: original: {originalId}, clone: {cloneId}, already added???");
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

        public void SaveMaterialIntoPreset(string itemId, string materialName, string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                return;
            }
            if (!GetMaterialInfo(itemId, materialName).Some(out var materialInfo))
            {
                return;
            }
            if (MaterialPresets.TryGetValue(presetName, out var oldPresetMaterialInfo))
            {
                oldPresetMaterialInfo.Material = materialInfo.GetCopy();
                WriteMaterialPresetToFile(presetName, oldPresetMaterialInfo);
            }
            else
            {
                var newPresetMaterialInfo = new MaterialPreset()
                {
                    SchemaVersion = MaterialsInfo.CurrentSchemaVersion,
                    Material = materialInfo.GetCopy(),
                };
                MaterialPresets.Add(presetName, newPresetMaterialInfo);
                WriteMaterialPresetToFile(presetName, newPresetMaterialInfo);
            }
        }

        public void SwitchToMaterialPreset(string itemId, string materialName, string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                return;
            }
            if (!GetMaterialInfo(itemId, materialName).Some(out var materialInfo))
            {
                return;
            }
            if (!MaterialPresets.TryGetValue(presetName, out var preset))
            {
                return;
            }

            ForEveryMaterialOnItem
            (
                itemId, materialName,
                (targetMaterial, materialInfo) => ResetMaterial(targetMaterial, materialInfo)
            );

            materialInfo.Texture = preset.Material.Texture;
            materialInfo.TextureUV = preset.Material.TextureUV;
            materialInfo.ColorHSV = preset.Material.ColorHSV;
            materialInfo.Glossness = preset.Material.Glossness;
            materialInfo.Specularness = preset.Material.Specularness;

            ForEveryMaterialOnItem
            (
                itemId, materialName,
                (targetMaterial, materialInfo) => ApplyAllOverrides(targetMaterial, materialInfo)
            );
        }

        public void ForEveryMaterialOnItem(string itemId, string materialName, Action<TargetMaterial, MaterialInfo> changeMaterial)
        {
            var itemsWithMaterials = ItemsWithMaterials[itemId];
            var materialInfo = itemsWithMaterials.MaterialsInfo.Materials[materialName];
            foreach (var itemWithMaterials in itemsWithMaterials.Items.Values)
            {
                var targetMaterial = itemWithMaterials.Materials[materialName];
                changeMaterial(targetMaterial, materialInfo);
            }
        }

        public Dictionary<string, MaterialsInfo> SnapshotLocalMaterials()
        {
            var snapshot = new Dictionary<string, MaterialsInfo>();
    		if (!TarkovApplication.Exist(out var tarkovApplication))
            {
                return snapshot;
            }

            // copies all items inside player equipment (on hands/sling/holster, inside backpack, rig, etc)
            var profile = tarkovApplication.Session.Profile;
            var equipmentItems = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment);

            foreach (var item in equipmentItems)
            {
                if (ItemsWithMaterials.TryGetValue(item.Id, out var itemsWithMaterials))
                {
                    snapshot[item.Id] = CopyMaterialsInfo(itemsWithMaterials.MaterialsInfo);
                }
            }

            return snapshot;
        }

        public MaterialsInfo CopyMaterialsInfo(MaterialsInfo source)
        {
            var destination = new MaterialsInfo()
            {
                Materials = new Dictionary<string, MaterialInfo>(source.Materials.Count)
            };

            CopyMaterialsInfo(source, destination);
            return destination;
        }

        public void CopyMaterialsInfo(MaterialsInfo source, MaterialsInfo destination)
        {
            destination.SchemaVersion = source.SchemaVersion;
            destination.SaveTime = source.SaveTime;
            destination.Materials.Clear();
            foreach (var (materialName, materialInfo) in source.Materials)
            {
                destination.Materials.Add(materialName, materialInfo.GetCopy());
            }
        }

        public void IngestRemoteMaterials(Dictionary<string, MaterialsInfo> remoteMaterials)
        {
            foreach (var (itemId, materialsInfo) in remoteMaterials)
            {
                IngestRemoteMaterials(itemId, materialsInfo);
            }
        }

        public void IngestRemoteMaterials(string itemId, MaterialsInfo remoteMaterialsInfo)
        {
            if (remoteMaterialsInfo.Materials.Count == 0)
            {
                Logger.LogWarning($"IngestRemoteMaterials: {itemId} has no materials, but was replicated?");
                return;
            }

            // TODO not sure if copying remoteMaterialsInfo is necessary
            if (ItemsWithMaterials.ContainsKey(itemId))
            {
                // pick newer version
                var itemsWithMaterials = ItemsWithMaterials[itemId];
                var materialsInfo = itemsWithMaterials.MaterialsInfo;
                if (materialsInfo.SaveTime >= remoteMaterialsInfo.SaveTime)
                {
                    Logger.LogInfo($"IngestRemoteMaterials: {itemId}, mine is newer");
                    return;
                }

                CopyMaterialsInfo(remoteMaterialsInfo, materialsInfo);
                WriteMaterialsToFile(itemId, materialsInfo);
                Logger.LogInfo($"IngestRemoteMaterials: {itemId}, his is newer, already spawned count: {itemsWithMaterials.Items.Count}");
            }
            else
            {
                var materialsInfo = CopyMaterialsInfo(remoteMaterialsInfo);
                var itemsWithMaterials = new ItemsWithMaterials()
                {
                    Items = new(),
                    MaterialsInfo = materialsInfo,
                };

                ItemsWithMaterials.Add(itemId, itemsWithMaterials);
                WriteMaterialsToFile(itemId, materialsInfo);
                Logger.LogInfo($"IngestRemoteMaterials: {itemId}, new");
            }
        }

    }
}
