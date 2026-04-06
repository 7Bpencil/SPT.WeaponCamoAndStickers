//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Diz.Skinning;
using EFT;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using EFT.Visual;
using EFT.UI;
using EFT.UI.WeaponModding;
using SevenBoldPencil.Common;
using System;
using System.Reflection;
using System.Collections.Generic;
using SPT.Reflection.Patching;
using HarmonyLib;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers
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

	public struct LoddedSkin_Proxy
	{
		private static TypedFieldInfo<LoddedSkin, AbstractSkin[]> __lods = new("_lods");

		public AbstractSkin[] _lods { get { return __lods.Get(__instance); } set { __lods.Set(__instance, value); } }

        private LoddedSkin __instance;

        public LoddedSkin_Proxy(LoddedSkin instance)
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
			// this called when WeaponPreview is opened and fully initialized,
			// WeaponPreview is used both by weapon modding screen and item overview
   			var weaponPreview = __instance.weaponPreview_0;
			var _weaponPreview = new WeaponPreview_Proxy(__instance.weaponPreview_0);
			var item = _weaponPreview.item_0;
			if (item == null)
			{
				return;
			}
			if (TryGetWeaponPrefab(_weaponPreview, out var weaponPrefab, out var previewPivot))
			{
				var itemId = item.Id;
				var camera = weaponPreview.WeaponPreviewCamera;
				Plugin.Instance.OnWeaponPreviewOpened(camera, itemId, weaponPrefab, weaponPreview.Rotator, previewPivot);
			}
		}

		public static bool TryGetWeaponPrefab(WeaponPreview_Proxy weaponPreview, out WeaponPrefab weaponPrefab, out PreviewPivot previewPivot)
		{
			// it takes time to load gameObjects so if you ask too early they will be null
			var itemGO = weaponPreview.gameObject_0;

			if (itemGO &&
				itemGO.TryGetComponent<WeaponPrefab>(out weaponPrefab) &&
				itemGO.TryGetComponent<PreviewPivot>(out previewPivot))
			{
				return true;
			}

			weaponPrefab = default;
			previewPivot = default;
			return false;
		}
	}

	public class Patch_WeaponPreview_Rotate : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponPreview), nameof(WeaponPreview.Rotate));
        }

        [PatchPrefix]
        public static bool Prefix(WeaponPreview __instance)
		{
			var _weaponPreview = new WeaponPreview_Proxy(__instance);
			var item = _weaponPreview.item_0;
			if (item != null)
			{
				return Plugin.Instance.CanWeaponPreviewRotate(item.Id);
			}

			return true;
		}
	}

	public class Patch_WeaponPreview_Hide : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponPreview), nameof(WeaponPreview.Hide));
        }

        [PatchPrefix]
        public static bool Prefix(WeaponPreview __instance)
		{
			var _weaponPreview = new WeaponPreview_Proxy(__instance);
			var item = _weaponPreview.item_0;
			if (item != null)
			{
				var camera = __instance.WeaponPreviewCamera;
				if (camera)
				{
					Plugin.Instance.OnWeaponPreviewClosed(camera, item.Id);
				}
			}

			return true;
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
			// this is called when user presses modify on weapon context menu
			// we use modding screen because user can only modify weapons that he actually has access to,
			// unlike trader guns, or guns in builds window
			//
			// if this method is called then next WeaponPreview.Class3271.method_1
			// is guaranteed to be weapon preview for this WeaponModdingScreen

			Plugin.Instance.WaitForWeaponPreview();
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
			// Some WeaponPrefabs return to pools, others simply get destroyed,
			// notice WeaponPrefab doesn't override OnDestroy, so we have to do it this way
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

	// this method is used everywhere to clone items:
	// - hideout shooting range
	// - raid loading screen
	// - raid exit screen
	// - profile overview screen
	public class Patch_GClass3380_smethod_2 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
			Type[] parameters = null;
			Type[] generics = [typeof(Item)];
            return AccessTools.Method(typeof(GClass3380), nameof(GClass3380.smethod_2), parameters, generics);
        }

        [PatchPostfix]
        public static void Postfix(GClass3380 __instance, ref Item __result, Item originalItem, IIdGenerator idGenerator = null, bool skipInvisibleContent = false, bool resetSpawnedInSession = false)
		{
			// only weapons support for now
			if (originalItem is Weapon weapon)
			{
				Plugin.Instance.OnCloneItem(weapon.Id, __result.Id);
			}
		}
	}

	// this method is used everywhere to set cursor visible or invisible
	public class Patch_GClass2304_smethod_0 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass2304), nameof(GClass2304.smethod_0));
        }

        [PatchPrefix]
        public static bool Prefix(GClass2304 __instance, bool isCursorVisible)
		{
			if (!isCursorVisible)
			{
				return Plugin.Instance.CanHideCursor();
			}

			return true;
		}
	}

	// this method is called when PlayerModelView is opened and finishes loading
	public class Patch_PlayerModelView_method_0 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerModelView), nameof(PlayerModelView.method_0));
        }

        [PatchPostfix]
        public static void Postfix(PlayerModelView __instance)
		{
			var instanceTransform = __instance.transform;
			for (var i = 0; i < instanceTransform.childCount; i++)
			{
				var child = instanceTransform.GetChild(i);
				if (child.TryGetComponent<Camera>(out var camera))
				{
					Plugin.Instance.OnPlayerModelViewShown(camera);
					break;
				}
			}
		}
	}

	// this method is called when PlayerModelView is closed
	public class Patch_PlayerModelView_method_1 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerModelView), nameof(PlayerModelView.method_1));
        }

        [PatchPrefix]
        public static void Prefix(PlayerModelView __instance)
		{
			var instanceTransform = __instance.transform;
			for (var i = 0; i < instanceTransform.childCount; i++)
			{
				var child = instanceTransform.GetChild(i);
				if (child.TryGetComponent<Camera>(out var camera))
				{
					Plugin.Instance.OnPlayerModelViewClosed(camera);
					break;
				}
			}
		}
	}

	public class Patch_PlayerBody_SetSkin : ModulePatch
	{
		public static readonly int _StencilType = Shader.PropertyToID("_StencilType");

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerBody), nameof(PlayerBody.SetSkin));
        }

        [PatchPostfix]
        public static void Postfix(PlayerBody __instance, KeyValuePair<EBodyModelPart, ResourceKey> part, Skeleton skeleton)
		{
			var skin = __instance.BodySkins[part.Key];
			var _skin = new LoddedSkin_Proxy(skin);
			foreach (var lod in _skin._lods)
			{
				var skinnedMeshRenderer = lod.SkinnedMeshRenderer;
                foreach (var material in skinnedMeshRenderer.materials)
                {
					var shaderName = material.shader.name;
					if (shaderName == "p0/Reflective/Bumped Specular SMap" ||
						shaderName == "p0/Reflective/Bumped Specular SMap_Decal")
					{
						// decal shader works only on fragments with _StencilType = 2
						// so set everything on player body to 1, to keept it clean from decals
						material.SetFloat(_StencilType, 1);
					}
                }
			}
		}
	}

}
