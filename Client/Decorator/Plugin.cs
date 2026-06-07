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
using SystemObject = System.Object;

namespace SevenBoldPencil.Decorator
{
    public class ItemsWithDecorators
    {
        // yes, there can be multiple items with same Id,
        // for example when you open item preview of weapon you already hold in hands,
        // or when hideout shooting range clones weapon (we pretend that they have the same Id)
        public Dictionary<int, ItemWithDecorators> Items; // TODO iterating dict is probably not the best idea, but list in annoying
        public DecoratorsInfo DecoratorsInfo;
    }

    public class ItemWithDecorators
    {
        public Transform Root;
        // public List<Decorator> Decorators;
    }

    public class DecoratorsInfo
    {
        public const int CurrentSchemaVersion = 0;

        public int SchemaVersion;
        public long SaveTime;
        public Dictionary<string, DecoratorInfo> Decorators;
    }

    public class DecoratorInfo
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion;
        public string Prefab;
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale;

        public DecoratorInfo GetCopy() => (DecoratorInfo)MemberwiseClone();
    }

    [BepInPlugin("7Bpencil.Decorator", "7Bpencil.Decorator", "1.14.1")]
    [BepInDependency("7Bpencil.WeaponCamoAndStickers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;

		public ManualLogSource LoggerInstance;

        private string ItemsDir;
        private CamoEditorResources CamoEditorResources;

        private Dictionary<string, ItemsWithDecorators> ItemsWithDecorators;
        private Dictionary<string, string> Clones;
        private Dictionary<ResourceKey, string> ResourceKeyToItem;
        private Dictionary<int, string> InstanceIdToItemId;

        private Option<CamoEditor> CamoEditor;
        private bool IsCamoEditorWaitingForWeaponPreview;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ItemsDir = Path.Combine(assemblyDir, "items-decorators");
            CamoEditorResources = new TypedFieldInfo<BigPlugin, CamoEditorResources>("CamoEditorResources").Get(BigPlugin.Instance);

            ItemsWithDecorators = LoadItemsWithDecorators();
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
            new Patch_WeaponModdingScreen_Close().Enable();
            new Patch_GClass3380_smethod_2().Enable();
            new Patch_GClass928_GetItemHash().Enable();
        }

        public Dictionary<string, ItemsWithDecorators> LoadItemsWithDecorators()
        {
            var filePaths = SafeIO.GetFiles(ItemsDir, "*.json");
            var result = new Dictionary<string, ItemsWithDecorators>();

            foreach (var filePath in filePaths)
            {
                var itemId = Path.GetFileNameWithoutExtension(filePath);
                if (SafeIO.ReadAllText(filePath).Ok(out var json, out var e))
                {
                    var decoratorsInfo = JsonConvert.DeserializeObject<DecoratorsInfo>(json);
                    UpgradeOldVersionsOfDecoratorsInfo(decoratorsInfo);
                    var itemsWithDecorators = new ItemsWithDecorators()
                    {
                        Items = new(),
                        DecoratorsInfo = decoratorsInfo,
                    };

                    result.Add(itemId, itemsWithDecorators);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Item", "Failed to load from disk", itemId, e);
                }
            }

            return result;
        }

        public static void UpgradeOldVersionsOfDecoratorsInfo(DecoratorsInfo decoratorsInfo)
        {
            foreach (var decoratorInfo in decoratorsInfo.Decorators.Values)
            {
                UpgradeOldVersionsOfDecoratorInfo(decoratorInfo);
            }
        }

        public static void UpgradeOldVersionsOfDecoratorInfo(DecoratorInfo decoratorInfo)
        {

        }

        public void OnCreateItemAsync(Item item)
        {
            var itemId = GetOriginalItemId(item.Id);
            if (!ItemsWithDecorators.ContainsKey(itemId))
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
                if (ItemsWithDecorators.TryGetValue(itemId, out var itemsWithDecorators))
                {
                    var instanceID = itemGameObject.GetInstanceID();
                    if (itemsWithDecorators.Items.ContainsKey(instanceID))
                    {
            			Logger.Log(LogLevel.Error, "Item", "Already added", itemId, itemPrefab.path, instanceID);
                        return;
                    }
                    if (itemGameObject.TryGetComponent<AssetPoolObject>(out var assetPoolObject))
                    {
                        // TODO create decorators
                        // var itemWithDecorators = BuildItemOverrides(assetPoolObject);
                        // PatchItem(itemWithDecorators, itemsWithDecorators.DecoratorsInfo);
                        // itemsWithDecorators.Items.Add(instanceID, itemWithDecorators);
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

            if (!ItemsWithDecorators.TryGetValue(itemId, out var itemsWithDecorators))
            {
    			Logger.Log(LogLevel.Error, "Item", "Tried to destroy not registered item", itemId, instanceID);
                return;
            }

            if (!itemsWithDecorators.Items.Remove(instanceID, out var itemWithDecorators))
            {
    			Logger.Log(LogLevel.Error, "Item", "Tried to destroy not registered clone", itemId, instanceID);
                return;
            }

            // TODO destroy decorators
            // foreach (var (decoratorName, decoratorInfo) in itemsWithDecorators.DecoratorsInfo.Decorators)
            // {
            //     Resetdecorator(itemWithDecorators, decoratorName, decoratorInfo);
            // }

			Logger.Log(LogLevel.Info, "Item", "Destroyed", itemId, instanceID);
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
            // var items = GetOrBuildItemWithAllItSlots(item, assetPoolObject);
            // CamoEditor = new(new CamoEditor()
            // {
            //     Plugin = this,
            //     BigPlugin = BigPlugin.Instance,
            //     CamoEditorResources = CamoEditorResources,
            //     Items = items,
            // });
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

            if (GetDecoratorsInfo(camoEditor.ItemId).Some(out var decoratorsInfo))
            {
                if (decoratorsInfo.Decorators.Count == 0)
                {
                    ItemsWithDecorators.Remove(camoEditor.ItemId);
                    InstanceIdToItemId.Remove(camoEditor.InstanceID);
                    RemoveDecoratorsFile(camoEditor.ItemId);
                    Logger.Log(LogLevel.Info, "CamoEditor", "Remove decorators", camoEditor.ItemId);
                }
                else
                {
                    decoratorsInfo.SaveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    WriteDecoratorsToFile(camoEditor.ItemId, decoratorsInfo);
                    Logger.Log(LogLevel.Info, "CamoEditor", "Rewrite decorators", camoEditor.ItemId);
                }
            }

            camoEditor.Destroy();
            CamoEditor = default;
        }

        public void WriteDecoratorsToFile(string itemId, DecoratorsInfo decoratorsInfo)
        {
            var json = JsonConvert.SerializeObject(decoratorsInfo, Formatting.Indented);
            var filePath = GetItemFilePath(itemId);
            SafeIO.WriteAllTextAsync(filePath, json);
        }

        public void RemoveDecoratorsFile(string itemId)
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

        public Option<DecoratorsInfo> GetDecoratorsInfo(string itemId)
        {
            if (ItemsWithDecorators.TryGetValue(itemId, out var itemsWithDecorators))
            {
                return new(itemsWithDecorators.DecoratorsInfo);
            }

            return default;
        }

        public Option<DecoratorInfo> GetDecoratorInfo(string itemId, string decoratorName)
        {
            if (!ItemsWithDecorators.TryGetValue(itemId, out var itemsWithDecorators))
            {
                return default;
            }
            if (!itemsWithDecorators.DecoratorsInfo.Decorators.TryGetValue(decoratorName, out var decoratorInfo))
            {
                return default;
            }

            return new(decoratorInfo);
        }

        // TODO I forget to clean clone dict in OnItemDestroy...
        public void OnCloneItem(string originalId, string cloneId)
        {
            // when user tries weapon in hideout shooting range,
            // all his gear gets copied to new items to preserve
            // original durability/ammo count/etc,
            // so we have to clone decals ourselves
            if (ItemsWithDecorators.ContainsKey(originalId))
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
    }
}
