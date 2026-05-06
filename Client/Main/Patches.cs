//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Diz.Skinning;
using Diz.Jobs;
using EFT;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using EFT.Visual;
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

        [PatchPrefix]
        public static void Prefix(WeaponModdingScreen __instance)
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

	// I am pretty certain all bot spawning functions eventually lead to this method
	public class Patch_BotCreatorClass_method_2 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotCreatorClass), nameof(BotCreatorClass.method_2));
        }

        [PatchPrefix]
        public static void Prefix(PlayerModelView __instance, Profile profile, GClass682 bornInfo, Action<BotOwner> callback, bool isLocalGame, CancellationToken cancellationToken)
		{
			var spawnChance = Plugin.Instance.GetCamoSpawnChanceFromBotRole(profile.Info.Settings.Role);
			if (spawnChance <= 0)
			{
				return;
			}

			// GetPlayerItems is pretty expensive, so should be avoided when possible
            var equipmentItems = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment);
            foreach (var item in equipmentItems)
            {
                if (item is Weapon)
                {
					Plugin.Instance.QueueItemForRandomCamoGeneration(item.Id, spawnChance);
				}
            }
		}
	}

	public class Patch_GClass926_GetItemIcon : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass926), nameof(GClass926.GetItemIcon));
        }

        [PatchPrefix]
		public static bool Prefix(GClass926 __instance, ref GClass929 __result, Item item, in XYCellSizeStruct size, bool forcedGeneration = false)
		{
			// only weapons with decals go through custom route
			if (item is Weapon && Plugin.Instance.GetDecalsCount(item.Id) > 0)
			{
				__result = GetItemIcon(__instance, item, size);
				return false;
			}

			return true;
		}

		// everything below is mostly copy-paste of original methods, read comments to know what changed
		public static GClass929 GetItemIcon(GClass926 __instance, Item item, in XYCellSizeStruct size, bool forcedGeneration = false)
		{
			int itemHash = GClass928.GetItemHash(item); // we postfix GetItemHash separately to keep it compatible with other mods that patch it too
			GClass929 icon;
			bool flag = __instance.method_7(itemHash, out icon);
			if (!forcedGeneration && flag && (GClass2340.InRaid || !icon.IsGeneratedInRaid))
			{
				return icon;
			}
			icon = new GClass929(itemHash)
			{
				IsGeneratedInRaid = GClass2340.InRaid
			};
			if (!forcedGeneration && __instance.method_8(itemHash, out var path))
			{
				__instance.method_6(icon, path, size).HandleExceptions();
				return icon;
			}
			method_1(__instance, icon, item, size, saveToFile: true, requireZeroMip: true).HandleExceptions(); // use our method_1
			return icon;
		}

		public static async Task method_1(GClass926 __instance, GClass929 icon, Item item, XYCellSizeStruct size, bool saveToFile, bool requireZeroMip)
		{
			__instance.Int_1++;
			__instance.Dictionary_0[icon.Hash] = icon;
			// in theory we could rewrite only this delegate, but sadly item is not passed inside and we need it,
			// and I dont want to build any more scaffolding to get around it
			GClass926.RenderModelResult renderModelResult = await __instance.RenderModel(item, async delegate(GameObject model, PreviewPivot pivot)
			{
				await __instance.method_0(); // this method loads camera first time when it doesnt exist, only after it its safe to use Camera_0
				while (__instance.Bool_0)
				{
					await JobScheduler.Yield();
				}
				__instance.Bool_0 = true;
				await JobScheduler.Yield();

				Plugin.Instance.BeforeInventoryIconRecorded(__instance.Camera_0, item.Id); // we need to know which camera renders which item
				Sprite result = method_4(__instance, model, in size, pivot); // use our method_4
				Plugin.Instance.AfterInventoryIconRecorded(__instance.Camera_0, item.Id); // clear info about that camera

				await JobScheduler.Yield();
				__instance.Bool_0 = false;
				return result;
			});
			if (renderModelResult.sprite != null)
			{
				GClass926.smethod_1(icon);
				Sprite sprite = renderModelResult.sprite;
				icon.Sprite = sprite;
				icon.Sprite.texture.filterMode = FilterMode.Trilinear;
				icon.Changed.Invoke();
				if ((!requireZeroMip) ? saveToFile : (saveToFile && renderModelResult.zeroMipWasLoaded))
				{
					await __instance.method_5(icon);
				}
			}
			else
			{
				Debug.LogError("Something went wrong! Sprite for " + icon.Hash + " was not created!");
			}
			__instance.Int_1 = Mathf.Max(__instance.Int_1 - 1, 0);
			if (__instance.Int_1 <= 0)
			{
				if (__instance.Nullable_0.HasValue)
				{
					QualitySettings.streamingMipmapsMaxLevelReduction = __instance.Nullable_0.Value;
					__instance.Nullable_0 = null;
				}
				if (__instance.Dictionary_1.Count > 0)
				{
					__instance.method_10();
					File.WriteAllText(__instance.String_1, JsonParserClass.ToJson(__instance.Dictionary_1));
				}
			}
		}

		public static Sprite method_4(GClass926 __instance, GameObject model, in XYCellSizeStruct size, PreviewPivot previewPivot)
		{
			if (model == null)
			{
				return null;
			}
			GClass926.Struct115 @struct = GClass926.Struct115.Store();
			GClass926.Struct115.Reset();
			// ShaderReplacer.Replace(model); // ShaderReplacer replaces deferred shaders with forward ones, we need original deferred, so disable
			__instance.method_2(model, in size, previewPivot);
			Light[] light_ = __instance.Light_0;
			for (int i = 0; i < light_.Length; i++)
			{
				light_[i].enabled = true;
			}
			model.SetActive(value: true);
			Texture2D texture = method_3(__instance, model, in size); // use our method_3
			model.SetActive(value: false);
			// ShaderReplacer.Restore();
			@struct.Restore();
			return GClass926.smethod_2(texture);
		}

		public static Texture2D method_3(GClass926 __instance, GameObject model, in XYCellSizeStruct size)
		{
			// by default icon camera is forward rendering,
			// probably because they really wanted to render icons with orthogonal projection,
			// they even have forward versions of shaders to make it work,
			// but decals can only work in deferred rendering,
			// so we have to switch camera renderingPath,
			// but deferred rendering doesnt work with orthographic projection for Unity reasons,
			// so we have to also switch to perspective, and change camera position/fov to keep object size the same,

			int x = size.X;
			int width = x * 2;
			int y = size.Y;
			int height = y * 2;

			// change depth to 24, otherwise background turns white
			RenderTexture temporary = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 8);
			temporary.name = "IconCreator TextureDouble";
			__instance.Camera_0.gameObject.SetActive(value: true);

			// calculate new camera position and fov
			var cameraTransform = __instance.Camera_0.transform;
			var modelTransform = model.transform;
			modelTransform.SetParent(null, worldPositionStays: true); // they keep model as child of camera

			var originalPosition = cameraTransform.position;
			var originalFov = __instance.Camera_0.fieldOfView;

			// by default distance is around 1, make it 15 for more "orthographic" look,
			// 15 is max, everything higher makes item disappear (there are probably a way to increase that, but 15 looks fine)
 			var targetDistance = 15;
			var currentDistance = (modelTransform.position - cameraTransform.position).magnitude;
			var offset = targetDistance - currentDistance;
			var newPosition = originalPosition - cameraTransform.forward * offset;
			var newFov = 2 * Mathf.Atan2(__instance.Camera_0.orthographicSize, targetDistance);

			// set
			__instance.Camera_0.orthographic = false;
			__instance.Camera_0.renderingPath = RenderingPath.DeferredShading;
			cameraTransform.position = newPosition;
			__instance.Camera_0.fieldOfView = newFov * Mathf.Rad2Deg;

			__instance.Camera_0.targetTexture = temporary;
			__instance.Camera_0.clearFlags = CameraClearFlags.Color;
			__instance.Camera_0.backgroundColor = new Color(0f, 0f, 0f, 0f);
			__instance.Camera_0.useOcclusionCulling = false;
			__instance.IconShadow_0.SetTexDimension(width, height);
			RenderTexture temporary2 = RenderTexture.GetTemporary(x, y);
			GClass860.ClearTexture(temporary2);
			__instance.Camera_0.Render();
			Graphics.Blit(temporary, temporary2);
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = temporary2;
			Texture2D texture2D = GClass926.smethod_0(x, y);
			texture2D.ReadPixels(new Rect(0f, 0f, __instance.Camera_0.pixelWidth, __instance.Camera_0.pixelHeight), 0, 0, recalculateMipMaps: false);
			texture2D.Apply();
			RenderTexture.active = active;
			__instance.Camera_0.targetTexture = null;
			RenderTexture.ReleaseTemporary(temporary);
			RenderTexture.ReleaseTemporary(temporary2);
			__instance.Camera_0.gameObject.SetActive(value: false);

			// revert
			__instance.Camera_0.orthographic = true;
			__instance.Camera_0.renderingPath = RenderingPath.Forward;
			cameraTransform.position = originalPosition;
			__instance.Camera_0.fieldOfView = originalFov;

			return texture2D;
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
			if (item is Weapon && Plugin.Instance.GetDecalsInfo(item.Id).Some(out var decalsInfo) && decalsInfo.Count > 0)
			{
				// all this shit to fit SaveTime inside int
				var saveTime = decalsInfo[0].SaveTime;
				var newStartPoint = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				var newStartPointOffset = new DateTimeOffset(newStartPoint).ToUnixTimeMilliseconds();
				var saveTimeOffset = saveTime - newStartPointOffset;
				var saveTimeOffsetSeconds = (int)(saveTimeOffset / 1000);
				__result ^= saveTimeOffsetSeconds;
			}
		}
	}
}
