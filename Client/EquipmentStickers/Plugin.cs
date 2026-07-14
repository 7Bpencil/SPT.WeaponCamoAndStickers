//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Logging;
using EFT;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using EFT.UI;
using SevenBoldPencil.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// some ideas:
// stickers only (dont let people camo it, they will be disappointed)
// less options (no uv)
//
// or add string Bone to DecalInfo from main plugin?
// at least reuse DecalInfo and Decal classes,
// can probably even reuse DecalRenderer, just get reference to  InstancesToId
// or just shove ItemsWithDecal : IItemWithDecals
// GetDecalRoot(string bone) -> Transform
// GetDecals() -> List<Decal>
// in camo editor if amount of bones is 0, dont show selector
// if amount of items is 1 dont show selector
// building from scratch just with shared DecalInfo and Decal is probably better idea

using BigPlugin = SevenBoldPencil.WeaponCamoAndStickers.Plugin;
using CamoEditorResources = SevenBoldPencil.WeaponCamoAndStickers.CamoEditorResources;
using DecalMirrorMode = SevenBoldPencil.WeaponCamoAndStickers.DecalMirrorMode;
using DecalPaintMode = SevenBoldPencil.WeaponCamoAndStickers.DecalPaintMode;
using DecalInfo = SevenBoldPencil.WeaponCamoAndStickers.DecalInfo;
using SkinnedDecalsHost = SevenBoldPencil.WeaponCamoAndStickers.SkinnedDecalsHost;

namespace SevenBoldPencil.EquipmentStickers
{
    public readonly record struct StartDecalTransform(string Name, string Bone, Vector3 LocalPosition, Vector3 LocalEulerAngles, Vector3 LocalScale);

    [BepInPlugin("7Bpencil.EquipmentStickers", "7Bpencil.EquipmentStickers", "1.16.1")]
    [BepInDependency("7Bpencil.WeaponCamoAndStickers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
		public const string BoneHead = "Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanNeck/Base HumanHead";
		public const string BoneRibcage = "Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanRibcage";
		public const string BoneRightUpperarm = "Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanRibcage/Base HumanRCollarbone/Base HumanRUpperarm";
		public const string BoneLeftUpperarm = "Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanRibcage/Base HumanLCollarbone/Base HumanLUpperarm";
		public const string BoneRightThigh2 = "Root_Joint/Base HumanPelvis/Base HumanRThigh1/Base HumanRThigh2";
		public const string BoneLeftThigh2 = "Root_Joint/Base HumanPelvis/Base HumanLThigh1/Base HumanLThigh2";

		// TODO honestly, I dont even care what bones rigs and armors have,
		// I attach decals to player body anyway. Not sure about backpacks, tho
		private Dictionary<EBodyModelPart, StartDecalTransform[]> StartDecalTransforms = new()
		{
			{
				// TODO add attachment points for different parts of the head
				EBodyModelPart.Head,
				[
					new("Head", BoneHead, new(-0.1f, 0.155f, 0), new(0, 270, 0), new(0.08f, 0.08f, 0.08f))
				]
			},
			{
				// TODO add attachment points for shoulders, middle section, groin
				EBodyModelPart.Body,
				[
					new("Center Upper Chest", BoneRibcage, new(-0.022f, 0.146f, 0.129f), new(43f, 234, 300), new(0.09f, 0.09f, 0.09f)),
					new("Right Upperarm", BoneRightUpperarm, new(-0.142f, 0, 0.11f), new(0, 83, 84f), new(0.068f, 0.1f, 0.068f)),
					new("Left Upperarm", BoneLeftUpperarm, new(-0.142f, 0.011f, -0.11f), new(356, 100, 282), new(0.068f, 0.1f, 0.068f)),
					new("Right Shoulder", BoneRightUpperarm, new(-0.05f, 0, 0.13f), new(0, 95, 87), new(0.1f, 0.1f, 0.1f)),
					new("Left Shoulder", BoneLeftUpperarm, new(-0.05f, 0, -0.13f), new(0, 87, 275), new(0.1f, 0.1f, 0.1f)),
				]
			},
			{
				// TODO add attachment points for different parts of the legs
				EBodyModelPart.Feet,
				[
					new("Right Thigh", BoneRightThigh2, new(0, 0.12f, 0.025f), new(350, 90, 0), new(0.1f, 0.1f, 0.1f)),
					new("Left Thigh", BoneLeftThigh2, new(0, 0.12f, 0.025f), new(350, 90, 0), new(0.1f, 0.1f, 0.1f)),
				]
			},
		};

        public static Plugin Instance;

		public ManualLogSource LoggerInstance;

		public BigPlugin BigPlugin;

        private CamoEditorResources CamoEditorResources;

        private Option<CamoEditor> CamoEditor;
        private bool IsCamoEditorWaitingForWeaponPreview;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

			BigPlugin = BigPlugin.Instance;

            CamoEditorResources = new TypedFieldInfo<BigPlugin, CamoEditorResources>("CamoEditorResources").Get(BigPlugin);

            new Patch_OverallScreen_Show().Enable();
            new Patch_PlayerModelView_method_0().Enable();
			new Patch_InventoryPlayerModelWithStatsWindow_method_4().Enable();
			new Patch_ScrollTrigger_OnScroll().Enable();
            new Patch_OverallScreen_Close().Enable();
        }

        public void WaitForWeaponPreview()
        {
    		IsCamoEditorWaitingForWeaponPreview = true;
        }

        public bool IsWaitingForWeaponPreview()
        {
            return IsCamoEditorWaitingForWeaponPreview;
        }

        public void OnClothesReloaded(string profileId, PlayerModelView playerModelView, Camera editorCamera, RawImage rawImage)
        {
            // closing camo editor puts IsCamoEditorWaitingForWeaponPreview to false,
            // so check CamoEditor.HasValue for proper behaviour
            if (IsCamoEditorWaitingForWeaponPreview || CamoEditor.HasValue)
            {
                SetupCamoEditorClothes(profileId, playerModelView, editorCamera, rawImage);
            }
        }

        public void SetupCamoEditorClothes(string profileId, PlayerModelView playerModelView, Camera editorCamera, RawImage rawImage)
        {
            // SetupCamoEditorClothes is called when:
            // 1) player opens Overall screen and PlayerModelView gets loaded
            // 2) player switches cloth piece in overall screen, in which case we must properly close previous editor

            // save editor position
            var isOpened = false;
            var windowRect = EquipmentStickers.CamoEditor.GetDefaultWindowRect();
            if (CamoEditor.Some(out var camoEditor))
            {
                isOpened = camoEditor.IsOpened;
                windowRect = camoEditor.WindowRect;
                CloseCamoEditor();
            }

            var runtimeGizmos = editorCamera.gameObject.AddComponent<RuntimeGizmos>();
            var items = BuildItemsFromBodySkins(profileId, playerModelView.PlayerBody);

            CamoEditor = new(new CamoEditor()
            {
                Plugin = this,
                BigPlugin = BigPlugin.Instance,
                CamoEditorResources = CamoEditorResources,
				Camera = editorCamera,
				RawImage = rawImage,
				RuntimeGizmos = runtimeGizmos,
				PlayerModelView = playerModelView,
				Items = items,
                IsOpened = isOpened,
                WindowRect = windowRect
            });
        }

        public List<CamoEditorItem> BuildItemsFromBodySkins(string profileId, PlayerBody playerBody)
		{
            var result = new List<CamoEditorItem>(8); // 3 body skins and 5 items

			TryBuildSkin(EBodyModelPart.Head, 0, profileId, playerBody, result);
			TryBuildSkin(EBodyModelPart.Body, 1, profileId, playerBody, result);
			TryBuildSkin(EBodyModelPart.Feet, 1, profileId, playerBody, result);

            TryBuildItem(EquipmentSlot.Headwear, EBodyModelPart.Head, playerBody, result);
            TryBuildItem(EquipmentSlot.FaceCover, EBodyModelPart.Head, playerBody, result);
            TryBuildItem(EquipmentSlot.ArmorVest, EBodyModelPart.Body, playerBody, result);
            TryBuildItem(EquipmentSlot.TacticalVest, EBodyModelPart.Body, playerBody, result);
            TryBuildItem(EquipmentSlot.Backpack, EBodyModelPart.Body, playerBody, result);

            return result;
		}

		public void TryBuildSkin(EBodyModelPart bodyPart, byte stencilType, string profileId, PlayerBody playerBody, List<CamoEditorItem> result)
		{
			if (!playerBody.BodySkins.TryGetValue(bodyPart, out var skin))
			{
				return;
			}
			if (!playerBody.BodyCustomization.TryGetValue(bodyPart, out var skinId))
			{
				return;
			}
			if (!StartDecalTransforms.TryGetValue(bodyPart, out var startTransforms))
			{
				return;
			}

            var itemId = profileId + skinId;
            var instanceID = skin.gameObject.GetInstanceID();
			var decalsHost = new SkinnedDecalsHost(skin.transform, playerBody.SkeletonRootJoint);

            Logger.Log(LogLevel.Info, "CamoEditor", "Setup skin", itemId, instanceID);

            result.Add(new CamoEditorItem
            (
                Name: skin.gameObject.name, // getting the same name as in Overall or Ragfair screens is unreasonably annoying
                ItemId: itemId,
                InstanceID: instanceID,
                DecalsHost: decalsHost,
                StencilType: stencilType,
				StartTransforms: startTransforms
            ));
		}

        public void TryBuildItem(EquipmentSlot slotType, EBodyModelPart bodyPart, PlayerBody playerBody, List<CamoEditorItem> result)
		{
			if (!playerBody.SlotViews.TryGetByKey(slotType, out var slot))
			{
				return;
			}
			if (!StartDecalTransforms.TryGetValue(bodyPart, out var startTransforms))
			{
				return;
			}

            var go = slot.GameObject_0;
            if (!go)
            {
                return;
            }
            if (!go.TryGetComponent<AssetPoolObject>(out var assetPoolObject))
            {
                return;
            }
			if (!go.TryGetComponent<DressItem>(out _))
			{
				// this editor is only for skinned meshes
				return;
			}

            var item = slot.Item_0;
			var itemId = item.Id;
			var instanceID = go.GetInstanceID();
			var decalsHost = new SkinnedDecalsHost(assetPoolObject.transform, playerBody.SkeletonRootJoint);

            Logger.Log(LogLevel.Info, "CamoEditor", "Setup item", itemId, instanceID);

            result.Add(new CamoEditorItem
            (
                Name: GClass2348.Localized(item.Name),
                ItemId: itemId,
                InstanceID: instanceID,
				DecalsHost: decalsHost,
				StencilType: 1,
				StartTransforms: startTransforms
            ));
		}

        public void LateUpdate()
        {
            if (CamoEditor.Some(out var camoEditor))
            {
                camoEditor.DrawDecalProjectionBox();
            }
        }

        public void OnGUI()
        {
            if (CamoEditor.Some(out var camoEditor))
            {
                camoEditor.DrawWindow();
            }
        }

		public DecalInfo GetNewDecalInfo(StartDecalTransform startTransform, byte stencilType)
		{
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var decalInfo = new DecalInfo()
            {
                SchemaVersion = DecalInfo.CurrentSchemaVersion,
                SaveTime = time,
                Name = "",
                Texture = BigPlugin.DefaultCamoName,
                TextureUV = new Vector4(0, 0, 1, 1),
                TextureAngle = 0,
                ColorHSVA = new Vector4(0, 0, 1, 1),
                Mask = BigPlugin.DefaultMaskName,
                MaskUV = new Vector4(0, 0, 1, 1),
                MaskAngle = 0,
                Bone = startTransform.Bone,
                LocalPosition = startTransform.LocalPosition,
                LocalEulerAngles = startTransform.LocalEulerAngles,
                LocalScale = startTransform.LocalScale,
                MaxAngle = 0.4f,
                IsVisible = true,
                MirrorMode = DecalMirrorMode.Disabled,
                PaintMode = DecalPaintMode.Paint,
                StencilType = stencilType,
            };

            return decalInfo;
		}

		public void SwitchStartTransform(string itemId, int decalIndex, DecalInfo decalInfo, StartDecalTransform startTransform)
		{
			decalInfo.Bone = startTransform.Bone;
            decalInfo.LocalPosition = startTransform.LocalPosition;
            decalInfo.LocalEulerAngles = startTransform.LocalEulerAngles;
			decalInfo.LocalScale = startTransform.LocalScale;

			BigPlugin.ApplyBone(itemId, decalIndex);
            BigPlugin.ApplyLocalPosition(itemId, decalIndex);
            BigPlugin.ApplyLocalEulerAngles(itemId, decalIndex);
			BigPlugin.FixScale(itemId, decalIndex, decalInfo);
		}

		public bool CanRotate()
		{
            if (CamoEditor.Some(out var camoEditor))
            {
                if (GUIUtility.hotControl != 0)
                {
                    return false;
                }
                if (camoEditor.TransformHandle &&
                    camoEditor.TransformHandle.IsDragging)
                {
                    return false;
                }
            }

			return true;
		}

		public bool CanScroll()
		{
            if (CamoEditor.Some(out var camoEditor))
            {
                if (camoEditor.WindowRect.Contains(Event.current.mousePosition))
                {
                    return false;
                }
            }

            return true;
		}

        public void CloseCamoEditor()
        {
            IsCamoEditorWaitingForWeaponPreview = false;

			// CloseCamoEditor method can be called
			// even when editor is not intialized, this happens in cases:
			// 1) user can quickly tap Modify and hit Escape,
			// which means weapon preview won't be fully loaded,
			// 2) WeaponModdingScreen.Close is called even if user
			// entered customization window on trader guns

            if (!CamoEditor.Some(out var camoEditor))
            {
                Logger.Log(LogLevel.Info, "CamoEditor", "Potential warning. Tried to close uninitialized decal editor");
                return;
            }

            foreach (var item in camoEditor.Items)
			{
				BigPlugin.SaveOrDeleteItemDecals(item.ItemId, item.InstanceID);
			}

            BigPlugin.Instance.SaveClosedTexturesDirectoriesToDisk();
            BigPlugin.Instance.SaveFavouriteTexturesToDisk();

            camoEditor.Destroy();
            CamoEditor = default;
        }
    }
}
