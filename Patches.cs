using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DeferredDecals;
using EFT;
using EFT.AssetsManager;
using EFT.Hideout;
using EFT.Hideout.ShootingRange;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.SynchronizableObjects;
using EFT.UI;
using EFT.UI.Screens;
using EFT.UI.WeaponModding;
using EFT.Vehicle;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using SPT.Reflection.Patching;
using JetBrains.Annotations;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace SevenBoldPencil.WeaponCamo
{
	public struct WeaponPreview_Proxy
	{
		private static TypedFieldInfo<WeaponPreview, GameObject> __gameObject_0 = new("gameObject_0");
		private static TypedFieldInfo<WeaponPreview, Item> __item_0 = new("item_0");

		public GameObject gameObject_0 { get { return __gameObject_0.Get(__instance); } set { __gameObject_0.Set(__instance, value); } }
		public Item item_0 { get { return __item_0.Get(__instance); } set { __item_0.Set(__instance, value); } }

        private WeaponPreview __instance;

        public WeaponPreview_Proxy(WeaponPreview instance)
        {
            __instance = instance;
        }
	}

	public struct WeaponPrefab_Proxy
	{
		private static TypedFieldInfo<WeaponPrefab, Weapon> __weapon_0 = new("weapon_0");

		public Weapon weapon_0 { get { return __weapon_0.Get(__instance); } set { __weapon_0.Set(__instance, value); } }

        private WeaponPrefab __instance;

        public WeaponPrefab_Proxy(WeaponPrefab instance)
        {
            __instance = instance;
        }
	}

	public class Patch_WeaponPreview_Class3271_method_1 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponPreview.Class3271), nameof(WeaponPreview.Class3271.method_1));
        }

        [PatchPostfix]
        public static void Postfix(WeaponPreview.Class3271 __instance)
        {
			// this called when item preview or weapon customization screen is opened and fully initialized,
			// but from here we dont know where item preview is used,
			// so we can only spawn decals associated with this item
   			var weaponPreview = __instance.weaponPreview_0;
			var _weaponPreview = new WeaponPreview_Proxy(__instance.weaponPreview_0);
			var item = _weaponPreview.item_0;
			if (item == null)
			{
				return;
			}

			if (TryGetWeaponPrefab(_weaponPreview, out var weaponPrefab))
			{
				if (Plugin.Instance.IsCamoEditorWaitingForWeaponPreview)
				{
					var itemId = item.Id;
					var camera = weaponPreview.WeaponPreviewCamera;
					Plugin.Instance.SetupCamoEditor(camera, itemId, weaponPrefab);
				}
			}
		}

		public static bool TryGetWeaponPrefab(WeaponPreview_Proxy weaponPreview, out WeaponPrefab weaponPrefab)
		{
			// it takes time to load gameobjects so if you ask too early they will be null
			var itemGO = weaponPreview.gameObject_0;
			if (itemGO && itemGO.TryGetComponent<WeaponPrefab>(out weaponPrefab))
			{
				return true;
			}

			weaponPrefab = default;
			return false;
		}
	}

	public class Patch_WeaponModdingScreen_Show : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponModdingScreen), nameof(WeaponModdingScreen.Show), [typeof(Item), typeof(InventoryController), typeof(CompoundItem[])]);
        }

        [PatchPostfix]
        public static void Postfix(WeaponModdingScreen __instance, Item item, InventoryController inventoryController, CompoundItem[] collections)
		{
			// this is called when user pressed modify on weapon context menu
			// we use this now because user can modify only weapons that he actually access to,
			// unlike trader guns, or guns in builds window
			//
			// if this method is called then next WeaponPreview.Class3271.method_1
			// is guaranteed to be weapon preview for WeaponModdingScreen

			Plugin.Instance.IsCamoEditorWaitingForWeaponPreview = true;
		}
	}

	public class Patch_WeaponModdingScreen_Close : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponModdingScreen), nameof(WeaponModdingScreen.Close));
        }

        [PatchPostfix]
        public static void Postfix(WeaponModdingScreen __instance)
		{
			Plugin.Instance.CloseCamoEditor();
		}
	}

	public class Patch_WeaponPrefab_InitHotObjects : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponPrefab), nameof(WeaponPrefab.InitHotObjects));
        }

        [PatchPostfix]
        public static void Postfix(WeaponPrefab __instance)
		{
			// believe it or not, but InitHotObjects is THE method,
			// that actually sets up weapon model and shit,
			// just keep in mind that it can be called on already init WeaponPrefab
			var __instance__ = new WeaponPrefab_Proxy(__instance);
			var item = __instance__.weapon_0;
			if (item != null)
			{
				Plugin.Instance.OnWeaponPrefabCreated(item.Id, __instance);
			}
		}
	}

	public class Patch_WeaponPrefab_ReturnToPool : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponPrefab), nameof(WeaponPrefab.ReturnToPool));
        }

        [PatchPrefix]
        public static void Prefix(WeaponPrefab __instance)
		{
			var __instance__ = new WeaponPrefab_Proxy(__instance);
			var item = __instance__.weapon_0;
			if (item != null)
			{
				Plugin.Instance.OnWeaponPrefabDestroyed(item.Id, __instance);
			}
		}
	}

	public class Patch_AssetPoolObject_OnDestroy : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AssetPoolObject), nameof(AssetPoolObject.OnDestroy));
        }

        [PatchPrefix]
        public static void Prefix(AssetPoolObject __instance)
		{
			// WeaponPrefab doesn't override OnDestroy, so we have to do it this way
			if (__instance is WeaponPrefab weaponPrefab)
			{
				var _weaponPrefab = new WeaponPrefab_Proxy(weaponPrefab);
				var item = _weaponPrefab.weapon_0;
				if (item != null)
				{
					Plugin.Instance.OnWeaponPrefabDestroyed(item.Id, weaponPrefab);
				}
			}
		}
	}

	public class Patch_GClass3380_CloneItem : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
			Type[] generics = [typeof(Item)];
            return AccessTools.Method(typeof(GClass3380), nameof(GClass3380.CloneItem), null, generics);
        }

        [PatchPostfix]
        public static void Postfix(GClass3380 __instance, ref Item __result, Item originalItem)
		{
			// only weapons support for now, to not ddos hash tables
			if (originalItem is Weapon weapon)
			{
				Plugin.Instance.OnCloneItem(weapon.Id, __result.Id);
			}
		}
	}
}
