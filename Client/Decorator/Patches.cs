//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Comfort.Common;
using Diz.Skinning;
using EFT;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using EFT.Visual;
using EFT.CameraControl;
using EFT.UI;
using EFT.UI.WeaponModding;
using System;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using SPT.Reflection.Patching;
using JetBrains.Annotations;
using HarmonyLib;
using UnityEngine;

using WeaponPreview_Proxy = SevenBoldPencil.WeaponCamoAndStickers.WeaponPreview_Proxy;

namespace SevenBoldPencil.Decorator
{
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
			if (WeaponCamoAndStickers.Patch_ItemUiContext_GetItemContextInteractions.InRaid())
			{
				return;
			}

			var __result__ = new WeaponCamoAndStickers.ItemInfoInteractionsAbstractClass_Proxy<EItemInfoButton>(__result);
			var interactions = __result__.Dictionary_0;
			var item = itemContext.Item;

			var key = "DECORATE";
			var icon = EFTHardSettings.Instance.StaticIcons.WishlistSprites[EWishlistGroup.Other];
	        interactions[key] = new WeaponCamoAndStickers.Custom_DynamicInteractionClass(item.Id, key, () => OpenDecorateWindow(__result), icon)
			{
				NonInteractiveTooltip = WeaponCamoAndStickers.Patch_ItemUiContext_GetItemContextInteractions.GetRequiresBenchTooltip(),
			};
	    }

		public static void OpenDecorateWindow(ItemInfoInteractionsAbstractClass<EItemInfoButton> result)
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
			if (TryGetAssetPoolObject(_weaponPreview, out var assetPoolObject, out var previewPivot))
			{
				var camera = weaponPreview.WeaponPreviewCamera;
				Plugin.Instance.OnWeaponPreviewOpened(camera, item, assetPoolObject, previewPivot);
			}
		}

		public static bool TryGetAssetPoolObject(WeaponPreview_Proxy weaponPreview, out AssetPoolObject assetPoolObject, out PreviewPivot previewPivot)
		{
			// it takes time to load gameObjects so if you ask too early they will be null
			var itemGO = weaponPreview.gameObject_0;

			if (itemGO &&
				itemGO.TryGetComponent<AssetPoolObject>(out assetPoolObject) &&
				itemGO.TryGetComponent<PreviewPivot>(out previewPivot))
			{
				return true;
			}

			assetPoolObject = default;
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
				return Plugin.Instance.CanWeaponPreviewRotate();
			}

			return true;
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
			if (Plugin.Instance.GetDecoratorsInfo(item.Id).Some(out var decoratorsInfo) && decoratorsInfo.Decorators.Count > 0)
			{
				__result ^= WeaponCamoAndStickers.Patch_GClass928_GetItemHash.GetSaveTimeInt(decoratorsInfo.SaveTime);
			}
		}
	}

}
