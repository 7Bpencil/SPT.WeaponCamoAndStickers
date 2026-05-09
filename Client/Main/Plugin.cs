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

namespace SevenBoldPencil.ChangeEquipmentColor
{
    public class ItemsWithDecals
    {
        // yes, there can be multiple items with same Id,
        // for example when you open item preview of weapon you already hold in hands,
        // or when hideout shooting range clones weapon (we pretend that they have the same Id)
        public Dictionary<int, LODGroup> Items; // TODO iterating dict is probably not the best idea, but list in annoying
        public DecalInfo DecalInfo;
    }

    public class DecalInfo
    {
        public const int CurrentSchemaVersion = 0;

        public int SchemaVersion;
        public long SaveTime;
        public Vector4 ColorHSVA;

        public DecalInfo GetCopy()
        {
            // this is enough for now
            return (DecalInfo)MemberwiseClone();
        }
    }

    [BepInPlugin("7Bpencil.ChangeEquipmentColor", "7Bpencil.ChangeEquipmentColor", "1.8.0")]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private const double SaveLagTime = 60;

        public static Plugin Instance;

		public ManualLogSource LoggerInstance;

        private string PresetsPath;
        private string ItemsPath;

        private Dictionary<string, DecalInfo> DecalPresets;
        private Dictionary<string, ItemsWithDecals> ItemsWithDecals;
        private Dictionary<string, string> Clones;
        private Option<double> LastPresetsSaveTime;
        private Option<double> LastItemsSaveTime;

        public bool IsFikaSupportEnabled;
        public bool IsFikaHeadless;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            PresetsPath = Path.Combine(assemblyDir, "items.json");
            ItemsPath = Path.Combine(assemblyDir, "presets.json");
			var bundlePath = Path.Combine(assemblyDir, "bundles", "change-equipment-color");
            var bundle = AssetBundle.LoadFromFile(bundlePath);

            DecalPresets = LoadDecalPresets(PresetsPath);
            ItemsWithDecals = LoadItemsWithDecals(ItemsPath);
            Clones = new();

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

        public Dictionary<string, DecalInfo> LoadDecalPresets(string filePath)
        {
            var result = new Dictionary<string, DecalInfo>();
            return result;
        }

        public Dictionary<string, ItemsWithDecals> LoadItemsWithDecals(string filePath)
        {
            var result = new Dictionary<string, ItemsWithDecals>();
            return result;
        }

        public void Update()
        {
            // SaveLagTime and LastSaveTime are needed to not write to file
            // every time user changes scope transparency mode

            if (LastItemsSaveTime.Some(out var lastItemsSaveTime))
            {
                if (Time.realtimeSinceStartupAsDouble - lastItemsSaveTime >= SaveLagTime)
                {
                    // SaveTransparentScopesToFile(ConfigPath, TransparentScopes);
                    LastItemsSaveTime = default;
                }
            }
            if (LastPresetsSaveTime.Some(out var lastPresetsSaveTime))
            {
                if (Time.realtimeSinceStartupAsDouble - lastPresetsSaveTime >= SaveLagTime)
                {
                    // SaveTransparentScopesToFile(ConfigPath, TransparentScopes);
                    LastPresetsSaveTime = default;
                }
            }
        }

        public Option<DecalInfo> GetDecalInfo(string itemId)
        {
            if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                return new(itemsWithDecals.DecalInfo);
            }

            return default;
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

        public Dictionary<string, DecalInfo> SnapshotLocalDecals()
        {
            var snapshot = new Dictionary<string, DecalInfo>();
    		if (!TarkovApplication.Exist(out var tarkovApplication))
            {
                return snapshot;
            }

            // copies all guns inside player equipment (on hands/sling/holster, inside backpack, rig, etc)
            var profile = tarkovApplication.Session.Profile;
            var equipmentItems = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment);

            foreach (var item in equipmentItems)
            {
                if (ItemsWithDecals.TryGetValue(item.Id, out var itemsWithDecals))
                {
                    snapshot[item.Id] = itemsWithDecals.DecalInfo.GetCopy();
                }
            }

            return snapshot;
        }

        public void IngestRemoteDecals(Dictionary<string, DecalInfo> remoteDecals)
        {
            foreach (var (itemId, decalInfo) in remoteDecals)
            {
                IngestRemoteDecals(itemId, decalInfo);
            }
        }

        public void IngestRemoteDecals(string itemId, DecalInfo remoteDecalInfo)
        {
            // TODO not sure if copying remoteDecalsInfo is necessary
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                // pick newer version
                var itemsWithDecals = ItemsWithDecals[itemId];
                if (itemsWithDecals.DecalInfo.SaveTime >= remoteDecalInfo.SaveTime)
                {
                    Logger.LogInfo($"IngestRemoteDecals: {itemId}, mine is newer");
                    return;
                }

                itemsWithDecals.DecalInfo = remoteDecalInfo.GetCopy();
                WriteDecalsToFile();
                Logger.LogInfo($"IngestRemoteDecals: {itemId}, his is newer, already spawned count: {itemsWithDecals.Items.Count}");
            }
            else
            {
                Logger.LogInfo($"IngestRemoteDecals: {itemId}, new");
                var decalInfo = remoteDecalInfo.GetCopy();
                var itemsWithDecals = new ItemsWithDecals()
                {
                    Items = new(),
                    DecalInfo = decalInfo,
                };

                ItemsWithDecals.Add(itemId, itemsWithDecals);
                WriteDecalsToFile();
            }
        }

        public void WriteDecalsToFile()
        {
            LastItemsSaveTime = new(Time.realtimeSinceStartupAsDouble);
        }

    }
}
