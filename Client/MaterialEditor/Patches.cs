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
using EFT.Counters;
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

namespace SevenBoldPencil.MaterialEditor
{
	public class Patch_ObjectsFactory_CreateItemAsync : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
			Type[] parameters = [typeof(Item), typeof(ECameraType), typeof(IPlayer), typeof(bool), typeof(YieldDelegate), typeof(CancellationToken)];
            return AccessTools.Method(typeof(ObjectsFactory), nameof(ObjectsFactory.CreateItemAsync), parameters);
        }

        [PatchPrefix]
        public static void Prefix(ObjectsFactory __instance, Item item, ECameraType cameraType, [CanBeNull] IPlayer player, bool isAnimated, YieldDelegate yield, CancellationToken ct = default(CancellationToken))
		{
			Plugin.Instance.OnCreateItemAsync(item);
		}
	}

	public class Patch_ObjectsFactory_PopOrCreate : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ObjectsFactory), nameof(ObjectsFactory.PopOrCreate));
        }

        [PatchPostfix]
        public static void Postfix(ObjectsFactory __instance, GameObject __result, ResourceKey resourceKey, ObjectsFactory.PoolsCategory poolCategory)
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
	    private static void Postfix(ContextInteractions<EItemInfoButton> __result, ItemUiContext __instance, DragItemContext itemContext)
	    {
			if (itemContext.ViewType != EItemViewType.Inventory)
			{
				return;
			}
			if (WeaponCamoAndStickers.Patch_ItemUiContext_GetItemContextInteractions.InRaid())
			{
				return;
			}

			var interactions = __result._dynamicInteractions;
			var item = itemContext.Item;

			var key = "CHANGE MATERIAL";
			var icon = EFTHardSettings.Instance.StaticIcons.WishlistSprites[EWishlistGroup.Other];
	        interactions[key] = new WeaponCamoAndStickers.Custom_DynamicContextInteraction(item.Id, key, () => OpenChangeMaterialWindow(__result), icon)
			{
				NonInteractiveTooltip = WeaponCamoAndStickers.Patch_ItemUiContext_GetItemContextInteractions.GetRequiresBenchTooltip(),
			};
	    }

		public static void OpenChangeMaterialWindow(ContextInteractions<EItemInfoButton> result)
		{
			if (result is BaseItemContextInteractions gclass)
			{
				Plugin.Instance.WaitForWeaponPreview();
				gclass.method_28();
			}
		}
	}

	// this method tries to initialize gui for all slots in weapon,
	// if item is not compound item there are no slots, so safeguard it
	public class Patch_WeaponModdingScreen_CreateModSlotViews : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponModdingScreen), nameof(WeaponModdingScreen.CreateModSlotViews));
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
	public class Patch_ClientApplicationInitOperation_CursorVisibilityChangedHandler : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ClientApplicationInitOperation), nameof(ClientApplicationInitOperation.CursorVisibilityChangedHandler));
        }

        [PatchPrefix]
        public static bool Prefix(ClientApplicationInitOperation __instance, bool isCursorVisible)
		{
			if (!isCursorVisible)
			{
				return Plugin.Instance.CanHideCursor();
			}

			return true;
		}
	}

	public class Patch_WeaponPreview_CG_SetupItemPreview_method_1 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponPreview.CG_SetupItemPreview), nameof(WeaponPreview.CG_SetupItemPreview.method_1));
        }

        [PatchPostfix]
        public static void Postfix(WeaponPreview.CG_SetupItemPreview __instance)
        {
			// this called when WeaponPreview is opened and fully initialized,
			// WeaponPreview is used both by weapon modding screen and item overview
   			var weaponPreview = __instance.weaponPreview_0;
			var _weaponPreview = new WeaponPreview_Proxy(__instance.weaponPreview_0);
			var item = _weaponPreview._currentItem;
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
			var itemGO = weaponPreview._originalObject;

			if (itemGO && itemGO.TryGetComponent<AssetPoolObject>(out assetPoolObject))
			{
				return true;
			}

			assetPoolObject = default;
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
			var item = _weaponPreview._currentItem;
			if (item != null)
			{
				return Plugin.Instance.CanWeaponPreviewRotate();
			}

			return true;
		}
	}

	public class Patch_ScrollTrigger_OnScroll : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ScrollTrigger), nameof(ScrollTrigger.OnScroll));
        }

        [PatchPrefix]
        public static bool Prefix()
		{
			return Plugin.Instance.CanScroll();
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
	public class Patch_ItemExtensions_CloneItemInternal : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
			Type[] parameters = null;
			Type[] generics = [typeof(Item)];
            return AccessTools.Method(typeof(ItemExtensions), nameof(ItemExtensions.CloneItemInternal), parameters, generics);
        }

        [PatchPostfix]
        public static void Postfix(Item __result, Item originalItem, IDatabaseIdGenerator idGenerator = null, bool skipInvisibleContent = false, bool resetSpawnedInSession = false)
		{
			Plugin.Instance.OnCloneItem(originalItem.Id, __result.Id);
		}
	}

	// sadly postfixing GetItemHash and HashForItem is not enough,
	// full solution is to rewrite entire GetItemHash method chain
 	public class Patch_IconsHash_GetItemHash : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(IconsHash), nameof(IconsHash.GetItemHash));
        }

        [PatchPostfix]
        public static void Postfix(Item item, ref int __result)
		{
			if (Plugin.Instance.GetMaterialsInfo(item.Id).Some(out var materialsInfo) && materialsInfo.Materials.Count > 0)
			{
				__result ^= WeaponCamoAndStickers.Patch_IconsHash_GetItemHash.GetSaveTimeInt(materialsInfo.SaveTime);
			}
		}
	}

	public class Patch_IconsHash_HashForItem : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(IconsHash), nameof(IconsHash.HashForItem));
        }

        [PatchPostfix]
        public static void Postfix(Item item, ref int __result)
		{
			if (Plugin.Instance.GetMaterialsInfo(item.Id).Some(out var materialsInfo) && materialsInfo.Materials.Count > 0)
			{
				__result ^= WeaponCamoAndStickers.Patch_IconsHash_GetItemHash.GetSaveTimeInt(materialsInfo.SaveTime) / 2;
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
        public static bool Prefix(Renderer ____renderer, float temperatureCelsio, bool force = false)
		{
			// HotObjects (barrels, silencers, etc) override renderer materials parameters (_HeatSize, _HeatTemp, etc)
			// the same way as we via MaterialPropertyBlock, which results in them overriding our changes,
			// so stop them from doing that! (maybe we could combine their changes, but its already complicated enough)
			return !Plugin.Instance.IsPatchedRenderer(____renderer);
		}
	}

	public class Patch_RainCondensator_OnEnable : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RainCondensator), nameof(RainCondensator.OnEnable));
        }

        [PatchPrefix]
        public static bool Prefix(Renderer ____renderer)
		{
			// I dont think this one is necessary, but for some reason
			// some people still get reset by rain, so lets try
			// to disable it completely
			return !Plugin.Instance.IsPatchedRenderer(____renderer);
		}
	}

	public class Patch_RainCondensator_UpdateValues : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RainCondensator), nameof(RainCondensator.UpdateValues));
        }

        [PatchPrefix]
        public static bool Prefix(Renderer ____renderer)
		{
			// RainCondensator works the same way as HotObject
			return !Plugin.Instance.IsPatchedRenderer(____renderer);
		}
	}

	public class Patch_RainCondensator_OnDisable : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RainCondensator), nameof(RainCondensator.OnDisable));
        }

        [PatchPrefix]
        public static bool Prefix(Renderer ____renderer)
		{
			// I dont think this one is necessary, but for some reason
			// some people still get reset by rain, so lets try
			// to disable it completely
			return !Plugin.Instance.IsPatchedRenderer(____renderer);
		}
	}

	// this is the method that inits skin and has access to skin id and body part
	public class Patch_PlayerBody_SetSkin : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerBody), nameof(PlayerBody.SetSkin));
        }

        [PatchPostfix]
        public static void Postfix(PlayerBody __instance, KeyValuePair<EBodyModelPart, ResourceKey> part, Skeleton skeleton)
		{
			string profileId = default;

			// for some reason parent can be null at this moment,
			// usually this happens in Overall screen,
			// I guess it gets parented to PlayerModelView later
			var parent = __instance.transform.parent;
			if (parent)
			{
				// We dont support changed materials on bots at this moment.
				// AI has AccountId = "0",
				// you would think that better way is to check player.IsAI,
				// but it set to false even on AI at this stage in initialization.
				if (parent.TryGetComponent<Player>(out var player) && player.AccountId != "0")
				{
					// we are in raid or walking in hideout
					profileId = player.ProfileId;
				}
			}
			else
			{
				// profile is null in character creation screen
	    		if (TarkovApplication.Exist(out var tarkovApplication) &&
					tarkovApplication.Session != null &&
					tarkovApplication.Session.Profile != null)
	            {
					// we are in hideout ui screens
		            profileId = tarkovApplication.Session.Profile.Id;
	            }
			}

			if (profileId != default)
			{
				var skinId = __instance.BodyCustomization[part.Key];
				var skin = __instance.BodySkins[part.Key];
				Plugin.Instance.OnSkinCreated(profileId, skinId, skin);
			}
		}
	}

	// this is used right before lodded skin is destroyed
	public class Patch_LoddedSkin_Unskin : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LoddedSkin), nameof(LoddedSkin.Unskin));
        }

        [PatchPrefix]
        public static void Prefix(LoddedSkin __instance)
		{
			Plugin.Instance.OnSkinDestroyed(__instance);
		}
	}

	public class Patch_OverallScreen_Show : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(OverallScreen), nameof(OverallScreen.Show));
        }

        [PatchPostfix]
        public static void Postfix(OverallScreen __instance, Profile currentProfile, Profile[] allProfiles, CountersCollection overallAccountStats, [CanBeNull] InventoryController inventoryController, bool isInMatching)
		{
			Plugin.Instance.WaitForWeaponPreview();
		}
	}

	// this method is called when PlayerModelView is opened and finishes loading
	public class Patch_PlayerModelView_OnLoadingCompleted : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerModelView), nameof(PlayerModelView.OnLoadingCompleted));
        }

        [PatchPostfix]
        public static void Postfix(PlayerModelView __instance)
		{
			if (WeaponCamoAndStickers.Patch_ItemUiContext_GetItemContextInteractions.InRaid())
			{
				return;
			}
			// profile is null in character creation screen
    		if (TarkovApplication.Exist(out var tarkovApplication) &&
				tarkovApplication.Session != null &&
				tarkovApplication.Session.Profile != null &&
				Singleton<BonusController>.Instance.HasBonus(EBonusType.UnlockWeaponModification))
            {
	            var profileId = tarkovApplication.Session.Profile.Id;
				Plugin.Instance.OnClothesReloaded(profileId, __instance);
            }
		}
	}

	public class Patch_OverallScreen_Close : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(OverallScreen), nameof(OverallScreen.Close));
        }

        [PatchPrefix]
        public static void Prefix(OverallScreen __instance)
		{
			Plugin.Instance.CloseCamoEditor();
		}
	}

	// I am pretty certain all bot spawning functions eventually lead to this method
	public class Patch_BotCreatorClient_CreateBot : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotCreatorClient), nameof(BotCreatorClient.CreateBot));
        }

        [PatchPrefix]
        public static void Prefix(PlayerModelView __instance, Profile profile, PositionNote bornInfo, Action<BotOwner> callback, bool isLocalGame, CancellationToken cancellationToken)
		{
			var botRole = profile.Info.Settings.Role;

            var equipmentItems = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment);
            foreach (var item in equipmentItems)
            {
				Plugin.Instance.QueueWeaponForRandomCamoGeneration(botRole, item);
            }
		}
	}

	public class Patch_LocalPlayer_Create : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LocalPlayer), nameof(LocalPlayer.Create));
        }

        [PatchPrefix]
		private static void Prefix(Profile profile)
	    {
			var botRole = profile.Info.Settings.Role;

            var equipmentItems = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment);
            foreach (var item in equipmentItems)
            {
				Plugin.Instance.QueueWeaponForRandomCamoGeneration(botRole, item);
            }
	    }
	}

}
