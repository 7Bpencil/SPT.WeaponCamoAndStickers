using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DeferredDecals;
using EFT;
using EFT.Hideout;
using EFT.Hideout.ShootingRange;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.SynchronizableObjects;
using EFT.UI;
using EFT.UI.Screens;
using EFT.Vehicle;
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
	public class Patch_DeferredDecalRenderer_SingleDecal_Init : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(DeferredDecalRenderer.SingleDecal), nameof(DeferredDecalRenderer.SingleDecal.Init));
        }

        [PatchPostfix]
        public static void Postfix(DeferredDecalRenderer.SingleDecal __instance)
        {
			if (__instance.DynamicDecalMaterial.shader.name == "Decal/Deferred DecalShader Diffuse+Normals Dynamic")
			{
				var replacementShader = Plugin.Instance.DecalDynamicShader;
				var replacementMaterial = new Material(replacementShader);
				replacementMaterial.CopyPropertiesFromMaterial(__instance.DynamicDecalMaterial);
				__instance.DynamicDecalMaterial = replacementMaterial;
			}
		}
	}

	public class Patch_DeferredDecalRenderer_Update : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(DeferredDecalRenderer), nameof(DeferredDecalRenderer.Update));
        }

        [PatchPrefix]
        public static bool Prefix(DeferredDecalRenderer __instance)
        {
			return false;
		}
	}

	public class Patch_DeferredDecalRenderer_OnPreCameraRender : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(DeferredDecalRenderer), nameof(DeferredDecalRenderer.OnPreCameraRender));
        }

        [PatchPrefix]
        public static bool Prefix(DeferredDecalRenderer __instance, Camera currentCamera)
        {
            var __instance__ = new Proxy_DeferredDecalRenderer(__instance);
			var list_0 = __instance__.list_0;

			bool flag = false;
			for (int i = 0; i < list_0.Count; i++)
			{
				if (list_0[i].enabled && list_0[i].ManualUpdate())
				{
					flag = true;
				}
			}
			if (flag)
			{
				__instance.method_13();
			}

			return true;
		}
	}
}
