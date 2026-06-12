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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO
// plan mostly patches, but for that I have to make full editor anyway,
// maybe just assign to the closest bone (but only from the predefined list so it wont attach to clavicles instead of shoulders)
// all bones are inside PlayerBones class, it can be found:
// Player.PlayerBones
// PlayerBody.PlayerBones

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
        public Transform DecoratorsRoot;
        public List<Decorator> Decorators;
    }

    public class DecoratorsInfo
    {
        public const int CurrentSchemaVersion = 0;

        public int SchemaVersion;
        public long SaveTime;
        public List<DecoratorInfo> Decorators;
    }

    public class DecoratorInfo
    {
        public const int CurrentSchemaVersion = 0;

        public int SchemaVersion;
        public string Name;
        public string Prefab;
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale;
        public bool IsVisible;

        public DecoratorInfo GetCopy() => (DecoratorInfo)MemberwiseClone();
    }

    public class DecoratorPrefabData
    {
        public Texture2D Preview;
        public string BundleFilePath;
        public string AssetFilePath;
        public bool Error; // this flag is needed so we dont try to load corrupted asset over and over again
    }

    public class DecoratorPrefabAsset
    {
        public bool IsLoaded;
        public int InstancesCount;
        public Dictionary<SystemObject, Action<SystemObject, Texture>> WaitingAfterLoad;
        public GameObject GameObject;

        public void Release()
        {
            // TODO how to properly destroy all assets to free memory?
            // maybe move to Addressables?
        }
    }

    [BepInPlugin("7Bpencil.Decorator", "7Bpencil.Decorator", "1.14.1")]
    [BepInDependency("7Bpencil.WeaponCamoAndStickers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;

		public ManualLogSource LoggerInstance;

        private string DecoratorsDir;
        private string ItemsDir;
        private Texture2D ErrorTexture; // TODO need separate "No preview texture"
        private CamoEditorResources CamoEditorResources;
        private Dictionary<string, DecoratorPrefabData> DecoratorPrefabs;
        private Dictionary<string, DecoratorPrefabAsset> DecoratorPrefabAssets;
        private string[] Decorators;

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
            DecoratorsDir = Path.Combine(assemblyDir, "decorators");
            ItemsDir = Path.Combine(assemblyDir, "items-decorators");
            ErrorTexture = new TypedFieldInfo<BigPlugin, Texture2D>("ErrorTexture").Get(BigPlugin.Instance);
            CamoEditorResources = new TypedFieldInfo<BigPlugin, CamoEditorResources>("CamoEditorResources").Get(BigPlugin.Instance);

            DecoratorPrefabs = new();
            DecoratorPrefabAssets = new();

            Decorators = LoadDecorators(DecoratorsDir);

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

        public string[] LoadDecorators(string directoryPath)
        {
            var filePaths = SafeIO.GetFiles(directoryPath, "*.bundle", SearchOption.AllDirectories);
            var decorators = new string[filePaths.Length];
            for (var i = 0; i < filePaths.Length; i++)
            {
                var filePath = filePaths[i];
                var decoratorIndex = i;
                TryLoadDecorator(filePath, decoratorIndex, decorators);
            }

            return decorators;
        }

        public void TryLoadDecorator(string decoratorBundleFilePath, int decoratorIndex, string[] decorators)
        {
            var (decoratorShortName, decoratorLongName) = GetDecoratorName(decoratorBundleFilePath);
            decorators[decoratorIndex] = decoratorLongName;
            var previewFilePath = Path.Combine(DecoratorsDir, $"{decoratorLongName}.png"); // TODO not really optimal
            Logger.LogWarning(previewFilePath);
            var preview = LoadPreviewFromDisk(previewFilePath).Else(ErrorTexture);
            var prefabData = new DecoratorPrefabData()
            {
                Preview = preview,
                BundleFilePath = decoratorBundleFilePath,
                AssetFilePath = $"Assets/{decoratorShortName}/prefab.prefab",
                Error = false,
            };

            DecoratorPrefabs.Add(decoratorLongName, prefabData);
        }

        public Option<Texture2D> LoadPreviewFromDisk(string previewFilePath)
        {
            if (!File.Exists(previewFilePath))
            {
                return default;
            }

            var previewFileData = File.ReadAllBytes(previewFilePath);
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: false, createUninitialized: true);
            if (ImageConversion.LoadImage(preview, previewFileData, markNonReadable: true))
            {
                return new(preview);
            }
            else
            {
                Destroy(preview);
                return default;
            }
        }

        public (string shortName, string longName) GetDecoratorName(string filePath)
        {
            var shortName = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var longName = filePath
                .Replace(DecoratorsDir, "")
                .Replace(extension, "")
                .Remove(0, 1) // remove first slash
                .Replace(@"\", @"/"); // replace windows slashes with unix ones

            return (shortName, longName);
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
            foreach (var decoratorInfo in decoratorsInfo.Decorators)
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
                        var itemWithDecorators = BuildDecorators(assetPoolObject, itemsWithDecorators.DecoratorsInfo);
                        itemsWithDecorators.Items.Add(instanceID, itemWithDecorators);
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

        public ItemWithDecorators BuildDecorators(AssetPoolObject assetPoolObject, DecoratorsInfo decoratorsInfo)
        {
            var decorators = new List<Decorator>(decoratorsInfo.Decorators.Count);
            var decoratorsRoot = GetDecoratorsRoot(assetPoolObject);
            foreach (var decoratorInfo in decoratorsInfo.Decorators)
            {
                var decorator = CreateDecorator(decoratorInfo, decoratorsRoot);
                decorators.Add(decorator);
            }

            var itemWithDecorators = new ItemWithDecorators()
            {
                DecoratorsRoot = decoratorsRoot,
                Decorators = decorators,
            };

            return itemWithDecorators;
        }

        public Transform GetDecoratorsRoot(AssetPoolObject assetPoolObject)
        {
            if (assetPoolObject is WeaponPrefab weaponPrefab)
            {
                return WeaponCamoAndStickers.Plugin.GetWeaponRoot(weaponPrefab);
            }

            return assetPoolObject.transform;
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
            }

            for (var i = 0; i < itemWithDecorators.Decorators.Count; i++)
            {
                var decorator = itemWithDecorators.Decorators[i];
                var decoratorInfo = itemsWithDecorators.DecoratorsInfo.Decorators[i];
                DestroyDecorator(decorator, decoratorInfo);
            }
            itemWithDecorators.Decorators.Clear();

			Logger.Log(LogLevel.Info, "Item", "Destroyed", itemId, instanceID);
        }

        public void OnWeaponPreviewOpened(Camera weaponPreviewCamera, Item item, AssetPoolObject assetPoolObject, PreviewPivot previewPivot)
        {
            var itemId = GetOriginalItemId(item.Id);
			Logger.Log(LogLevel.Info, "WeaponPreview", "Opened", itemId);
			if (IsCamoEditorWaitingForWeaponPreview)
			{
				SetupCamoEditor(weaponPreviewCamera, item, assetPoolObject, previewPivot);
			}
        }

        public void SetupCamoEditor(Camera editorCamera, Item item, AssetPoolObject assetPoolObject, PreviewPivot previewPivot)
        {
            var itemId = GetOriginalItemId(item.Id);
			Logger.Log(LogLevel.Info, "CamoEditor", "Setup", itemId);
            IsCamoEditorWaitingForWeaponPreview = false;
            var instanceID = assetPoolObject.gameObject.GetInstanceID();
            CamoEditor = new(new CamoEditor()
            {
                Plugin = this,
                BigPlugin = BigPlugin.Instance,
                CamoEditorResources = CamoEditorResources,
                Camera = editorCamera,
                ItemId = itemId,
                InstanceID = instanceID,
                AssetPoolObject = assetPoolObject,
                PreviewPivot = previewPivot,
            });
        }

        public int AddNewDecorator(string itemId, int instanceID, AssetPoolObject assetPoolObject, PreviewPivot previewPivot)
        {
            var decoratorInfo = new DecoratorInfo()
            {
                SchemaVersion = DecoratorInfo.CurrentSchemaVersion,
                Name = "",
                Prefab = "7Bpencil/cube", // TODO make default cube, which user cannot delete
                LocalPosition = previewPivot.pivotPosition, // TODO this doesnt work for weapons, because their decorator root is multiple levels deep
                LocalEulerAngles = Vector3.zero,
                LocalScale = Vector3.one,
                IsVisible = true,
            };

            if (ItemsWithDecorators.TryGetValue(itemId, out var itemsWithDecorators))
            {
                var decoratorIndex = itemsWithDecorators.DecoratorsInfo.Decorators.Count;
                SpawnNewDecoratorOnItems(itemId, decoratorIndex, decoratorInfo);
                return decoratorIndex;
            }
            else
            {
                CreateNewItemsWithDecorators(itemId, instanceID, assetPoolObject, decoratorInfo);
                return 0;
            }
        }

        public void SpawnNewDecoratorOnItems(string itemId, int decoratorIndex, DecoratorInfo decoratorInfo)
        {
            var itemsWithDecorators = ItemsWithDecorators[itemId];
            itemsWithDecorators.DecoratorsInfo.Decorators.Insert(decoratorIndex, decoratorInfo);
            foreach (var itemWithDecorators in itemsWithDecorators.Items.Values)
            {
                var decorator = CreateDecorator(decoratorInfo, itemWithDecorators.DecoratorsRoot);
                itemWithDecorators.Decorators.Insert(decoratorIndex, decorator);
            }
        }

        public Decorator CreateDecorator(DecoratorInfo decoratorInfo, Transform decoratorRoot)
        {
            var decorator = new GameObject("Decorator", typeof(Decorator)).GetComponent<Decorator>();
            decorator.Init(decoratorInfo, decoratorRoot);
            StartCoroutine(LoadPrefabAsset(decorator, decoratorInfo.Prefab));
            return decorator;
        }

        private Dictionary<string, AssetBundle> LoadedBundles = new();
        public IEnumerator LoadPrefabAsset(Decorator decorator, string prefabName)
        {
            // TODO check if file exist or if .Error = true and set error asset

            var prefabData = GetPrefabData(prefabName);
            if (!LoadedBundles.TryGetValue(prefabName, out var loadedAssetBundle))
            {
                var bundleLoadRequest = AssetBundle.LoadFromFileAsync(prefabData.BundleFilePath);
                yield return bundleLoadRequest;

                loadedAssetBundle = bundleLoadRequest.assetBundle;
                if (!loadedAssetBundle)
                {
                    prefabData.Error = true;
                    // TODO set error asset
                    Logger.Log(LogLevel.Error, "Prefab", "Failed to load");
                    yield break;
                }

                LoadedBundles.Add(prefabName, loadedAssetBundle);
            }
            // var bundleLoadRequest = AssetBundle.LoadFromFileAsync(prefabData.BundleFilePath);
            // yield return bundleLoadRequest;

            // var loadedAssetBundle = bundleLoadRequest.assetBundle;

            var assetLoadRequest = loadedAssetBundle.LoadAssetAsync<GameObject>(prefabData.AssetFilePath);
            yield return assetLoadRequest;

            // TODO check if decorator still exists, usual stuff
            if (assetLoadRequest.asset is GameObject prefab)
            {
                var prefabGameObject = Instantiate(prefab);
                decorator.Set(prefabGameObject);
            }

            // TODO should we unload it?
            // loadedAssetBundle.Unload(false);
        }

        public void CreateNewItemsWithDecorators(string itemId, int instanceID, AssetPoolObject assetPoolObject, DecoratorInfo decoratorInfo)
        {
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var decoratorsRoot = GetDecoratorsRoot(assetPoolObject);
            var decorator = CreateDecorator(decoratorInfo, decoratorsRoot);
            var decorators = new List<Decorator>() { decorator };
            var decoratorsInfo = new DecoratorsInfo()
            {
                SchemaVersion = DecoratorsInfo.CurrentSchemaVersion,
                SaveTime = time,
                Decorators = new() { decoratorInfo },
            };
            var itemsWithDecorators = new ItemsWithDecorators()
            {
                Items = new Dictionary<int, ItemWithDecorators>()
                {
                    {
                        instanceID,
                        new ItemWithDecorators()
                        {
                            DecoratorsRoot = decoratorsRoot,
                            Decorators = decorators,
                        }
                    }
                },
                DecoratorsInfo = decoratorsInfo
            };

            Logger.LogWarning($"New decorator: {itemId} {instanceID}");
            ItemsWithDecorators.Add(itemId, itemsWithDecorators);
            InstanceIdToItemId.Add(instanceID, itemId);
        }

        public void Delete(string itemId, int decoratorIndex)
        {
            var itemsWithDecorators = ItemsWithDecorators[itemId];
            var decoratorInfo = itemsWithDecorators.DecoratorsInfo.Decorators[decoratorIndex];
            itemsWithDecorators.DecoratorsInfo.Decorators.RemoveAt(decoratorIndex);
            foreach (var itemWithDecorators in itemsWithDecorators.Items.Values)
            {
                var decorator = itemWithDecorators.Decorators[decoratorIndex];
                itemWithDecorators.Decorators.RemoveAt(decoratorIndex);
                DestroyDecorator(decorator, decoratorInfo);
            }
        }

        public void ChangePrefab(string itemId, int decoratorIndex, DecoratorInfo decoratorInfo, string prefabName)
        {
            var oldPrefabName = decoratorInfo.Prefab;
            decoratorInfo.Prefab = prefabName;

            ModifyDecoratorOnItems(itemId, decoratorIndex, (decorator, decoratorInfo) =>
            {
                // TODO release asset data correctly
                if (decorator.Prefab)
                {
                    Destroy(decorator.Prefab);
                }

                // TODO rethink how assets are handled
                StartCoroutine(LoadPrefabAsset(decorator, decoratorInfo.Prefab));
            });
        }

        public void ApplyLocalPosition(string itemId, int decoratorIndex)
        {
            ModifyDecoratorOnItems(itemId, decoratorIndex, static (decorator, decoratorInfo) =>
            {
                decorator.DecoratorTransform.localPosition = decoratorInfo.LocalPosition;
            });
        }

        public void ApplyLocalEulerAngles(string itemId, int decoratorIndex)
        {
            ModifyDecoratorOnItems(itemId, decoratorIndex, static (decorator, decoratorInfo) =>
            {
                decorator.DecoratorTransform.localEulerAngles = decoratorInfo.LocalEulerAngles;
            });
        }

        public void ApplyLocalScale(string itemId, int decoratorIndex)
        {
            ModifyDecoratorOnItems(itemId, decoratorIndex, static (decorator, decoratorInfo) =>
            {
                decorator.DecoratorTransform.localScale = decoratorInfo.LocalScale;
            });
        }

        // notice that we modify decorator on all items
        public void ModifyDecoratorOnItems(string itemId, int decoratorIndex, Action<Decorator, DecoratorInfo> changeDecorator)
        {
            var itemsWithDecorators = ItemsWithDecorators[itemId];
            var decoratorInfo = itemsWithDecorators.DecoratorsInfo.Decorators[decoratorIndex];
            foreach (var itemWithDecorators in itemsWithDecorators.Items.Values)
            {
                var decorator = itemWithDecorators.Decorators[decoratorIndex];
                changeDecorator(decorator, decoratorInfo);
            }
        }

        public void DestroyDecorator(Decorator decorator, DecoratorInfo decoratorInfo)
        {
            // TODO clean resources
            if (decorator)
            {
                Destroy(decorator.gameObject);
            }
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
        // or tune decorator placement
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

            camoEditor.ForceOnEndedDraggingHandle();

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

        public Option<DecoratorInfo> GetDecoratorInfo(string itemId, int decoratorIndex)
        {
            if (!ItemsWithDecorators.TryGetValue(itemId, out var itemsWithDecorators))
            {
                return default;
            }

            return new(itemsWithDecorators.DecoratorsInfo.Decorators[decoratorIndex]);
        }

        public int GetDecoratorsCount(string itemId)
        {
            if (ItemsWithDecorators.TryGetValue(itemId, out var itemsWithDecorators))
            {
                return itemsWithDecorators.DecoratorsInfo.Decorators.Count;
            }

            return 0;
        }

        public int GetTotalDecoratorsCount()
        {
            return Decorators.Length;
        }

        public DecoratorPrefabData GetPrefabData(string prefabName)
        {
			if (DecoratorPrefabs.TryGetValue(prefabName, out var prefabData))
            {
                return prefabData;
            }

            throw new ArgumentException();
            // TODO we need error prefab,
            // I guess we will have to ship new bundle along weapon-camo-and-stickers
            // return ErrorTextureData;
        }

        // TODO this sometimes panics, no idea why
        public (DecoratorInfo, Decorator) GetDecorator(string itemId, int instanceID, int decoratorIndex)
        {
            var itemsWithDecorators = ItemsWithDecorators[itemId];
            var decoratorInfo = itemsWithDecorators.DecoratorsInfo.Decorators[decoratorIndex];
            var decorator = itemsWithDecorators.Items[instanceID].Decorators[decoratorIndex];
            return (decoratorInfo, decorator);
        }

        public string[] GetAllDecorators()
        {
            return Decorators;
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
