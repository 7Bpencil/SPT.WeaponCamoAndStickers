//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Comfort.Common;
using EFT;
using EFT.Counters;
using EFT.InventoryLogic;
using EFT.UI;
using System.Reflection;
using SPT.Reflection.Patching;
using JetBrains.Annotations;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

using CameraImage_Proxy = SevenBoldPencil.WeaponCamoAndStickers.CameraImage_Proxy;

namespace SevenBoldPencil.EquipmentStickers
{
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
				Singleton<BonusController>.Instance.HasBonus(EBonusType.UnlockWeaponModification) &&
				TryGetCameraImage(__instance, out var camera, out var rawImage))
            {
	            var profileId = tarkovApplication.Session.Profile.Id;
				Plugin.Instance.OnClothesReloaded(profileId, __instance, camera, rawImage);
            }
		}

		public static bool TryGetCameraImage(PlayerModelView __instance, out Camera camera, out RawImage rawImage)
		{
			if (__instance.transform.parent.TryGetComponent<CameraImage>(out var cameraImage))
			{
				var _cameraImage = new CameraImage_Proxy(cameraImage);
				camera = cameraImage.targetCamera;
				rawImage = _cameraImage._rawImage;
				return true;
			}

			camera = default;
			rawImage = default;
			return false;
		}
	}

	public class Patch_InventoryPlayerModelWithStatsWindow_DragHandler : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryPlayerModelWithStatsWindow), nameof(InventoryPlayerModelWithStatsWindow.DragHandler));
        }

        [PatchPrefix]
        public static bool Prefix()
		{
			return Plugin.Instance.CanRotate();
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

}
