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
}
