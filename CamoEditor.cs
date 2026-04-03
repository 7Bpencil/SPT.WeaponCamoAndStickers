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
        public Texture2D SaveIcon;
        public Texture2D ColorWheelHSV;

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
            SaveIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/diskette.png");
            ColorWheelHSV = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/hsv-circle.png");
        }
    }

    public class CamoStyle
    {
        public GUIStyle LabelStyleName;
        public GUIStyle TextureNameStyle;
        public GUIStyle LabelStyleValue;
        public GUIStyle TextFieldStyle;
		public GUIStyle ColorPickerButtonStyle;

        public CamoStyle(GUISkin currentSkin)
        {
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

            TextFieldStyle = new(currentSkin.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                contentOffset = new Vector2(CamoEditor.mediumMargin, 0)
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
        public CamoStyle CamoStyle;
        public Camera Camera;
        public RuntimeGizmos RuntimeGizmos;
        public string ItemId;
        public int InstanceID;
        public Transform DecalsRoot;
        public bool IsOpened;
        public bool IsPresetsOpened;
        public string CurrentPresetName;
        public Vector2 PresetsScrollPosition;
        public Vector2 DecalsScrollPosition;
        public Option<int> CurrentlyEditedDecalIndex;
        public Vector2 TexturesScrollPosition;
        public bool IsColorPickerOpened;
        public RuntimeTransformHandle TransformHandle;
		public Rect WindowRect;

        // brace for imGUI shitshow

        public const int iconColumns = 5;
        public const int maxIconRows = 6;
        public const int maxDecalsVisibleWhenPresetsAreNotOpened = 9;
        public const int maxDecalsVisibleWhenPresetsAreOpened = 5;
        public const int maxPresetsVisible = 9;

        public const int smallMargin = 4;
        public const int mediumMargin = 8;
        public const int bigMargin = 14;

        public const int startX = 25;
        public const int startY = 19;
        public const int windowWidth = bigMargin + (iconSize + smallMargin) * iconColumns - smallMargin + bigMargin;
        public const int buttonHeight = 32;
        public const int iconSize = buttonHeight * 2 + smallMargin;
        public const int boxWidth = windowWidth - bigMargin * 2;
        public const int boxHeight = iconSize + smallMargin * 2;
        public const int nameWidth = 120;
        public const int longFieldWidth = 60;
        public const int fixTransformButtonWidth = 110;
        public const int openCloseButtonWidth = 22;
        public const int openCloseButtonHeight = 66;
        public static readonly Rect openCloseButtonIconRect = new(2, 3, 18, 61);
        public static readonly Rect colorPickerRect = new(0, 200, 231, 331);
        public const int hsCircleDiameter = 201;
        public const int mainIconWidth = 62;
        public static readonly Color backgroundColor = new(0.15f, 0.15f, 0.15f, 1f);
        public static readonly Color separatorColor = new(0.1f, 0.1f, 0.1f, 1f);
        public static readonly Color scrollBarHandleColor = new(183, 195, 202, 255);
        public const int scrollBarWidth = 4;

        public static Rect GetDefaultWindowRect()
        {
            return new(startX, startY, mainIconWidth, openCloseButtonHeight);
        }

        public static void DrawColor(Rect rect, Color color)
        {
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, color, 0, 0);
        }

        public void DrawWindow()
        {
            // we copy some styles from GUI.skin which can be accessed only from OnGUI call
            if (CamoStyle == null)
            {
                CamoStyle = new(GUI.skin);
            }

            if (IsOpened)
            {
                if (CurrentlyEditedDecalIndex.HasValue)
                {
                    WindowRect.height = CalculateDecalEditWindowHeight();
                    WindowRect = GUI.Window(1, WindowRect, DrawDecalEditUI, GUIContent.none);

                    var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                    GUI.Window(2, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);

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
                else
                {
                    var presetsWindowHeight = CalculatePresetsWindowHeight();
                    var decalsWindowHeight = CalculateDecalsWindowHeight();

                    WindowRect.height = presetsWindowHeight + smallMargin + decalsWindowHeight;
                    WindowRect = GUI.Window(1, WindowRect, DrawOpenedWindow, GUIContent.none);

                    var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                    GUI.Window(2, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);
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

        private int CalculateDecalEditWindowHeight()
        {
            var totalRows = DivideIntRoundUp(Plugin.GetTexturesCount(), iconColumns);
            var (_, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalRows, maxIconRows, iconSize, smallMargin);
            return
                bigMargin + buttonHeight + bigMargin +
                4 * (buttonHeight + smallMargin) +
                buttonHeight + bigMargin +
                buttonHeight + smallMargin +
                buttonHeight + bigMargin +
                iconSize + bigMargin +
                4 + bigMargin +
                visibleHeight +
                bigMargin;
        }

        private int CalculatePresetsWindowHeight()
        {
            if (IsPresetsOpened)
            {
                var totalPresets = Plugin.GetPresetsCount();
                if (totalPresets > 0)
                {
                    var (_, presetsScrollHeight) = CalculateScrollViewTotalAndVisibleHeight(totalPresets, maxPresetsVisible, buttonHeight, smallMargin);
                    return
                        bigMargin + buttonHeight + mediumMargin + buttonHeight +
                        mediumMargin + presetsScrollHeight + bigMargin;
                }
                else
                {
                    return
                        bigMargin + buttonHeight + mediumMargin + buttonHeight +
                        mediumMargin + buttonHeight + bigMargin;
                }
            }
            else
            {
                return
                    bigMargin + buttonHeight + mediumMargin + buttonHeight +
                    bigMargin;
            }
        }

        private int CalculateDecalsWindowHeight()
        {
            var totalDecalsCount = Plugin.GetDecalsCount(ItemId);
            var maxDecalsVisible = IsPresetsOpened ? maxDecalsVisibleWhenPresetsAreOpened : maxDecalsVisibleWhenPresetsAreNotOpened;
            var (_, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalDecalsCount, maxDecalsVisible, boxHeight, mediumMargin);
            return
                bigMargin +
                visibleHeight +
                mediumMargin +
                buttonHeight +
                bigMargin;
        }

        private void DrawOpenedWindow(int windowID)
		{
            DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var y = 0;

            DrawPresetsListUI(y);
            y += CalculatePresetsWindowHeight();

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin;

            DrawDecalsListUI(y);

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

        public void DrawPresetsListUI(int windowY)
        {
            var x = bigMargin;
            var y = windowY;

            y += bigMargin;

            var presetButtonWidth = boxWidth - buttonHeight - smallMargin;
            CurrentPresetName = GUI.TextField(new Rect(x, y, presetButtonWidth, buttonHeight), CurrentPresetName, 25, CamoStyle.TextFieldStyle);
            if (string.IsNullOrWhiteSpace(CurrentPresetName))
            {
                GUI.Label(new Rect(x + CamoStyle.TextFieldStyle.contentOffset.x + 3, y, presetButtonWidth, buttonHeight), "enter preset name", CamoStyle.LabelStyleName);
            }

            var saveX = x + boxWidth - buttonHeight;
            if (GUI.Button(new Rect(saveX, y, buttonHeight, buttonHeight), CamoEditorResources.SaveIcon))
            {
                Plugin.SaveDecalsIntoPreset(ItemId, CurrentPresetName);
            }
            y += buttonHeight + mediumMargin;

            if (IsPresetsOpened)
            {
                if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Hide Presets"))
                {
                    IsPresetsOpened = false;
                }
                y += buttonHeight + mediumMargin;

                var presetsCount = Plugin.GetPresetsCount();
                if (presetsCount > 0)
                {
                    var decalsY = y;

                    var (totalHeight, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(presetsCount, maxPresetsVisible, buttonHeight, smallMargin);
                    var totalRect = new Rect(x, decalsY, boxWidth, totalHeight);
                    var visibleRect = new Rect(x, decalsY, boxWidth + 16, visibleHeight);

                    DrawScrollBar(x + boxWidth + 5, decalsY, totalHeight, visibleHeight, PresetsScrollPosition);
                    PresetsScrollPosition = GUI.BeginScrollView(visibleRect, PresetsScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

                    foreach (var name in Plugin.GetPresetNames())
                    {
                        if (GUI.Button(new Rect(x, decalsY, presetButtonWidth, buttonHeight), name))
                        {
                            Plugin.SwitchToPreset(ItemId, InstanceID, DecalsRoot, Camera, name);
                        }
                        if (GUI.Button(new Rect(x + presetButtonWidth + smallMargin, decalsY, buttonHeight, buttonHeight), CamoEditorResources.DeleteIcon))
                        {
                            Plugin.DeletePreset(name);
                        }
                        decalsY += buttonHeight + smallMargin;
                    }

                    GUI.EndScrollView();
                }
                else
                {
                    GUI.Label(new Rect(x, y, boxWidth, buttonHeight), "No Presets Available", CamoStyle.LabelStyleValue);
                }
            }
            else
            {
                if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Show Presets"))
                {
                    IsPresetsOpened = true;
                }
            }
        }

        public void DrawDecalsListUI(int windowY)
        {
            var x = bigMargin;
            var y = windowY;

            y += bigMargin;

            if (Plugin.GetDecalsInfo(ItemId).Some(out var decalsInfo))
            {
                var decalsY = y;

                var maxDecalsVisible = IsPresetsOpened ? maxDecalsVisibleWhenPresetsAreOpened : maxDecalsVisibleWhenPresetsAreNotOpened;
                var (totalHeight, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(decalsInfo.Count, maxDecalsVisible, boxHeight, mediumMargin);
                var totalRect = new Rect(x, decalsY, boxWidth, totalHeight);
                var visibleRect = new Rect(x, decalsY, boxWidth + 16, visibleHeight);

                DrawScrollBar(x + boxWidth + 5, decalsY, totalHeight, visibleHeight, DecalsScrollPosition);
                DecalsScrollPosition = GUI.BeginScrollView(visibleRect, DecalsScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

                for (var i = 0; i < decalsInfo.Count; i++)
                {
                    var decalInfo = decalsInfo[i];
                    DrawDecalElementUI(x, decalsY, i, decalInfo);
                    decalsY += boxHeight + mediumMargin;
                }

                GUI.EndScrollView();

                y += visibleHeight;
                y += mediumMargin;
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

            var topLineY = y + smallMargin;
            var bottomLineY = topLineY + buttonHeight + smallMargin;

            var textureIconX = x + smallMargin;
            if (GUI.Button(new Rect(textureIconX, topLineY, iconSize, iconSize), texture))
            {
                CurrentlyEditedDecalIndex = new(decalIndex);
            }

            var labelX = textureIconX + iconSize + smallMargin + 2;
            GUI.Label(new Rect(labelX, topLineY + 1, 230, iconSize), decalInfo.Texture, CamoStyle.TextureNameStyle);

            var deleteX = x + boxWidth - (smallMargin + buttonHeight) * 3;
            if (GUI.Button(new Rect(deleteX, bottomLineY, buttonHeight, buttonHeight), CamoEditorResources.DeleteIcon))
            {
                Plugin.Delete(ItemId, decalIndex);
            }

            var duplicateX = deleteX + buttonHeight + smallMargin;
            if (GUI.Button(new Rect(duplicateX, bottomLineY, buttonHeight, buttonHeight), CamoEditorResources.DuplicateIcon))
            {
                var newDecalIndex = Plugin.Duplicate(ItemId, decalIndex);
                CurrentlyEditedDecalIndex = new(newDecalIndex);
            }

            var arrowX = duplicateX + buttonHeight + smallMargin;
            if (GUI.Button(new Rect(arrowX, topLineY, buttonHeight, buttonHeight), CamoEditorResources.MoveUpIcon))
            {
                Plugin.Swap(ItemId, decalIndex, decalIndex - 1);
            }
            if (GUI.Button(new Rect(arrowX, bottomLineY, buttonHeight, buttonHeight), CamoEditorResources.MoveDownIcon))
            {
                Plugin.Swap(ItemId, decalIndex, decalIndex + 1);
            }
        }

        private void DrawColorPickerWindow(int windowID)
        {
            var decalIndex = CurrentlyEditedDecalIndex.Value;
            var (decalInfo, decal) = Plugin.GetDecal(ItemId, InstanceID, decalIndex);

            DrawColor(new Rect(0, 0, colorPickerRect.width, colorPickerRect.height), backgroundColor);

            var hsCircleRect = new Rect(bigMargin, bigMargin, hsCircleDiameter, hsCircleDiameter);

			if (GUI.RepeatButton(hsCircleRect, CamoEditorResources.ColorWheelHSV, CamoStyle.ColorPickerButtonStyle))
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

                var labelX = bigMargin;
                var sliderX = labelX + nameWidth + smallMargin - 42;

                var hueY = bigMargin + hsCircleDiameter + bigMargin - 9;
                var saturationY = hueY + buttonHeight + smallMargin;
                var valueY = saturationY + buttonHeight + smallMargin;

                GUI.Label(new Rect(labelX, hueY, nameWidth, buttonHeight), "Hue:", CamoStyle.LabelStyleName);
                var newHue = GUI.HorizontalSlider(new Rect(sliderX, hueY + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.x, 0f, 1f);
                if (newHue != decalInfo.ColorHSVA.x)
                {
                    decalInfo.ColorHSVA.x = newHue;
                    Plugin.ApplyColor(ItemId, decalIndex, decalInfo);
                }

                GUI.Label(new Rect(labelX, saturationY, nameWidth, buttonHeight), "Saturation:", CamoStyle.LabelStyleName);
                var newSaturation = GUI.HorizontalSlider(new Rect(sliderX, saturationY + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.y, 0f, 1f);
                if (newSaturation != decalInfo.ColorHSVA.y)
                {
                    decalInfo.ColorHSVA.y = newSaturation;
                    Plugin.ApplyColor(ItemId, decalIndex, decalInfo);
                }

                GUI.Label(new Rect(labelX, valueY, nameWidth, buttonHeight), "Value:", CamoStyle.LabelStyleName);
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

        private void DrawDecalEditUI(int windowID)
        {
            DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var decalIndex = CurrentlyEditedDecalIndex.Value;
            var (decalInfo, decal) = Plugin.GetDecal(ItemId, InstanceID, decalIndex);
            var texture = Plugin.GetTexture(decalInfo.Texture);

            var x = bigMargin;
            var y = bigMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                CurrentlyEditedDecalIndex = default;
                DestroyTransformHandle();
            }
            y += buttonHeight + bigMargin;

            {
                var columnY = y;

                if (GUI.Button(new Rect(x, columnY, buttonHeight, buttonHeight), CamoEditorResources.EditPositionIcon))
                {
                    SetupTransformHandle(HandleType.Position, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localPosition.x:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localPosition.y:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localPosition.z:F3}", CamoStyle.LabelStyleName);
                }
                {
                    if (GUI.Button(new Rect(x + boxWidth - fixTransformButtonWidth, columnY, fixTransformButtonWidth, buttonHeight), "flip left/right"))
                    {
                        Plugin.FlipSideLeftRight(ItemId, decalIndex, decalInfo);
                        SyncTransformHandle();
                    }
                }
                columnY += buttonHeight + smallMargin;


                if (GUI.Button(new Rect(x, columnY, buttonHeight, buttonHeight), CamoEditorResources.EditRotationIcon))
                {
                    SetupTransformHandle(HandleType.Rotation, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localEulerAngles.x:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localEulerAngles.y:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localEulerAngles.z:F3}", CamoStyle.LabelStyleName);
                }
                {
                    if (GUI.Button(new Rect(x + boxWidth - fixTransformButtonWidth, columnY, fixTransformButtonWidth, buttonHeight), "round to degree"))
                    {
                        Plugin.RoundLocalEulerAnglesToDegree(ItemId, decalIndex, decalInfo);
                        SyncTransformHandle();
                    }
                }
                columnY += buttonHeight + smallMargin;

                if (GUI.Button(new Rect(x, columnY, buttonHeight, buttonHeight), CamoEditorResources.EditScaleIcon))
                {
                    SetupTransformHandle(HandleType.Scale, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localScale.x:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localScale.y:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localScale.z:F3}", CamoStyle.LabelStyleName);
                }
                {
                    if (GUI.Button(new Rect(x + boxWidth - fixTransformButtonWidth, columnY, fixTransformButtonWidth, buttonHeight), "fix aspect ratio"))
                    {
                        Plugin.FixAspectRatio(ItemId, decalIndex, decalInfo);
                    }
                }
                columnY += buttonHeight + smallMargin;

                if (GUI.Button(new Rect(x, columnY, buttonHeight, buttonHeight), CamoEditorResources.EditTextureTilingIcon))
                {
                    SetupTransformHandle(HandleType.TextureTiling, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decalInfo.UV.z:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decalInfo.UV.w:F3}", CamoStyle.LabelStyleName);
                }
                {
                    if (GUI.Button(new Rect(x + boxWidth - fixTransformButtonWidth, columnY, fixTransformButtonWidth, buttonHeight), "fix aspect ratio"))
                    {
                        Plugin.FixUVAspectRatio(ItemId, decalIndex, decalInfo);
                    }
                }
                columnY += buttonHeight + smallMargin;

                {
                    var colorButtonRect = new Rect(x, columnY, buttonHeight, buttonHeight);

                    DrawColor(colorButtonRect, decalInfo.ColorHSVA.HSVAtoRGBA().WithAlpha(1f));
                    if (GUI.Button(colorButtonRect, GUIContent.none, GUIStyle.none))
                    {
                        IsColorPickerOpened = !IsColorPickerOpened;
                    }

                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"H: {decalInfo.ColorHSVA.x:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"S: {decalInfo.ColorHSVA.y:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"V: {decalInfo.ColorHSVA.z:F3}", CamoStyle.LabelStyleName);
                }
                columnY += buttonHeight + bigMargin;

                y = columnY;
            }

            {
                var sliderWidth = 212;

                var labelX = x;
                var sliderX = labelX + nameWidth + smallMargin - 42;
                var valueX = sliderX + sliderWidth + smallMargin;

                var opacityY = y;
                var maxAngleY = opacityY + buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, opacityY, nameWidth, buttonHeight), "Opacity:", CamoStyle.LabelStyleName);
                var newAlpha = GUI.HorizontalSlider(new Rect(sliderX, opacityY + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.w, 0f, 1f);
                if (newAlpha != decalInfo.ColorHSVA.w)
                {
                    decalInfo.ColorHSVA.w = newAlpha;
                    Plugin.ApplyColor(ItemId, decalIndex, decalInfo);
                }
                GUI.Label(new Rect(valueX, opacityY, longFieldWidth, buttonHeight), $"{decalInfo.ColorHSVA.w:F3}", CamoStyle.LabelStyleValue);


                GUI.Label(new Rect(labelX, maxAngleY, nameWidth, buttonHeight), "MaxAngle:", CamoStyle.LabelStyleName);
                var newMaxAngle = GUI.HorizontalSlider(new Rect(sliderX, maxAngleY + 11, sliderWidth, buttonHeight), decalInfo.MaxAngle, 0f, 1f);
                if (newMaxAngle != decalInfo.MaxAngle)
                {
                    decalInfo.MaxAngle = newMaxAngle;
                    Plugin.ApplyMaxAngle(ItemId, decalIndex, decalInfo);
                }
                GUI.Label(new Rect(valueX, maxAngleY, longFieldWidth, buttonHeight), $"{decalInfo.MaxAngle:F3}", CamoStyle.LabelStyleValue);


                y = maxAngleY + buttonHeight + bigMargin;
            }

            {
                GUI.Button(new Rect(x, y, iconSize, iconSize), texture);

                var labelX = x + iconSize + smallMargin + 12;
                GUI.Label(new Rect(labelX, y + 1, 256, buttonHeight), decalInfo.Texture, CamoStyle.TextureNameStyle);

                y += iconSize + bigMargin;
            }

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            DrawAllTextures(x, y, decalIndex, decalInfo);

			GUI.DragWindow();
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
            var (totalHeight, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalRows, maxIconRows, iconSize, smallMargin);
            var totalRect = new Rect(x, y, boxWidth, totalHeight);
            var visibleRect = new Rect(x, y, boxWidth + 16, visibleHeight);

            DrawScrollBar(x + boxWidth + 5, y, totalHeight, visibleHeight, TexturesScrollPosition);
            TexturesScrollPosition = GUI.BeginScrollView(visibleRect, TexturesScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

            for (var i = 0; i < Plugin.GetTexturesCount(); i++)
            {
                var textureName = Plugin.GetTextureName(i);
                var texture = Plugin.GetTexture(textureName);

                var ix = i % iconColumns;
                var iy = i / iconColumns;

                var xi = x + ix * (iconSize + smallMargin);
                var yi = y + iy * (iconSize + smallMargin);

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
                var visibleHeight = maxCount * (itemHeight + separatorHeight) - separatorHeight;
                return (totalHeight, visibleHeight);
            }
            else
            {
                return (totalHeight, totalHeight);
            }
        }

        // render my own vertical scroll bar because unity's one cannot be set slimmer than 15 px...
        public static void DrawScrollBar(int x, int y, int totalHeight, int visibleHeight, Vector2 scrollPosition)
        {
            if (totalHeight > visibleHeight)
            {
                var handleHeight = visibleHeight * visibleHeight / (float)totalHeight;
                var handlePositionT = scrollPosition.y / (float)totalHeight;
                var handlePosition = handlePositionT * visibleHeight;
                DrawColor(new Rect(x, y, scrollBarWidth, visibleHeight), separatorColor);
                DrawColor(new Rect(x, y + handlePosition, scrollBarWidth, handleHeight), scrollBarHandleColor);
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
