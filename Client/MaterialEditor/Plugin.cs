//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using EFT;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using EFT.Visual;
using EFT.UI;
using Newtonsoft.Json;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

// TODO add reset button right to every changed field

using BigPlugin = SevenBoldPencil.WeaponCamoAndStickers.Plugin;
using CamoEditorResources = SevenBoldPencil.WeaponCamoAndStickers.CamoEditorResources;
using DecalTextureType = SevenBoldPencil.WeaponCamoAndStickers.DecalTextureType;
using LoddedSkin_Proxy = SevenBoldPencil.WeaponCamoAndStickers.LoddedSkin_Proxy;
using DecalTextureFormat = SevenBoldPencil.WeaponCamoAndStickers.DecalTextureFormat;
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
        // TODO putting it here feels wrong, but I don't have better ideas
        // TODO fuck it, just do it the simple way, in most cases theres only one instance of item anyway
        // TODO there can be all sorts of sync issues, like user toggles SpecularCompensation while texture is still loading...
        public Texture OriginalTexture;
        public Option<CustomTexture> CustomTexture;
    }

    public readonly record struct CustomTexture(Texture Color, Option<RenderTexture> Combined, bool IsVideo);

    public class VideoData
    {
        public MaterialInfo MaterialInfo;
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
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion;
        public Vector3 ColorHSV;
        public Vector3 SpecColorHSV;
		public float Glossness;
		public float Specularness;
        public Vector3 ReflectColorHSV;
        public string Texture;
        public Vector4 TextureUV;
        public Vector2 SpecVals; // defined as float3 in shader, but only x and y are used
        public Vector2 DefVals; // defined as float3 in shader, but only x and y are used
        public bool CompensateSpecular;

        public Color GetColor()
        {
            return ColorHSV.HSVtoRGBA();
        }

        public Color GetSpecColor()
        {
            return SpecColorHSV.HSVtoRGBA();
        }

        public Color GetReflectColor()
        {
            return ReflectColorHSV.HSVtoRGBA();
        }

        public MaterialInfo GetCopy() => (MaterialInfo)MemberwiseClone();
    }

    public class ItemPreset
    {
        public const int CurrentSchemaVersion = 0;

        public int SchemaVersion;
        // templateId -> { index -> { materialName -> materialInfo } }
        // we can have multiple items with same template id
        // (like multiple small rails, or meme kit with big 2U flashlights),
        // also save their indices (local to their template group) to at least try to keep correct order,
        // and each item can have multiple different materials of course
        public Dictionary<string, Dictionary<int, Dictionary<string, MaterialInfo>>> Materials;
    }

    public class MaterialPreset
    {
        public const int CurrentSchemaVersion = 0;

        public int SchemaVersion;
        public MaterialInfo Material;
    }

    [BepInPlugin("7Bpencil.MaterialEditor", "7Bpencil.MaterialEditor", "1.17.1")]
    [BepInDependency("7Bpencil.WeaponCamoAndStickers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static readonly int _Color = Shader.PropertyToID("_Color");
        public static readonly int _SpecColor = Shader.PropertyToID("_SpecColor");
        public static readonly int _Glossness = Shader.PropertyToID("_Specularness"); // yes, its swapped in the BSG shader
        public static readonly int _Specularness = Shader.PropertyToID("_Glossness"); // yes, its swapped in the BSG shader
        public static readonly int _ReflectColor = Shader.PropertyToID("_ReflectColor");
        public static readonly int _MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int _MainTex_ST = Shader.PropertyToID("_MainTex_ST");
        public static readonly int _SpecVals = Shader.PropertyToID("_SpecVals");
        public static readonly int _DefVals = Shader.PropertyToID("_DefVals");
        public static readonly int _ColorTex = Shader.PropertyToID("_ColorTex");
        public static readonly int _ColorTex_ST = Shader.PropertyToID("_ColorTex_ST");
        public static readonly int _AlphaTex = Shader.PropertyToID("_AlphaTex");

        public static Plugin Instance;

		public ManualLogSource LoggerInstance;

        private string ItemPresetsDir;
        private string BotItemPresetsDir;
        private string MaterialPresetsDir;
        private string ItemsDir;
        private Material CombineTexturesMaterial;
        private CamoEditorResources CamoEditorResources;
        private Dictionary<TargetMaterial, VideoData> Videos;

        private Dictionary<string, ItemPreset> ItemPresets;
        private Dictionary<int, Dictionary<string, List<ItemPreset>>> BotItemPresets;
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
            ItemPresetsDir = Path.Combine(assemblyDir, "presets-item-materials");
            BotItemPresetsDir = Path.Combine(assemblyDir, "bot-presets");
            MaterialPresetsDir = Path.Combine(assemblyDir, "presets-materials");
            ItemsDir = Path.Combine(assemblyDir, "items-materials");
            var combineTexturesShader = new TypedFieldInfo<BigPlugin, Shader>("CombineTexturesShader").Get(BigPlugin.Instance);
            CombineTexturesMaterial = new Material(combineTexturesShader);
            CamoEditorResources = new TypedFieldInfo<BigPlugin, CamoEditorResources>("CamoEditorResources").Get(BigPlugin.Instance);
            Videos = new();

            ItemPresets = LoadItemPresets();
            BotItemPresets = LoadBotItemPresets();
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
            new Patch_WeaponPreview_Rotate().Enable();
            new Patch_ScrollTrigger_OnScroll().Enable();
            new Patch_WeaponModdingScreen_Close().Enable();
            new Patch_GClass3380_smethod_2().Enable();
            new Patch_GClass928_GetItemHash().Enable();
            new Patch_GClass928_smethod_1().Enable();
            new Patch_HotObject_SetTemperatureToRenderer().Enable();
            new Patch_RainCondensator_OnEnable().Enable();
            new Patch_RainCondensator_UpdateValues().Enable();
            new Patch_RainCondensator_OnDisable().Enable();
            new Patch_PlayerBody_SetSkin().Enable();
            new Patch_LoddedSkin_Unskin().Enable();
            new Patch_OverallScreen_Show().Enable();
            new Patch_PlayerModelView_method_0().Enable();
            new Patch_OverallScreen_Close().Enable();
            new Patch_BotCreatorClient_CreateBot().Enable();
            new Patch_LocalPlayer_Create().Enable();

            TryEnableFikaSupport(assemblyDir);
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

        public Dictionary<string, ItemPreset> LoadItemPresets()
        {
            var filePaths = SafeIO.GetFiles(ItemPresetsDir, "*.json");
            var result = new Dictionary<string, ItemPreset>();

            foreach (var filePath in filePaths)
            {
                var presetName = Path.GetFileNameWithoutExtension(filePath);
                if (SafeIO.ReadAllText(filePath).Ok(out var json, out var e))
                {
                    var preset = JsonConvert.DeserializeObject<ItemPreset>(json);
                    UpgradeOldVersionsOfItemPreset(preset);
                    result.Add(presetName, preset);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Item Preset", "Failed to load from disk", presetName, e);
                }
            }

            return result;
        }

        public Dictionary<int, Dictionary<string, List<ItemPreset>>> LoadBotItemPresets()
        {
            var botDirectoryPaths = SafeIO.GetDirectories(BotItemPresetsDir);
            var bots = new Dictionary<int, Dictionary<string, List<ItemPreset>>>(botDirectoryPaths.Length);
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
                var items = new Dictionary<string, List<ItemPreset>>(itemDirectoryPaths.Length);

                foreach (var itemDirectoryPath in itemDirectoryPaths)
                {
                    var itemTemplateId = Path.GetFileName(itemDirectoryPath);
                    Logger.LogError(itemTemplateId);

                    var presetsPath = Path.Combine(itemDirectoryPath, "presets-item-materials");
                    var presetPaths = SafeIO.GetFiles(presetsPath, "*.json");
                    var presets = new List<ItemPreset>(presetPaths.Length);

                    foreach (var presetPath in presetPaths)
                    {
                        Logger.LogError(presetPath);
                        if (SafeIO.ReadAllText(presetPath).Ok(out var json, out var e))
                        {
                            var preset = JsonConvert.DeserializeObject<ItemPreset>(json);
                            UpgradeOldVersionsOfItemPreset(preset);
                            presets.Add(preset);
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

        public static void UpgradeOldVersionsOfItemPreset(ItemPreset itemPreset)
        {
            foreach (var items in itemPreset.Materials.Values)
            {
                foreach (var item in items.Values)
                {
                    foreach (var material in item.Values)
                    {
                        UpgradeOldVersionsOfMaterialInfo(material);
                    }
                }
            }
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
                    UpgradeOldVersionsOfMaterialPreset(preset);
                    result.Add(presetName, preset);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Material Preset", "Failed to load from disk", presetName, e);
                }
            }

            return result;
        }

        public static void UpgradeOldVersionsOfMaterialPreset(MaterialPreset materialPreset)
        {
            UpgradeOldVersionsOfMaterialInfo(materialPreset.Material);
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
                    UpgradeOldVersionsOfMaterialsInfo(materialsInfo);
                    var itemsWithMaterials = new ItemsWithMaterials()
                    {
                        Items = new(),
                        MaterialsInfo = materialsInfo,
                    };

                    result.Add(itemId, itemsWithMaterials);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Item", "Failed to load from disk", itemId, e);
                }
            }

            return result;
        }

        public static void UpgradeOldVersionsOfMaterialsInfo(MaterialsInfo materialsInfo)
        {
            foreach (var materialInfo in materialsInfo.Materials.Values)
            {
                UpgradeOldVersionsOfMaterialInfo(materialInfo);
            }
        }

        public static void UpgradeOldVersionsOfMaterialInfo(MaterialInfo materialInfo)
        {
            if (materialInfo.SchemaVersion == 0)
            {
                materialInfo.SchemaVersion = 1;
                // actual solution is to get original values, but its too much hassle
                // just tell people to reset material if looks bad (it wont)
                materialInfo.Specularness /= 0.2f; // we will multiply it by 0.2 anyway
                materialInfo.SpecColorHSV = new(0, 0, 0.8f);
                materialInfo.ReflectColorHSV = new(0, 0, 0.8f);
                materialInfo.SpecVals = new(1, 1);
                materialInfo.DefVals = new(1, 1);
                materialInfo.CompensateSpecular = true;
            }
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
                    Logger.Log(LogLevel.Info, "Item", "Potential warning, already loading (ignore if happened on weapon reload)", itemId, item.Prefab.path);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Item", "Collision", itemId, existingItemId, item.Prefab.path);
                }
            }
            else
            {
                ResourceKeyToItem.Add(item.Prefab, itemId);
                Logger.Log(LogLevel.Info, "Item", "Loading", itemId, item.Prefab.path);
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
            			Logger.Log(LogLevel.Error, "Item", "Already added", itemId, itemPrefab.path, instanceID);
                        return;
                    }
                    if (itemGameObject.TryGetComponent<AssetPoolObject>(out var assetPoolObject))
                    {
                        var itemWithMaterials = BuildItemOverrides(assetPoolObject);
                        PatchItem(itemWithMaterials, itemsWithMaterials.MaterialsInfo);
                        itemsWithMaterials.Items.Add(instanceID, itemWithMaterials);
                        InstanceIdToItemId.Add(instanceID, itemId);
            			Logger.Log(LogLevel.Info, "Item", "Loaded", itemId, itemPrefab.path, instanceID);
                    }
                    else
                    {
            			Logger.Log(LogLevel.Error, "Item", "No AssetPoolObject", itemId, itemPrefab.path, instanceID);
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

        public ItemWithMaterials BuildItemOverrides(LoddedSkin skin)
        {
            var _skin = new LoddedSkin_Proxy(skin);
            var targetMaterials = new Dictionary<string, TargetMaterial>();
            foreach (var lod in _skin._lods)
            {
                BuildRendererOverrides(lod.SkinnedMeshRenderer, targetMaterials);
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
                            OriginalTexture = material.GetTexture(_MainTex), // we assume that original texture comes from first encountered material, in most cases its lod 0, so we are fine...
                            CustomTexture = default,
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
                Logger.Log(LogLevel.Warning, "Item has null material");
                return false;
            }

            // TODO I noticed LOD1 have p0/Reflective/Specular shader, so we skip LOD1 entirely, not good, but it has different properties...
            // TODO how to support other shaders? switch with predetermined list in enum
            var materialShaderName = material.shader.name;
			if (materialShaderName == "p0/Cutout/Bumped Diffuse" ||
			    materialShaderName == "p0/Reflective/Specular" ||
			    materialShaderName == "p0/Reflective/Bumped Specular" ||
			    materialShaderName == "p0/Reflective/Bumped Specular SMap" ||
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

        public Dictionary<string, MaterialInfo> GetOriginalMaterials(LoddedSkin skin)
        {
            var _skin = new LoddedSkin_Proxy(skin);
            var originals = new Dictionary<string, MaterialInfo>();
            foreach (var lod in _skin._lods)
            {
                GetOriginalMaterials(lod.SkinnedMeshRenderer, originals);
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
                SchemaVersion = MaterialInfo.CurrentSchemaVersion,
                ColorHSV = material.GetColor(_Color).RGBAtoHSV(),
                SpecColorHSV = material.GetColor(_SpecColor).RGBAtoHSV(),
                Glossness = material.GetFloat(_Glossness),
                Specularness = material.GetFloat(_Specularness),
                ReflectColorHSV = material.GetColor(_ReflectColor).RGBAtoHSV(),
                Texture = "",
                TextureUV = material.GetVector(_MainTex_ST),
                SpecVals = material.GetVector(_SpecVals),
                DefVals = material.GetVector(_DefVals),
                CompensateSpecular = true,
            };
        }

        public void PatchItem(ItemWithMaterials item, MaterialsInfo materialsInfo)
        {
            foreach (var (materialName, materialInfo) in materialsInfo.Materials)
            {
                if (item.Materials.TryGetValue(materialName, out var targetMaterial))
                {
                    ApplyAllOverrides(targetMaterial, materialInfo);
                    Logger.Log(LogLevel.Info, "Patch", materialName, targetMaterial.Renderers.Count);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Patch", "Failure", materialName);
                }
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
                return;
            }

            if (!ItemsWithMaterials.TryGetValue(itemId, out var itemsWithMaterials))
            {
    			Logger.Log(LogLevel.Error, "Item", "Tried to destroy not registered item", itemId, instanceID);
                return;
            }

            if (!itemsWithMaterials.Items.Remove(instanceID, out var itemWithMaterials))
            {
    			Logger.Log(LogLevel.Error, "Item", "Tried to destroy not registered clone", itemId, instanceID);
                return;
            }

            foreach (var (materialName, materialInfo) in itemsWithMaterials.MaterialsInfo.Materials)
            {
                ResetMaterial(itemWithMaterials, materialName, materialInfo);
            }

			Logger.Log(LogLevel.Info, "Item", "Destroyed", itemId, instanceID);
        }

        public bool IsPatchedRenderer(Renderer renderer)
        {
            return PatchedRenderers.Contains(renderer);
        }

        public void OnWeaponPreviewOpened(Item item, AssetPoolObject assetPoolObject)
        {
            var itemId = GetOriginalItemId(item.Id);
			Logger.Log(LogLevel.Info, "WeaponPreview", "Opened", itemId);
			if (IsCamoEditorWaitingForWeaponPreview)
			{
				SetupCamoEditor(item, assetPoolObject);
			}
        }

        public void SetupCamoEditor(Item item, AssetPoolObject assetPoolObject)
        {
            var (items, itemsDict) = GetOrBuildItemWithAllItSlots(item, assetPoolObject);
            CamoEditor = new(new CamoEditor()
            {
                Plugin = this,
                BigPlugin = BigPlugin.Instance,
                CamoEditorResources = CamoEditorResources,
                Items = items,
                ItemsDict = itemsDict,
                WindowRect = MaterialEditor.CamoEditor.GetDefaultWindowRect_Item(),
            });
        }

        public (List<CamoEditorItem>, Dictionary<string, List<int>>) GetOrBuildItemWithAllItSlots(Item item, AssetPoolObject assetPoolObject)
        {
            List<CamoEditorItem> result;
            Dictionary<string, List<int>> resultDict;

            if (assetPoolObject.ContainerCollectionView != null)
            {
                var containerBones = assetPoolObject.ContainerCollectionView.ContainerBones;
                result = new(containerBones.Count + 1);
                resultDict = new(containerBones.Count + 1);
                AddCamoEditorItem(item, assetPoolObject, result, resultDict);
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
                        AddCamoEditorItem(containerData.Item, subItemAssetPoolObject, result, resultDict);
                    }
                }
            }
            else
            {
                result = new(1);
                resultDict = new(1);
                AddCamoEditorItem(item, assetPoolObject, result, resultDict);
            }

            return (result, resultDict);
        }

        public void AddCamoEditorItem(Item item, AssetPoolObject assetPoolObject, List<CamoEditorItem> items, Dictionary<string, List<int>> itemsDict)
        {
            var editorItem = GetOrBuildItem(item, assetPoolObject);

            items.Add(editorItem);

            var itemIndex = items.Count - 1;
            var templateId = item.StringTemplateId;
            if (itemsDict.TryGetValue(templateId, out var sameItems))
            {
                sameItems.Add(itemIndex);
            }
            else
            {
                sameItems = new List<int>();
                sameItems.Add(itemIndex);
                itemsDict.Add(templateId, sameItems);
            }
        }

        public CamoEditorItem GetOrBuildItem(Item item, AssetPoolObject assetPoolObject)
        {
            var itemId = GetOriginalItemId(item.Id);
            var instanceID = assetPoolObject.gameObject.GetInstanceID();
            var itemWithMaterials = GetOrBuildItemWithMaterials(itemId, instanceID, assetPoolObject);
            var originalMaterials = GetOriginalMaterials(assetPoolObject);

            Logger.Log(LogLevel.Info, "CamoEditor", "Setup", itemId);

            return new CamoEditorItem
            (
                Name: GClass2348.Localized(item.Name),
                ItemId: itemId,
                InstanceID: instanceID,
                ItemWithMaterials: itemWithMaterials,
                OriginalMaterials: originalMaterials
            );
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

        // TODO add option to copy material from body part to hands (problem: they have different materials)
        public void OnSkinCreated(string profileId, string skinId, LoddedSkin skin)
        {
			// opposite to other items skinId is the same for all players,
			// we could make separate system for such type of items,
			// but its simpler to just prepend it by player id
			var itemId = profileId + skinId;
            if (ItemsWithMaterials.TryGetValue(itemId, out var itemsWithMaterials))
            {
                var instanceID = skin.gameObject.GetInstanceID();
                if (itemsWithMaterials.Items.ContainsKey(instanceID))
                {
        			Logger.Log(LogLevel.Error, "Skin", "Already created", itemId, instanceID);
                    return;
                }

                var itemWithMaterials = BuildItemOverrides(skin);
                PatchItem(itemWithMaterials, itemsWithMaterials.MaterialsInfo);
                itemsWithMaterials.Items.Add(instanceID, itemWithMaterials);
                InstanceIdToItemId.Add(instanceID, itemId);
    			Logger.Log(LogLevel.Info, "Skin", "Created", itemId, instanceID);
            }
        }

        public void OnSkinDestroyed(LoddedSkin skin)
        {
            var instanceID = skin.gameObject.GetInstanceID();
            OnItemDestroyed(instanceID);
        }

        public void OnClothesReloaded(string profileId, PlayerModelView playerModelView)
        {
            // closing camo editor puts IsCamoEditorWaitingForWeaponPreview to false,
            // so check CamoEditor.HasValue for proper behaviour
            if (IsCamoEditorWaitingForWeaponPreview || CamoEditor.HasValue)
            {
                SetupCamoEditorClothes(profileId, playerModelView);
            }
        }

        public void SetupCamoEditorClothes(string profileId, PlayerModelView playerModelView)
        {
            // SetupCamoEditorClothes is called when:
            // 1) player opens Overall screen and PlayerModelView gets loaded
            // 2) player switches cloth piece in overall screen, in which case we must properly close previous editor

            // save editor position
            var isOpened = false;
            var windowRect = MaterialEditor.CamoEditor.GetDefaultWindowRect_Clothes();
            if (CamoEditor.Some(out var camoEditor))
            {
                isOpened = camoEditor.IsOpened;
                windowRect = camoEditor.WindowRect;
                CloseCamoEditor();
            }

            var (items, itemsDict) = GetOrBuildItemsFromBodySkins(profileId, playerModelView);
            CamoEditor = new(new CamoEditor()
            {
                Plugin = this,
                BigPlugin = BigPlugin.Instance,
                CamoEditorResources = CamoEditorResources,
                Items = items,
                ItemsDict = itemsDict,
                IsOpened = isOpened,
                WindowRect = windowRect
            });
        }

        public (List<CamoEditorItem>, Dictionary<string, List<int>>) GetOrBuildItemsFromBodySkins(string profileId, PlayerModelView playerModelView)
        {
            var bodySkins = playerModelView.PlayerBody.BodySkins;
            var bodyCustomization = playerModelView.PlayerBody.BodyCustomization;
            var result = new List<CamoEditorItem>(bodySkins.Count);
            var resultDict = new Dictionary<string, List<int>>(bodySkins.Count);
            foreach (var (bodyPart, skin) in bodySkins)
            {
                var skinId = bodyCustomization[bodyPart];
                AddCamoEditorItem(profileId, skinId, skin, result, resultDict);
            }
            return (result, resultDict);
        }

        public void AddCamoEditorItem(string profileId, string skinId, LoddedSkin skin, List<CamoEditorItem> items, Dictionary<string, List<int>> itemsDict)
        {
            var editorItem = GetOrBuildItem(profileId, skinId, skin);

            items.Add(editorItem);

            var itemIndex = items.Count - 1;
            var templateId = skinId;
            if (itemsDict.TryGetValue(templateId, out var sameItems))
            {
                sameItems.Add(itemIndex);
            }
            else
            {
                sameItems = new List<int>();
                sameItems.Add(itemIndex);
                itemsDict.Add(templateId, sameItems);
            }
        }

        public CamoEditorItem GetOrBuildItem(string profileId, string skinId, LoddedSkin skin)
        {
            var itemId = profileId + skinId;
            var instanceID = skin.gameObject.GetInstanceID();
            var itemWithMaterials = GetOrBuildItemWithMaterials(itemId, instanceID, skin);
            var originalMaterials = GetOriginalMaterials(skin);

            Logger.Log(LogLevel.Info, "CamoEditor", "Setup item", itemId);

            return new CamoEditorItem
            (
                Name: skin.gameObject.name, // getting the same name as in Overall or Ragfair screens is unreasonably annoying
                ItemId: itemId,
                InstanceID: instanceID,
                ItemWithMaterials: itemWithMaterials,
                OriginalMaterials: originalMaterials
            );
        }

        public ItemWithMaterials GetOrBuildItemWithMaterials(string itemId, int instanceID, LoddedSkin skin)
        {
            if (ItemsWithMaterials.TryGetValue(itemId, out var itemsWithMaterials) &&
                itemsWithMaterials.Items.TryGetValue(instanceID, out var itemWithMaterials))
            {
                return itemWithMaterials;
            }

            return BuildItemOverrides(skin);
        }

        public void WaitForWeaponPreview()
        {
			IsCamoEditorWaitingForWeaponPreview = true;
        }

        public bool IsWaitingForWeaponPreview()
        {
            return IsCamoEditorWaitingForWeaponPreview;
        }

        // game hides cursor and resets it to the center,
        // when player drags in weapon modding screen, which
        // fucks up dragging transform handles and sliders,
        // so keep cursor visible
        public bool CanHideCursor()
        {
            return !CamoEditor.HasValue;
        }

        // its annoying to drag sliders
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
                Logger.Log(LogLevel.Info, "CamoEditor", "Potential warning. Tried to close uninitialized decal editor");
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
                        Logger.Log(LogLevel.Info, "CamoEditor", "Remove materials", item.ItemId);
                    }
                    else
                    {
                        materialsInfo.SaveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        WriteMaterialsToFile(item.ItemId, materialsInfo);
                        Logger.Log(LogLevel.Info, "CamoEditor", "Rewrite materials", item.ItemId);
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

        public string GetItemPresetFilePath(string presetName)
        {
            var fileName = $"{presetName}.json";
            var filePath = Path.Combine(ItemPresetsDir, fileName);
            return filePath;
        }

        public void WriteItemPresetToFile(string presetName, ItemPreset preset)
        {
            var json = JsonConvert.SerializeObject(preset, Formatting.Indented);
            var filePath = GetItemPresetFilePath(presetName);
            SafeIO.WriteAllTextAsync(filePath, json);
        }

        public void DeleteItemPreset(string presetName)
        {
            if (ItemPresets.Remove(presetName))
            {
                var filePath = GetItemPresetFilePath(presetName);
                SafeIO.DeleteFile(filePath);
            }
        }

        public void LateUpdate()
        {
            // we combine color channel from custom texture
            // with alpha channel (specular map) from original texture,
            // videos change texture every frame, so we have to rerender
            // combined texture every frame too

            foreach (var (targetMaterial, videoData) in Videos)
            {
                if (targetMaterial.CustomTexture.Some(out var customTexture) &&
                    customTexture.Combined.Some(out var combined))
                {
                    RenderCombinedTexture(combined, customTexture.Color, videoData.MaterialInfo.TextureUV, targetMaterial.OriginalTexture);
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

        public int GetItemPresetsCount()
        {
            return ItemPresets.Count;
        }

        public int GetMaterialPresetsCount()
        {
            return MaterialPresets.Count;
        }

        public Dictionary<string, ItemPreset>.KeyCollection GetItemPresetNames()
        {
            return ItemPresets.Keys;
        }

        public Dictionary<string, MaterialPreset>.KeyCollection GetMaterialPresetNames()
        {
            return MaterialPresets.Keys;
        }

        public void OverrideMaterial(ItemWithMaterials itemWithMaterials, Dictionary<string, MaterialInfo> originalMaterials, string itemId, int instanceID, string materialName)
        {
            if (!originalMaterials.TryGetValue(materialName, out var originalMaterial))
            {
                Logger.Log(LogLevel.Error, "OverrideMaterial", "No original material", itemId, instanceID, materialName);
                return;
            }

            if (ItemsWithMaterials.ContainsKey(itemId))
            {
                var itemsWithMaterials = ItemsWithMaterials[itemId];
                var materials = itemsWithMaterials.MaterialsInfo.Materials;
                if (materials.ContainsKey(materialName))
                {
                    Logger.Log(LogLevel.Info, "OverrideMaterial", "Potential warning. Already overriden", itemId, instanceID, materialName);
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
                    MaterialsInfo = new MaterialsInfo()
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

        // this version is user-faced,
        // it actually cleans up material on all items and removes record from dict,
        // other versions are for internal use
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

        private void ResetMaterial(ItemWithMaterials itemWithMaterials, string materialName, MaterialInfo materialInfo)
        {
            if (itemWithMaterials.Materials.TryGetValue(materialName, out var targetMaterial))
            {
                ResetMaterial(targetMaterial, materialInfo);
            }
        }

        private void ResetMaterial(TargetMaterial targetMaterial, MaterialInfo materialInfo)
        {
            targetMaterial.PropertyBlock.Clear();
            if (!string.IsNullOrWhiteSpace(materialInfo.Texture))
            {
                TryDestroyCombinedTexture(targetMaterial);
                BigPlugin.Instance.ReleaseDecalTextureAsset(targetMaterial, materialInfo.Texture);
            }
            foreach (var (renderer, index) in targetMaterial.Renderers)
            {
                renderer.SetPropertyBlock(null, index);
                PatchedRenderers.Remove(renderer);
            }
        }

        public void TryDestroyCombinedTexture(TargetMaterial targetMaterial)
        {
            if (targetMaterial.CustomTexture.Some(out var customTexture))
            {
                targetMaterial.CustomTexture = default;
                if (customTexture.IsVideo)
                {
                    Videos.Remove(targetMaterial);
                }
                if (customTexture.Combined.Some(out var combined))
                {
                    Destroy(combined);
                }
            }
        }

        public void ApplyAllOverrides(TargetMaterial targetMaterial, MaterialInfo materialInfo)
        {
            var propertyBlock = targetMaterial.PropertyBlock;

            propertyBlock.SetColor(_Color, materialInfo.GetColor());
            propertyBlock.SetColor(_SpecColor, materialInfo.GetSpecColor());
            propertyBlock.SetFloat(_Glossness, materialInfo.Glossness);
            propertyBlock.SetFloat(_Specularness, materialInfo.Specularness);
            propertyBlock.SetColor(_ReflectColor, materialInfo.GetReflectColor());
            propertyBlock.SetVector(_SpecVals, materialInfo.SpecVals);
            propertyBlock.SetVector(_DefVals, materialInfo.DefVals);

            if (!string.IsNullOrWhiteSpace(materialInfo.Texture))
            {
                // MaterialChangeTexture will call ApplyPropertyBlock for us
                BigPlugin.Instance.AcquireDecalTextureAsset(
                    targetMaterial, materialInfo.Texture,
                    (key, texture) => MaterialChangeTexture(key, texture, materialInfo),
                    (key, texture) => MaterialChangeTexture(key, texture, materialInfo)
                );
            }
            else
            {
                // otherwise, call it ourselves
                ApplyPropertyBlock(targetMaterial);
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
                static (targetMaterial, materialInfo) => targetMaterial.PropertyBlock.SetColor(_Color, materialInfo.GetColor())
            );
        }

        public void ChangeSpecColor(string itemId, string materialName, Vector3 specColorHSV)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.SpecColorHSV = specColorHSV,
                static (targetMaterial, materialInfo) => targetMaterial.PropertyBlock.SetColor(_SpecColor, materialInfo.GetSpecColor())
            );
        }

        public void ChangeReflectColor(string itemId, string materialName, Vector3 reflectColorHSV)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.ReflectColorHSV = reflectColorHSV,
                static (targetMaterial, materialInfo) => targetMaterial.PropertyBlock.SetColor(_ReflectColor, materialInfo.GetReflectColor())
            );
        }

        public void ChangeCompensateSpecular(string itemId, string materialName, bool compensateSpecular)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.CompensateSpecular = compensateSpecular,
                ChangeSpecularCompensation
            );
        }

        private void ChangeSpecularCompensation(TargetMaterial targetMaterial, MaterialInfo materialInfo)
        {
            if (!targetMaterial.CustomTexture.Some(out var customTexture))
            {
                // SpecularCompensation does nothing when original texture is set
                return;
            }
            if (customTexture.Combined.Some(out var combined))
            {
                if (!materialInfo.CompensateSpecular)
                {
                    // our goal is to go from combined texture back to Color texture, so
                    // set Color texture to propertyBlock and destroy render texture

                    targetMaterial.CustomTexture = new(customTexture with { Combined = default });
                    targetMaterial.PropertyBlock.SetTexture(_MainTex, customTexture.Color);
                    Destroy(combined);
                }
            }
            else
            {
                if (materialInfo.CompensateSpecular)
                {
                    // our goal is to compensate specular via combining original specular map
                    // with custom color texture, do nothing if there is already combined texture

                    var alpha = targetMaterial.OriginalTexture;
                    var renderTexture = CreateCombinedTexture(alpha);
                    if (!customTexture.IsVideo)
                    {
                        // video will get rerendered in LateUpdate anyway
                        RenderCombinedTexture(renderTexture, customTexture.Color, materialInfo.TextureUV, alpha);
                    }
                    targetMaterial.CustomTexture = new(customTexture with { Combined = new(renderTexture) });
                    targetMaterial.PropertyBlock.SetTexture(_MainTex, renderTexture);
                }
            }
        }

        public void ChangeGlossness(string itemId, string materialName, float glossness)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.Glossness = glossness,
                static (targetMaterial, materialInfo) => targetMaterial.PropertyBlock.SetFloat(_Glossness, materialInfo.Glossness)
            );
        }

        public void ChangeSpecularness(string itemId, string materialName, float specularness)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.Specularness = specularness,
                static (targetMaterial, materialInfo) => targetMaterial.PropertyBlock.SetFloat(_Specularness, materialInfo.Specularness)
            );
        }

        public void ChangeSpecVals(string itemId, string materialName, Vector2 specVals)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.SpecVals = specVals,
                static (targetMaterial, materialInfo) => targetMaterial.PropertyBlock.SetVector(_SpecVals, materialInfo.SpecVals)
            );
        }

        public void ChangeDefVals(string itemId, string materialName, Vector2 defVals)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.DefVals = defVals,
                static (targetMaterial, materialInfo) => targetMaterial.PropertyBlock.SetVector(_DefVals, materialInfo.DefVals)
            );
        }

        public void ChangeTextureUV(string itemId, string materialName, Vector4 textureUV)
        {
            ModifyMaterialOnItems
            (
                itemId, materialName,
                (materialInfo) => materialInfo.TextureUV = textureUV,
                (targetMaterial, materialInfo) =>
                {
                    // texture tiling and offset is valid option only
                    // if user has custom texture without proper specular map
                    // (custom texture + specular compensation = combined texture),
                    // people wont like ugly tiling/offset anyway

                    if (targetMaterial.CustomTexture.Some(out var customTexture) &&
                        customTexture.Combined.Some(out var combined) &&
                        !customTexture.IsVideo)
                    {
                        // no need to rerender video, it will get rerendered in LateUpdate anyway,
                        RenderCombinedTexture(combined, customTexture.Color, materialInfo.TextureUV, targetMaterial.OriginalTexture);
                    }
                }
            );
        }

        // notice that we modify material on all items
        public void ModifyMaterialOnItems(
            string itemId, string materialName,
            Action<MaterialInfo> changeMaterial,
            Action<TargetMaterial, MaterialInfo> changeTargetMaterial)
        {
            var itemsWithMaterials = ItemsWithMaterials[itemId];
            var materialInfo = itemsWithMaterials.MaterialsInfo.Materials[materialName];
            changeMaterial(materialInfo);
            foreach (var itemWithMaterials in itemsWithMaterials.Items.Values)
            {
                var targetMaterial = itemWithMaterials.Materials[materialName];
                changeTargetMaterial(targetMaterial, materialInfo);
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
                    TryDestroyCombinedTexture(targetMaterial);
                    BigPlugin.Instance.ReleaseDecalTextureAsset(targetMaterial, oldTextureName);
                }
                if (!string.IsNullOrWhiteSpace(materialInfo.Texture))
                {
                    BigPlugin.Instance.AcquireDecalTextureAsset(
                        targetMaterial, materialInfo.Texture,
                        (key, texture) => MaterialChangeTexture(key, texture, materialInfo),
                        (key, texture) => MaterialChangeTexture(key, texture, materialInfo)
                    );
                }
            }
        }

        public void MaterialChangeTexture(SystemObject key, Texture texture, MaterialInfo materialInfo)
        {
            if (key is not TargetMaterial targetMaterial)
            {
                return;
            }

            var customTexture = GetCustomTexture(targetMaterial, texture, materialInfo);
            targetMaterial.CustomTexture = new(customTexture);

            if (customTexture.Combined.Some(out var combined))
            {
                targetMaterial.PropertyBlock.SetTexture(_MainTex, combined);
            }
            else
            {
                targetMaterial.PropertyBlock.SetTexture(_MainTex, customTexture.Color);
            }

            if (customTexture.IsVideo && !Videos.ContainsKey(targetMaterial))
            {
                // I am pretty sure if Videos contains targetMaterial, it has already been setuped
                // with exactly this materialInfo when low res preview was set
                Videos.Add(targetMaterial, new VideoData() { MaterialInfo = materialInfo });
            }

            ApplyPropertyBlock(targetMaterial);
        }

        public CustomTexture GetCustomTexture(TargetMaterial targetMaterial, Texture texture, MaterialInfo materialInfo)
        {
            var textureData = BigPlugin.Instance.GetTextureData(materialInfo.Texture);
            var isVideo = textureData.Format == DecalTextureFormat.Video;

            if (materialInfo.CompensateSpecular)
            {
                // this version is for average users whose textures
                // dont have valid specular map in alpha channel
                RenderTexture renderTexture;

                var alpha = targetMaterial.OriginalTexture;

                if (targetMaterial.CustomTexture.Some(out var customTexture) &&
                    customTexture.Combined.Some(out var combined))
                {
                    // this happens because low res preview is combined first,
                    // also since render texture is created with dimensions
                    // and settings from original texture, we can simply reuse it
                    renderTexture = combined;
                }
                else
                {
                    renderTexture = CreateCombinedTexture(alpha);
                }

                if (!isVideo)
                {
                    // video will get rerendered in LateUpdate anyway
                    RenderCombinedTexture(renderTexture, texture, materialInfo.TextureUV, alpha);
                }

                return new CustomTexture(texture, new(renderTexture), isVideo);
            }
            else
            {
                // this version is for folks who do proper retexture
                // with valid specular map in alpha channel,
                // I assume they dont care about texture UV setting,
                // so no need to render combined texture
                return new CustomTexture(texture, default, isVideo);
            }
        }

        public static RenderTexture CreateCombinedTexture(Texture alpha)
        {
            // use parameters of original texture
            var renderTexture = new RenderTexture(alpha.width, alpha.height, 0);
            renderTexture.anisoLevel = alpha.anisoLevel;
            renderTexture.filterMode = alpha.filterMode;
            renderTexture.wrapMode = alpha.wrapMode;
            return renderTexture;
        }

        public void RenderCombinedTexture(RenderTexture renderTexture, Texture color, Vector4 colorUV, Texture alpha)
        {
            // we dont plug texture uv into _MainTex_ST of item material,
            // otherwise it will show repetition and offset in specular and gloss maps,
            // which looks ugly, so we sample color texture with that UV instead
            CombineTexturesMaterial.SetVector(_ColorTex_ST, colorUV);
            CombineTexturesMaterial.SetTexture(_ColorTex, color);
            CombineTexturesMaterial.SetTexture(_AlphaTex, alpha);
            Graphics.Blit(null, renderTexture, CombineTexturesMaterial);
            CombineTexturesMaterial.SetTexture(_ColorTex, null);
            CombineTexturesMaterial.SetTexture(_AlphaTex, null);
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
                    Logger.Log(LogLevel.Warning, "Clone", "Same Id", originalId);
                    return;
                }
                if (Clones.TryAdd(cloneId, originalId))
                {
                    Logger.Log(LogLevel.Info, "Clone", "Added", originalId, cloneId);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Clone", "Already added", originalId, cloneId);
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
            if (MaterialPresets.TryGetValue(presetName, out var oldPreset))
            {
                oldPreset.Material = materialInfo.GetCopy();
                WriteMaterialPresetToFile(presetName, oldPreset);
            }
            else
            {
                var newPreset = new MaterialPreset()
                {
                    SchemaVersion = MaterialPreset.CurrentSchemaVersion,
                    Material = materialInfo.GetCopy(),
                };
                MaterialPresets.Add(presetName, newPreset);
                WriteMaterialPresetToFile(presetName, newPreset);
            }
        }

        public void SwitchToMaterialPreset(string itemId, string materialName, string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                return;
            }
            if (!MaterialPresets.TryGetValue(presetName, out var preset))
            {
                return;
            }

            SwitchToMaterialPreset(itemId, materialName, preset.Material);
        }

        public void SwitchToMaterialPreset(string itemId, string materialName, MaterialInfo source)
        {
            if (!GetMaterialInfo(itemId, materialName).Some(out var target))
            {
                return;
            }

            ForEveryMaterialOnItem(itemId, materialName, ResetMaterial);

            target.ColorHSV = source.ColorHSV;
            target.SpecColorHSV = source.SpecColorHSV;
    		target.Glossness = source.Glossness;
    		target.Specularness = source.Specularness;
            target.ReflectColorHSV = source.ReflectColorHSV;
            target.Texture = source.Texture;
            target.TextureUV = source.TextureUV;
            target.SpecVals = source.SpecVals;
            target.DefVals = source.DefVals;
            target.CompensateSpecular = source.CompensateSpecular;

            ForEveryMaterialOnItem(itemId, materialName, ApplyAllOverrides);
        }

        public void SaveItemMaterialsIntoPreset(List<CamoEditorItem> items, Dictionary<string, List<int>> itemsDict, string presetName)
        {
            if (!SafeIO.IsValidFileName(presetName))
            {
                return;
            }
            if (ItemPresets.TryGetValue(presetName, out var oldPreset))
            {
                CopyAllMaterials(items, itemsDict, oldPreset.Materials);
                WriteItemPresetToFile(presetName, oldPreset);
            }
            else
            {
                var newMaterials = new Dictionary<string, Dictionary<int, Dictionary<string, MaterialInfo>>>(itemsDict.Count);
                CopyAllMaterials(items, itemsDict, newMaterials);
                var newPreset = new ItemPreset()
                {
                    SchemaVersion = ItemPreset.CurrentSchemaVersion,
                    Materials = newMaterials,
                };
                ItemPresets.Add(presetName, newPreset);
                WriteItemPresetToFile(presetName, newPreset);
            }
        }

        public void CopyAllMaterials(
            List<CamoEditorItem> items,
            Dictionary<string, List<int>> itemsDict,
            Dictionary<string, Dictionary<int, Dictionary<string, MaterialInfo>>> allMaterials)
        {
            allMaterials.Clear();
            foreach (var (templateId, itemIndices) in itemsDict)
            {
                Dictionary<int, Dictionary<string, MaterialInfo>> itemsInfo = null;
                for (var i = 0; i < itemIndices.Count; i++)
                {
                    var item = items[itemIndices[i]];
                    if (GetMaterialsInfo(item.ItemId).Some(out var materialsInfo))
                    {
                        if (materialsInfo.Materials.Count == 0)
                        {
                            // TODO clean database entry on reset override
                            // this happens when item was painted, then reset, then preset was created,
                            // entry in database is still there but without any materials
                            continue;
                        }
                        var materials = new Dictionary<string, MaterialInfo>(materialsInfo.Materials.Count);
                        foreach (var (materialName, materialInfo) in materialsInfo.Materials)
                        {
                            materials.Add(materialName, materialInfo.GetCopy());
                        }
                        if (materials.Count == 0)
                        {
                            continue;
                        }
                        if (itemsInfo == null)
                        {
                            itemsInfo = new(itemIndices.Count);
                        }
                        itemsInfo.Add(i, materials);
                    }
                }
                if (itemsInfo != null)
                {
                    allMaterials.Add(templateId, itemsInfo);
                }
            }
        }

        public void SwitchToItemPreset(List<CamoEditorItem> items, Dictionary<string, List<int>> itemsDict, string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                return;
            }
            if (!ItemPresets.TryGetValue(presetName, out var preset))
            {
                return;
            }

            // technically we dont need to reset all materials,
            // we can just apply overrides from preset and do not touch others,
            // but its easier to check presets when they are applied
            // cleanly and look exactly how preset author intended

            // this will try to reset all materials on all items,
            // some items do not have overrides at all, but thats fine,
            // ResetMaterial will just skip them
            foreach (var item in items)
            {
                foreach (var materialName in item.ItemWithMaterials.Materials.Keys)
                {
                    ResetMaterial(item.ItemId, materialName);
                }
            }

            // we reset all materials, so now we have to override them again,
            // and then switch to material from preset
            foreach (var (templateId, presetItems) in preset.Materials)
            {
                if (itemsDict.TryGetValue(templateId, out var templateItems))
                {
                    foreach (var (localItemIndex, presetItem) in presetItems)
                    {
                        if (localItemIndex < templateItems.Count)
                        {
                            var itemIndex = templateItems[localItemIndex];
                            var item = items[itemIndex];
                            foreach (var (materialName, materialInfo) in presetItem)
                            {
                                OverrideMaterial(item.ItemWithMaterials, item.OriginalMaterials, item.ItemId, item.InstanceID, materialName);
                                SwitchToMaterialPreset(item.ItemId, materialName, materialInfo);
                            }
                        }
                    }
                }
            }
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

            // copies all items with changed materials inside player equipment (on hands/sling/holster, inside backpack, rig, etc)
            var profile = tarkovApplication.Session.Profile;
            var equipmentItems = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment);

            foreach (var item in equipmentItems)
            {
                if (ItemsWithMaterials.TryGetValue(item.Id, out var itemsWithMaterials))
                {
                    snapshot[item.Id] = CopyMaterialsInfo(itemsWithMaterials.MaterialsInfo);
                }
            }

            // copies all clothes with changed materials
            var profileId = profile.Id;
            foreach (var skinId in profile.Customization.Values)
            {
                var itemId = profileId + skinId;
                if (ItemsWithMaterials.TryGetValue(itemId, out var itemsWithMaterials))
                {
                    snapshot[itemId] = CopyMaterialsInfo(itemsWithMaterials.MaterialsInfo);
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
                Logger.Log(LogLevel.Warning, "RemoteMaterials", "Has no materials, but was replicated", itemId);
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
                    Logger.Log(LogLevel.Info, "RemoteMaterials", "Mine is newer", itemId);
                    return;
                }

                CopyMaterialsInfo(remoteMaterialsInfo, materialsInfo);
                WriteMaterialsToFile(itemId, materialsInfo);
                Logger.Log(LogLevel.Info, "RemoteMaterials", "His is newer", itemId, itemsWithMaterials.Items.Count);
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
                Logger.Log(LogLevel.Info, "RemoteMaterials", "New", itemId);
            }
        }

        public void QueueWeaponForRandomCamoGeneration(WildSpawnType botRole, Item item)
        {
            if (ItemsWithMaterials.ContainsKey(item.Id))
            {
                Logger.Log(LogLevel.Warning, "RandomCamo", "Tried to queue weapon for camo generation, but weapon already has one", item.Id);
                return;
            }
            if (!BotItemPresets.TryGetValue((int)botRole, out var botItems))
            {
                return;
            }
            if (!botItems.TryGetValue(item.StringTemplateId, out var itemPresets))
            {
                return;
            }

            var preset = itemPresets[UnityEngine.Random.Range(0, itemPresets.Count)];
            var presetMaterials = preset.Materials;

            Dictionary<string, List<string>> itemsDict = new();

			item.GetAllItemsNonAlloc(CompoundItem.ContainedItems, onlyMerged: false, includeParent: true);
            foreach (var subitem in CompoundItem.ContainedItems)
            {
                if (itemsDict.TryGetValue(subitem.StringTemplateId, out var sameItems))
                {
                    sameItems.Add(subitem.Id);
                }
                else
                {
                    sameItems = new List<string>();
                    sameItems.Add(subitem.Id);
                    itemsDict.Add(subitem.StringTemplateId, sameItems);
                }
            }
            CompoundItem.ContainedItems.Clear();


            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var (templateId, presetItems) in preset.Materials)
            {
                if (itemsDict.TryGetValue(templateId, out var templateItems))
                {
                    foreach (var (localItemIndex, presetItem) in presetItems)
                    {
                        if (localItemIndex < templateItems.Count)
                        {
                            var itemId = templateItems[localItemIndex];
                            var itemsWithMaterials = new ItemsWithMaterials()
                            {
                                Items = new(),
                                MaterialsInfo = new MaterialsInfo()
                                {
                                    SchemaVersion = MaterialsInfo.CurrentSchemaVersion,
                                    SaveTime = time,
                                    Materials = presetItem
                                }
                            };
                            ItemsWithMaterials.Add(itemId, itemsWithMaterials);
                            WriteMaterialsToFile(itemId, itemsWithMaterials.MaterialsInfo);
                        }
                    }
                }
            }

            Logger.Log(LogLevel.Info, "RandomCamo", "Queue item", item.Id);
        }

        public void QueueSkinForRandomCamoGeneration(WildSpawnType botRole, string profileId, string skinId)
        {
            var itemId = profileId + skinId;
            if (ItemsWithMaterials.ContainsKey(itemId))
            {
                Logger.Log(LogLevel.Warning, "RandomCamo", "Tried to queue weapon for camo generation, but weapon already has one", skinId);
                return;
            }
            if (!BotItemPresets.TryGetValue((int)botRole, out var botItems))
            {
                return;
            }
            if (!botItems.TryGetValue(skinId, out var itemPresets))
            {
                return;
            }

            var preset = itemPresets[UnityEngine.Random.Range(0, itemPresets.Count)];
            var presetMaterials = preset.Materials;

            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (presetMaterials.TryGetValue(skinId, out var presetItems))
            {
                if (presetItems.TryGetValue(0, out var presetItem))
                {
                    var itemsWithMaterials = new ItemsWithMaterials()
                    {
                        Items = new(),
                        MaterialsInfo = new MaterialsInfo()
                        {
                            SchemaVersion = MaterialsInfo.CurrentSchemaVersion,
                            SaveTime = time,
                            Materials = presetItem
                        }
                    };
                    ItemsWithMaterials.Add(itemId, itemsWithMaterials);
                    WriteMaterialsToFile(itemId, itemsWithMaterials.MaterialsInfo);
                }
            }

            Logger.Log(LogLevel.Info, "RandomCamo", "Queue item", itemId);
        }
    }
}
