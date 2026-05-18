//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Comfort.Common;
using Diz.Skinning;
using Diz.Jobs;
using EFT;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using EFT.Visual;
using EFT.CameraControl;
using EFT.UI;
using EFT.UI.WeaponModding;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using SPT.Reflection.Patching;
using JetBrains.Annotations;
using HarmonyLib;
using UnityEngine;

namespace SevenBoldPencil.MaterialEditor
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

	public class Patch_PoolManagerClass_CreateItemAsync : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
			Type[] parameters = [typeof(Item), typeof(ECameraType), typeof(IPlayer), typeof(bool), typeof(GDelegate62), typeof(CancellationToken)];
            return AccessTools.Method(typeof(PoolManagerClass), nameof(PoolManagerClass.CreateItemAsync), parameters);
        }

        [PatchPrefix]
        public static void Prefix(PoolManagerClass __instance, Item item, ECameraType cameraType, [CanBeNull] IPlayer player, bool isAnimated, GDelegate62 yield, CancellationToken ct = default(CancellationToken))
		{
			Plugin.Instance.OnCreateItemAsync(item);
		}
	}

	public class Patch_PoolManagerClass_method_2 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PoolManagerClass), nameof(PoolManagerClass.method_2));
        }

        [PatchPostfix]
        public static void Postfix(PoolManagerClass __instance, GameObject __result, ResourceKey resourceKey, PoolManagerClass.PoolsCategory poolCategory)
		{
			Plugin.Instance.OnCreatedItemGameObject(resourceKey, __result);
		}
	}

	public class Patch_AssetPoolObject_ReturnToPool : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AssetPoolObject), nameof(AssetPoolObject.ReturnToPool));
        }

        [PatchPrefix]
        public static void Prefix(AssetPoolObject __instance)
		{
			Plugin.Instance.OnItemDestroyed(__instance);
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
			Plugin.Instance.OnItemDestroyed(__instance);
		}
	}

	public class Patch_ItemUiContext_GetItemContextInteractions : ModulePatch
	{
	    protected override MethodBase GetTargetMethod()
	    {
	        return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.GetItemContextInteractions));
	    }

	    [PatchPostfix]
	    private static void Postfix(ItemInfoInteractionsAbstractClass<EItemInfoButton> __result, ItemUiContext __instance, ItemContextClass itemContext)
	    {
			if (itemContext.ViewType != EItemViewType.Inventory)
			{
				return;
			}
			if (SevenBoldPencil.WeaponCamoAndStickers.Patch_ItemUiContext_GetItemContextInteractions.InRaid())
			{
				return;
			}

			var __result__ = new SevenBoldPencil.WeaponCamoAndStickers.ItemInfoInteractionsAbstractClass_Proxy<EItemInfoButton>(__result);
			var interactions = __result__.Dictionary_0;
			var item = itemContext.Item;

			var key = "CHANGE MATERIAL";
			var icon = EFTHardSettings.Instance.StaticIcons.WishlistSprites[EWishlistGroup.Other];
	        interactions[key] = new SevenBoldPencil.WeaponCamoAndStickers.Custom_DynamicInteractionClass(item.Id, key, () => OpenChangeMaterialWindow(__result), icon)
			{
				NonInteractiveTooltip = SevenBoldPencil.WeaponCamoAndStickers.Patch_ItemUiContext_GetItemContextInteractions.GetRequiresBenchTooltip(),
			};
	    }

		public static void OpenChangeMaterialWindow(ItemInfoInteractionsAbstractClass<EItemInfoButton> result)
		{
			if (result is ContextInteractionsAbstractClass gclass)
			{
				Plugin.Instance.WaitForWeaponPreview();
				gclass.method_28();
			}
		}
	}

	// this method tries to initialize gui for all slots in weapon,
	// if item is not compound item there are no slots, so safeguard it
	public class Patch_WeaponModdingScreen_method_6 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponModdingScreen), nameof(WeaponModdingScreen.method_6));
        }

        [PatchPrefix]
        public static bool Prefix(WeaponModdingScreen __instance, CompoundItem weapon)
		{
			if (Plugin.Instance.IsWaitingForWeaponPreview())
			{
				return false;
			}

			return weapon is CompoundItem;
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
			if (TryGetAssetPoolObject(_weaponPreview, out var assetPoolObject))
			{
				Plugin.Instance.OnWeaponPreviewOpened(item, assetPoolObject);
			}
		}

		public static bool TryGetAssetPoolObject(WeaponPreview_Proxy weaponPreview, out AssetPoolObject assetPoolObject)
		{
			// it takes time to load gameObjects so if you ask too early they will be null
			var itemGO = weaponPreview.gameObject_0;

			if (itemGO && itemGO.TryGetComponent<AssetPoolObject>(out assetPoolObject))
			{
				return true;
			}

			assetPoolObject = default;
			return false;
		}
	}

	public class Patch_WeaponModdingScreen_Close : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponModdingScreen), nameof(WeaponModdingScreen.Close));
        }

        [PatchPrefix]
        public static void Prefix(WeaponModdingScreen __instance)
		{
			Plugin.Instance.CloseCamoEditor();
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
        public static void Postfix(GClass3380 __instance, Item __result, Item originalItem, IIdGenerator idGenerator = null, bool skipInvisibleContent = false, bool resetSpawnedInSession = false)
		{
			Plugin.Instance.OnCloneItem(originalItem.Id, __result.Id);
		}
	}

	public class Patch_GClass928_GetItemHash : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass928), nameof(GClass928.GetItemHash));
        }

        [PatchPostfix]
        public static void Postfix(Item item, ref int __result)
		{
			if (Plugin.Instance.GetMaterialsInfo(item.Id).Some(out var materialsInfo) && materialsInfo.Materials.Count > 0)
			{
				// all this shit to fit SaveTime inside int
				var saveTime = materialsInfo.SaveTime;
				var newStartPoint = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				var newStartPointOffset = new DateTimeOffset(newStartPoint).ToUnixTimeMilliseconds();
				var saveTimeOffset = saveTime - newStartPointOffset;
				var saveTimeOffsetSeconds = (int)(saveTimeOffset / 1000);
				__result ^= saveTimeOffsetSeconds;
			}
		}
	}

	public class Patch_HotObject_SetTemperatureToRenderer : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
			Type[] parameters = [typeof(float), typeof(bool)];
            return AccessTools.Method(typeof(HotObject), nameof(HotObject.SetTemperatureToRenderer), parameters);
        }

        [PatchPrefix]
        public static bool Prefix(Renderer ___renderer_0, float temperatureCelsio, bool force = false)
		{
			// HotObjects (barrels, silencers, etc) override renderer materials parameters (_HeatSize, _HeatTemp, etc)
			// the same as way we do via MaterialPropertyBlock, which results in them overriding our changes,
			// so stop them from doing that! (maybe we could combine their changes, but its already complicated enough)
			return !Plugin.Instance.IsPatchedRenderer(___renderer_0);
		}
	}

	public class Patch_RainCondensator_UpdateValues : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RainCondensator), nameof(RainCondensator.UpdateValues));
        }

        [PatchPrefix]
        public static bool Prefix(Renderer ___renderer_0)
		{
			// RainCondensator works the same way as HotObject
			return !Plugin.Instance.IsPatchedRenderer(___renderer_0);
		}
	}
}
