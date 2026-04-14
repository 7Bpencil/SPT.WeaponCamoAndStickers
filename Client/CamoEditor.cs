//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.Common;
using System;
using RuntimeHandle;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers
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
        public Texture2D EditUVOffsetIcon;
        public Texture2D EditUVAngleIcon;
        public Texture2D EditUVTilingIcon;
        public Texture2D DuplicateIcon;
        public Texture2D DeleteIcon;
        public Texture2D SaveIcon;
        public Texture2D ColorWheelHSV;

        public string[] DecalSettingsToolbar;
        public string[] DecalTypesToolbar;

        public CamoEditorResources(AssetBundle bundle)
        {
            PositionHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamoAndStickers/Shaders/HandleShader.shader");
            RotationHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamoAndStickers/Shaders/AdvancedHandleShader.shader");
            ScaleHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamoAndStickers/Shaders/HandleShader.shader");

            MainIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/color-palette.png");
            ClosedIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/closed-arrow.png");
            OpenedIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/opened-arrow.png");
            MoveUpIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/up-arrow.png");
            MoveDownIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/down-arrow.png");
            EditPositionIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/Move-Icon.png");
            EditRotationIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/Rotate-Icon.png");
            EditScaleIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/Scale-Icon.png");
            EditUVOffsetIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/UV-Move-Icon.png");
            EditUVAngleIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/UV-Rotate-Icon.png");
            EditUVTilingIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/UV-Scale-Icon.png");
            DuplicateIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/copy.png");
            DeleteIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/bin.png");
            SaveIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/diskette.png");
            ColorWheelHSV = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/hsv-circle.png");

            DecalSettingsToolbar = ["Texture", "Mask"];
            DecalTypesToolbar = ["Camos", "Stickers"];
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

    public enum DecalSettingType
    {
        Texture,
        Mask
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
        public Transform WeaponPreviewRotator;
        public float PreviewPivotZ;
        public bool IsOpened;
        public bool IsPresetsOpened;
        public string CurrentPresetName;
        public Vector2 PresetsScrollPosition;
        public Vector2 DecalsScrollPosition;
        public Option<int> CurrentlyEditedDecalIndex;
        public DecalSettingType DecalSettingType;
        public DecalTextureType DecalTypeMenu;
        public Vector2 CamosScrollPosition;
        public Vector2 StickersScrollPosition;
        public Vector2 MasksScrollPosition;
        public bool IsColorPickerOpened;
        public RuntimeTransformHandle TransformHandle;
		public Rect WindowRect;

        // brace for imGUI shitshow

        public const int iconColumns = 5;
        public const int maxIconRows = 5;
        public const int maxDecalsVisibleWhenPresetsAreNotOpened = 9;
        public const int maxDecalsVisibleWhenPresetsAreOpened = 5;
        public const int maxPresetsVisible = 9;
        public const int maxPresetNameLength = 25;
        public const int maxDecalNameLength = 30;

        public const int smallMargin = 4;
        public const int mediumMargin = 8;
        public const int bigMargin = 14;

        public const int startX = 10;
        public const int startY = 10;
        public const int windowWidth = bigMargin + (iconSize + smallMargin) * iconColumns - smallMargin + bigMargin;
        public const int buttonHeight = 32;
        public const int iconSize = buttonHeight * 2 + smallMargin;
        public const int boxWidth = windowWidth - bigMargin * 2;
        public const int boxHeight = iconSize + smallMargin * 2;
        public const int nameWidth = 120;
        public const int longFieldWidth = 60;
        public const int halfBoxWidthButton = (boxWidth - smallMargin) / 2;
        public const int thirdBoxWidthButton = (boxWidth - smallMargin * 2) / 3;
        public const int fourthBoxWidthButton = (halfBoxWidthButton - smallMargin) / 2;
        public const int sixthBoxWidthButton = (thirdBoxWidthButton - smallMargin) / 2;
        public const int openCloseButtonWidth = 22;
        public const int openCloseButtonHeight = 66;
        public static readonly Rect openCloseButtonIconRect = new(2, 3, 18, 61);
        public static readonly Rect colorPickerRect = new(0, 258, 230, 304);
        public const int hsCircleDiameter = 174;
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

                    if (DecalSettingType == DecalSettingType.Texture)
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
            if (DecalSettingType == DecalSettingType.Texture)
            {
                var totalRows = DivideIntRoundUp(Plugin.GetTexturesCount(DecalTypeMenu), iconColumns);
                var (_, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalRows, maxIconRows, iconSize, smallMargin);
                return
                    bigMargin + buttonHeight + bigMargin + // back button
                    buttonHeight + mediumMargin + // decal name
                    4 * (buttonHeight + smallMargin) - smallMargin + bigMargin + // position, rotation, scale, flip
                    smallMargin + bigMargin + // separator
                    buttonHeight + mediumMargin + // toolbar texture/mask
                    buttonHeight + smallMargin + // UV offset
                    buttonHeight + smallMargin + // UV angle
                    buttonHeight + mediumMargin + // UV tiling
                    buttonHeight + bigMargin + // color
                    buttonHeight + smallMargin + // opacity
                    buttonHeight + bigMargin + // max angle
                    iconSize + bigMargin + // icon
                    smallMargin + bigMargin + // separator
                    buttonHeight + mediumMargin + // toolbar camos/stickers
                    visibleHeight + bigMargin; // icons grid
            }
            else
            {
                var totalRows = DivideIntRoundUp(Plugin.GetTexturesCount(DecalTextureType.Mask), iconColumns);
                var (_, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalRows, maxIconRows, iconSize, smallMargin);
                return
                    bigMargin + buttonHeight + bigMargin + // back button
                    buttonHeight + mediumMargin + // decal name
                    4 * (buttonHeight + smallMargin) - smallMargin + bigMargin + // position, rotation, scale, flip
                    smallMargin + bigMargin + // separator
                    buttonHeight + mediumMargin + // toolbar texture/mask
                    buttonHeight + smallMargin + // UV offset
                    buttonHeight + smallMargin + // UV angle
                    buttonHeight + mediumMargin + // UV tiling
                    iconSize + bigMargin + // icon
                    smallMargin + bigMargin + // separator
                    visibleHeight + bigMargin; // icons grid
            }
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
                        bigMargin + buttonHeight + mediumMargin + // preset name
                        buttonHeight + mediumMargin + // hide presets button
                        presetsScrollHeight + bigMargin; // presets
                }
                else
                {
                    return
                        bigMargin + buttonHeight + mediumMargin + // preset name
                        buttonHeight + mediumMargin + // hide presets button
                        buttonHeight + bigMargin; // no presets text
                }
            }
            else
            {
                return
                    bigMargin + buttonHeight + mediumMargin + // preset name
                    buttonHeight + bigMargin; // show presets button
            }
        }

        private int CalculateDecalsWindowHeight()
        {
            var totalDecalsCount = Plugin.GetDecalsCount(ItemId);
            var maxDecalsVisible = IsPresetsOpened ? maxDecalsVisibleWhenPresetsAreOpened : maxDecalsVisibleWhenPresetsAreNotOpened;
            var (_, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalDecalsCount, maxDecalsVisible, boxHeight, mediumMargin);
            return
                bigMargin + visibleHeight + mediumMargin + // decals
                buttonHeight + bigMargin; // add new decal button
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
            CurrentPresetName = GUI.TextField(new Rect(x, y, presetButtonWidth, buttonHeight), CurrentPresetName, maxPresetNameLength, CamoStyle.TextFieldStyle);
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

                    Option<string> deletedPresetNameOption = default;
                    foreach (var name in Plugin.GetPresetNames())
                    {
                        if (GUI.Button(new Rect(x, decalsY, presetButtonWidth, buttonHeight), name))
                        {
                            Plugin.SwitchToPreset(ItemId, InstanceID, DecalsRoot, Camera, name);
                        }
                        if (GUI.Button(new Rect(x + presetButtonWidth + smallMargin, decalsY, buttonHeight, buttonHeight), CamoEditorResources.DeleteIcon))
                        {
                            // theres no way user will click on multiple buttons in one frame, right?
                            deletedPresetNameOption = new(name);
                        }
                        decalsY += buttonHeight + smallMargin;
                    }
                    if (deletedPresetNameOption.Some(out var deletedPresetName))
                    {
                        Plugin.DeletePreset(deletedPresetName);
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
                var newDecalIndex = Plugin.AddNewDecal(ItemId, InstanceID, DecalsRoot, WeaponPreviewRotator, PreviewPivotZ, Camera);
                var (decalInfo, _) = Plugin.GetDecal(ItemId, InstanceID, newDecalIndex);
                var textureData = Plugin.GetTextureData(decalInfo.Texture);
                SetCurrentlyEditedDecal(newDecalIndex, textureData.Type);
            }
        }

        private void SetCurrentlyEditedDecal(int decalIndex, DecalTextureType decalTextureType)
        {
            CurrentlyEditedDecalIndex = new(decalIndex);
            DecalTypeMenu = decalTextureType;
        }

        private void DrawDecalElementUI(int x, int y, int decalIndex, DecalInfo decalInfo)
        {
            var textureData = Plugin.GetTextureData(decalInfo.Texture);

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), default(string));

            var topLineY = y + smallMargin;
            var bottomLineY = topLineY + buttonHeight + smallMargin;

            var textureIconX = x + smallMargin;
            if (GUI.Button(new Rect(textureIconX, topLineY, iconSize, iconSize), textureData.Texture))
            {
                SetCurrentlyEditedDecal(decalIndex, textureData.Type);
            }

            var labelX = textureIconX + iconSize + smallMargin + 2;
            var decalName = string.IsNullOrWhiteSpace(decalInfo.Name) ? decalInfo.Texture : decalInfo.Name;
            GUI.Label(new Rect(labelX, topLineY + 1, 230, iconSize), decalName, CamoStyle.TextureNameStyle);

            var deleteX = x + boxWidth - (smallMargin + buttonHeight) * 3;
            if (GUI.Button(new Rect(deleteX, bottomLineY, buttonHeight, buttonHeight), CamoEditorResources.DeleteIcon))
            {
                Plugin.Delete(ItemId, decalIndex);
            }

            var duplicateX = deleteX + buttonHeight + smallMargin;
            if (GUI.Button(new Rect(duplicateX, bottomLineY, buttonHeight, buttonHeight), CamoEditorResources.DuplicateIcon))
            {
                var newDecalIndex = Plugin.Duplicate(ItemId, decalIndex);
                SetCurrentlyEditedDecal(newDecalIndex, textureData.Type);
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

            var hsCircleX = (colorPickerRect.width - hsCircleDiameter) / 2;
            var hsCircleRect = new Rect(hsCircleX, bigMargin, hsCircleDiameter, hsCircleDiameter);

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
            var textureData = Plugin.GetTextureData(decalInfo.Texture);
            var maskData = Plugin.GetTextureData(decalInfo.Mask);

            var x = bigMargin;
            var y = bigMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                CurrentlyEditedDecalIndex = default;
                DestroyTransformHandle();
            }
            y += buttonHeight + bigMargin;


            decalInfo.Name = GUI.TextField(new Rect(x, y, boxWidth, buttonHeight), decalInfo.Name, maxDecalNameLength, CamoStyle.TextFieldStyle);
            if (string.IsNullOrWhiteSpace(decalInfo.Name))
            {
                GUI.Label(new Rect(x + CamoStyle.TextFieldStyle.contentOffset.x + 3, y, boxWidth, buttonHeight), "enter decal name (optional)", CamoStyle.LabelStyleName);
            }
            y += buttonHeight + mediumMargin;


            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditPositionIcon))
            {
                SetupTransformHandle(HandleType.Position, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localPosition.x:F3}", CamoStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localPosition.y:F3}", CamoStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localPosition.z:F3}", CamoStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - thirdBoxWidthButton, y, thirdBoxWidthButton, buttonHeight), "mirror left/right"))
            {
                Plugin.MirrorLeftRight(ItemId, decalIndex, decalInfo);
                SyncTransformHandle(decalInfo, decal);
            }
            y += buttonHeight + smallMargin;


            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditRotationIcon))
            {
                SetupTransformHandle(HandleType.Rotation, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localEulerAngles.x:F3}", CamoStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localEulerAngles.y:F3}", CamoStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localEulerAngles.z:F3}", CamoStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - thirdBoxWidthButton, y, sixthBoxWidthButton, buttonHeight), "round"))
            {
                Plugin.RoundLocalEulerAnglesToDegree(ItemId, decalIndex, decalInfo);
                SyncTransformHandle(decalInfo, decal);
            }
            if (GUI.Button(new Rect(x + boxWidth - thirdBoxWidthButton + sixthBoxWidthButton + smallMargin, y, sixthBoxWidthButton, buttonHeight), "-90°Z"))
            {
                Plugin.RotateZ(ItemId, decalIndex, decalInfo, -90);
                SyncTransformHandle(decalInfo, decal);
            }
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditScaleIcon))
            {
                SetupTransformHandle(HandleType.Scale, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localScale.x:F3}", CamoStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localScale.y:F3}", CamoStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localScale.z:F3}", CamoStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - thirdBoxWidthButton, y, thirdBoxWidthButton, buttonHeight), "fix scale"))
            {
                Plugin.FixScale(ItemId, decalIndex, decalInfo);
                SyncTransformHandle(decalInfo, decal);
            }
            y += buttonHeight + smallMargin;

            {
                var lineX = x;
                if (GUI.Button(new Rect(lineX, y, thirdBoxWidthButton, buttonHeight), "flip horz"))
                {
                    Plugin.FlipHorizontally(ItemId, decalIndex, decalInfo);
                    SyncTransformHandle(decalInfo, decal);
                }
                lineX += thirdBoxWidthButton + smallMargin;

                if (GUI.Button(new Rect(lineX, y, thirdBoxWidthButton, buttonHeight), "flip vert"))
                {
                    Plugin.FlipVertically(ItemId, decalIndex, decalInfo);
                    SyncTransformHandle(decalInfo, decal);
                }
                lineX += thirdBoxWidthButton + smallMargin;

                if (GUI.Button(new Rect(lineX, y, thirdBoxWidthButton, buttonHeight), "flip dir"))
                {
                    Plugin.FlipDirection(ItemId, decalIndex, decalInfo);
                    SyncTransformHandle(decalInfo, decal);
                }
            }
            y += buttonHeight + bigMargin;

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            DecalSettingType = (DecalSettingType)GUI.Toolbar(new Rect(x, y, boxWidth, buttonHeight), (int)DecalSettingType, CamoEditorResources.DecalSettingsToolbar);
            y += buttonHeight + mediumMargin;

            if (DecalSettingType == DecalSettingType.Texture)
            {
                if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditUVOffsetIcon))
                {
                    SetupTransformHandle(HandleType.TextureOffset, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"X: {decalInfo.TextureUV.x:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"Y: {decalInfo.TextureUV.y:F3}", CamoStyle.LabelStyleName);
                }
                if (GUI.Button(new Rect(x + boxWidth - halfBoxWidthButton, y, fourthBoxWidthButton, buttonHeight), "reset"))
                {
                    Plugin.ResetTextureUVOffset(ItemId, decalIndex, decalInfo);
                    SyncTransformHandle(decalInfo, decal);
                }
                y += buttonHeight + smallMargin;

                if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditUVAngleIcon))
                {
                    // TODO
                }
                {
                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"X: {decalInfo.TextureUVAngle:F3}", CamoStyle.LabelStyleName);
                }
                if (GUI.Button(new Rect(x + boxWidth - halfBoxWidthButton, y, fourthBoxWidthButton, buttonHeight), "reset"))
                {
                    // TODO
                }
                y += buttonHeight + smallMargin;

                if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditUVTilingIcon))
                {
                    SetupTransformHandle(HandleType.TextureTiling, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"X: {decalInfo.TextureUV.z:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"Y: {decalInfo.TextureUV.w:F3}", CamoStyle.LabelStyleName);
                }
                {
                    var valueX = x + boxWidth - halfBoxWidthButton;
                    if (GUI.Button(new Rect(valueX, y, fourthBoxWidthButton, buttonHeight), "reset"))
                    {
                        Plugin.ResetTextureUVScale(ItemId, decalIndex, decalInfo);
                        SyncTransformHandle(decalInfo, decal);
                    }
                    valueX += fourthBoxWidthButton + smallMargin;

                    if (GUI.Button(new Rect(valueX, y, fourthBoxWidthButton, buttonHeight), "fix UV"))
                    {
                        Plugin.FixTextureUV(ItemId, decalIndex, decalInfo);
                        SyncTransformHandle(decalInfo, decal);
                    }
                }
                y += buttonHeight + mediumMargin;

                {
                    var colorButtonRect = new Rect(x, y, buttonHeight, buttonHeight);

                    DrawColor(colorButtonRect, decalInfo.ColorHSVA.HSVAtoRGBA().WithAlpha(1f));
                    if (GUI.Button(colorButtonRect, GUIContent.none, GUIStyle.none))
                    {
                        IsColorPickerOpened = !IsColorPickerOpened;
                    }

                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"H: {decalInfo.ColorHSVA.x:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"S: {decalInfo.ColorHSVA.y:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"V: {decalInfo.ColorHSVA.z:F3}", CamoStyle.LabelStyleName);
                }
                y += buttonHeight + bigMargin;


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
                    GUI.Button(new Rect(x, y, iconSize, iconSize), textureData.Texture);

                    var labelX = x + iconSize + smallMargin + 12;
                    GUI.Label(new Rect(labelX, y + 1, 256, buttonHeight), decalInfo.Texture, CamoStyle.TextureNameStyle);

                    y += iconSize + bigMargin;
                }

                DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
                y += smallMargin + bigMargin;

                DecalTypeMenu = (DecalTextureType)GUI.Toolbar(new Rect(x, y, boxWidth, buttonHeight), (int)DecalTypeMenu, CamoEditorResources.DecalTypesToolbar);
                y += buttonHeight + mediumMargin;

                DrawAllTextures(x, y, decalIndex, decalInfo, decal, DecalTypeMenu);
            }
            else
            {
                if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditUVOffsetIcon))
                {
                    SetupTransformHandle(HandleType.MaskOffset, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"X: {decalInfo.MaskUV.x:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"Y: {decalInfo.MaskUV.y:F3}", CamoStyle.LabelStyleName);
                }
                y += buttonHeight + smallMargin;

                if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditUVAngleIcon))
                {
                    // TODO
                }
                {
                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"X: {decalInfo.MaskUVAngle:F3}", CamoStyle.LabelStyleName);
                }
                y += buttonHeight + smallMargin;

                if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditUVTilingIcon))
                {
                    SetupTransformHandle(HandleType.MaskTiling, decalIndex, decalInfo, decal);
                }
                {
                    var valueX = x + buttonHeight + smallMargin + 7;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"X: {decalInfo.MaskUV.z:F3}", CamoStyle.LabelStyleName);
                    valueX += longFieldWidth + smallMargin;

                    GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"Y: {decalInfo.MaskUV.w:F3}", CamoStyle.LabelStyleName);
                }
                y += buttonHeight + mediumMargin;

                {
                    GUI.Button(new Rect(x, y, iconSize, iconSize), maskData.Texture);

                    var labelX = x + iconSize + smallMargin + 12;
                    GUI.Label(new Rect(labelX, y + 1, 256, buttonHeight), decalInfo.Mask, CamoStyle.TextureNameStyle);

                    y += iconSize + bigMargin;
                }

                DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
                y += smallMargin + bigMargin;

                DrawAllTextures(x, y, decalIndex, decalInfo, decal, DecalTextureType.Mask);
            }

			GUI.DragWindow();
        }

        public void SetupTransformHandle(HandleType handleType)
        {
            if (CurrentlyEditedDecalIndex.Some(out var decalIndex))
            {
                var (decalInfo, decal) = Plugin.GetDecal(ItemId, InstanceID, decalIndex);
                SetupTransformHandle(handleType, decalIndex, decalInfo, decal);
            }
        }

        public void SetupTransformHandle(HandleType handleType, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            if (TransformHandle)
            {
                if (TransformHandle.type == handleType)
                {
                    return;
                }

                ForceOnEndedDraggingHandle();
                TransformHandle.DestroyHandles();
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
            if (handleType == HandleType.TextureOffset)
            {
                TransformHandle.CreateHandleTextureOffset(decalInfo, decal);
            }
            if (handleType == HandleType.TextureTiling)
            {
                TransformHandle.CreateHandleTextureTiling(decalInfo, decal);
            }
            if (handleType == HandleType.MaskOffset)
            {
                TransformHandle.CreateHandleMaskOffset(decalInfo, decal);
            }
            if (handleType == HandleType.MaskTiling)
            {
                TransformHandle.CreateHandleMaskTiling(decalInfo, decal);
            }

            SyncTransformHandle(decalInfo, decal);
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
            if (handleType == HandleType.TextureOffset)
            {
                Plugin.ApplyTextureUV(ItemId, decalIndex, decalInfo);
            }
            if (handleType == HandleType.TextureTiling)
            {
                Plugin.ApplyTextureUV(ItemId, decalIndex, decalInfo);
            }
            if (handleType == HandleType.MaskOffset)
            {
                Plugin.ApplyMaskUV(ItemId, decalIndex, decalInfo);
            }
            if (handleType == HandleType.MaskTiling)
            {
                Plugin.ApplyMaskUV(ItemId, decalIndex, decalInfo);
            }
        }

        public void SyncTransformHandle(DecalInfo decalInfo, Decal decal)
        {
            if (TransformHandle)
            {
                TransformHandle.ResetHandleTransform(decalInfo, decal);
            }
        }

        public void ForceOnEndedDraggingHandle()
        {
            if (TransformHandle && TransformHandle.IsDragging)
            {
                TransformHandle.OnEndedDraggingHandle.Invoke();
            }
        }

        public void DestroyTransformHandle()
        {
            if (TransformHandle)
            {
                GameObject.Destroy(TransformHandle.gameObject);
            }
        }

        private ref Vector2 GetScrollPosition(DecalTextureType textureType)
        {
            switch (textureType)
            {
                case DecalTextureType.Camo: return ref CamosScrollPosition;
                case DecalTextureType.Sticker: return ref StickersScrollPosition;
                case DecalTextureType.Mask: return ref MasksScrollPosition;
                default: throw new ArgumentException();
            }
        }

        private void DrawAllTextures(int x, int y, int decalIndex, DecalInfo decalInfo, Decal decal, DecalTextureType decalTextureType)
        {
            var totalRows = DivideIntRoundUp(Plugin.GetTexturesCount(decalTextureType), iconColumns);
            var (totalHeight, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalRows, maxIconRows, iconSize, smallMargin);
            var totalRect = new Rect(x, y, boxWidth, totalHeight);
            var visibleRect = new Rect(x, y, boxWidth + 16, visibleHeight);

            ref var scrollPosition = ref GetScrollPosition(decalTextureType);
            DrawScrollBar(x + boxWidth + 5, y, totalHeight, visibleHeight, scrollPosition);
            scrollPosition = GUI.BeginScrollView(visibleRect, scrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

            for (var i = 0; i < Plugin.GetTexturesCount(decalTextureType); i++)
            {
                var textureName = Plugin.GetTextureName(decalTextureType, i);
                var textureData = Plugin.GetTextureData(textureName);

                var ix = i % iconColumns;
                var iy = i / iconColumns;

                var xi = x + ix * (iconSize + smallMargin);
                var yi = y + iy * (iconSize + smallMargin);

                if (GUI.Button(new Rect(xi, yi, iconSize, iconSize), textureData.Texture))
                {
                    if (textureData.Type == DecalTextureType.Camo && decalInfo.Texture != textureName)
                    {
                        decalInfo.Texture = textureName;
                        Plugin.ApplyTexture(ItemId, decalIndex, decalInfo);
                        Plugin.FixTextureUV(ItemId, decalIndex, decalInfo);
                        SyncTransformHandle(decalInfo, decal);
                    }
                    if (textureData.Type == DecalTextureType.Sticker && decalInfo.Texture != textureName)
                    {
                        decalInfo.Texture = textureName;
                        Plugin.ApplyTexture(ItemId, decalIndex, decalInfo);
                        Plugin.FixScale(ItemId, decalIndex, decalInfo);
                        SyncTransformHandle(decalInfo, decal);
                    }
                    if (textureData.Type == DecalTextureType.Mask && decalInfo.Mask != textureName)
                    {
                        decalInfo.Mask = textureName;
                        Plugin.ApplyMask(ItemId, decalIndex, decalInfo);
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
