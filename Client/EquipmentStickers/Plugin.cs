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

using BigPlugin = SevenBoldPencil.WeaponCamoAndStickers.Plugin;
using CamoEditorResources = SevenBoldPencil.WeaponCamoAndStickers.CamoEditorResources;
using DecalMirrorMode = SevenBoldPencil.WeaponCamoAndStickers.DecalMirrorMode;
using DecalPaintMode = SevenBoldPencil.WeaponCamoAndStickers.DecalPaintMode;
using DecalInfo = SevenBoldPencil.WeaponCamoAndStickers.DecalInfo;
using SkinnedDecalsHost = SevenBoldPencil.WeaponCamoAndStickers.SkinnedDecalsHost;

// TODO make all clothes - stencil 0, all equipment - stencil 1

namespace SevenBoldPencil.EquipmentStickers
{
    public readonly record struct StartDecalTransform(string Name, string Bone, Vector3 LocalPosition, Vector3 LocalEulerAngles, Vector3 LocalScale);

    [BepInPlugin("7Bpencil.EquipmentStickers", "7Bpencil.EquipmentStickers", "1.16.1")]
    [BepInDependency("7Bpencil.WeaponCamoAndStickers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
		public const string BoneSpine1 = "Root_Joint/Base HumanPelvis/Base HumanSpine1";
		public const string BoneSpine2 = "Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2";
		public const string BoneSpine3 = "Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3";
		public const string BoneNeck = "Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanNeck";
		public const string BoneHead = "Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanNeck/Base HumanHead";
		public const string BoneRightUpperarm = "Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanRibcage/Base HumanRCollarbone/Base HumanRUpperarm";
		public const string BoneLeftUpperarm = "Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanRibcage/Base HumanLCollarbone/Base HumanLUpperarm";
		public const string BoneRightThigh1 = "Root_Joint/Base HumanPelvis/Base HumanRThigh1";
		public const string BoneRightThigh2 = "Root_Joint/Base HumanPelvis/Base HumanRThigh1/Base HumanRThigh2";
		public const string BoneRightCalf = "Root_Joint/Base HumanPelvis/Base HumanRThigh1/Base HumanRThigh2/Base HumanRCalf";
		public const string BoneRightFoot = "Root_Joint/Base HumanPelvis/Base HumanRThigh1/Base HumanRThigh2/Base HumanRCalf/Base HumanRFoot";
		public const string BoneRightToe = "Root_Joint/Base HumanPelvis/Base HumanRThigh1/Base HumanRThigh2/Base HumanRCalf/Base HumanRFoot/Base HumanRToe";
		public const string BoneLeftThigh1 = "Root_Joint/Base HumanPelvis/Base HumanLThigh1";
		public const string BoneLeftThigh2 = "Root_Joint/Base HumanPelvis/Base HumanLThigh1/Base HumanLThigh2";
		public const string BoneLeftCalf = "Root_Joint/Base HumanPelvis/Base HumanLThigh1/Base HumanLThigh2/Base HumanLCalf";
		public const string BoneLeftFoot = "Root_Joint/Base HumanPelvis/Base HumanLThigh1/Base HumanLThigh2/Base HumanLCalf/Base HumanLFoot";
		public const string BoneLeftToe = "Root_Joint/Base HumanPelvis/Base HumanLThigh1/Base HumanLThigh2/Base HumanLCalf/Base HumanLFoot/Base HumanLToe";

		// TODO honestly, I dont even care what bones rigs and armors have,
		// I attach decals to player body anyway. Not sure about backpacks, tho
		private Dictionary<EBodyModelPart, StartDecalTransform[][]> StartDecalTransforms = new()
		{
			{
				// TODO add attachment points for different parts of the head
				EBodyModelPart.Head,
				[
					[
						new("Head Forehead", BoneHead, new(-0.104f, 0.14f, 0), new(11.173f, 270, 0), new(0.08f, 0.08f, 0.08f)),
						new("Head Right", BoneHead, new(-0.105f, 0.038f, -0.089f), new(354.98f, 277.755f, 79.388f), new(0.106f, 0.106f, 0.106f)),
						new("Head Left", BoneHead, new(-0.105f, 0.038f, 0.089f), new(354.98f, 264.424f, 273.614f), new(0.106f, 0.106f, 0.106f)),
						new("Head Back", BoneHead, new(-0.073f, -0.093f, 0), new(3.2f, 270, 180), new(0.106f, 0.106f, 0.106f)),
						new("Head Top", BoneHead, new(-0.184f, 0.048f, 0), new(281.9f, 90, 0), new(0.106f, 0.106f, 0.106f)),
					],
					[
						new("Head Cheek Right", BoneHead, new(-0.004f, 0.09f, -0.071f), new(351.173f, 263.495f, 69.262f), new(0.077f, 0.077f, 0.077f)),
						new("Head Cheek Left", BoneHead, new(-0.004f, 0.09f, 0.071f), new(351.173f, 276.424f, 286.962f), new(0.077f, 0.077f, 0.077f)),
					],
					[
						new("Neck Back", BoneNeck, new(-0.067f, -0.066f, 0), new(14.524f, 270, 180), new(0.08f, 0.08f, 0.08f)),
					]
				]
			},
			{
				// TODO add attachment points for shoulders, middle section, groin
				// TODO upperarm side, upperarm front, upperarm back
				// TODO forearm, IK or not?
				EBodyModelPart.Body,
				[
					[
						new("Chest Front", BoneSpine3, new(0, 0, 0), new(0, 0, 0), new(0.09f, 0.09f, 0.09f)),
						new("Chest Back", BoneSpine3, new(0, 0, 0), new(0, 0, 0), new(0.09f, 0.09f, 0.09f)),
					],
					[
						new("Abdomen Front", BoneSpine2, new(0, 0, 0), new(0, 0, 0), new(0.09f, 0.09f, 0.09f)),
						new("Abdomen Right", BoneSpine2, new(0, 0, 0), new(0, 0, 0), new(0.09f, 0.09f, 0.09f)),
						new("Abdomen Left", BoneSpine2, new(0, 0, 0), new(0, 0, 0), new(0.09f, 0.09f, 0.09f)),
						new("Abdomen Back", BoneSpine2, new(0, 0, 0), new(0, 0, 0), new(0.09f, 0.09f, 0.09f)),
					],
					[
						new("Groin Front", BoneSpine1, new(0, 0, 0), new(0, 0, 0), new(0.09f, 0.09f, 0.09f)),
						new("Groin Back", BoneSpine2, new(0, 0, 0), new(0, 0, 0), new(0.09f, 0.09f, 0.09f)),
					],
					[
						new("Upperarm Right", BoneRightUpperarm, new(-0.142f, 0, 0.11f), new(0, 83, 84f), new(0.068f, 0.1f, 0.068f)),
						new("Upperarm Left", BoneLeftUpperarm, new(-0.142f, 0.011f, -0.11f), new(356, 100, 282), new(0.068f, 0.1f, 0.068f)),
					],
					[
						new("Shoulder Right", BoneRightUpperarm, new(-0.05f, 0, 0.13f), new(0, 95, 87), new(0.1f, 0.1f, 0.1f)),
						new("Shoulder Left", BoneLeftUpperarm, new(-0.05f, 0, -0.13f), new(0, 87, 275), new(0.1f, 0.1f, 0.1f)),
					]
				]
			},
			{
				// TODO add attachment points for different parts of the legs
				EBodyModelPart.Feet,
				[
					[
						new("Right Thigh 1", BoneRightThigh1, new(0, 0.0f, 0.0f), new(0, 0, 0), new(0.1f, 0.1f, 0.1f)),
						new("Right Thigh 2", BoneRightThigh2, new(0, 0.12f, 0.025f), new(350, 90, 0), new(0.1f, 0.1f, 0.1f)),
						new("Right Calf", BoneRightCalf, new(0, 0.0f, 0.0f), new(0, 0, 0), new(0.1f, 0.1f, 0.1f)),
						new("Right Foot", BoneRightFoot, new(0, 0.0f, 0.0f), new(0, 0, 0), new(0.1f, 0.1f, 0.1f)),
						new("Right Toe", BoneRightToe, new(0, 0.0f, 0.0f), new(0, 0, 0), new(0.1f, 0.1f, 0.1f)),
					],
					[
						new("Left Thigh 1", BoneLeftThigh1, new(0, 0.0f, 0.0f), new(0, 0, 0), new(0.1f, 0.1f, 0.1f)),
						new("Left Thigh 2", BoneLeftThigh2, new(0, 0.12f, 0.025f), new(350, 90, 0), new(0.1f, 0.1f, 0.1f)),
						new("Left Calf", BoneLeftCalf, new(0, 0.0f, 0.0f), new(0, 0, 0), new(0.1f, 0.1f, 0.1f)),
						new("Left Foot", BoneLeftFoot, new(0, 0.0f, 0.0f), new(0, 0, 0), new(0.1f, 0.1f, 0.1f)),
						new("Left Toe", BoneLeftToe, new(0, 0.0f, 0.0f), new(0, 0, 0), new(0.1f, 0.1f, 0.1f)),
					]
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
