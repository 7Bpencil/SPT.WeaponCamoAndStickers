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
        public Camera Camera;
        public RuntimeGizmos RuntimeGizmos;
        public string ItemId;
        public int InstanceID;
        public Transform DecalsRoot;
        public bool IsOpened;
        public Option<int> CurrentlyEditedDecalIndex;
        public Vector2 TexturesScrollPosition;
        public bool IsColorPickerOpened;
        public RuntimeTransformHandle TransformHandle;
		public Rect WindowRect;

        // brace for imGUI shitshow!


        private const float startX = 25;
        private const float startY = 19;
        private const float windowWidth = margin + (iconSize + iconSeparator) * iconColumns - iconSeparator + margin;
        private const float buttonHeight = 30;
        private const float buttonSeparator = 4;
        private const float margin = 14;
        private const float decalSeparator = 8;
        private const float iconSize = 66;
        private const float iconSeparator = 3;
        private const float smallIconSize = (iconSize - iconSeparator) / 2;
        private const int iconColumns = 5;
        private const int maxIconRows = 5;
        private const float boxWidth = windowWidth - margin * 2;
        private const float boxHeight = iconSize + boxMargin * 2;
        private const float boxMargin = 3;
        private const float nameWidth = 120;
        private const float longFieldWidth = 60;
        private const float fixTransformButtonWidth = 110;
        private const float openCloseButtonWidth = 22;
        private const float openCloseButtonHeight = 66;
        private Rect openCloseButtonIconRect = new(2, 3, 18, 61);
        private Rect colorPickerRect = new(0, 159, 231, 331);
        private float hsCircleDiameter = 201;
        private const float mainIconWidth = 62;
        private Color backgroundColor = new(0.15f, 0.15f, 0.15f, 1f);
        private Color separatorColor = new(0.1f, 0.1f, 0.1f, 1f);

        private void DrawColor(Rect rect, Color color)
        {
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, color, 0, 0);
        }

        public void OnGUI()
        {
            if (CamoEditor.Some(out var camoEditor))
            {
                if (camoEditor.IsOpened)
                {
        			ref var windowRect = ref camoEditor.WindowRect;
    				windowRect.height = CalculateWindowHeight(camoEditor);
                    windowRect = GUI.Window(1, windowRect, DrawOpenedWindow, GUIContent.none);

                    var closeButtonWindowRect = new Rect(windowRect.xMax, windowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                    GUI.Window(2, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);

                    if (camoEditor.CurrentlyEditedDecalIndex.HasValue)
                    {
                        if (camoEditor.IsColorPickerOpened)
                        {
                            var colorPickerWindowRect = new Rect(windowRect.xMax, windowRect.y + colorPickerRect.y, colorPickerRect.width, colorPickerRect.height);
                            GUI.Window(3, colorPickerWindowRect, DrawColorPickerWindow, GUIContent.none);

                            var closeColorPickerWindowRect = new Rect(colorPickerWindowRect.xMax, colorPickerWindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                            GUI.Window(4, closeColorPickerWindowRect, DrawColorPickerWindowCloseButton, GUIContent.none);
                        }
                        else
                        {
                            var openColorPickerWindowRect = new Rect(windowRect.xMax, windowRect.y + colorPickerRect.y, openCloseButtonWidth, openCloseButtonHeight);
                            GUI.Window(3, openColorPickerWindowRect, DrawColorPickerWindowOpenButton, GUIContent.none);
                        }
                    }
                }
                else
                {
        			ref var windowRect = ref camoEditor.WindowRect;
                    windowRect = GUI.Window(1, windowRect, DrawClosedWindow, GUIContent.none);

                    var openColorPickerWindowRect = new Rect(windowRect.xMax, windowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                    GUI.Window(2, openColorPickerWindowRect, DrawClosedWindowOpenButton, GUIContent.none);
                }
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
            var camoEditor = CamoEditor.Value;

            DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.ClosedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
    			ref var windowRect = ref camoEditor.WindowRect;
                camoEditor.IsOpened = true;
				windowRect.width = windowWidth;
            }
        }

        private float CalculateWindowHeight(CamoEditor camoEditor)
        {
            if (camoEditor.CurrentlyEditedDecalIndex.Some(out var currentlyEditedDecalIndex))
            {
                var totalRows = DivideIntRoundUp(LoadedDecalTexturesList.Count, iconColumns);
                var visibleRows = Math.Min(totalRows, maxIconRows);
                var visibleHeight = visibleRows * (iconSize + iconSeparator) + iconSize / 2;
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
                var height = margin;
                if (ItemsWithDecals.TryGetValue(camoEditor.ItemId, out var itemsWithDecals))
                {
                    height += itemsWithDecals.DecalsInfo.Count * (boxHeight + decalSeparator);
                }

                height += buttonHeight;
                height += margin;

                return height;
            }
        }

        private void DrawOpenedWindow(int windowID)
		{
            var camoEditor = CamoEditor.Value;

            DrawColor(new Rect(0, 0, camoEditor.WindowRect.width, camoEditor.WindowRect.height), backgroundColor);
            if (camoEditor.CurrentlyEditedDecalIndex.Some(out var currentlyEditedDecalIndex))
            {
                DrawDecalEditUI(camoEditor, currentlyEditedDecalIndex);
            }
            else
            {
                DrawDecalsListUI(camoEditor);
            }

			GUI.DragWindow();
        }

        private void DrawOpenedWindowCloseButton(int windowID)
        {
            var camoEditor = CamoEditor.Value;

            DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.OpenedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
    			ref var windowRect = ref camoEditor.WindowRect;
                camoEditor.IsOpened = false;
				windowRect.width = mainIconWidth;
				windowRect.height = openCloseButtonHeight;
            }
        }

        public void DrawDecalsListUI(CamoEditor camoEditor)
        {
            var x = margin;
            var y = margin;

            if (ItemsWithDecals.TryGetValue(camoEditor.ItemId, out var itemsWithDecals))
            {
                // TODO add scroll view
                var decalsInfo = itemsWithDecals.DecalsInfo;
                for (var i = 0; i < decalsInfo.Count; i++)
                {
                    var decalInfo = decalsInfo[i];
                    DrawDecalElementUI(camoEditor, x, y, i, decalInfo);
                    y += boxHeight + decalSeparator;
                }
            }

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Add New Decal"))
            {
                var newDecalIndex = AddNewDecal(camoEditor.ItemId, camoEditor.InstanceID, camoEditor.DecalsRoot, camoEditor.Camera);
                camoEditor.CurrentlyEditedDecalIndex = new(newDecalIndex);
            }
        }

        private void DrawDecalElementUI(CamoEditor camoEditor, float x, float y, int decalIndex, DecalInfo decalInfo)
        {
            var texture = LoadedDecalTextures[decalInfo.Texture];

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), default(string));

            var topLineY = y + boxMargin;
            var bottomLineY = topLineY + smallIconSize + iconSeparator;

            var textureIconX = x + boxMargin;
            if (GUI.Button(new Rect(textureIconX, topLineY, iconSize, iconSize), texture))
            {
                camoEditor.CurrentlyEditedDecalIndex = new(decalIndex);
            }

            var labelX = textureIconX + iconSize + iconSeparator + 2;
            GUI.Label(new Rect(labelX, topLineY + 1, 230, iconSize), decalInfo.Texture, CamoEditorResources.TextureNameStyle);

            var deleteX = x + boxWidth - (iconSeparator + smallIconSize) * 3;
            if (GUI.Button(new Rect(deleteX, bottomLineY, smallIconSize, smallIconSize), CamoEditorResources.DeleteIcon))
            {
                Delete(camoEditor.ItemId, decalIndex);
            }

            var duplicateX = deleteX + smallIconSize + iconSeparator;
            if (GUI.Button(new Rect(duplicateX, bottomLineY, smallIconSize, smallIconSize), CamoEditorResources.DuplicateIcon))
            {
                var newDecalIndex = Duplicate(camoEditor.ItemId, decalIndex);
                camoEditor.CurrentlyEditedDecalIndex = new(newDecalIndex);
            }

            var arrowX = duplicateX + smallIconSize + iconSeparator;
            if (GUI.Button(new Rect(arrowX, topLineY, smallIconSize, smallIconSize), CamoEditorResources.MoveUpIcon))
            {
                Swap(camoEditor.ItemId, decalIndex, decalIndex - 1);
            }
            if (GUI.Button(new Rect(arrowX, bottomLineY, smallIconSize, smallIconSize), CamoEditorResources.MoveDownIcon))
            {
                Swap(camoEditor.ItemId, decalIndex, decalIndex + 1);
            }
        }

        private void DrawColorPickerWindow(int windowID)
        {
            var camoEditor = CamoEditor.Value;
            var decalIndex = camoEditor.CurrentlyEditedDecalIndex.Value;
            var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];

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
                ApplyColor(camoEditor.ItemId, decalIndex, decalInfo);
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
                    ApplyColor(camoEditor.ItemId, decalIndex, decalInfo);
                }

                GUI.Label(new Rect(labelX, saturationY, nameWidth, buttonHeight), "Saturation:", CamoEditorResources.LabelStyleName);
                var newSaturation = GUI.HorizontalSlider(new Rect(sliderX, saturationY + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.y, 0f, 1f);
                if (newSaturation != decalInfo.ColorHSVA.y)
                {
                    decalInfo.ColorHSVA.y = newSaturation;
                    ApplyColor(camoEditor.ItemId, decalIndex, decalInfo);
                }

                GUI.Label(new Rect(labelX, valueY, nameWidth, buttonHeight), "Value:", CamoEditorResources.LabelStyleName);
                var newValue = GUI.HorizontalSlider(new Rect(sliderX, valueY + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.z, 0f, 1f);
                if (newValue != decalInfo.ColorHSVA.z)
                {
                    decalInfo.ColorHSVA.z = newValue;
                    ApplyColor(camoEditor.ItemId, decalIndex, decalInfo);
                }
            }
        }

        private void DrawColorPickerWindowCloseButton(int windowID)
        {
            var camoEditor = CamoEditor.Value;

            DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.OpenedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                camoEditor.IsColorPickerOpened = false;
            }
        }

        private void DrawColorPickerWindowOpenButton(int windowID)
        {
            var camoEditor = CamoEditor.Value;

            DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.ClosedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                camoEditor.IsColorPickerOpened = true;
            }
        }

        private void DrawDecalEditUI(CamoEditor camoEditor, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];
            var decal = itemsWithDecals.Items[camoEditor.InstanceID].Decals[decalIndex];
            var texture = LoadedDecalTextures[decalInfo.Texture];

            var x = margin;
            var y = margin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                camoEditor.CurrentlyEditedDecalIndex = default;
                DestroyTransformHandle(camoEditor);
            }
            y += buttonHeight + margin;

            {
                var columnY = y;

                if (GUI.Button(new Rect(x, columnY, smallIconSize, smallIconSize), CamoEditorResources.EditPositionIcon))
                {
                    SetupTransformHandle(camoEditor, HandleType.Position, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + smallIconSize + iconSeparator + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localPosition.x:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localPosition.y:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localPosition.z:F3}", CamoEditorResources.LabelStyleName);
                }
                columnY += smallIconSize + iconSeparator;


                if (GUI.Button(new Rect(x, columnY, smallIconSize, smallIconSize), CamoEditorResources.EditRotationIcon))
                {
                    SetupTransformHandle(camoEditor, HandleType.Rotation, decalIndex, decalInfo, decal);
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
                        RoundLocalEulerAnglesToDegree(camoEditor.ItemId, decalIndex, decalInfo);
                    }
                }
                columnY += smallIconSize + iconSeparator;

                if (GUI.Button(new Rect(x, columnY, smallIconSize, smallIconSize), CamoEditorResources.EditScaleIcon))
                {
                    SetupTransformHandle(camoEditor, HandleType.Scale, decalIndex, decalInfo, decal);
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
                        FixAspectRatio(camoEditor.ItemId, decalIndex, decalInfo);
                    }
                }
                columnY += smallIconSize + iconSeparator;

                if (GUI.Button(new Rect(x, columnY, smallIconSize, smallIconSize), CamoEditorResources.EditTextureTilingIcon))
                {
                    SetupTransformHandle(camoEditor, HandleType.TextureTiling, decalIndex, decalInfo, decal);
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
                        FixUVAspectRatio(camoEditor.ItemId, decalIndex, decalInfo);
                    }
                }
                columnY += smallIconSize + iconSeparator;

                {
                    var colorButtonRect = new Rect(x, columnY, smallIconSize, smallIconSize);

                    DrawColor(colorButtonRect, decalInfo.ColorHSVA.HSVAtoRGBA());
                    if (GUI.Button(colorButtonRect, GUIContent.none, GUIStyle.none))
                    {
                        camoEditor.IsColorPickerOpened = !camoEditor.IsColorPickerOpened;
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
                    ApplyColor(camoEditor.ItemId, decalIndex, decalInfo);
                }
                GUI.Label(new Rect(valueX, opacityY, longFieldWidth, buttonHeight), $"{decalInfo.ColorHSVA.w:F3}", CamoEditorResources.LabelStyleValue);


                GUI.Label(new Rect(labelX, maxAngleY, nameWidth, buttonHeight), "MaxAngle:", CamoEditorResources.LabelStyleName);
                var newMaxAngle = GUI.HorizontalSlider(new Rect(sliderX, maxAngleY + 11, sliderWidth, buttonHeight), decalInfo.MaxAngle, 0f, 1f);
                if (newMaxAngle != decalInfo.MaxAngle)
                {
                    decalInfo.MaxAngle = newMaxAngle;
                    ApplyMaxAngle(camoEditor.ItemId, decalIndex, decalInfo);
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

            DrawAllTextures(camoEditor, x, y, decalIndex, decalInfo, itemsWithDecals);
        }

        public void SetupTransformHandle(CamoEditor camoEditor, HandleType handleType, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            if (camoEditor.TransformHandle)
            {
                if (camoEditor.TransformHandle.type == handleType)
                {
                    return;
                }
            }
            else
            {
                camoEditor.TransformHandle = RuntimeTransformHandle.Create
                (
                    decal.DecalTransform,
                    camoEditor.Camera,
                    CamoEditorResources.PositionHandleShader,
                    CamoEditorResources.RotationHandleShader,
                    CamoEditorResources.ScaleHandleShader,
                    LayerMaskClass.WeaponPreview
                );

                camoEditor.TransformHandle.OnEndedDraggingHandle += () => OnEndedDraggingHandle(camoEditor, decalIndex, decalInfo, decal);
            }

            camoEditor.TransformHandle.DestroyHandles();

			if (handleType == HandleType.Position)
			{
                camoEditor.TransformHandle.CreateHandlePosition();
			}
			if (handleType == HandleType.Rotation)
			{
                camoEditor.TransformHandle.CreateHandleRotation();
			}
			if (handleType == HandleType.Scale)
			{
                camoEditor.TransformHandle.CreateHandleScale();
			}
            if (handleType == HandleType.TextureTiling)
            {
                camoEditor.TransformHandle.CreateHandleTextureTiling(decalInfo, decal);
            }

			TransformHelperClass.SetLayersRecursively(camoEditor.TransformHandle.gameObject, LayerMaskClass.WeaponPreview);
        }

        public void OnEndedDraggingHandle(CamoEditor camoEditor, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
            var handleType = camoEditor.TransformHandle.type;

            if (handleType == HandleType.Position)
            {
                decalInfo.LocalPosition = decal.DecalTransform.localPosition;
                ApplyLocalPosition(camoEditor.ItemId, decalIndex, decalInfo);
            }
            if (handleType == HandleType.Rotation)
            {
                decalInfo.LocalEulerAngles = decal.DecalTransform.localEulerAngles;
                ApplyLocalEulerAngles(camoEditor.ItemId, decalIndex, decalInfo);
            }
            if (handleType == HandleType.Scale)
            {
                decalInfo.LocalScale = decal.DecalTransform.localScale;
                ApplyLocalScale(camoEditor.ItemId, decalIndex, decalInfo);
            }
            if (handleType == HandleType.TextureTiling)
            {
                ApplyUV(camoEditor.ItemId, decalIndex, decalInfo);
            }
        }

        public void DestroyTransformHandle(CamoEditor camoEditor)
        {
            if (camoEditor.TransformHandle)
            {
                Destroy(camoEditor.TransformHandle.gameObject);
            }
        }

        private void DrawAllTextures(CamoEditor camoEditor, float x, float y, int decalIndex, DecalInfo decalInfo, ItemsWithDecals itemsWithDecals)
        {
            var totalRows = DivideIntRoundUp(LoadedDecalTexturesList.Count, iconColumns);
            var visibleRows = Math.Min(totalRows, maxIconRows);
            var totalHeight = totalRows * (iconSize + iconSeparator) - iconSeparator;
            var visibleHeight = visibleRows * (iconSize + iconSeparator) + iconSize / 2;
            var totalRect = new Rect(x, y, boxWidth, totalHeight);
            var visibleRect = new Rect(x, y, boxWidth + 16, visibleHeight);

            // render my own vertical scroll bar because unity's one is cannot be set slimmer than 15 px...
            if (totalRows > visibleRows)
            {
                var handleHeight = visibleHeight * visibleHeight / (float)totalHeight;
                var handlePositionT = camoEditor.TexturesScrollPosition.y / (float)totalHeight;
                var handlePosition = handlePositionT * visibleHeight;
                var scrollBarWidth = 4;
                var scrollBarX = x + boxWidth + 5;
                var handleColor = new Color32(183, 195, 202, 255);
                DrawColor(new Rect(scrollBarX, y, scrollBarWidth, visibleHeight), separatorColor);
                DrawColor(new Rect(scrollBarX, y + handlePosition, scrollBarWidth, handleHeight), handleColor);
            }

            camoEditor.TexturesScrollPosition = GUI.BeginScrollView(visibleRect, camoEditor.TexturesScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

            for (var i = 0; i < LoadedDecalTexturesList.Count; i++)
            {
                var textureName = LoadedDecalTexturesList[i];
                var texture = LoadedDecalTextures[textureName];

                var ix = i % iconColumns;
                var iy = i / iconColumns;

                var xi = x + ix * (iconSize + iconSeparator);
                var yi = y + iy * (iconSize + iconSeparator);

                if (GUI.Button(new Rect(xi, yi, iconSize, iconSize), texture))
                {
                    if (decalInfo.Texture != textureName)
                    {
                        decalInfo.Texture = textureName;
                        ApplyTexture(camoEditor.ItemId, decalIndex, decalInfo);
                        // TODO if images have the same aspect ratio, do not fix
                        FixAspectRatio(camoEditor.ItemId, decalIndex, decalInfo);
                    }
                }
            }
            GUI.EndScrollView();
        }

        private static int DivideIntRoundUp(int left, int right)
        {
            return (left + right - 1) / right;
        }
    }
}
