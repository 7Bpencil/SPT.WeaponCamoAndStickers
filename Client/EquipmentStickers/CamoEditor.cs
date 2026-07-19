//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using EFT.UI;
using SevenBoldPencil.Common;
using System;
using System.Collections.Generic;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.UI;

using BigPlugin = SevenBoldPencil.WeaponCamoAndStickers.Plugin;
using BigCamoEditor = SevenBoldPencil.WeaponCamoAndStickers.CamoEditor;
using CamoEditorStyle = SevenBoldPencil.WeaponCamoAndStickers.CamoEditorStyle;
using CamoEditorResources = SevenBoldPencil.WeaponCamoAndStickers.CamoEditorResources;
using DecalInfo = SevenBoldPencil.WeaponCamoAndStickers.DecalInfo;
using Decal = SevenBoldPencil.WeaponCamoAndStickers.Decal;
using DecalTextureType = SevenBoldPencil.WeaponCamoAndStickers.DecalTextureType;
using DecalSettingType = SevenBoldPencil.WeaponCamoAndStickers.DecalSettingType;
using HandleType = SevenBoldPencil.WeaponCamoAndStickers.HandleType;
using PositionHandle = SevenBoldPencil.WeaponCamoAndStickers.PositionHandle;
using RotationHandle = SevenBoldPencil.WeaponCamoAndStickers.RotationHandle;
using ScaleHandle = SevenBoldPencil.WeaponCamoAndStickers.ScaleHandle;
using SkinnedDecalsHost = SevenBoldPencil.WeaponCamoAndStickers.SkinnedDecalsHost;
using PresetsWindow = SevenBoldPencil.WeaponCamoAndStickers.PresetsWindow;
using static SevenBoldPencil.WeaponCamoAndStickers.CamoEditorConstants;

namespace SevenBoldPencil.EquipmentStickers
{
	public class RawImageCameraProvider(Camera camera, RawImage rawImage) : ICameraProvider
	{
		private readonly Camera _camera = camera;
		private readonly Transform _cameraTransform = camera.transform;
		private readonly RawImage _rawImage = rawImage;
		private readonly RectTransform _rawImageRectTransform = rawImage.rectTransform;

		public Camera GetCamera() => _camera;
		public Transform GetCameraTransform() => _cameraTransform;

		public Ray GetCameraRay()
		{
		    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rawImageRectTransform, Input.mousePosition, null, out var localPoint))
		    {
				// not really possible, I think
		        return new Ray(Vector3.zero, Vector3.forward * -1);
		    }

		    var rect = _rawImageRectTransform.rect;
		    var u = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
		    var v = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
		    return _camera.ViewportPointToRay(new Vector2(u, v));
		}
	}

    public class DecalStringCache
	{
		public static readonly Dictionary<string, string> BonesReadableNames = new()
		{
			{ Plugin.BoneSpine1, "Spine 1" },
			{ Plugin.BoneSpine2, "Spine 2" },
			{ Plugin.BoneSpine3, "Spine 3" },
			{ Plugin.BoneNeck, "Neck" },
			{ Plugin.BoneHead, "Head" },
			{ Plugin.BoneRightUpperarm, "Right Upperarm" },
			{ Plugin.BoneLeftUpperarm, "Left Upperarm" },
		};

		public StringCache<string> SelectBone = new(v => $"Select Bone | {BonesReadableNames[v]}");

        public StringCache<float> LocalPositionX = new(v => $"X: {v:F3}");
        public StringCache<float> LocalPositionY = new(v => $"Y: {v:F3}");
        public StringCache<float> LocalPositionZ = new(v => $"Z: {v:F3}");

        public StringCache<float> LocalEulerAnglesX = new(v => $"X: {v:F3}");
        public StringCache<float> LocalEulerAnglesY = new(v => $"Y: {v:F3}");
        public StringCache<float> LocalEulerAnglesZ = new(v => $"Z: {v:F3}");

        public StringCache<float> LocalScaleX = new(v => $"X: {v:F3}");
        public StringCache<float> LocalScaleY = new(v => $"Y: {v:F3}");
        public StringCache<float> LocalScaleZ = new(v => $"Z: {v:F3}");

        public StringCache<float> ColorH = new(v => $"H: {v:F3}");
        public StringCache<float> ColorS = new(v => $"S: {v:F3}");
        public StringCache<float> ColorV = new(v => $"V: {v:F3}");

        public StringCache<float> ColorA = new(v => $"{v:F3}");

        public StringCache<float> MaxAngle = new(v => $"{v:F3}");
	}

    public record CamoEditorItem
    (
        string Name,
        string ItemId,
        int InstanceID,
        SkinnedDecalsHost DecalsHost,
        byte StencilType,
        StartDecalTransform[][] StartTransforms
    );

    public class CamoEditor
    {
        public Plugin Plugin;
        public BigPlugin BigPlugin;
        public CamoEditorResources CamoEditorResources;
        public CamoEditorStyle CamoEditorStyle;
        public DecalStringCache Strings = new();
        public Camera Camera;
		public RawImage RawImage;
        public RuntimeGizmos RuntimeGizmos;
		public PlayerModelView PlayerModelView;
        public List<CamoEditorItem> Items;
        public bool IsOpened;
        public bool ArePresetsOpened;
        public PresetsWindow PresetsWindow;
        public Vector2 DecalsScrollPosition;
        public Option<int> CurrentlyEditedItemIndex;
        public Option<int> CurrentlyEditedDecalIndex;
        public DecalSettingType DecalSettingType;
        public Vector2 StickersScrollPosition;
        public Vector2 MasksScrollPosition;
		public bool IsStartTransformsListOpened;
        public bool IsColorPickerOpened;
        public WeaponCamoAndStickers.TextField<Vector3> ColorTextField = new(ColorExtensions.HSVtoHexRGB, ColorExtensions.HexRGBtoHSV);
        public RuntimeTransformHandle TransformHandle;
		public Rect WindowRect = GetDefaultWindowRect();

        // brace for imGUI shitshow

        public const int maxDecalsVisible = 9;
        public const int maxPresetsVisible = 21;
        public const int maxTextureIconsVisibleHeight = 11 * (buttonHeight + smallMargin) - smallMargin;
        public const int maxMaskIconsVisibleHeight = 13 * (buttonHeight + smallMargin) - smallMargin;

        public static Rect GetDefaultWindowRect()
        {
            return new(startX, 53 + mainIconWidth + bigMargin, mainIconWidth, openCloseButtonHeight);
        }

        public void DrawWindow()
        {
            // we copy some styles from GUI.skin which can be accessed only from OnGUI call
            if (CamoEditorStyle == null)
            {
                CamoEditorStyle = new(GUI.skin);
                PresetsWindow = new()
                {
                    CamoEditorResources = CamoEditorResources,
                    CamoEditorStyle = CamoEditorStyle,
                    MaxPresetsVisible = maxPresetsVisible,
                    SavePreset = SaveDecalsIntoPreset,
                    SwitchToPreset = SwitchToPreset,
                    DeletePreset = BigPlugin.DeletePreset,
                };
            }

            var originalMatrix = GUI.matrix;
            GUI.matrix = BigCamoEditor.CalculateUIScale();

            if (IsOpened)
            {
                if (CurrentlyEditedItemIndex.Some(out var itemIndex))
                {
					if (CurrentlyEditedDecalIndex.Some(out var decalIndex))
					{
	                    WindowRect.height = CalculateDecalEditWindowHeight(itemIndex, decalIndex);
	                    WindowRect = GUI.Window(10, WindowRect, DrawDecalEditUI, GUIContent.none);

	                    var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
	                    GUI.Window(11, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);
					}
					else
					{
	                    if (ArePresetsOpened)
						{
		                    WindowRect.height = CalculatePresetsWindowHeight(itemIndex);
		                    WindowRect = GUI.Window(10, WindowRect, DrawOpenedWindowPresets, GUIContent.none);

		                    var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
		                    GUI.Window(11, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);
						}
						else
						{
		                    WindowRect.height = CalculateDecalsWindowHeight(itemIndex);
		                    WindowRect = GUI.Window(10, WindowRect, DrawOpenedWindowDecals, GUIContent.none);

		                    var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
		                    GUI.Window(11, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);
						}
					}
                }
                else
                {
                    WindowRect.height = CalculateItemsWindowHeight();
                    WindowRect = GUI.Window(10, WindowRect, DrawOpenedWindowItems, GUIContent.none);

                    var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                    GUI.Window(11, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);
                }
            }
            else
            {
                WindowRect = GUI.Window(10, WindowRect, DrawClosedWindow, GUIContent.none);

                var openButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                GUI.Window(11, openButtonWindowRect, DrawClosedWindowOpenButton, GUIContent.none);
            }

            GUI.matrix = originalMatrix;
        }

        private int CalculateItemsWindowHeight()
        {
            // I dont think there will ever be 0 items
			if (Items.Count == 0)
			{
				return
					bigMargin + buttonHeight + bigMargin; // no items text
			}
			else
			{
				return
					bigMargin +
					Items.Count * (buttonHeight + mediumMargin) - mediumMargin + // items
					bigMargin;
			}
        }

        private int CalculateDecalsWindowHeight(int itemIndex)
        {
            var item = Items[itemIndex];
            var totalDecalsCount = BigPlugin.GetDecalsCount(item.ItemId);
            var (_, visibleHeight) = BigCamoEditor.CalculateScrollViewTotalAndVisibleHeight(totalDecalsCount, maxDecalsVisible, boxHeight, mediumMargin);
            return
                smallMargin + buttonHeight + smallMargin + // item name
                buttonHeight + mediumMargin + // back button
                buttonHeight + bigMargin + // show/hide presets button
                smallMargin + bigMargin + // separator
                visibleHeight + mediumMargin + // decals
                buttonHeight + bigMargin; // add new decal button
        }

		private int CalculatePresetsWindowHeight(int itemIndex)
		{
            var header =
                smallMargin + buttonHeight + smallMargin + // item name
                buttonHeight + mediumMargin + // back button
                buttonHeight + bigMargin + // show/hide presets button
                smallMargin + bigMargin + // separator
                buttonHeight + mediumMargin; // preset name

            var totalPresets = BigPlugin.GetPresetsCount();
            if (totalPresets > 0)
            {
                var (_, visibleHeight) = BigCamoEditor.CalculateScrollViewTotalAndVisibleHeight(totalPresets, maxPresetsVisible, buttonHeight, smallMargin);
                return
                    header +
                    visibleHeight + bigMargin; // presets
            }
            else
            {
                return
                    header +
                    buttonHeight + bigMargin; // no presets text
            }
		}

		private int CalculateDecalEditWindowHeight(int itemIndex, int decalIndex)
		{
			var item = Items[itemIndex];
			if (IsStartTransformsListOpened)
			{
				var startTransformsHeight = CalculateStartTransformsListHeight(item.StartTransforms);
				return
					bigMargin +
	                buttonHeight + mediumMargin + // back button
	                buttonHeight + mediumMargin + // decal name
					buttonHeight + bigMargin + // select start transform
	                smallMargin + bigMargin + // separator
					startTransformsHeight + bigMargin; // start transforms
			}
			else
			{
                if (DecalSettingType == DecalSettingType.Texture)
				{
		            var texturesDirectory = BigPlugin.GetTexturesDirectory(DecalTextureType.Sticker);
		            var (totalHeight, visibleHeight) = BigCamoEditor.CalculateTexturesDirectoryHeight(texturesDirectory, maxTextureIconsVisibleHeight);
		            return
		                bigMargin +
		                buttonHeight + mediumMargin + // back button
		                buttonHeight + mediumMargin + // decal name
		                buttonHeight + mediumMargin + // select start transform
		                4 * (buttonHeight + smallMargin) - smallMargin + bigMargin + // position, rotation, scale, flip
		                smallMargin + bigMargin + // separator
		                buttonHeight + bigMargin + // toolbar texture/mask
		                buttonHeight + bigMargin + // color
		                buttonHeight + smallMargin + // opacity
		                buttonHeight + bigMargin + // max angle
		                iconSize + bigMargin + // icon
		                smallMargin + bigMargin + // separator
		                visibleHeight + bigMargin; // icons grid
				}
				else
				{
                    var texturesDirectory = BigPlugin.GetTexturesDirectory(DecalTextureType.Mask);
                    var (totalHeight, visibleHeight) = BigCamoEditor.CalculateTexturesDirectoryHeight(texturesDirectory, maxMaskIconsVisibleHeight);
                    return
                        bigMargin +
                        buttonHeight + mediumMargin + // back button
                        buttonHeight + mediumMargin + // decal name
		                buttonHeight + mediumMargin + // select start transform
                        4 * (buttonHeight + smallMargin) - smallMargin + bigMargin + // position, rotation, scale, flip
                        smallMargin + bigMargin + // separator
                        buttonHeight + bigMargin + // toolbar texture/mask
                        iconSize + bigMargin + // icon
                        smallMargin + bigMargin + // separator
                        visibleHeight + bigMargin; // icons grid
				}
			}
		}

		private int CalculateStartTransformsListHeight(StartDecalTransform[][] startTransforms)
		{
			var result = 0;
			foreach (var startTransformsGroup in startTransforms)
			{
				foreach (var startTransform in startTransformsGroup)
				{
					result += buttonHeight + smallMargin;
				}
				result -= smallMargin;
				result += bigMargin;
			}
			result -= bigMargin;

			return result;
		}

        private void DrawOpenedWindowItems(int windowID)
        {
            BigCamoEditor.DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var x = bigMargin;
            var y = bigMargin;

			if (Items.Count == 0)
			{
                GUI.Label(new Rect(x, y, boxWidth, buttonHeight), "No Suitable Items", CamoEditorStyle.LabelStyleValue);
			}
			else
			{
				for (var i = 0; i < Items.Count; i++)
				{
					var item = Items[i];
					if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), item.Name))
					{
                        CurrentlyEditedItemIndex = new(i);
					}
					y += buttonHeight + mediumMargin;
				}
			}

			GUI.DragWindow();
        }

        private void DrawOpenedWindowDecals(int windowID)
        {
            BigCamoEditor.DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var itemIndex = CurrentlyEditedItemIndex.Value;
            var item = Items[itemIndex];

            var x = bigMargin;
            var y = smallMargin;

            GUI.Label(new Rect(x, y, boxWidth, buttonHeight), item.Name, CamoEditorStyle.LabelStyleValue);
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                CurrentlyEditedItemIndex = default;
            }
            y += buttonHeight + mediumMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Show Presets"))
            {
                ArePresetsOpened = true;
            }
            y += buttonHeight + bigMargin;

            BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            if (BigPlugin.GetDecalsInfo(item.ItemId).Some(out var decalsInfo))
            {
                var decalsY = y;

                var (totalHeight, visibleHeight) = BigCamoEditor.CalculateScrollViewTotalAndVisibleHeight(decalsInfo.Count, maxDecalsVisible, boxHeight, mediumMargin);
                var totalRect = new Rect(x, decalsY, boxWidth, totalHeight);
                var visibleRect = new Rect(x, decalsY, boxWidth + 16, visibleHeight);

                BigCamoEditor.DrawScrollBar(x + boxWidth + 5, decalsY, totalHeight, visibleHeight, DecalsScrollPosition);
                DecalsScrollPosition = GUI.BeginScrollView(visibleRect, DecalsScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

                for (var i = 0; i < decalsInfo.Count; i++)
                {
                    var decalInfo = decalsInfo[i];
                    BigCamoEditor.DrawDecalElementUI
                    (
                        x, decalsY, i, decalInfo,
                        item.ItemId, item.InstanceID, BigPlugin, CamoEditorResources, CamoEditorStyle,
                        SetCurrentlyEditedDecal
                    );
                    decalsY += boxHeight + mediumMargin;
                }
                y += visibleHeight + mediumMargin;

                GUI.EndScrollView();
            }

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Add Sticker"))
            {
                var newDecalInfo = Plugin.GetNewDecalInfo(item.StartTransforms[0][0], item.StencilType);
                var newDecalIndex = BigPlugin.AddNewPaintDecal(item.ItemId, item.InstanceID, newDecalInfo, item.DecalsHost, Camera);
                SetCurrentlyEditedDecal(item.ItemId, item.InstanceID, newDecalIndex);
				IsStartTransformsListOpened = true;
            }

			GUI.DragWindow();
        }

		private void DrawOpenedWindowPresets(int windowID)
		{
            BigCamoEditor.DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var itemIndex = CurrentlyEditedItemIndex.Value;
            var item = Items[itemIndex];

            var x = bigMargin;
            var y = smallMargin;

            GUI.Label(new Rect(x, y, boxWidth, buttonHeight), item.Name, CamoEditorStyle.LabelStyleValue);
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                CurrentlyEditedItemIndex = default;
            }
            y += buttonHeight + mediumMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Hide Presets"))
            {
                ArePresetsOpened = false;
            }
            y += buttonHeight + bigMargin;

            BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            PresetsWindow.DrawPresetNameTextField(ref x, ref y);
            PresetsWindow.DrawPresets(ref x, ref y, BigPlugin.GetPresetNames());

			GUI.DragWindow();
		}

        private void SaveDecalsIntoPreset(string presetName)
        {
			if (CurrentlyEditedItemIndex.Some(out var itemIndex))
			{
	            var item = Items[itemIndex];
	            BigPlugin.SaveDecalsIntoPreset(item.ItemId, presetName);
			}
        }

        private void SwitchToPreset(string presetName)
        {
			if (CurrentlyEditedItemIndex.Some(out var itemIndex))
			{
	            var item = Items[itemIndex];
	            BigPlugin.SwitchToPreset(item.ItemId, item.InstanceID, item.DecalsHost, Camera, presetName);
			}
        }

        private void SetCurrentlyEditedDecal(string itemId, int instanceID, int decalIndex)
        {
            var (decalInfo, decal) = BigPlugin.GetDecal(itemId, instanceID, decalIndex);

            // TODO
            // these should be grouped together with decal index,
            // so we dont forget to correctly init/clean those fields

            CurrentlyEditedDecalIndex = new(decalIndex);
            ColorTextField.SetValue(decalInfo.ColorHSVA);
			FreezeAnimator();
        }

		private void DrawDecalEditUI(int windowID)
		{
            BigCamoEditor.DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var itemIndex = CurrentlyEditedItemIndex.Value;
			var decalIndex = CurrentlyEditedDecalIndex.Value;
            var item = Items[itemIndex];
            var (decalInfo, decal) = BigPlugin.GetDecal(item.ItemId, item.InstanceID, decalIndex);

            var x = bigMargin;
            var y = bigMargin;

            DrawDecalEditUI_Header(x, ref y, decalIndex, decalInfo, decal);

			if (IsStartTransformsListOpened)
			{
				DrawStartTransformsList(x, ref y, item.ItemId, decalIndex, decalInfo, item.StartTransforms);
			}
			else
			{
	            DrawDecalEditUI_Transform(x, ref y, item.ItemId, decalIndex, decalInfo, decal);

	            BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
	            y += smallMargin + bigMargin;

                DecalSettingType = (DecalSettingType)GUI.Toolbar(new Rect(x, y, boxWidth, buttonHeight), (int)DecalSettingType, CamoEditorResources.DecalSettingsToolbar);
                y += buttonHeight + bigMargin;

                if (DecalSettingType == DecalSettingType.Texture)
                {
                    DrawDecalEditUI_Texture(x, ref y, item.ItemId, decalIndex, decalInfo, decal);
                }
                else
                {
                    DrawDecalEditUI_Mask(x, ref y, item.ItemId, decalIndex, decalInfo, decal);
                }
			}

			GUI.DragWindow();
		}

        private void DrawDecalEditUI_Header(int x, ref int y, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                CurrentlyEditedDecalIndex = default;
                DestroyTransformHandle();
				UnfreezeAnimator();
            }
            y += buttonHeight + mediumMargin;


            decalInfo.Name = GUI.TextField(new Rect(x, y, boxWidth, buttonHeight), decalInfo.Name, maxDecalNameLength, CamoEditorStyle.TextFieldStyle);
            if (string.IsNullOrWhiteSpace(decalInfo.Name))
            {
                GUI.Label(new Rect(x + CamoEditorStyle.TextFieldStyle.contentOffset.x + 3, y, boxWidth, buttonHeight), "enter decal name (optional)", CamoEditorStyle.LabelStyleName);
            }
            y += buttonHeight + mediumMargin;
        }

		private void DrawStartTransformsList(int x, ref int y, string itemId, int decalIndex, DecalInfo decalInfo, StartDecalTransform[][] startTransforms)
		{
            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Close"))
			{
				IsStartTransformsListOpened = false;
			}
            y += buttonHeight + bigMargin;

            BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

			foreach (var startTransformsGroup in startTransforms)
			{
				foreach (var startTransform in startTransformsGroup)
				{
		            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), startTransform.Name))
					{
						Plugin.SwitchStartTransform(itemId, decalIndex, decalInfo, startTransform);
	                    SyncTransformHandle();
						IsStartTransformsListOpened = false;
					}
					y += buttonHeight + smallMargin;
				}
				y -= smallMargin;
				y += bigMargin;
			}
		}

        private void DrawDecalEditUI_Transform(int x, ref int y, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), Strings.SelectBone.Get(decalInfo.Bone)))
			{
				IsStartTransformsListOpened = true;
			}
            y += buttonHeight + mediumMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditPositionIcon))
            {
                SetupTransformHandle(HandleType.Position, itemId, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalPositionX.Get(decal.DecalTransform.localPosition.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalPositionY.Get(decal.DecalTransform.localPosition.y), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalPositionZ.Get(decal.DecalTransform.localPosition.z), CamoEditorStyle.LabelStyleName);
            }
            y += buttonHeight + smallMargin;


            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditRotationIcon))
            {
                SetupTransformHandle(HandleType.Rotation, itemId, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalEulerAnglesX.Get(decal.DecalTransform.localEulerAngles.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalEulerAnglesY.Get(decal.DecalTransform.localEulerAngles.y), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalEulerAnglesZ.Get(decal.DecalTransform.localEulerAngles.z), CamoEditorStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - thirdBoxWidthButton, y, thirdBoxWidthButton, buttonHeight), "round"))
            {
                BigPlugin.RoundLocalEulerAnglesToDegree(itemId, decalIndex, decalInfo);
                SyncTransformHandle();
            }
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditScaleIcon))
            {
                SetupTransformHandle(HandleType.Scale, itemId, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalScaleX.Get(decal.DecalTransform.localScale.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalScaleY.Get(decal.DecalTransform.localScale.y), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalScaleZ.Get(decal.DecalTransform.localScale.z), CamoEditorStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - thirdBoxWidthButton, y, thirdBoxWidthButton, buttonHeight), "fix scale"))
            {
                BigPlugin.FixScale(itemId, decalIndex, decalInfo);
                SyncTransformHandle();
            }
            y += buttonHeight + smallMargin;

            {
                var lineX = x;
                if (GUI.Button(new Rect(lineX, y, halfBoxWidthButton, buttonHeight), "flip horz"))
                {
                    BigPlugin.FlipHorizontally(itemId, decalIndex, decalInfo);
                    SyncTransformHandle();
                }
                lineX += halfBoxWidthButton + smallMargin;

                if (GUI.Button(new Rect(lineX, y, halfBoxWidthButton, buttonHeight), "flip vert"))
                {
                    BigPlugin.FlipVertically(itemId, decalIndex, decalInfo);
                    SyncTransformHandle();
                }
            }
            y += buttonHeight + bigMargin;
        }

		private void DrawDecalEditUI_Texture(int x, ref int y, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal)
		{
            var textureData = BigPlugin.GetTextureData(decalInfo.Texture);

            {
                var colorButtonRect = new Rect(x, y, buttonHeight, buttonHeight);

                BigCamoEditor.DrawColor(colorButtonRect, decalInfo.ColorHSVA.HSVAtoRGBA().WithAlpha(1f));
                if (GUI.Button(colorButtonRect, GUIContent.none, GUIStyle.none))
                {
                    IsColorPickerOpened = !IsColorPickerOpened;
                }

                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.ColorH.Get(decalInfo.ColorHSVA.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.ColorS.Get(decalInfo.ColorHSVA.y), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.ColorV.Get(decalInfo.ColorHSVA.z), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                {
                    var textFieldX = x + boxWidth - fourthBoxWidthButton;
                    GUI.Label(new Rect(textFieldX - 39, y, longFieldWidth, buttonHeight), "RGB:", CamoEditorStyle.LabelStyleName);

                    var previousBackgroundColor = GUI.backgroundColor;
                    var buttonBackgroundColor = ColorTextField.IsValid ? previousBackgroundColor : Color.red;

                    GUI.backgroundColor = buttonBackgroundColor;
                    var newColorHex = GUI.TextField(new Rect(textFieldX, y, fourthBoxWidthButton, buttonHeight), ColorTextField.Value, 7, CamoEditorStyle.RGBHexTextFieldStyle);
                    GUI.backgroundColor = previousBackgroundColor;

                    if (ColorTextField.TrySetValue(newColorHex, out var newColorOption) && newColorOption.Some(out var newColor))
                    {
                        decalInfo.ColorHSVA = newColor.WithAlpha(decalInfo.ColorHSVA.w);
                        BigPlugin.ApplyColor(itemId, decalIndex);
                    }
                }
            }
            y += buttonHeight + bigMargin;


            {
                var sliderWidth = 212;

                var labelX = x;
                var sliderX = labelX + nameWidth + smallMargin - 42;
                var valueX = sliderX + sliderWidth + smallMargin;

                var opacityY = y;
                var maxAngleY = opacityY + buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, opacityY, nameWidth, buttonHeight), "Opacity:", CamoEditorStyle.LabelStyleName);
                var newAlpha = GUI.HorizontalSlider(new Rect(sliderX, opacityY + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.w, 0f, 1f);
                if (newAlpha != decalInfo.ColorHSVA.w)
                {
                    decalInfo.ColorHSVA.w = newAlpha;
                    BigPlugin.ApplyColor(itemId, decalIndex);
                }
                GUI.Label(new Rect(valueX, opacityY, longFieldWidth, buttonHeight), Strings.ColorA.Get(decalInfo.ColorHSVA.w), CamoEditorStyle.LabelStyleValue);


                GUI.Label(new Rect(labelX, maxAngleY, nameWidth, buttonHeight), "Max Angle:", CamoEditorStyle.LabelStyleName);
                var newMaxAngle = GUI.HorizontalSlider(new Rect(sliderX, maxAngleY + 11, sliderWidth, buttonHeight), decalInfo.MaxAngle, 0f, 1f);
                if (newMaxAngle != decalInfo.MaxAngle)
                {
                    decalInfo.MaxAngle = newMaxAngle;
                    BigPlugin.ApplyMaxAngle(itemId, decalIndex);
                }
                GUI.Label(new Rect(valueX, maxAngleY, longFieldWidth, buttonHeight), Strings.MaxAngle.Get(decalInfo.MaxAngle), CamoEditorStyle.LabelStyleValue);


                y = maxAngleY + buttonHeight + bigMargin;
            }

            {
                GUI.Button(new Rect(x, y, iconSize, iconSize), textureData.Preview);

                var labelX = x + iconSize + smallMargin + 12;
                GUI.Label(new Rect(labelX, y + 1, 256, buttonHeight), decalInfo.Texture, CamoEditorStyle.TextureNameStyle);

                y += iconSize + bigMargin;
            }

            BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            BigCamoEditor.DrawAllTextures(
                x, y, itemId, decalIndex, decalInfo, decal, DecalTextureType.Sticker, maxTextureIconsVisibleHeight,
                BigPlugin, GetScrollPosition, SyncTransformHandle,
                CamoEditorResources, CamoEditorStyle);
		}

		private void DrawDecalEditUI_Mask(int x, ref int y, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal)
		{
            var maskData = BigPlugin.GetTextureData(decalInfo.Mask);

            {
                GUI.Button(new Rect(x, y, iconSize, iconSize), maskData.Preview);

                var labelX = x + iconSize + smallMargin + 12;
                GUI.Label(new Rect(labelX, y + 1, 256, buttonHeight), decalInfo.Mask, CamoEditorStyle.TextureNameStyle);

                y += iconSize + bigMargin;
            }

            BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            BigCamoEditor.DrawAllTextures(
                x, y, itemId, decalIndex, decalInfo, decal, DecalTextureType.Mask, maxMaskIconsVisibleHeight,
                BigPlugin, GetScrollPosition, SyncTransformHandle,
                CamoEditorResources, CamoEditorStyle);
		}

        private ref Vector2 GetScrollPosition(DecalTextureType textureType)
        {
            switch (textureType)
            {
                case DecalTextureType.Sticker: return ref StickersScrollPosition;
                case DecalTextureType.Mask: return ref MasksScrollPosition;
                default: throw new ArgumentException();
            }
        }

        private void DrawOpenedWindowCloseButton(int windowID)
        {
            BigCamoEditor.DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.OpenedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                IsOpened = false;
				WindowRect.width = mainIconWidth;
				WindowRect.height = openCloseButtonHeight;
            }
        }

        private void DrawClosedWindow(int windowID)
        {
            BigCamoEditor.DrawColor(new Rect(0, 0, mainIconWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(new Rect(0, 0, mainIconWidth, openCloseButtonHeight), CamoEditorResources.MainIcon, ScaleMode.StretchToFill);

			GUI.DragWindow();
        }

        private void DrawClosedWindowOpenButton(int windowID)
        {
            BigCamoEditor.DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.ClosedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                IsOpened = true;
				WindowRect.width = windowWidth;
            }
        }

#if DEBUG
        public void PrintDecalsTransforms()
        {
			if (!CurrentlyEditedItemIndex.Some(out var itemIndex))
			{
				return;
			}

			var item = Items[itemIndex];
            if (!BigPlugin.GetDecalsInfo(item.ItemId).Some(out var decalsInfo))
            {
				return;
            }

			for (var i = 0; i < decalsInfo.Count; i++)
			{
	            var (decalInfo, decal) = BigPlugin.GetDecal(item.ItemId, item.InstanceID, i);
				var p = decal.DecalTransform.localPosition;
				var r = decal.DecalTransform.localEulerAngles;
				var s = decal.DecalTransform.localScale;
				Plugin.Instance.LoggerInstance.LogWarning($"new(\"{decalInfo.Name}\", \"{decalInfo.Bone}\", new({p.x:F3}f, {p.y:F3}f, {p.z:F3}f), new({r.x:F3}f, {r.y:F3}f, {r.z:F3}f), new({s.x:F3}f, {s.y:F3}f, {s.z:F3}f)),");
			}
        }
#endif

        public void SetupTransformHandle(HandleType handleType)
        {
			if (!CurrentlyEditedItemIndex.Some(out var itemIndex))
			{
				return;
			}
            if (!CurrentlyEditedDecalIndex.Some(out var decalIndex))
            {
				return;
            }

			var item = Items[itemIndex];
            var (decalInfo, decal) = BigPlugin.GetDecal(item.ItemId, item.InstanceID, decalIndex);
            SetupTransformHandle(handleType, item.ItemId, decalIndex, decalInfo, decal);
        }

        private void SetupTransformHandle(HandleType handleType, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            if (TransformHandle)
            {
                ForceOnEndedDraggingHandle();
                DestroyTransformHandle();
            }

            var handle = CreateTransformHandle(handleType, itemId, decalIndex, decalInfo, decal);
			var cameraProvider = new RawImageCameraProvider(Camera, RawImage);
            TransformHandle = RuntimeTransformHandle.Create(handle, decal.DecalRoot, cameraProvider, 1 << LayerMaskClass.WeaponPreview);
			TransformHelperClass.SetLayersRecursively(TransformHandle.gameObject, LayerMaskClass.WeaponPreview);
        }

		public void FreezeAnimator()
		{
			// I would prefer to store direct reference to PlayerAnimatorController, but in some cases its null
			if (PlayerModelView && PlayerModelView.ModelPlayerPoser && PlayerModelView.ModelPlayerPoser.PlayerAnimatorController)
			{
				PlayerModelView.ModelPlayerPoser.PlayerAnimatorController.enabled = false;
			}
		}

		public void UnfreezeAnimator()
		{
			if (PlayerModelView && PlayerModelView.ModelPlayerPoser && PlayerModelView.ModelPlayerPoser.PlayerAnimatorController)
			{
				PlayerModelView.ModelPlayerPoser.PlayerAnimatorController.enabled = true;
			}
		}

        public ITransformHandle CreateTransformHandle(HandleType handleType, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
     		if (handleType == HandleType.Position)
			{
                return new PositionHandle(BigPlugin, itemId, decalIndex, decalInfo, decal, CamoEditorResources.PositionHandleShader);
			}
			if (handleType == HandleType.Rotation)
			{
                return new RotationHandle(BigPlugin, itemId, decalIndex, decalInfo, decal, CamoEditorResources.RotationHandleShader);
			}
			if (handleType == HandleType.Scale)
			{
                return new ScaleHandle(BigPlugin, itemId, decalIndex, decalInfo, decal, CamoEditorResources.ScaleHandleShader);
			}

            throw new ArgumentException($"unknown handleType: {handleType}");
        }

        private void SyncTransformHandle()
        {
            if (TransformHandle)
            {
                TransformHandle.ResetHandleTransform();
            }
        }

        public void ForceOnEndedDraggingHandle()
        {
            if (TransformHandle && TransformHandle.IsDragging)
            {
                TransformHandle.InvokeOnInteractionEnd();
            }
        }

        private void DestroyTransformHandle()
        {
            if (TransformHandle)
            {
                GameObject.Destroy(TransformHandle.gameObject);
            }
        }

        public void Destroy()
        {
            if (RuntimeGizmos)
            {
                GameObject.Destroy(RuntimeGizmos);
            }

            DestroyTransformHandle();
			UnfreezeAnimator();
        }

        public void DrawDecalProjectionBox()
        {
			if (!CurrentlyEditedItemIndex.Some(out var itemIndex))
			{
				return;
			}
            if (!CurrentlyEditedDecalIndex.Some(out var decalIndex))
            {
				return;
            }

			var item = Items[itemIndex];
            var (decalInfo, decal) = BigPlugin.GetDecal(item.ItemId, item.InstanceID, decalIndex);
            BigCamoEditor.DrawDecalProjectionBox(RuntimeGizmos, decalInfo, decal);
        }
	}
}
