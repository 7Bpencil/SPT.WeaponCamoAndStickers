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
using EFT.Utilities;
using SevenBoldPencil.Common;
using System;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using SPT.Reflection.Patching;
using JetBrains.Annotations;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

using WeaponPreview_Proxy = SevenBoldPencil.WeaponCamoAndStickers.WeaponPreview_Proxy;
using Patch_PlayerModelView_method_0 = SevenBoldPencil.WeaponCamoAndStickers.Patch_PlayerModelView_method_0;

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

	public struct InventoryPlayerModelWithStatsWindow_Proxy(InventoryPlayerModelWithStatsWindow instance)
	{
		private static TypedFieldInfo<InventoryPlayerModelWithStatsWindow, PlayerModelView> __playerModelView = new("_playerModelView");
		private static TypedFieldInfo<InventoryPlayerModelWithStatsWindow, XCoordRotation> __rotator = new("_rotator");
		private static TypedFieldInfo<InventoryPlayerModelWithStatsWindow, DragTrigger> __dragTrigger = new("_dragTrigger");
		private static TypedFieldInfo<InventoryPlayerModelWithStatsWindow, AddViewListClass> _UI = new("UI");

		public PlayerModelView _playerModelView { get { return __playerModelView.Get(__instance); } set { __playerModelView.Set(__instance, value); } }
		public XCoordRotation _rotator { get { return __rotator.Get(__instance); } set { __rotator.Set(__instance, value); } }
		public DragTrigger _dragTrigger { get { return __dragTrigger.Get(__instance); } set { __dragTrigger.Set(__instance, value); } }
		public AddViewListClass UI { get { return _UI.Get(__instance); } set { _UI.Set(__instance, value); } }

        private readonly InventoryPlayerModelWithStatsWindow __instance = instance;
	}

	public struct ScrollTrigger_Proxy(ScrollTrigger instance)
	{
		private static TypedFieldInfo<ScrollTrigger, Action<PointerEventData>> _action_0 = new("action_0");

		public Action<PointerEventData> action_0 { get { return _action_0.Get(__instance); } set { _action_0.Set(__instance, value); } }

        private readonly ScrollTrigger __instance = instance;
	}

	public struct DragTrigger_Proxy(DragTrigger instance)
	{
		private static TypedFieldInfo<DragTrigger, Action<PointerEventData>> _onDrag = new("onDrag");

		public Action<PointerEventData> onDrag { get { return _onDrag.Get(__instance); } set { _onDrag.Set(__instance, value); } }

        private readonly DragTrigger __instance = instance;
	}

	// this method is called when PlayerModelView is loaded
	public class Patch_InventoryPlayerModelWithStatsWindow_method_5 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryPlayerModelWithStatsWindow), nameof(InventoryPlayerModelWithStatsWindow.method_5));
        }

        private const float MAX_ZOOM_IN_Z = 4.69f;
        private const float MAX_ZOOM_OUT_Z = 0.49f; // not sure if 0.49 is correct value for all cases
		private const float MIN_Y = -1f;
		private const float MAX_Y = 1f;

        [PatchPrefix]
        public static bool Prefix(InventoryPlayerModelWithStatsWindow __instance)
		{
			// TODO deltatime? probably no need to
			// TODO tyfon ui fixes collision: Say people to disable "Limit Nonstandard Drags" in F12 menu

			var __instance__ = new InventoryPlayerModelWithStatsWindow_Proxy(__instance);
			var _playerModelView = __instance__._playerModelView;
			var _rotator = __instance__._rotator;
			var _dragTrigger = __instance__._dragTrigger;
			var UI = __instance__.UI;

			if (!_playerModelView)
			{
				return false;
			}
			if (!Patch_PlayerModelView_method_0.TryGetCamera(_playerModelView).Some(out var camera))
			{
				return false;
			}

			var cameraTransform = camera.transform;
			camera.nearClipPlane = 0.1f;
			cameraTransform.localPosition = new(-0.0001f, -0.2f, 0.49f);

			_rotator.Init(_playerModelView.ModelPlayerPoser.transform);

			var dragTriggerGO = _dragTrigger.gameObject;
			if (!dragTriggerGO.TryGetComponent<ScrollTrigger>(out var scrollTrigger))
			{
				scrollTrigger = dragTriggerGO.AddComponent<ScrollTrigger>();
			}

			var __dragTrigger = new DragTrigger_Proxy(_dragTrigger);
			var _scrollTrigger = new ScrollTrigger_Proxy(scrollTrigger);

			__dragTrigger.onDrag = null;
			_dragTrigger.onDrag += (pointerData) => RotatePanCamera(pointerData, __instance, cameraTransform);

			_scrollTrigger.action_0 = null;
			scrollTrigger.OnOnScroll += (pointerData) => ZoomCamera(pointerData, cameraTransform);

			UI.AddDisposable(delegate
			{
				__dragTrigger.onDrag = null;
				_scrollTrigger.action_0 = null;
			});

			return false;
		}

		public static void ZoomCamera(PointerEventData pointerData, Transform cameraTransform)
		{
			var zoom = pointerData.scrollDelta.y * 0.12f;

            cameraTransform.Translate(Vector3.forward * zoom);

            var localPosition = cameraTransform.localPosition;
            localPosition.z = Mathf.Clamp(localPosition.z, MAX_ZOOM_OUT_Z, MAX_ZOOM_IN_Z);
            cameraTransform.localPosition = localPosition;
		}

		public static void RotatePanCamera(PointerEventData pointerData, InventoryPlayerModelWithStatsWindow __instance, Transform cameraTransform)
		{
			if (pointerData.button == PointerEventData.InputButton.Left)
			{
				// rotate
				__instance.method_4(pointerData);
			}
			if (pointerData.button == PointerEventData.InputButton.Middle)
			{
				// pan
				var baseSpeed = 0.001f;
	            var currentZ = cameraTransform.localPosition.z;
	            var zoomFactor = GetZoomFactor(currentZ);
	            var currentPanSpeed = baseSpeed * zoomFactor;
	            var deltaMove = pointerData.delta.y * currentPanSpeed * -1;
				cameraTransform.Translate(Vector3.up * deltaMove);

	            var localPosition = cameraTransform.localPosition;
	            localPosition.y = Mathf.Clamp(localPosition.y, MIN_Y, MAX_Y);
	            cameraTransform.localPosition = localPosition;
			}
		}

		public static float GetZoomFactor(float x)
		{
			// approximation of:
			// x: 0.49 1.09 1.57 1.81 2.05 3.13 3.73 4.00 4.33 4.57 4.69
			// y: 2.68 2.33 1.98 1.9 1.67 1 0.65 0.4925 0.33 0.2 0.13
			var x3 = x * x * x;
			var x2 = x * x;
			return 0.0082f * x3 - 0.0491f * x2 - 0.5536f * x + 2.9671f;
		}
	}

}
