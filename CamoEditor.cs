//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using DeferredDecals;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using EFT.UI.WeaponModding;
using Newtonsoft.Json;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Systems.Effects;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.Rendering;

namespace SevenBoldPencil.WeaponCamo
{
    public class CamoEditorResources
    {
        public Shader PositionHandleShader;
        public Shader RotationHandleShader;
        public Shader ScaleHandleShader;

        public Texture2D MainIcon;
        public Texture2D ClosedIcon;
        public Texture2D OpenedIcon;
        public Texture2D MoveUpIcon;
        public Texture2D MoveDownIcon;
        public Texture2D EditPositionIcon;
        public Texture2D EditRotationIcon;
        public Texture2D EditScaleIcon;
        public Texture2D EditTextureTilingIcon;
        public Texture2D DuplicateIcon;
        public Texture2D DeleteIcon;
        public Texture2D ColorWheelHSV;

        public GUIStyle LabelStyleName;
        public GUIStyle TextureNameStyle;
        public GUIStyle LabelStyleValue;
		public GUIStyle ColorPickerButtonStyle;

        public CamoEditorResources(AssetBundle bundle)
        {
            PositionHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamo/Shaders/HandleShader.shader");
            RotationHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamo/Shaders/AdvancedHandleShader.shader");
            ScaleHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamo/Shaders/HandleShader.shader");

            MainIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/color-palette.png");
            ClosedIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/closed-arrow.png");
            OpenedIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/opened-arrow.png");
            MoveUpIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/up-arrow.png");
            MoveDownIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/down-arrow.png");
            EditPositionIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/Move-Icon.png");
            EditRotationIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/Rotate-Icon.png");
            EditScaleIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/Scale-Icon.png");
            EditTextureTilingIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/UV-Scale-Icon.png");
            DuplicateIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/copy.png");
            DeleteIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/bin.png");
            ColorWheelHSV = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/hsv-circle.png");

            LabelStyleName = new()
            {
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState()
                {
                    textColor = Color.white
                }
            };

            TextureNameStyle = new()
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = new GUIStyleState()
                {
                    textColor = Color.white
                }
            };

            LabelStyleValue = new()
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState()
                {
                    textColor = Color.white
                }
            };

			ColorPickerButtonStyle = new GUIStyle()
			{
				stretchWidth = true,
				stretchHeight = true,
			};
        }
    }

    public class CamoEditor
    {
        public Plugin Plugin;
        public CamoEditorResources CamoEditorResources;
        public Camera Camera;
        public RuntimeGizmos RuntimeGizmos;
        public string ItemId;
        public int InstanceID;
        public Transform DecalsRoot;
        public bool IsOpened;
        public Vector2 DecalsScrollPosition;
        public Option<int> CurrentlyEditedDecalIndex;
        public Vector2 TexturesScrollPosition;
        public bool IsColorPickerOpened;
        public RuntimeTransformHandle TransformHandle;
		public Rect WindowRect;

        // brace for imGUI shitshow

        private const int startX = 25;
        private const int startY = 19;
        private const int windowWidth = margin + (iconSize + iconSeparator) * iconColumns - iconSeparator + margin;
        private const int buttonHeight = 30;
        private const int buttonSeparator = 4;
        private const int margin = 14;
        private const int decalSeparator = 8;
        private const int iconSize = smallIconSize * 2 + iconSeparator;
        private const int iconSeparator = 4;
        private const int smallIconSize = 32;
        private const int iconColumns = 5;
        private const int maxIconRows = 5;
        private const int maxDecalsVisible = 11;
        private const int boxWidth = windowWidth - margin * 2;
        private const int boxHeight = iconSize + boxMargin * 2;
        private const int boxMargin = 3;
        private const int nameWidth = 120;
        private const int longFieldWidth = 60;
        private const int fixTransformButtonWidth = 110;
        private const int openCloseButtonWidth = 22;
        private const int openCloseButtonHeight = 66;
        private static readonly Rect openCloseButtonIconRect = new(2, 3, 18, 61);
        private static readonly Rect colorPickerRect = new(0, 159, 231, 331);
        private const int hsCircleDiameter = 201;
        private const int mainIconWidth = 62;
        private static readonly Color backgroundColor = new(0.15f, 0.15f, 0.15f, 1f);
        private static readonly Color separatorColor = new(0.1f, 0.1f, 0.1f, 1f);
        private static readonly Color scrollBarHandleColor = new(183, 195, 202, 255);
        private const int scrollBarWidth = 4;

        public static Rect GetDefaultWindowRect()
        {
            return new(startX, startY, mainIconWidth, openCloseButtonHeight);
        }

        private void DrawColor(Rect rect, Color color)
        {
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, color, 0, 0);
        }

        public void DrawWindow()
        {
            if (IsOpened)
            {
				WindowRect.height = CalculateWindowHeight();
                WindowRect = GUI.Window(1, WindowRect, DrawOpenedWindow, GUIContent.none);

                var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                GUI.Window(2, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);

                if (CurrentlyEditedDecalIndex.HasValue)
                {
                    if (IsColorPickerOpened)
                    {
                        var colorPickerWindowRect = new Rect(WindowRect.xMax, WindowRect.y + colorPickerRect.y, colorPickerRect.width, colorPickerRect.height);
                        GUI.Window(3, colorPickerWindowRect, DrawColorPickerWindow, GUIContent.none);

                        var closeColorPickerWindowRect = new Rect(colorPickerWindowRect.xMax, colorPickerWindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                        GUI.Window(4, closeColorPickerWindowRect, DrawColorPickerWindowCloseButton, GUIContent.none);
                    }
                    else
                    {
                        var openColorPickerWindowRect = new Rect(WindowRect.xMax, WindowRect.y + colorPickerRect.y, openCloseButtonWidth, openCloseButtonHeight);
                        GUI.Window(3, openColorPickerWindowRect, DrawColorPickerWindowOpenButton, GUIContent.none);
                    }
                }
            }
            else
            {
                WindowRect = GUI.Window(1, WindowRect, DrawClosedWindow, GUIContent.none);

                var openColorPickerWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                GUI.Window(2, openColorPickerWindowRect, DrawClosedWindowOpenButton, GUIContent.none);
            }
        }

        private void DrawClosedWindow(int windowID)
        {
            DrawColor(new Rect(0, 0, mainIconWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(new Rect(0, 0, mainIconWidth, openCloseButtonHeight), CamoEditorResources.MainIcon, ScaleMode.StretchToFill);

			GUI.DragWindow();
        }

        private void DrawClosedWindowOpenButton(int windowID)
        {
            DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.ClosedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                IsOpened = true;
				WindowRect.width = windowWidth;
            }
        }

        private float CalculateWindowHeight()
        {
            if (CurrentlyEditedDecalIndex.Some(out var currentlyEditedDecalIndex))
            {
                var totalRows = DivideIntRoundUp(Plugin.GetTexturesCount(), iconColumns);
                var (_, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalRows, maxIconRows, iconSize, iconSeparator);
                return
                    margin + buttonHeight + margin +
                    4 * (smallIconSize + iconSeparator) +
                    smallIconSize + margin +
                    buttonHeight + iconSeparator +
                    buttonHeight + margin +
                    iconSize + margin +
                    4 + margin +
                    visibleHeight +
                    margin;
            }
            else
            {
                var totalDecalsCount = Plugin.GetDecalsCount(ItemId);
                var (_, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalDecalsCount, maxDecalsVisible, boxHeight, decalSeparator);
                return
                    margin +
                    visibleHeight +
                    decalSeparator +
                    buttonHeight +
                    margin;
            }
        }

        private void DrawOpenedWindow(int windowID)
		{
            DrawColor(new Rect(0, 0, WindowRect.width, WindowRect.height), backgroundColor);
            if (CurrentlyEditedDecalIndex.Some(out var currentlyEditedDecalIndex))
            {
                DrawDecalEditUI(currentlyEditedDecalIndex);
            }
            else
            {
                DrawDecalsListUI();
            }

			GUI.DragWindow();
        }

        private void DrawOpenedWindowCloseButton(int windowID)
        {
            DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.OpenedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                IsOpened = false;
				WindowRect.width = mainIconWidth;
				WindowRect.height = openCloseButtonHeight;
            }
        }

        public void DrawDecalsListUI()
        {
            var x = margin;
            var y = margin;

            if (Plugin.GetDecalsInfo(ItemId).Some(out var decalsInfo))
            {
                var decalsY = y;

                var (totalHeight, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(decalsInfo.Count, maxDecalsVisible, boxHeight, decalSeparator);
                var totalRect = new Rect(x, decalsY, boxWidth, totalHeight);
                var visibleRect = new Rect(x, decalsY, boxWidth + 16, visibleHeight);

                // render my own vertical scroll bar because unity's one is cannot be set slimmer than 15 px...
                if (decalsInfo.Count > maxDecalsVisible)
                {
                    var handleHeight = visibleHeight * visibleHeight / (float)totalHeight;
                    var handlePositionT = DecalsScrollPosition.y / (float)totalHeight;
                    var handlePosition = handlePositionT * visibleHeight;
                    var scrollBarX = x + boxWidth + 5;
                    DrawColor(new Rect(scrollBarX, decalsY, scrollBarWidth, visibleHeight), separatorColor);
                    DrawColor(new Rect(scrollBarX, decalsY + handlePosition, scrollBarWidth, handleHeight), scrollBarHandleColor);
                }

                DecalsScrollPosition = GUI.BeginScrollView(visibleRect, DecalsScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

                for (var i = 0; i < decalsInfo.Count; i++)
                {
                    var decalInfo = decalsInfo[i];
                    DrawDecalElementUI(x, decalsY, i, decalInfo);
                    decalsY += boxHeight + decalSeparator;
                }

                GUI.EndScrollView();

                y += visibleHeight;
                y += decalSeparator;
            }

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Add New Decal"))
            {
                var newDecalIndex = Plugin.AddNewDecal(ItemId, InstanceID, DecalsRoot, Camera);
                CurrentlyEditedDecalIndex = new(newDecalIndex);
            }
        }

        private void DrawDecalElementUI(int x, int y, int decalIndex, DecalInfo decalInfo)
        {
            var texture = Plugin.GetTexture(decalInfo.Texture);

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), default(string));

            var topLineY = y + boxMargin;
            var bottomLineY = topLineY + smallIconSize + iconSeparator;

            var textureIconX = x + boxMargin;
            if (GUI.Button(new Rect(textureIconX, topLineY, iconSize, iconSize), texture))
            {
                CurrentlyEditedDecalIndex = new(decalIndex);
            }

            var labelX = textureIconX + iconSize + iconSeparator + 2;
            GUI.Label(new Rect(labelX, topLineY + 1, 230, iconSize), decalInfo.Texture, CamoEditorResources.TextureNameStyle);

            var deleteX = x + boxWidth - (iconSeparator + smallIconSize) * 3;
            if (GUI.Button(new Rect(deleteX, bottomLineY, smallIconSize, smallIconSize), CamoEditorResources.DeleteIcon))
            {
                Plugin.Delete(ItemId, decalIndex);
            }

            var duplicateX = deleteX + smallIconSize + iconSeparator;
            if (GUI.Button(new Rect(duplicateX, bottomLineY, smallIconSize, smallIconSize), CamoEditorResources.DuplicateIcon))
            {
                var newDecalIndex = Plugin.Duplicate(ItemId, decalIndex);
                CurrentlyEditedDecalIndex = new(newDecalIndex);
            }

            var arrowX = duplicateX + smallIconSize + iconSeparator;
            if (GUI.Button(new Rect(arrowX, topLineY, smallIconSize, smallIconSize), CamoEditorResources.MoveUpIcon))
            {
                Plugin.Swap(ItemId, decalIndex, decalIndex - 1);
            }
            if (GUI.Button(new Rect(arrowX, bottomLineY, smallIconSize, smallIconSize), CamoEditorResources.MoveDownIcon))
            {
                Plugin.Swap(ItemId, decalIndex, decalIndex + 1);
            }
        }

        private void DrawColorPickerWindow(int windowID)
        {
            var decalIndex = CurrentlyEditedDecalIndex.Value;
            var (decalInfo, decal) = Plugin.GetDecal(ItemId, InstanceID, decalIndex);

            DrawColor(new Rect(0, 0, colorPickerRect.width, colorPickerRect.height), backgroundColor);

            var hsCircleRect = new Rect(margin, margin, hsCircleDiameter, hsCircleDiameter);

			if (GUI.RepeatButton(hsCircleRect, CamoEditorResources.ColorWheelHSV, CamoEditorResources.ColorPickerButtonStyle))
            {
				var direction = Event.current.mousePosition - hsCircleRect.center;
				var directionScaled = direction / (hsCircleDiameter * 0.5f);
				var directionClamped = Vector2.ClampMagnitude(directionScaled, 1f);
				var directionFinal = new Vector2(directionClamped.x, -directionClamped.y);
				var angle = Mathf.Atan2(directionFinal.y, directionFinal.x) / (Mathf.PI * 2);
				if (angle < 0)
				{
					angle += 1;
				}

				var hue = angle;
				var saturation = directionClamped.magnitude;

                decalInfo.ColorHSVA.x = hue;
                decalInfo.ColorHSVA.y = saturation;
                Plugin.ApplyColor(ItemId, decalIndex, decalInfo);
            }

            {
                var sliderWidth = 120;

                var labelX = margin;
                var sliderX = labelX + nameWidth + iconSeparator - 42;

                var hueY = margin + hsCircleDiameter + margin - 9;
                var saturationY = hueY + buttonHeight + iconSeparator;
                var valueY = saturationY + buttonHeight + iconSeparator;

                GUI.Label(new Rect(labelX, hueY, nameWidth, buttonHeight), "Hue:", CamoEditorResources.LabelStyleName);
                var newHue = GUI.HorizontalSlider(new Rect(sliderX, hueY + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.x, 0f, 1f);
                if (newHue != decalInfo.ColorHSVA.x)
                {
                    decalInfo.ColorHSVA.x = newHue;
                    Plugin.ApplyColor(ItemId, decalIndex, decalInfo);
                }

                GUI.Label(new Rect(labelX, saturationY, nameWidth, buttonHeight), "Saturation:", CamoEditorResources.LabelStyleName);
                var newSaturation = GUI.HorizontalSlider(new Rect(sliderX, saturationY + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.y, 0f, 1f);
                if (newSaturation != decalInfo.ColorHSVA.y)
                {
                    decalInfo.ColorHSVA.y = newSaturation;
                    Plugin.ApplyColor(ItemId, decalIndex, decalInfo);
                }

                GUI.Label(new Rect(labelX, valueY, nameWidth, buttonHeight), "Value:", CamoEditorResources.LabelStyleName);
                var newValue = GUI.HorizontalSlider(new Rect(sliderX, valueY + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.z, 0f, 1f);
                if (newValue != decalInfo.ColorHSVA.z)
                {
                    decalInfo.ColorHSVA.z = newValue;
                    Plugin.ApplyColor(ItemId, decalIndex, decalInfo);
                }
            }
        }

        private void DrawColorPickerWindowCloseButton(int windowID)
        {
            DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.OpenedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                IsColorPickerOpened = false;
            }
        }

        private void DrawColorPickerWindowOpenButton(int windowID)
        {
            DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.ClosedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                IsColorPickerOpened = true;
            }
        }

        private void DrawDecalEditUI(int decalIndex)
        {
            var (decalInfo, decal) = Plugin.GetDecal(ItemId, InstanceID, decalIndex);
            var texture = Plugin.GetTexture(decalInfo.Texture);

            var x = margin;
            var y = margin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                CurrentlyEditedDecalIndex = default;
                DestroyTransformHandle();
            }
            y += buttonHeight + margin;

            {
                var columnY = y;

                if (GUI.Button(new Rect(x, columnY, smallIconSize, smallIconSize), CamoEditorResources.EditPositionIcon))
                {
                    SetupTransformHandle(HandleType.Position, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + smallIconSize + iconSeparator + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localPosition.x:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localPosition.y:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localPosition.z:F3}", CamoEditorResources.LabelStyleName);
                }
                {
                    if (GUI.Button(new Rect(x + boxWidth - fixTransformButtonWidth, columnY, fixTransformButtonWidth, smallIconSize), "flip left/right"))
                    {
                        Plugin.FlipSideLeftRight(ItemId, decalIndex, decalInfo);
                        SyncTransformHandle();
                    }
                }
                columnY += smallIconSize + iconSeparator;


                if (GUI.Button(new Rect(x, columnY, smallIconSize, smallIconSize), CamoEditorResources.EditRotationIcon))
                {
                    SetupTransformHandle(HandleType.Rotation, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + smallIconSize + iconSeparator + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localEulerAngles.x:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localEulerAngles.y:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localEulerAngles.z:F3}", CamoEditorResources.LabelStyleName);
                }
                {
                    if (GUI.Button(new Rect(x + boxWidth - fixTransformButtonWidth, columnY, fixTransformButtonWidth, smallIconSize), "round to degree"))
                    {
                        Plugin.RoundLocalEulerAnglesToDegree(ItemId, decalIndex, decalInfo);
                        SyncTransformHandle();
                    }
                }
                columnY += smallIconSize + iconSeparator;

                if (GUI.Button(new Rect(x, columnY, smallIconSize, smallIconSize), CamoEditorResources.EditScaleIcon))
                {
                    SetupTransformHandle(HandleType.Scale, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + smallIconSize + iconSeparator + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localScale.x:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localScale.y:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localScale.z:F3}", CamoEditorResources.LabelStyleName);
                }
                {
                    if (GUI.Button(new Rect(x + boxWidth - fixTransformButtonWidth, columnY, fixTransformButtonWidth, smallIconSize), "fix aspect ratio"))
                    {
                        Plugin.FixAspectRatio(ItemId, decalIndex, decalInfo);
                    }
                }
                columnY += smallIconSize + iconSeparator;

                if (GUI.Button(new Rect(x, columnY, smallIconSize, smallIconSize), CamoEditorResources.EditTextureTilingIcon))
                {
                    SetupTransformHandle(HandleType.TextureTiling, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + smallIconSize + iconSeparator + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decalInfo.UV.z:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decalInfo.UV.w:F3}", CamoEditorResources.LabelStyleName);
                }
                {
                    if (GUI.Button(new Rect(x + boxWidth - fixTransformButtonWidth, columnY, fixTransformButtonWidth, smallIconSize), "fix aspect ratio"))
                    {
                        Plugin.FixUVAspectRatio(ItemId, decalIndex, decalInfo);
                    }
                }
                columnY += smallIconSize + iconSeparator;

                {
                    var colorButtonRect = new Rect(x, columnY, smallIconSize, smallIconSize);

                    DrawColor(colorButtonRect, decalInfo.ColorHSVA.HSVAtoRGBA().WithAlpha(1f));
                    if (GUI.Button(colorButtonRect, GUIContent.none, GUIStyle.none))
                    {
                        IsColorPickerOpened = !IsColorPickerOpened;
                    }

                    var valueX = x + smallIconSize + iconSeparator + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"H: {decalInfo.ColorHSVA.x:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"S: {decalInfo.ColorHSVA.y:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"V: {decalInfo.ColorHSVA.z:F3}", CamoEditorResources.LabelStyleName);
                }
                columnY += smallIconSize + margin;

                y = columnY;
            }

            {
                var sliderWidth = 212;

                var labelX = x;
                var sliderX = labelX + nameWidth + iconSeparator - 42;
                var valueX = sliderX + sliderWidth + iconSeparator;

                var opacityY = y;
                var maxAngleY = opacityY + buttonHeight + iconSeparator;


                GUI.Label(new Rect(labelX, opacityY, nameWidth, buttonHeight), "Opacity:", CamoEditorResources.LabelStyleName);
                var newAlpha = GUI.HorizontalSlider(new Rect(sliderX, opacityY + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.w, 0f, 1f);
                if (newAlpha != decalInfo.ColorHSVA.w)
                {
                    decalInfo.ColorHSVA.w = newAlpha;
                    Plugin.ApplyColor(ItemId, decalIndex, decalInfo);
                }
                GUI.Label(new Rect(valueX, opacityY, longFieldWidth, buttonHeight), $"{decalInfo.ColorHSVA.w:F3}", CamoEditorResources.LabelStyleValue);


                GUI.Label(new Rect(labelX, maxAngleY, nameWidth, buttonHeight), "MaxAngle:", CamoEditorResources.LabelStyleName);
                var newMaxAngle = GUI.HorizontalSlider(new Rect(sliderX, maxAngleY + 11, sliderWidth, buttonHeight), decalInfo.MaxAngle, 0f, 1f);
                if (newMaxAngle != decalInfo.MaxAngle)
                {
                    decalInfo.MaxAngle = newMaxAngle;
                    Plugin.ApplyMaxAngle(ItemId, decalIndex, decalInfo);
                }
                GUI.Label(new Rect(valueX, maxAngleY, longFieldWidth, buttonHeight), $"{decalInfo.MaxAngle:F3}", CamoEditorResources.LabelStyleValue);


                y = maxAngleY + buttonHeight + margin;
            }

            {
                GUI.Button(new Rect(x, y, iconSize, iconSize), texture);

                var labelX = x + iconSize + iconSeparator + 12;
                GUI.Label(new Rect(labelX, y + 1, 256, smallIconSize), decalInfo.Texture, CamoEditorResources.TextureNameStyle);

                y += iconSize + margin;
            }

            DrawColor(new Rect(x, y, boxWidth, 4), separatorColor);
            y += 4 + margin;

            DrawAllTextures(x, y, decalIndex, decalInfo);
        }

        public void SetupTransformHandle(HandleType handleType, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            if (TransformHandle)
            {
                if (TransformHandle.type == handleType)
                {
                    return;
                }
            }
            else
            {
                TransformHandle = RuntimeTransformHandle.Create
                (
                    decal.DecalTransform,
                    Camera,
                    CamoEditorResources.PositionHandleShader,
                    CamoEditorResources.RotationHandleShader,
                    CamoEditorResources.ScaleHandleShader,
                    1 << LayerMaskClass.WeaponPreview
                );

                TransformHandle.OnEndedDraggingHandle += () => OnEndedDraggingHandle(decalIndex, decalInfo, decal);
            }

            TransformHandle.DestroyHandles();

			if (handleType == HandleType.Position)
			{
                TransformHandle.CreateHandlePosition();
			}
			if (handleType == HandleType.Rotation)
			{
                TransformHandle.CreateHandleRotation();
			}
			if (handleType == HandleType.Scale)
			{
                TransformHandle.CreateHandleScale();
			}
            if (handleType == HandleType.TextureTiling)
            {
                TransformHandle.CreateHandleTextureTiling(decalInfo, decal);
            }

			TransformHelperClass.SetLayersRecursively(TransformHandle.gameObject, LayerMaskClass.WeaponPreview);
        }

        public void OnEndedDraggingHandle(int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            var handleType = TransformHandle.type;

            if (handleType == HandleType.Position)
            {
                decalInfo.LocalPosition = decal.DecalTransform.localPosition;
                Plugin.ApplyLocalPosition(ItemId, decalIndex, decalInfo);
            }
            if (handleType == HandleType.Rotation)
            {
                decalInfo.LocalEulerAngles = decal.DecalTransform.localEulerAngles;
                Plugin.ApplyLocalEulerAngles(ItemId, decalIndex, decalInfo);
            }
            if (handleType == HandleType.Scale)
            {
                decalInfo.LocalScale = decal.DecalTransform.localScale;
                Plugin.ApplyLocalScale(ItemId, decalIndex, decalInfo);
            }
            if (handleType == HandleType.TextureTiling)
            {
                Plugin.ApplyUV(ItemId, decalIndex, decalInfo);
            }
        }

        public void SyncTransformHandle()
        {
            if (TransformHandle)
            {
                TransformHandle.handleTransform.position = TransformHandle.targetTransform.position;
                TransformHandle.handleTransform.rotation = TransformHandle.targetTransform.rotation;
            }
        }

        public void DestroyTransformHandle()
        {
            if (TransformHandle)
            {
                GameObject.Destroy(TransformHandle.gameObject);
            }
        }

        private void DrawAllTextures(int x, int y, int decalIndex, DecalInfo decalInfo)
        {
            var totalRows = DivideIntRoundUp(Plugin.GetTexturesCount(), iconColumns);
            var (totalHeight, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalRows, maxIconRows, iconSize, iconSeparator);
            var totalRect = new Rect(x, y, boxWidth, totalHeight);
            var visibleRect = new Rect(x, y, boxWidth + 16, visibleHeight);

            // render my own vertical scroll bar because unity's one is cannot be set slimmer than 15 px...
            if (totalRows > maxIconRows)
            {
                var handleHeight = visibleHeight * visibleHeight / (float)totalHeight;
                var handlePositionT = TexturesScrollPosition.y / (float)totalHeight;
                var handlePosition = handlePositionT * visibleHeight;
                var scrollBarX = x + boxWidth + 5;
                DrawColor(new Rect(scrollBarX, y, scrollBarWidth, visibleHeight), separatorColor);
                DrawColor(new Rect(scrollBarX, y + handlePosition, scrollBarWidth, handleHeight), scrollBarHandleColor);
            }

            TexturesScrollPosition = GUI.BeginScrollView(visibleRect, TexturesScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

            for (var i = 0; i < Plugin.GetTexturesCount(); i++)
            {
                var textureName = Plugin.GetTextureName(i);
                var texture = Plugin.GetTexture(textureName);

                var ix = i % iconColumns;
                var iy = i / iconColumns;

                var xi = x + ix * (iconSize + iconSeparator);
                var yi = y + iy * (iconSize + iconSeparator);

                if (GUI.Button(new Rect(xi, yi, iconSize, iconSize), texture))
                {
                    if (decalInfo.Texture != textureName)
                    {
                        decalInfo.Texture = textureName;
                        Plugin.ApplyTexture(ItemId, decalIndex, decalInfo);
                        // TODO if images have the same aspect ratio, do not fix
                        Plugin.FixAspectRatio(ItemId, decalIndex, decalInfo);
                    }
                }
            }
            GUI.EndScrollView();
        }

        public void Destroy()
        {
            if (RuntimeGizmos)
            {
                GameObject.Destroy(RuntimeGizmos);
            }

            DestroyTransformHandle();
        }

        private static int DivideIntRoundUp(int left, int right)
        {
            return (left + right - 1) / right;
        }

        private static (int totalHeight, int visibleHeight) CalculateScrollViewTotalAndVisibleHeight(int totalCount, int maxCount, int itemHeight, int separatorHeight)
        {
            var totalHeight = totalCount * (itemHeight + separatorHeight) - separatorHeight;
            if (totalCount > maxCount)
            {
                var visibleHeight = maxCount * (itemHeight + separatorHeight) + itemHeight / 2;
                return (totalHeight, visibleHeight);
            }
            else
            {
                return (totalHeight, totalHeight);
            }
        }

        public void DrawDecalProjectionBox()
        {
            if (CurrentlyEditedDecalIndex.Some(out var currentlyEditedDecalIndex) && RuntimeGizmos)
            {
                var (decalInfo, decal) = Plugin.GetDecal(ItemId, InstanceID, currentlyEditedDecalIndex);
                var decalTransform = decal.DecalTransform;
                var position = decalTransform.position;
                var scale = decalTransform.lossyScale;
                var offset = decalTransform.up * (scale.y * 0.5f);

                RuntimeGizmos.Cubes.Add(new RuntimeGizmos.Cube()
                {
                    Position = position - offset,
                    Rotation = decalTransform.rotation,
                    Scale = scale,
                });
            }
        }
    }
}
