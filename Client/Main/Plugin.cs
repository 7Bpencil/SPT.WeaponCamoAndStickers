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

// TODO fde is 0.09 hue and 6.7 glossness
// TODO add separate CHANGE COLOR button in interaction menu
// TODO I guess we track when item gets created first time, but not it comes from pool

using BigPlugin = SevenBoldPencil.WeaponCamoAndStickers.Plugin;
using CamoEditorResources = SevenBoldPencil.WeaponCamoAndStickers.CamoEditorResources;
using DecalTextureType = SevenBoldPencil.WeaponCamoAndStickers.DecalTextureType;
using SystemObject = System.Object;

namespace SevenBoldPencil.ChangeEquipmentColor
{
    public class ItemsWithDecals
    {
        // yes, there can be multiple items with same Id,
        // for example when you open item preview of weapon you already hold in hands,
        // or when hideout shooting range clones weapon (we pretend that they have the same Id)
        public Dictionary<int, ItemWithDecals> Items; // TODO iterating dict is probably not the best idea, but list in annoying
        public MaterialsInfo MaterialsInfo;
    }

    public class ItemWithDecals
    {
        public AssetPoolObject Item;
        public Dictionary<string, MaterialOverride> Overrides;
    }

    public class MaterialOverride
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

        public MaterialInfo GetCopy()
        {
            // this is enough for now
            return (MaterialInfo)MemberwiseClone();
        }
    }

    [BepInPlugin("7Bpencil.ChangeEquipmentColor", "7Bpencil.ChangeEquipmentColor", "1.0.0")]
    [BepInDependency("7Bpencil.WeaponCamoAndStickers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static readonly int _MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int _MainTex_ST = Shader.PropertyToID("_MainTex_ST");
        public static readonly int _Color = Shader.PropertyToID("_Color");
        public static readonly int _Glossness = Shader.PropertyToID("_Specularness"); // yes, its swapped in the BSG shader
        public static readonly int _Specularness = Shader.PropertyToID("_Glossness");

        private const double SaveLagTime = 60;

        public static Plugin Instance;

		public ManualLogSource LoggerInstance;

        private string PresetsDir;
        private string ItemsDir;
        private CamoEditorResources CamoEditorResources;

        private Dictionary<string, ItemsWithDecals> ItemsWithDecals;
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
            PresetsDir = Path.Combine(assemblyDir, "presets-materials");
            ItemsDir = Path.Combine(assemblyDir, "items-materials");
            CamoEditorResources = new TypedFieldInfo<BigPlugin, CamoEditorResources>("CamoEditorResources").Get(BigPlugin.Instance);

            ItemsWithDecals = LoadItemsWithMaterials(ItemsDir);
            Clones = new();
            ResourceKeyToItem = new();
            InstanceIdToItemId = new();

            new Patch_PoolManagerClass_CreateItemAsync().Enable();
            new Patch_PoolManagerClass_method_2().Enable();
            new Patch_AssetPoolObject_ReturnToPool().Enable();
            new Patch_AssetPoolObject_OnDestroy().Enable();
            new Patch_ItemUiContext_GetItemContextInteractions().Enable();
            new Patch_WeaponModdingScreen_method_6().Enable();
            new Patch_WeaponPreview_Class3271_method_1().Enable();
            new Patch_WeaponModdingScreen_Close().Enable();
            new Patch_GClass3380_smethod_2().Enable();
            new Patch_GClass928_GetItemHash().Enable();

            TryEnableFikaSupport(assemblyDir);

            // TODO
            // should we color only item itself, or all subitems too?

            // TODO
            // Do not release without separation of items per profile
            // Also since data per item is small, keep all of it in giant json (or maybe binary with some compression?),
            // save that json after 1 min since last modification similar to transparent scopes,
            // same with presets

            // TODO limit scope of items to weapons, mods, equipment
        }

        public void TryEnableFikaSupport(string mainAssemblyDir)
        {
            if (!Chainloader.PluginInfos.ContainsKey("com.fika.core"))
            {
                return;
            }

            var fikaAssemblyPath = Path.Combine(mainAssemblyDir, "7Bpencil.ChangeEquipmentColor.Fika.dll");
            if (!File.Exists(fikaAssemblyPath))
            {
                return;
            }

            var fikaAssembly = Assembly.LoadFrom(fikaAssemblyPath);
            var fikaPluginType = fikaAssembly.GetType("SevenBoldPencil.ChangeEquipmentColor.Fika.Plugin");
            var fikaPluginAwake = fikaPluginType.GetMethod("Awake");
            var fikaPlugin = Activator.CreateInstance(fikaPluginType);
            fikaPluginAwake.Invoke(fikaPlugin, null);
        }

        public Dictionary<string, ItemsWithDecals> LoadItemsWithMaterials(string directoryPath)
        {
            var filePaths = SafeIO.GetFiles(directoryPath, "*.json");
            var result = new Dictionary<string, ItemsWithDecals>();

            foreach (var filePath in filePaths)
            {
                var itemId = Path.GetFileNameWithoutExtension(filePath);
                if (SafeIO.ReadAllText(filePath).Ok(out var json, out var e))
                {
                    var materialsInfo = JsonConvert.DeserializeObject<MaterialsInfo>(json);
                    UpgradeOldVersionsOfDecalsInfo(materialsInfo);
                    var itemsWithDecals = new ItemsWithDecals()
                    {
                        Items = new(),
                        MaterialsInfo = materialsInfo,
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

        public static void UpgradeOldVersionsOfDecalsInfo(MaterialsInfo materialsInfo)
        {

        }

        public void OnCreateItemAsync(Item item)
        {
            var itemId = GetOriginalItemId(item.Id);
            if (!ItemsWithDecals.ContainsKey(itemId))
            {
                return;
            }
            if (ResourceKeyToItem.TryAdd(item.Prefab, itemId))
            {
                Logger.LogWarning($"OnCreateItemAsync: {itemId} | {item.Prefab.path}");
            }
            else
            {
                Logger.LogError($"OnCreateItemAsync: {itemId} | {item.Prefab.path}, collision!");
            }
        }

        public void OnCreatedItemGameObject(ResourceKey itemPrefab, GameObject itemGameObject)
        {
            if (ResourceKeyToItem.Remove(itemPrefab, out var itemId))
            {
                if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
                {
                    var instanceID = itemGameObject.GetInstanceID();
                    if (itemsWithDecals.Items.ContainsKey(instanceID))
                    {
            			Logger.LogWarning($"OnCreatedItemGameObject: {itemId} | {itemPrefab.path} | {instanceID}, already added?");
                        return;
                    }
                    if (itemGameObject.TryGetComponent<AssetPoolObject>(out var assetPoolObject))
                    {
                        var itemWithDecals = BuildItemOverrides(assetPoolObject);
                        PatchItem(itemWithDecals, itemsWithDecals.MaterialsInfo);
                        itemsWithDecals.Items.Add(instanceID, itemWithDecals);
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

        public ItemWithDecals BuildItemOverrides(AssetPoolObject assetPoolObject)
        {
            var overrides = new Dictionary<string, MaterialOverride>();

            foreach (var renderer in assetPoolObject.Renderers)
            {
                BuildRendererOverrides(renderer, overrides);
            }
#if DEBUG
            foreach (var (materialName, materialOverrides) in overrides)
            {
                Logger.LogWarning($"BuildItemOverrides: {materialName} | {materialOverrides.Renderers.Count}");
            }
#endif
            return new()
            {
                Item = assetPoolObject,
                Overrides = overrides,
            };
        }

        public void BuildRendererOverrides(Renderer renderer, Dictionary<string, MaterialOverride> totalOverrides)
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
    			if (IsSupportedShader(material))
                {
                    var materialName = GetMaterialName(material);
                    var pair = (renderer, i);

                    if (totalOverrides.TryGetValue(materialName, out var existingOverrides))
                    {
                        existingOverrides.Renderers.Add(pair);
                    }
                    else
                    {
                        totalOverrides.Add(materialName, new MaterialOverride()
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
            // TODO I noticed LOD1 have p0/Reflective/Specular shader, so we skip LOD1 entirely, not good
            // TODO how to support other shaders? just a switch lol with predetermined list in enum
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
                        originalMaterials.Add(materialName, new MaterialInfo()
                        {
                            Texture = "",
                            TextureUV = material.GetVector(_MainTex_ST),
                            ColorHSV = material.GetColor(_Color).RGBAtoHSV(),
                            Glossness = material.GetFloat(_Glossness),
                            Specularness = material.GetFloat(_Specularness),
                        });
                    }
                }
            }
        }

        public void PatchItem(ItemWithDecals item, MaterialsInfo materialsInfo)
        {
            foreach (var (materialName, materialInfo) in materialsInfo.Materials)
            {
                if (item.Overrides.TryGetValue(materialName, out var materialOverride))
                {
                    ApplyAllOverrides(materialOverride, materialInfo);
                    Logger.LogWarning($"Patch: {materialName} | {materialOverride.Renderers.Count}");
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

            if (!ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
    			Logger.LogWarning($"OnItemDestroyed: {itemId} | {instanceID}, not registered item?");
                return;
            }

            if (!itemsWithDecals.Items.Remove(instanceID, out var itemWithDecals))
            {
    			Logger.LogError($"OnItemDestroyed: {itemId} | {instanceID}, not registered clone?");
                return;
            }

            foreach (var renderer in assetPoolObject.Renderers)
            {
                var materialsCount = renderer.sharedMaterials.Length;
                for (var i = 0; i < materialsCount; i++)
                {
                    renderer.SetPropertyBlock(null, i);
                }
            }

			Logger.LogWarning($"OnItemDestroyed: {itemId} | {instanceID}");
        }

        public void OnWeaponPreviewOpened(Item item, AssetPoolObject assetPoolObject)
        {
            // TODO limit to only some types of items
            var itemId = GetOriginalItemId(item.Id);
			Logger.LogInfo($"OnWeaponPreviewOpened: {itemId}");
			if (IsCamoEditorWaitingForWeaponPreview)
			{
				SetupCamoEditor(itemId, assetPoolObject);
			}
        }

        public void SetupCamoEditor(string itemId, AssetPoolObject assetPoolObject)
        {
            itemId = GetOriginalItemId(itemId);
            Logger.LogInfo($"SetupCamoEditor: {itemId}");

            var instanceID = assetPoolObject.gameObject.GetInstanceID();

            ItemWithDecals getItemWithDecals()
            {
                if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals) &&
                    itemsWithDecals.Items.TryGetValue(instanceID, out var itemWithDecals))
                {
                    return itemWithDecals;
                }

                return BuildItemOverrides(assetPoolObject);
            }

            var itemWithDecals = getItemWithDecals();
            var originalMaterials = GetOriginalMaterials(assetPoolObject);
            CamoEditor = new(new CamoEditor()
            {
                Plugin = this,
                CamoEditorResources = CamoEditorResources,
                ItemId = itemId,
                InstanceID = instanceID,
                ItemWithDecals = itemWithDecals,
                OriginalMaterials = originalMaterials,
                IsOpened = false,
                IsColorPickerOpened = false,
                DecalTypeMenu = DecalTextureType.Camo,
                WindowRect = SevenBoldPencil.ChangeEquipmentColor.CamoEditor.GetDefaultWindowRect()
            });
        }

        public void WaitForWeaponPreview()
        {
			IsCamoEditorWaitingForWeaponPreview = true;
        }

        public bool IsWaitingForWeaponPreview()
        {
            return IsCamoEditorWaitingForWeaponPreview;
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
            if (GetMaterialsInfo(itemId).Some(out var materialsInfo))
            {
                if (materialsInfo.Materials.Count == 0)
                {
                    ItemsWithDecals.Remove(itemId);
                    RemoveMaterialsFile(itemId);
                    Logger.LogInfo($"CloseCamoEditor: {itemId} remove materials");
                }
                else
                {
                    var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    materialsInfo.SaveTime = time;
                    WriteMaterialsToFile(itemId, materialsInfo);
                    Logger.LogInfo($"CloseCamoEditor: {itemId} rewrite materials");
                }
            }

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

        public void OnGUI()
        {
            if (CamoEditor.Some(out var camoEditor))
            {
                camoEditor.DrawWindow();
            }
        }

        public Option<MaterialsInfo> GetMaterialsInfo(string itemId)
        {
            if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                return new(itemsWithDecals.MaterialsInfo);
            }

            return default;
        }

        public MaterialInfo GetMaterialInfo(string itemId, string materialName)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            return itemsWithDecals.MaterialsInfo.Materials[materialName];
        }

        public void OverrideMaterial(ItemWithDecals itemWithDecals, Dictionary<string, MaterialInfo> originalMaterials, string itemId, int instanceID, string materialName)
        {
            if (!originalMaterials.TryGetValue(materialName, out var originalMaterial))
            {
                Logger.LogError($"OverrideMaterial: {itemId} {instanceID} {materialName}, no original material?");
                return;
            }

            if (ItemsWithDecals.ContainsKey(itemId))
            {
                var itemsWithDecals = ItemsWithDecals[itemId];
                var materials = itemsWithDecals.MaterialsInfo.Materials;
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
                var itemsWithDecals = new ItemsWithDecals()
                {
                    Items = new() { { instanceID, itemWithDecals } },
                    MaterialsInfo = new()
                    {
                        SchemaVersion = MaterialsInfo.CurrentSchemaVersion,
                        SaveTime = time,
                        Materials = new() { { materialName, originalMaterial.GetCopy() } }
                    }
                };

                ItemsWithDecals.Add(itemId, itemsWithDecals);
                InstanceIdToItemId.Add(instanceID, itemId);
            }
        }

        public void ResetMaterial(string itemId, string materialName)
        {
            if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals) &&
                itemsWithDecals.MaterialsInfo.Materials.Remove(materialName))
            {
                foreach (var itemWithDecals in itemsWithDecals.Items.Values)
                {
                    if (itemWithDecals.Overrides.TryGetValue(materialName, out var materialOverride))
                    {
                        materialOverride.PropertyBlock.Clear();
                        foreach (var (renderer, index) in materialOverride.Renderers)
                        {
                            renderer.SetPropertyBlock(null, index);
                        }
                    }
                }
            }
        }

        public void ApplyAllOverrides(MaterialOverride materialOverride, MaterialInfo materialInfo)
        {
            var propertyBlock = materialOverride.PropertyBlock;

            var color = materialInfo.ColorHSV.HSVtoRGBA();
            propertyBlock.SetColor(_Color, color);
            propertyBlock.SetFloat(_Glossness, materialInfo.Glossness);
            propertyBlock.SetFloat(_Specularness, materialInfo.Specularness);
            propertyBlock.SetVector(_MainTex_ST, materialInfo.TextureUV);
            ApplyPropertyBlock(materialOverride, propertyBlock);

            if (!string.IsNullOrWhiteSpace(materialInfo.Texture))
            {
                BigPlugin.Instance.AcquireDecalTextureAsset(materialOverride, materialInfo.Texture, MaterialChangeTexture, MaterialChangeTexture);
            }
        }

        public void ApplyPropertyBlock(MaterialOverride materialOverride, MaterialPropertyBlock propertyBlock)
        {
            foreach (var (renderer, index) in materialOverride.Renderers)
            {
                renderer.SetPropertyBlock(propertyBlock, index);
            }
        }

        public void ApplyColor(string itemId, string materialName)
        {
            ModifyMaterialOnItems(itemId, materialName, ApplyColor);
        }

        public void ApplyColor(MaterialOverride materialOverride, MaterialInfo materialInfo)
        {
            var propertyBlock = materialOverride.PropertyBlock;
            var color = materialInfo.ColorHSV.HSVtoRGBA();
            propertyBlock.SetColor(_Color, color);
            ApplyPropertyBlock(materialOverride, propertyBlock);
        }

        public void ApplyGlossness(string itemId, string materialName)
        {
            ModifyMaterialOnItems(itemId, materialName, ApplyGlossness);
        }

        public void ApplyGlossness(MaterialOverride materialOverride, MaterialInfo materialInfo)
        {
            var propertyBlock = materialOverride.PropertyBlock;
            propertyBlock.SetFloat(_Glossness, materialInfo.Glossness);
            ApplyPropertyBlock(materialOverride, propertyBlock);
        }

        public void ApplySpecularness(string itemId, string materialName)
        {
            ModifyMaterialOnItems(itemId, materialName, ApplySpecularness);
        }

        public void ApplySpecularness(MaterialOverride materialOverride, MaterialInfo materialInfo)
        {
            var propertyBlock = materialOverride.PropertyBlock;
            propertyBlock.SetFloat(_Specularness, materialInfo.Specularness);
            ApplyPropertyBlock(materialOverride, propertyBlock);
        }

        public void ApplyTextureUV(string itemId, string materialName)
        {
            ModifyMaterialOnItems(itemId, materialName, ApplyTextureUV);
        }

        public void ApplyTextureUV(MaterialOverride materialOverride, MaterialInfo materialInfo)
        {
            var propertyBlock = materialOverride.PropertyBlock;
            propertyBlock.SetVector(_MainTex_ST, materialInfo.TextureUV);
            ApplyPropertyBlock(materialOverride, propertyBlock);
        }

        public void ChangeTexture(string itemId, string materialName, MaterialInfo materialInfo, string textureName)
        {
            var oldTextureName = materialInfo.Texture;
            materialInfo.Texture = textureName;
            ApplyTexture(itemId, materialName, oldTextureName);
        }

        public void ApplyTexture(string itemId, string materialName, string oldTextureName)
        {
            ModifyMaterialOnItems(itemId, materialName, (materialOverride, materialInfo) =>
            {
                if (!string.IsNullOrWhiteSpace(materialInfo.Texture))
                {
                    BigPlugin.Instance.ReleaseDecalTextureAsset(materialOverride, oldTextureName);
                }
                BigPlugin.Instance.AcquireDecalTextureAsset(materialOverride, materialInfo.Texture, MaterialChangeTexture, MaterialChangeTexture);
            });
        }

        public void MaterialChangeTexture(SystemObject key, Texture texture)
        {
            if (key is MaterialOverride materialOverride)
            {
                var propertyBlock = materialOverride.PropertyBlock;
                propertyBlock.SetTexture(_MainTex, texture);
                ApplyPropertyBlock(materialOverride, propertyBlock);
            }
        }

        // notice that we modify material on all items
        public void ModifyMaterialOnItems(string itemId, string materialName, Action<MaterialOverride, MaterialInfo> changeMaterial)
        {
            var itemsWithDecals = ItemsWithDecals[itemId];
            var materialInfo = itemsWithDecals.MaterialsInfo.Materials[materialName];
            foreach (var itemWithDecals in itemsWithDecals.Items.Values)
            {
                var materialOverride = itemWithDecals.Overrides[materialName];
                changeMaterial(materialOverride, materialInfo);
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

        public Dictionary<string, MaterialsInfo> SnapshotLocalDecals()
        {
            var snapshot = new Dictionary<string, MaterialsInfo>();
            return snapshot;
        }

        public void IngestRemoteDecals(Dictionary<string, MaterialsInfo> remoteDecals)
        {
            foreach (var (itemId, decalInfo) in remoteDecals)
            {
                IngestRemoteDecals(itemId, decalInfo);
            }
        }

        public void IngestRemoteDecals(string itemId, MaterialsInfo remoteDecalInfo)
        {

        }

    }
}
