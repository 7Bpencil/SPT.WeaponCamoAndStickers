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
using EFT.CameraControl;
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
using JetBrains.Annotations;
using HarmonyLib;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	// this method tries to initialize gui for all slots in weapon,
	// pretend that there are no slots when applying paint to not obscure view
	public class Patch_WeaponModdingScreen_CreateModSlotViews : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponModdingScreen), nameof(WeaponModdingScreen.CreateModSlotViews));
        }

        [PatchPrefix]
        public static bool Prefix(WeaponModdingScreen __instance, CompoundItem weapon)
		{
			return !Plugin.Instance.IsWaitingForWeaponPreview();
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
			if (!Plugin.CanItemHaveDecals(item))
			{
				return;
			}
			if (TryGetAssetPoolObject(_weaponPreview, out var assetPoolObject, out var previewPivot))
			{
				Plugin.Instance.OnWeaponPreviewOpened(item, assetPoolObject, weaponPreview.Rotator, previewPivot, weaponPreview.WeaponPreviewCamera);
			}
		}

		public static bool TryGetAssetPoolObject(WeaponPreview_Proxy weaponPreview, out AssetPoolObject assetPoolObject, out PreviewPivot previewPivot)
		{
			// it takes time to load gameObjects so if you ask too early they will be null
			var itemGO = weaponPreview._originalObject;

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
			var item = _weaponPreview._currentItem;
			if (item != null)
			{
				var camera = __instance.WeaponPreviewCamera;
				if (camera)
				{
					Plugin.Instance.OnWeaponPreviewClosed(item.Id, camera);
				}
			}

			return true;
		}
	}

	// adds APPLY PAINT button to interaction menu
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
			if (InRaid())
			{
				return;
			}

			var interactions = __result._dynamicInteractions;
			var item = itemContext.Item;

			var key = "APPLY PAINT";
			var icon = EFTHardSettings.Instance.StaticIcons.WishlistSprites[EWishlistGroup.Other];
	        interactions[key] = new Custom_DynamicContextInteraction(item.Id, key, () => OpenApplyPaintWindow(__result), icon)
			{
				NonInteractiveTooltip = Plugin.CanItemHaveDecals(item) ? GetRequiresBenchTooltip() : new(new FailedResult("Only following items can be painted: Weapons, Knives, Helmets, Facemasks, Containers")),
			};
	    }

		public static Option<FailedResult> GetRequiresBenchTooltip()
		{
			if (Singleton<BonusController>.Instance.HasBonus(EBonusType.UnlockWeaponModification))
			{
				return default;
			}

			// Razolak liked that paint shop requires bench 1, I think it makes sense too
			return new(new FailedResult("bonus/UnlockWeaponModification_required"));
		}

	    public static bool InRaid()
	    {
	        var instance = Singleton<AbstractGame>.Instance;
	        return instance != null && instance.InRaid;
	    }

		public static void OpenApplyPaintWindow(ContextInteractions<EItemInfoButton> result)
		{
			if (result is InventoryItemContextInteractions gclass)
			{
				Plugin.Instance.WaitForWeaponPreview();
				gclass.method_28();
			}
		}
	}

	public class Custom_DynamicContextInteraction : DynamicContextInteraction
	{
		public Option<FailedResult> NonInteractiveTooltip;
		public Custom_DynamicContextInteraction(string id, string key, Action callback, Sprite icon) : base(id, key, callback, icon) {}
	}

	// here custom buttons are actually constructed
	public class Patch_InteractionButtonsContainer_CreateDynamicContextButton : ModulePatch
	{
	    protected override MethodBase GetTargetMethod()
	    {
	        return AccessTools.Method(typeof(InteractionButtonsContainer), nameof(InteractionButtonsContainer.CreateDynamicContextButton));
	    }

	    [PatchPrefix]
	    private static bool Prefix(InteractionButtonsContainer __instance, DynamicContextInteraction interaction)
	    {
	        if (interaction is Custom_DynamicContextInteraction customInteraction)
	        {
				AddButton(__instance, __instance._buttonTemplate, __instance._buttonsContainer, customInteraction);
	            return false;
	        }
	        return true;
	    }

		private static void AddButton(InteractionButtonsContainer instance, SimpleContextMenuButton buttonTemplate, RectTransform buttonsContainer, Custom_DynamicContextInteraction interaction)
		{
	        var button = instance.CreateContextButton
			(
	            interaction.Key,
	            interaction.Key,
	            buttonTemplate,
	            buttonsContainer,
	            interaction.Icon,
	            () => { if (!interaction.NonInteractiveTooltip.HasValue) { interaction.Execute(); } },
	            () => { }
	        );
	        button.SetButtonInteraction(interaction.NonInteractiveTooltip.HasValue ? interaction.NonInteractiveTooltip.Value : SuccessfulResult.New);
	        instance.BindButton(button);
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

	public class Patch_WeaponPrefab_InitHotObjects : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponPrefab), nameof(WeaponPrefab.InitHotObjects));
        }

        [PatchPostfix]
        public static void Postfix(WeaponPrefab __instance)
		{
			// this is the old way of tracking when weapon spawns (when decals were only for weapons),
			// but for some reason new way doesn't work for bots, so reintroduce it back,
			// OnItemPrefabCreated safely catches multiple inits anyway

			var __instance__ = new WeaponPrefab_Proxy(__instance);
			var item = __instance__._weaponData;
			if (item != null)
			{
				Plugin.Instance.OnDecalsHostCreated_Weapon(item.Id, ItemType.Weapon, __instance);
			}
		}
	}

	public class Patch_PlayerBody_SlotView_CreateAndParent : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerBody.SlotView), nameof(PlayerBody.SlotView.CreateAndParent));
        }

        [PatchPostfix]
        public static void Postfix(PlayerBody playerBody, GameObject model)
		{
			Plugin.Instance.OnEquippedInSlot(playerBody, model);
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

	// this is the method that inits skin and has access to skin id and body part
	public class Patch_PlayerBody_SetSkin_CreateItem : ModulePatch
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
				if (parent.TryGetComponent<Player>(out var player))
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
				Plugin.Instance.OnSkinCreated(profileId, skinId, skin, skeleton);
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
			if (Plugin.CanItemHaveDecals(originalItem))
			{
				Plugin.Instance.OnCloneItem(originalItem.Id, __result.Id);
			}
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
			if (TarkovApplication.Exist(out var tarkovApplication) &&
				tarkovApplication.Session != null &&
				tarkovApplication.Session.Profile != null &&
				TryGetCamera(__instance).Some(out var camera))
			{
				Plugin.Instance.OnPlayerModelViewShown(tarkovApplication.Session.Profile, camera);
			}
		}

		public static Option<Camera> TryGetCamera(PlayerModelView __instance)
		{
			var instanceTransform = __instance.transform;
			for (var i = 0; i < instanceTransform.childCount; i++)
			{
				var child = instanceTransform.GetChild(i);
				if (child.TryGetComponent<Camera>(out var camera))
				{
					return new(camera);
				}
			}

			return default;
		}
	}

	// this method is called when PlayerModelView is closed
	public class Patch_PlayerModelView_Destroy : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerModelView), nameof(PlayerModelView.Destroy));
        }

        [PatchPrefix]
        public static void Prefix(PlayerModelView __instance)
		{
			if (Patch_PlayerModelView_OnLoadingCompleted.TryGetCamera(__instance).Some(out var camera))
			{
				Plugin.Instance.OnPlayerModelViewClosed(camera);
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
			foreach (var lod in skin._lods)
			{
				var skinnedMeshRenderer = lod.SkinnedMeshRenderer;
                foreach (var material in skinnedMeshRenderer.sharedMaterials)
                {
					var shaderName = material.shader.name;
					if (shaderName == "p0/Reflective/Bumped Specular SMap" ||
						shaderName == "p0/Reflective/Bumped Specular SMap_Decal")
					{
						// weapons and mods have _StencilType = 2,
						// equipment (including helmets) have _StencilType = 1,
						// cases have _StencilType = 0,
						// map environment decals and bullet holes target _StencilType = 0,
						// which means to keep head clean from helmet decals, we should set it stencil to 0,
						// which means to keep hands and body clean from weapon decals, we should set it stencil to 1
						// would be nice to set all body parts to 0, but then environment decals will be projected
						// on legs and hands which is noticable

						if (part.Key == EBodyModelPart.Head)
						{
							material.SetFloat(_StencilType, 0);
						}
						else
						{
							material.SetFloat(_StencilType, 1);
						}
					}
                }
			}
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
				Plugin.Instance.QueueWeaponForRandomCamoGeneration(botRole, item.Id, item.StringTemplateId);
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
				Plugin.Instance.QueueWeaponForRandomCamoGeneration(botRole, item.Id, item.StringTemplateId);
            }
	    }
	}

	public class Patch_ItemIconCreator_GetItemIcon : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemIconCreator), nameof(ItemIconCreator.GetItemIcon));
        }

        [PatchPrefix]
		public static bool Prefix(ItemIconCreator __instance, ref ItemIcon __result, Item item, in IntVec2 size, bool forcedGeneration = false)
		{
			// only items with decals go through custom route
			if (Plugin.CanItemHaveDecals(item) &&
				Plugin.Instance.GetDecalsCount(item.Id) > 0)
			{
				__result = GetItemIcon(__instance, item, size);
				return false;
			}

			return true;
		}

		// everything below is mostly copy-paste of original methods, read comments to know what changed
		public static ItemIcon GetItemIcon(ItemIconCreator __instance, Item item, in IntVec2 size, bool forcedGeneration = false)
		{
			int itemHash = IconsHash.GetItemHash(item); // we postfix GetItemHash separately to keep it compatible with other mods that patch it too
			ItemIcon icon;
			bool flag = __instance.TryGetCachedIcon(itemHash, out icon);
			if (!forcedGeneration && flag && (InGameStatus.InRaid || !icon.IsGeneratedInRaid))
			{
				return icon;
			}
			icon = new ItemIcon(itemHash)
			{
				IsGeneratedInRaid = InGameStatus.InRaid
			};
			if (!forcedGeneration && __instance.TryGetIconPath(itemHash, out var path))
			{
				__instance.LoadFromUserCacheAsync(icon, path, size).HandleExceptions();
				return icon;
			}
			FillIconWithNewSpriteAsync(__instance, icon, item, size, saveToFile: true, requireZeroMip: true).HandleExceptions(); // use our FillIconWithNewSpriteAsync
			return icon;
		}

		public static async Task FillIconWithNewSpriteAsync(ItemIconCreator __instance, ItemIcon icon, Item item, IntVec2 size, bool saveToFile, bool requireZeroMip)
		{
			__instance._queueCount++;
			__instance._memoryCacheIndex[icon.Hash] = icon;
			// in theory we could rewrite only this delegate, but sadly item is not passed inside and we need it,
			// and I dont want to build any more scaffolding to get around it
			ItemIconCreator.RenderModelResult renderModelResult = await __instance.RenderModel(item, async delegate(GameObject model, PreviewPivot pivot)
			{
				await __instance.PrepareCameraAsync(); // this method loads camera first time when it doesnt exist, only after it its safe to use camera_0
				while (__instance._isIconCreating)
				{
					await JobScheduler.Yield();
				}
				__instance._isIconCreating = true;
				await JobScheduler.Yield();

				Plugin.Instance.BeforeInventoryIconRecorded(item.Id, __instance.camera_0); // we need to know which camera renders which item
				Sprite result = CaptureSpriteOfModel(__instance, model, in size, pivot); // use our CaptureSpriteOfModel
				Plugin.Instance.AfterInventoryIconRecorded(item.Id, __instance.camera_0); // clear info about that camera

				await JobScheduler.Yield();
				__instance._isIconCreating = false;
				return result;
			});
			if (renderModelResult.sprite != null)
			{
				ItemIconCreator.CleanIcon(icon);
				Sprite sprite = renderModelResult.sprite;
				icon.Sprite = sprite;
				icon.Sprite.texture.filterMode = FilterMode.Trilinear;
				icon.Changed.Invoke();
				if ((!requireZeroMip) ? saveToFile : (saveToFile && renderModelResult.zeroMipWasLoaded))
				{
					await __instance.SaveIconAsync(icon);
				}
			}
			else
			{
				Debug.LogError("Something went wrong! Sprite for " + icon.Hash + " was not created!");
			}
			__instance._queueCount = Mathf.Max(__instance._queueCount - 1, 0);
			if (__instance._queueCount <= 0)
			{
				if (__instance._prevTextureLimit.HasValue)
				{
					QualitySettings.streamingMipmapsMaxLevelReduction = __instance._prevTextureLimit.Value;
					__instance._prevTextureLimit = null;
				}
				if (__instance._fileCacheIndex.Count > 0)
				{
					__instance.CheckCacheFolder();
					File.WriteAllText(__instance._indexPath, JsonExtensions.ToJson(__instance._fileCacheIndex));
				}
			}
		}

		public static Sprite CaptureSpriteOfModel(ItemIconCreator __instance, GameObject model, in IntVec2 size, PreviewPivot previewPivot)
		{
			if (model == null)
			{
				return null;
			}
			ItemIconCreator.IconRenderSettings @struct = ItemIconCreator.IconRenderSettings.Store();
			ItemIconCreator.IconRenderSettings.Reset();
			// ShaderReplacer.Replace(model); // ShaderReplacer replaces deferred shaders with forward ones, we need original deferred, so disable
			__instance.SetupScene(model, in size, previewPivot);
			Light[] light_ = __instance.light_0;
			for (int i = 0; i < light_.Length; i++)
			{
				light_[i].enabled = true;
			}
			model.SetActive(value: true);
			Texture2D texture = method_3(__instance, model, in size); // use our method_3
			model.SetActive(value: false);
			// ShaderReplacer.Restore();
			@struct.Restore();
			return ItemIconCreator.CreateSprite(texture);
		}

		public static Texture2D method_3(ItemIconCreator __instance, GameObject model, in IntVec2 size)
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
			__instance.camera_0.gameObject.SetActive(value: true);

			// calculate new camera position and fov
			var cameraTransform = __instance.camera_0.transform;
			var modelTransform = model.transform;
			modelTransform.SetParent(null, worldPositionStays: true); // they keep model as child of camera

			var originalPosition = cameraTransform.position;
			var originalFov = __instance.camera_0.fieldOfView;

			// by default distance is around 1, make it 15 for more "orthographic" look,
			// 15 is max, everything higher makes item disappear (there are probably a way to increase that, but 15 looks fine)
 			var targetDistance = 15;
			var currentDistance = (modelTransform.position - cameraTransform.position).magnitude;
			var offset = targetDistance - currentDistance;
			var newPosition = originalPosition - cameraTransform.forward * offset;
			var newFov = 2 * Mathf.Atan2(__instance.camera_0.orthographicSize, targetDistance);

			// set
			__instance.camera_0.orthographic = false;
			__instance.camera_0.renderingPath = RenderingPath.DeferredShading;
			cameraTransform.position = newPosition;
			__instance.camera_0.fieldOfView = newFov * Mathf.Rad2Deg;

			__instance.camera_0.targetTexture = temporary;
			__instance.camera_0.clearFlags = CameraClearFlags.Color;
			__instance.camera_0.backgroundColor = new Color(0f, 0f, 0f, 0f);
			__instance.camera_0.useOcclusionCulling = false;
			__instance.iconShadow_0.SetTexDimension(width, height);
			RenderTexture temporary2 = RenderTexture.GetTemporary(x, y);
			RendererExtensions.ClearTexture(temporary2);
			__instance.camera_0.Render();
			Graphics.Blit(temporary, temporary2);
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = temporary2;
			Texture2D texture2D = ItemIconCreator.GetTexture(x, y);
			texture2D.ReadPixels(new Rect(0f, 0f, __instance.camera_0.pixelWidth, __instance.camera_0.pixelHeight), 0, 0, recalculateMipMaps: false);
			texture2D.Apply();
			RenderTexture.active = active;
			__instance.camera_0.targetTexture = null;
			RenderTexture.ReleaseTemporary(temporary);
			RenderTexture.ReleaseTemporary(temporary2);
			__instance.camera_0.gameObject.SetActive(value: false);

			// revert
			__instance.camera_0.orthographic = true;
			__instance.camera_0.renderingPath = RenderingPath.Forward;
			cameraTransform.position = originalPosition;
			__instance.camera_0.fieldOfView = originalFov;

			return texture2D;
		}
	}

	public class Patch_IconsHash_GetItemHash : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(IconsHash), nameof(IconsHash.GetItemHash));
        }

        [PatchPostfix]
        public static void Postfix(Item item, ref int __result)
		{
			if (Plugin.CanItemHaveDecals(item) &&
				Plugin.Instance.GetDecalsInfo(item.Id).Some(out var decalsInfo) &&
				decalsInfo.Count > 0)
			{
				__result ^= GetSaveTimeInt(decalsInfo[0].SaveTime);
			}
		}

		// all this shit to fit SaveTime inside int
		public static int GetSaveTimeInt(long saveTime)
		{
			var newStartPoint = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var newStartPointOffset = new DateTimeOffset(newStartPoint).ToUnixTimeMilliseconds();
			var saveTimeOffset = saveTime - newStartPointOffset;
			var saveTimeOffsetSeconds = (int)(saveTimeOffset / 1000);
			return saveTimeOffsetSeconds;
		}
	}
}
