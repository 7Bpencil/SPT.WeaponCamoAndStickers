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
    public class CamoEditorResources(AssetBundle bundle)
    {
        public Shader PositionHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamoAndStickers/Shaders/HandleShader.shader");
        public Shader RotationHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamoAndStickers/Shaders/AdvancedHandleShader.shader");
        public Shader ScaleHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamoAndStickers/Shaders/HandleShader.shader");

        public Texture2D MainIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/color-palette.png");
        public Texture2D MainIconMaterial = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/hsv-circle-icon.png");
        public Texture2D ClosedIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/closed-arrow.png");
        public Texture2D OpenedIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/opened-arrow.png");
        public Texture2D ClosedIconColorWheel = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/closed-arrow-color-wheel.png");
        public Texture2D OpenedIconColorWheel = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/opened-arrow-color-wheel.png");
        public Texture2D MoveUpIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/up-arrow.png");
        public Texture2D MoveDownIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/down-arrow.png");
        public Texture2D EditPositionIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/Move-Icon.png");
        public Texture2D EditRotationIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/Rotate-Icon.png");
        public Texture2D EditScaleIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/Scale-Icon.png");
        public Texture2D EditTextureUVOffsetIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/UV-Texture-Move-Icon.png");
        public Texture2D EditTextureUVAngleIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/UV-Texture-Rotate-Icon.png");
        public Texture2D EditTextureUVTilingIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/UV-Texture-Scale-Icon.png");
        public Texture2D EditMaskUVOffsetIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/UV-Mask-Move-Icon.png");
        public Texture2D EditMaskUVAngleIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/UV-Mask-Rotate-Icon.png");
        public Texture2D EditMaskUVTilingIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/UV-Mask-Scale-Icon.png");
        public Texture2D DuplicateIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/duplicate.png");
        public Texture2D CopyPaintIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/copy-paint.png");
        public Texture2D PastePaintIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/paste-paint.png");
        public Texture2D CopyEraserIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/copy-eraser.png");
        public Texture2D PasteEraserIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/paste-eraser.png");
        public Texture2D DeleteIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/bin.png");
        public Texture2D SaveIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/diskette.png");
        public Texture2D SaveErrorIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/diskette-error.png");
        public Texture2D ColorWheelHSV = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/hsv-circle.png");
        public Texture2D PlayIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/play-icon.png");
        public Texture2D HiddenIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/hidden.png");
        public Texture2D VisibleIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/visible.png");
        public Texture2D MirrorDisabled = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/mirror-off.png");
        public Texture2D MirrorEnabled = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/mirror-on.png");
        public Texture2D MirrorEnabledNoFilp = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/mirror-on-no-flip.png");
        public Texture2D Reset = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/undo.png");
        public Texture2D FavouriteIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/star.png");
        public Texture2D CheckboxOff = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/checkbox-off.png");
        public Texture2D CheckboxOn = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/checkbox-on.png");
        public Texture2D LinkOn = bundle.LoadAsset<Texture2D>("Assets/WeaponCamoAndStickers/Icons/link-on.png");

        public string[] DecalSettingsToolbar = ["Texture", "Mask"];
        public string[] DecalTypesToolbar = ["Camos", "Stickers"];
    }

    public class CamoEditorStyle
    {
        public GUIStyle LabelStyleName;
        public GUIStyle TextureNameStyle;
        public GUIStyle LabelStyleValue;
        public GUIStyle TextFieldStyle;
        public GUIStyle RGBHexTextFieldStyle;
		public GUIStyle ColorPickerButtonStyle;
        public GUIStyle DirectoryButtonStyle;

        public CamoEditorStyle(GUISkin currentSkin)
        {
            LabelStyleName = new(currentSkin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                wordWrap = false,
            };

            TextureNameStyle = new(currentSkin.label)
            {
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Overflow,
                wordWrap = true,
            };

            LabelStyleValue = new(currentSkin.label)
            {
                alignment = TextAnchor.MiddleCenter,
            };

            TextFieldStyle = new(currentSkin.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                contentOffset = new Vector2(CamoEditor.mediumMargin, 0)
            };

            RGBHexTextFieldStyle = new(currentSkin.textField)
            {
                alignment = TextAnchor.MiddleCenter,
            };

			ColorPickerButtonStyle = new GUIStyle()
			{
				stretchWidth = true,
				stretchHeight = true,
			};

            DirectoryButtonStyle = new(currentSkin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                wordWrap = false,
            };
        }
    }

    public class DecalStringCache
    {
        public StringCache<float> LocalPositionX = new(v => $"X: {v:F3}");
        public StringCache<float> LocalPositionY = new(v => $"Y: {v:F3}");
        public StringCache<float> LocalPositionZ = new(v => $"Z: {v:F3}");

        public StringCache<float> LocalEulerAnglesX = new(v => $"X: {v:F3}");
        public StringCache<float> LocalEulerAnglesY = new(v => $"Y: {v:F3}");
        public StringCache<float> LocalEulerAnglesZ = new(v => $"Z: {v:F3}");

        public StringCache<float> LocalScaleX = new(v => $"X: {v:F3}");
        public StringCache<float> LocalScaleY = new(v => $"Y: {v:F3}");
        public StringCache<float> LocalScaleZ = new(v => $"Z: {v:F3}");

        public StringCache<float> TextureUVx = new(v => $"X: {v:F3}");
        public StringCache<float> TextureUVy = new(v => $"Y: {v:F3}");

        public StringCache<float> TextureAngle = new(v => $"X: {v:F3}");

        public StringCache<float> TextureUVz = new(v => $"X: {v:F3}");
        public StringCache<float> TextureUVw = new(v => $"Y: {v:F3}");

        public StringCache<float> MaskUVx = new(v => $"X: {v:F3}");
        public StringCache<float> MaskUVy = new(v => $"Y: {v:F3}");

        public StringCache<float> MaskAngle = new(v => $"X: {v:F3}");

        public StringCache<float> MaskUVz = new(v => $"X: {v:F3}");
        public StringCache<float> MaskUVw = new(v => $"Y: {v:F3}");

        public StringCache<float> ColorH = new(v => $"H: {v:F3}");
        public StringCache<float> ColorS = new(v => $"S: {v:F3}");
        public StringCache<float> ColorV = new(v => $"V: {v:F3}");

        public StringCache<float> ColorA = new(v => $"{v:F3}");

        public StringCache<float> MaxAngle = new(v => $"{v:F3}");
    }

    public struct TextField<T> where T : IEquatable<T>
    {
        public StringCache<T> String;
        public Func<string, Option<T>> TryParse;
        public string Value;
        public bool IsValid;

        public TextField(Func<T, string> format, Func<string, Option<T>> tryParse)
        {
            String = new(format);
            TryParse = tryParse;
        }

        public void SetValue(T value)
        {
            Value = String.Get(value);
            IsValid = true;
        }

        public bool TrySetValue(string value, out Option<T> valueOption)
        {
            if (Value != value)
            {
                valueOption = TryParse(value);
                Value = value;
                IsValid = valueOption.HasValue;
                return true;
            }

            valueOption = default;
            return false;
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
        public CamoEditorStyle CamoEditorStyle;
        public DecalStringCache Strings = new();
        public Camera Camera;
        public RuntimeGizmos RuntimeGizmos;
        public string ItemId;
        public int InstanceID;
        public WeaponPrefab WeaponPrefab;
        public Transform WeaponPreviewRotator;
        public float PreviewPivotZ;
        public bool IsOpened;
        public bool ArePresetsOpened;
        public string CurrentPresetName = "";
        public bool IsCurrentPresetNameValid;
        public Vector2 PresetsScrollPosition;
        public Vector2 DecalsScrollPosition;
        public Option<int> CurrentlyEditedDecalIndex;
        public DecalSettingType DecalSettingType;
        public DecalTextureType DecalTypeMenu;
        public Vector2 CamosScrollPosition;
        public Vector2 StickersScrollPosition;
        public Vector2 MasksScrollPosition;
        public bool IsColorPickerOpened;
        public TextField<Vector3> ColorTextField = new(ColorExtensions.HSVtoHexRGB, ColorExtensions.HexRGBtoHSV);
        public RuntimeTransformHandle TransformHandle;
        public Option<DecalInfo> CopiedDecalInfo;
		public Rect WindowRect = GetDefaultWindowRect();

        // brace for imGUI shitshow

        public const int iconColumns = 5;
        public const int maxDecalsVisible = 10;
        public const int maxPresetsVisible = 22;
        public const int maxPresetNameLength = 25;
        public const int maxDecalNameLength = 30;

        public const int smallMargin = 4;
        public const int mediumMargin = 8;
        public const int bigMargin = 14;

        public const int startX = 10;
        public const int startY = 10;
        public const int windowWidth = bigMargin + (iconSize + smallMargin) * iconColumns - smallMargin + bigMargin;
        public const int buttonHeight = 32;
        public const int smallIconSize = 16;
        public const int iconSize = buttonHeight * 2 + smallMargin;
        public const int maxTextureIconsVisibleHeight = 9 * (buttonHeight + smallMargin) - smallMargin;
        public const int maxMaskIconsVisibleHeight = 13 * (buttonHeight + smallMargin) - smallMargin;
        public const int maxEraseMaskIconsVisibleHeight = 13 * (buttonHeight + smallMargin) - smallMargin;
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
        public static readonly Rect colorPickerRect = new(0, 252, 230, 304);
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
            if (CamoEditorStyle == null)
            {
                CamoEditorStyle = new(GUI.skin);
            }

            var originalMatrix = GUI.matrix;
            var baseUIScale = Math.Max(Screen.height / 1080, 1);
            var uiScale = baseUIScale * Plugin.UIScale.Value;
            GUI.matrix = Matrix4x4.Scale(new(uiScale, uiScale, 1f));

            if (IsOpened)
            {
                if (CurrentlyEditedDecalIndex.Some(out var decalIndex))
                {
                    var (decalInfo, decal) = Plugin.GetDecal(ItemId, InstanceID, decalIndex);

                    WindowRect.height = CalculateDecalEditWindowHeight(decalIndex, decalInfo, decal);
                    WindowRect = GUI.Window(1, WindowRect, DrawDecalEditUI, GUIContent.none);

                    var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                    GUI.Window(2, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);

                    if (decalInfo.PaintMode == DecalPaintMode.Paint)
                    {
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
                }
                else
                {
                    if (ArePresetsOpened)
                    {
                        WindowRect.height = CalculatePresetsWindowHeight();
                        WindowRect = GUI.Window(1, WindowRect, DrawPresetsListUI, GUIContent.none);
                    }
                    else
                    {
                        WindowRect.height = CalculateDecalsWindowHeight();
                        WindowRect = GUI.Window(1, WindowRect, DrawDecalsListUI, GUIContent.none);
                    }

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

            GUI.matrix = originalMatrix;
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

        private int CalculateDecalEditWindowHeight(int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            if (decalInfo.PaintMode == DecalPaintMode.Paint)
            {
                if (DecalSettingType == DecalSettingType.Texture)
                {
                    var (totalHeight, visibleHeight) = CalculateTexturesDirectoryHeight(DecalTypeMenu, maxTextureIconsVisibleHeight);
                    return
                        bigMargin +
                        buttonHeight + mediumMargin + // back button
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
                        buttonHeight + smallMargin + // toolbar camos/stickers
                        visibleHeight + bigMargin; // icons grid
                }
                else
                {
                    var (totalHeight, visibleHeight) = CalculateTexturesDirectoryHeight(DecalTextureType.Mask, maxMaskIconsVisibleHeight);
                    return
                        bigMargin +
                        buttonHeight + mediumMargin + // back button
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
            if (decalInfo.PaintMode == DecalPaintMode.Erase)
            {
                var (_, visibleHeight) = CalculateTexturesDirectoryHeight(DecalTextureType.Mask, maxEraseMaskIconsVisibleHeight);
                return
                    bigMargin +
                    buttonHeight + mediumMargin + // back button
                    buttonHeight + mediumMargin + // decal name
                    4 * (buttonHeight + smallMargin) - smallMargin + bigMargin + // position, rotation, scale, flip
                    smallMargin + bigMargin + // separator
                    buttonHeight + smallMargin + // UV offset
                    buttonHeight + smallMargin + // UV angle
                    buttonHeight + bigMargin + // UV tiling
                    buttonHeight + bigMargin + // max alpha
                    iconSize + bigMargin + // icon
                    smallMargin + bigMargin + // separator
                    visibleHeight + bigMargin; // icons grid
            }
            throw new ArgumentException($"unknown DecalPaintMode: {decalInfo.PaintMode}");
        }

        private (int totalHeight, int visibleHeight) CalculateTexturesDirectoryHeight(DecalTextureType decalTextureType, int maxIconsVisibleHeight)
        {
            var texturesDirectory = Plugin.GetTexturesDirectory(decalTextureType);
            return CalculateTexturesDirectoryHeight(texturesDirectory, maxIconsVisibleHeight);
        }

        public static (int totalHeight, int visibleHeight) CalculateTexturesDirectoryHeight(TexturesDirectory texturesDirectory, int maxIconsVisibleHeight)
        {
            var totalHeight = 0;
            CalculateTexturesDirectoryHeight(ref totalHeight, texturesDirectory, false);
            totalHeight -= smallMargin; // total height cannot be negative because of builtin folder
            var visibleHeight = Math.Min(totalHeight, maxIconsVisibleHeight);
            return (totalHeight, visibleHeight);
        }

        private static void CalculateTexturesDirectoryHeight(ref int y, TexturesDirectory texturesDirectory, bool drawName = true)
        {
            if (drawName)
            {
                y += buttonHeight + smallMargin;
            }

            if (texturesDirectory.IsClosed)
            {
                return;
            }

            foreach (var subDirectory in texturesDirectory.Directories)
            {
                CalculateTexturesDirectoryHeight(ref y, subDirectory);
            }

            var textures = texturesDirectory.Textures;
            var totalRows = DivideIntCeil(textures.Length, iconColumns);
            y += (iconSize + smallMargin) * totalRows;
        }

        private int CalculatePresetsWindowHeight()
        {
            var header =
                bigMargin + buttonHeight + bigMargin + // hide presets button
                smallMargin + bigMargin + // separator
                buttonHeight + mediumMargin + // preset name
                buttonHeight + mediumMargin; // generate random camo button

            var totalPresets = Plugin.GetPresetsCount();
            if (totalPresets > 0)
            {
                var (_, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalPresets, maxPresetsVisible, buttonHeight, smallMargin);
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

        private int CalculateDecalsWindowHeight()
        {
            var totalDecalsCount = Plugin.GetDecalsCount(ItemId);
            var (_, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalDecalsCount, maxDecalsVisible, boxHeight, mediumMargin);
            return
                bigMargin +
                buttonHeight + bigMargin + // show presets button
                smallMargin + bigMargin + // separator
                visibleHeight + mediumMargin + // decals
                buttonHeight + bigMargin; // add new decal button
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

        private void DrawPresetsListUI(int windowID)
        {
            DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var x = bigMargin;
            var y = bigMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Hide Presets"))
            {
                ArePresetsOpened = false;
            }
            y += buttonHeight + bigMargin;

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            // save button turns green only if there is valid input,
            // text field goes red only if there is actual invalid input, stays default if no input
            var previousBackgroundColor = GUI.backgroundColor;
            var hasInvalidInput = !string.IsNullOrWhiteSpace(CurrentPresetName) && !IsCurrentPresetNameValid;
            var buttonBackgroundColor = hasInvalidInput ? Color.red : previousBackgroundColor;

            GUI.backgroundColor = buttonBackgroundColor;
            var presetButtonWidth = boxWidth - buttonHeight - smallMargin;
            var newPresetName = GUI.TextField(new Rect(x, y, presetButtonWidth, buttonHeight), CurrentPresetName, maxPresetNameLength, CamoEditorStyle.TextFieldStyle);
            GUI.backgroundColor = previousBackgroundColor;

            if (newPresetName != CurrentPresetName)
            {
                CurrentPresetName = newPresetName;
                IsCurrentPresetNameValid = SafeIO.IsValidFileName(newPresetName);
            }
            if (string.IsNullOrWhiteSpace(CurrentPresetName))
            {
                GUI.Label(new Rect(x + CamoEditorStyle.TextFieldStyle.contentOffset.x + 3, y, presetButtonWidth, buttonHeight), "enter new preset name", CamoEditorStyle.LabelStyleName);
            }

            var saveX = x + boxWidth - buttonHeight;
            var saveIcon = IsCurrentPresetNameValid ? CamoEditorResources.SaveIcon : CamoEditorResources.SaveErrorIcon;
            if (GUI.Button(new Rect(saveX, y, buttonHeight, buttonHeight), saveIcon))
            {
                if (IsCurrentPresetNameValid)
                {
                    Plugin.SaveDecalsIntoPreset(ItemId, CurrentPresetName);
                }
            }
            y += buttonHeight + mediumMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Generate Random Camo"))
            {
                Plugin.SwitchToRandomPreset(ItemId, InstanceID, WeaponPrefab, Camera);
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
                foreach (var presetName in Plugin.GetPresetNames())
                {
                    if (GUI.Button(new Rect(x, decalsY, presetButtonWidth, buttonHeight), presetName))
                    {
                        CurrentPresetName = presetName;
                        IsCurrentPresetNameValid = true;
                        Plugin.SwitchToPreset(ItemId, InstanceID, WeaponPrefab, Camera, presetName);
                    }
                    if (GUI.Button(new Rect(x + presetButtonWidth + smallMargin, decalsY, buttonHeight, buttonHeight), CamoEditorResources.DeleteIcon))
                    {
                        // theres no way user will click on multiple buttons in one frame, right?
                        deletedPresetNameOption = new(presetName);
                    }
                    decalsY += buttonHeight + smallMargin;
                }
                if (deletedPresetNameOption.Some(out var deletedPresetName))
                {
                    Plugin.DeletePreset(deletedPresetName);
                }
                y += visibleHeight + bigMargin;

                GUI.EndScrollView();
            }
            else
            {
                GUI.Label(new Rect(x, y, boxWidth, buttonHeight), "No Presets Available", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + bigMargin;
            }

			GUI.DragWindow();
        }

        private void DrawDecalsListUI(int windowID)
        {
            DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var x = bigMargin;
            var y = bigMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Show Presets"))
            {
                ArePresetsOpened = true;
            }
            y += buttonHeight + bigMargin;

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            if (Plugin.GetDecalsInfo(ItemId).Some(out var decalsInfo))
            {
                var decalsY = y;

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
                y += visibleHeight + mediumMargin;

                GUI.EndScrollView();
            }

            {
                var lineX = x;
                if (GUI.Button(new Rect(x, y, halfBoxWidthButton, buttonHeight), "Add Eraser"))
                {
                    var newDecalIndex = Plugin.AddNewEraserDecal(ItemId, InstanceID, WeaponPrefab, WeaponPreviewRotator, PreviewPivotZ, Camera);
                    SetCurrentlyEditedDecal(newDecalIndex, DecalTextureType.Mask); // technicaly eraser doesnt have texture type, but whatever...
                }
                lineX += halfBoxWidthButton + smallMargin;

                if (GUI.Button(new Rect(lineX, y, halfBoxWidthButton, buttonHeight), "Add Paint"))
                {
                    var newDecalIndex = Plugin.AddNewPaintDecal(ItemId, InstanceID, WeaponPrefab, WeaponPreviewRotator, PreviewPivotZ, Camera);
                    var (decalInfo, _) = Plugin.GetDecal(ItemId, InstanceID, newDecalIndex);
                    var textureData = Plugin.GetTextureData(decalInfo.Texture);
                    SetCurrentlyEditedDecal(newDecalIndex, textureData.Type);
                }
            }

			GUI.DragWindow();
        }

        private void SetCurrentlyEditedDecal(int decalIndex, DecalTextureType decalTextureType)
        {
            var (decalInfo, decal) = Plugin.GetDecal(ItemId, InstanceID, decalIndex);

            // TODO
            // these should be grouped together with decal index,
            // so we dont forget to correctly init/clean those fields

            CurrentlyEditedDecalIndex = new(decalIndex);
            DecalTypeMenu = decalTextureType;
            ColorTextField.SetValue(decalInfo.ColorHSVA);
        }

        private void DrawDecalElementUI(int x, int y, int decalIndex, DecalInfo decalInfo)
        {
            var textureData = Plugin.GetTextureData(decalInfo.Texture);
            var maskData = Plugin.GetTextureData(decalInfo.Mask);

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), default(string));

            var topLineY = y + smallMargin;
            var bottomLineY = topLineY + buttonHeight + smallMargin;

            Texture2D getDecalIcon()
            {
                if (decalInfo.PaintMode == DecalPaintMode.Paint)
                {
                    return textureData.Preview;
                }
                if (decalInfo.PaintMode == DecalPaintMode.Erase)
                {
                    return maskData.Preview;
                }
                throw new ArgumentException($"unknown DecalPaintMode: {decalInfo.PaintMode}");
            }

            var textureIconX = x + smallMargin;
            var decalIcon = getDecalIcon();
            if (GUI.Button(new Rect(textureIconX, topLineY, iconSize, iconSize), decalIcon))
            {
                SetCurrentlyEditedDecal(decalIndex, textureData.Type);
            }

            static string getDecalName(DecalInfo decalInfo)
            {
                if (!string.IsNullOrWhiteSpace(decalInfo.Name))
                {
                    return decalInfo.Name;
                }
                if (decalInfo.PaintMode == DecalPaintMode.Paint)
                {
                    return decalInfo.Texture;
                }
                if (decalInfo.PaintMode == DecalPaintMode.Erase)
                {
                    return decalInfo.Mask;
                }
                throw new ArgumentException($"unknown DecalPaintMode: {decalInfo.PaintMode}");
            }

            var labelX = textureIconX + iconSize + smallMargin + 2;
            var decalName = getDecalName(decalInfo);
            GUI.Label(new Rect(labelX, topLineY + 1, 230, iconSize), decalName, CamoEditorStyle.TextureNameStyle);

            var lineX = x + boxWidth - (smallMargin + buttonHeight) * 4;
            if (GUI.Button(new Rect(lineX, bottomLineY, buttonHeight, buttonHeight), CamoEditorResources.DeleteIcon))
            {
                Plugin.Delete(ItemId, decalIndex);
            }
            lineX += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(lineX, bottomLineY, buttonHeight, buttonHeight), CamoEditorResources.DuplicateIcon))
            {
                var newDecalIndex = Plugin.Duplicate(ItemId, decalIndex);
                SetCurrentlyEditedDecal(newDecalIndex, textureData.Type);
            }
            lineX += buttonHeight + smallMargin;

            var isVisibleIcon = decalInfo.IsVisible ? CamoEditorResources.VisibleIcon : CamoEditorResources.HiddenIcon;
            if (GUI.Button(new Rect(lineX, bottomLineY, buttonHeight, buttonHeight), isVisibleIcon))
            {
                Plugin.SwitchIsVisible(ItemId, decalIndex, decalInfo);
            }
            lineX += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(lineX, topLineY, buttonHeight, buttonHeight), CamoEditorResources.MoveUpIcon))
            {
                Plugin.Move(ItemId, decalIndex, decalIndex - 1);
            }
            if (GUI.Button(new Rect(lineX, bottomLineY, buttonHeight, buttonHeight), CamoEditorResources.MoveDownIcon))
            {
                Plugin.Move(ItemId, decalIndex, decalIndex + 1);
            }
        }

        private void DrawColorPickerWindow(int windowID)
        {
            var decalIndex = CurrentlyEditedDecalIndex.Value;
            var (decalInfo, decal) = Plugin.GetDecal(ItemId, InstanceID, decalIndex);

            DrawColor(new Rect(0, 0, colorPickerRect.width, colorPickerRect.height), backgroundColor);

            var hsCircleX = (colorPickerRect.width - hsCircleDiameter) / 2;
            var hsCircleRect = new Rect(hsCircleX, bigMargin, hsCircleDiameter, hsCircleDiameter);

			if (GUI.RepeatButton(hsCircleRect, CamoEditorResources.ColorWheelHSV, CamoEditorStyle.ColorPickerButtonStyle))
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
                ColorTextField.SetValue(decalInfo.ColorHSVA);
                Plugin.ApplyColor(ItemId, decalIndex);
            }

            {
                var sliderWidth = 120 + 70 - 14;

                var labelX = bigMargin;
                var sliderX = labelX + nameWidth + smallMargin - 42 - 70 + 14;
                var y = bigMargin + hsCircleDiameter + bigMargin - 9;

                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "H:", CamoEditorStyle.LabelStyleName);
                var newHue = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.x, 0f, 1f);
                if (newHue != decalInfo.ColorHSVA.x)
                {
                    decalInfo.ColorHSVA.x = newHue;
                    ColorTextField.SetValue(decalInfo.ColorHSVA);
                    Plugin.ApplyColor(ItemId, decalIndex);
                }
                y += buttonHeight + smallMargin;

                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "S:", CamoEditorStyle.LabelStyleName);
                var newSaturation = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.y, 0f, 1f);
                if (newSaturation != decalInfo.ColorHSVA.y)
                {
                    decalInfo.ColorHSVA.y = newSaturation;
                    ColorTextField.SetValue(decalInfo.ColorHSVA);
                    Plugin.ApplyColor(ItemId, decalIndex);
                }
                y += buttonHeight + smallMargin;

                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "V:", CamoEditorStyle.LabelStyleName);
                var newValue = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), decalInfo.ColorHSVA.z, 0f, 1f);
                if (newValue != decalInfo.ColorHSVA.z)
                {
                    decalInfo.ColorHSVA.z = newValue;
                    ColorTextField.SetValue(decalInfo.ColorHSVA);
                    Plugin.ApplyColor(ItemId, decalIndex);
                }
            }
        }

        private void DrawColorPickerWindowCloseButton(int windowID)
        {
            DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.OpenedIconColorWheel, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                IsColorPickerOpened = false;
            }
        }

        private void DrawColorPickerWindowOpenButton(int windowID)
        {
            DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.ClosedIconColorWheel, ScaleMode.StretchToFill);
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

            var x = bigMargin;
            var y = bigMargin;

            DrawDecalEditUI_Header(x, ref y, decalIndex, decalInfo, decal);
            DrawDecalEditUI_Transform(x, ref y, decalIndex, decalInfo, decal);

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            if (decalInfo.PaintMode == DecalPaintMode.Paint)
            {
                DecalSettingType = (DecalSettingType)GUI.Toolbar(new Rect(x, y, boxWidth, buttonHeight), (int)DecalSettingType, CamoEditorResources.DecalSettingsToolbar);
                y += buttonHeight + mediumMargin;

                if (DecalSettingType == DecalSettingType.Texture)
                {
                    DrawDecalEditUI_Texture(x, ref y, decalIndex, decalInfo, decal);
                }
                else
                {
                    DrawDecalEditUI_Mask(x, ref y, decalIndex, decalInfo, decal);
                }
            }
            if (decalInfo.PaintMode == DecalPaintMode.Erase)
            {
                DrawDecalEditUI_Mask_Erase(x, ref y, decalIndex, decalInfo, decal);
            }

			GUI.DragWindow();
        }

        private void DrawDecalEditUI_Header(int x, ref int y, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            {
                var lineX = x;
                var buttonsCount = 3;
                var backButtonWidth = boxWidth - (buttonHeight + smallMargin) * buttonsCount;
                if (GUI.Button(new Rect(lineX, y, backButtonWidth, buttonHeight), "Back"))
                {
                    CurrentlyEditedDecalIndex = default;
                    DestroyTransformHandle();
                }
                lineX += backButtonWidth + smallMargin;

                if (decalInfo.PaintMode == DecalPaintMode.Paint)
                {
                    if (GUI.Button(new Rect(lineX, y, buttonHeight, buttonHeight), CamoEditorResources.CopyPaintIcon))
                    {
                        CopiedDecalInfo = new(decalInfo.GetCopy());
                    }
                    lineX += buttonHeight + smallMargin;

                    if (GUI.Button(new Rect(lineX, y, buttonHeight, buttonHeight), CamoEditorResources.PastePaintIcon))
                    {
                        if (CopiedDecalInfo.Some(out var copiedDecalInfo) && copiedDecalInfo.PaintMode == DecalPaintMode.Paint)
                        {
                            Plugin.ApplyPaintInfo(ItemId, decalIndex, decalInfo, copiedDecalInfo);
                            SyncTransformHandle();
                        }
                    }
                    lineX += buttonHeight + smallMargin;
                }
                if (decalInfo.PaintMode == DecalPaintMode.Erase)
                {
                    if (GUI.Button(new Rect(lineX, y, buttonHeight, buttonHeight), CamoEditorResources.CopyEraserIcon))
                    {
                        CopiedDecalInfo = new(decalInfo.GetCopy());
                    }
                    lineX += buttonHeight + smallMargin;

                    if (GUI.Button(new Rect(lineX, y, buttonHeight, buttonHeight), CamoEditorResources.PasteEraserIcon))
                    {
                        if (CopiedDecalInfo.Some(out var copiedDecalInfo) && copiedDecalInfo.PaintMode == DecalPaintMode.Erase)
                        {
                            Plugin.ApplyEraserInfo(ItemId, decalIndex, decalInfo, copiedDecalInfo);
                            SyncTransformHandle();
                        }
                    }
                    lineX += buttonHeight + smallMargin;
                }

                var mirrorModeIcon = decalInfo.MirrorMode switch
                {
                    DecalMirrorMode.Disabled => CamoEditorResources.MirrorDisabled,
                    DecalMirrorMode.Enabled => CamoEditorResources.MirrorEnabled,
                    DecalMirrorMode.EnabledNoFlip => CamoEditorResources.MirrorEnabledNoFilp,
                };
                if (GUI.Button(new Rect(lineX, y, buttonHeight, buttonHeight), mirrorModeIcon))
                {
                    Plugin.SwitchMirrorMode(ItemId, decalIndex, decalInfo);
                }
            }
            y += buttonHeight + mediumMargin;


            decalInfo.Name = GUI.TextField(new Rect(x, y, boxWidth, buttonHeight), decalInfo.Name, maxDecalNameLength, CamoEditorStyle.TextFieldStyle);
            if (string.IsNullOrWhiteSpace(decalInfo.Name))
            {
                GUI.Label(new Rect(x + CamoEditorStyle.TextFieldStyle.contentOffset.x + 3, y, boxWidth, buttonHeight), "enter decal name (optional)", CamoEditorStyle.LabelStyleName);
            }
            y += buttonHeight + mediumMargin;
        }

        private void DrawDecalEditUI_Transform(int x, ref int y, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditPositionIcon))
            {
                SetupTransformHandle(HandleType.Position, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalPositionX.Get(decal.DecalTransform.localPosition.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalPositionY.Get(decal.DecalTransform.localPosition.y), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalPositionZ.Get(decal.DecalTransform.localPosition.z), CamoEditorStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - thirdBoxWidthButton, y, thirdBoxWidthButton, buttonHeight), "mirror left/right"))
            {
                Plugin.MirrorLeftRight(ItemId, decalIndex, decalInfo);
                SyncTransformHandle();
            }
            y += buttonHeight + smallMargin;


            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditRotationIcon))
            {
                SetupTransformHandle(HandleType.Rotation, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalEulerAnglesX.Get(decal.DecalTransform.localEulerAngles.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalEulerAnglesY.Get(decal.DecalTransform.localEulerAngles.y), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalEulerAnglesZ.Get(decal.DecalTransform.localEulerAngles.z), CamoEditorStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - thirdBoxWidthButton, y, sixthBoxWidthButton, buttonHeight), "round"))
            {
                Plugin.RoundLocalEulerAnglesToDegree(ItemId, decalIndex, decalInfo);
                SyncTransformHandle();
            }
            if (GUI.Button(new Rect(x + boxWidth - thirdBoxWidthButton + sixthBoxWidthButton + smallMargin, y, sixthBoxWidthButton, buttonHeight), "-90°Z"))
            {
                Plugin.RotateZ(ItemId, decalIndex, decalInfo, -90);
                SyncTransformHandle();
            }
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditScaleIcon))
            {
                SetupTransformHandle(HandleType.Scale, decalIndex, decalInfo, decal);
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
                Plugin.FixScale(ItemId, decalIndex, decalInfo);
                SyncTransformHandle();
            }
            y += buttonHeight + smallMargin;

            {
                var lineX = x;
                if (GUI.Button(new Rect(lineX, y, thirdBoxWidthButton, buttonHeight), "flip horz"))
                {
                    Plugin.FlipHorizontally(ItemId, decalIndex, decalInfo);
                    SyncTransformHandle();
                }
                lineX += thirdBoxWidthButton + smallMargin;

                if (GUI.Button(new Rect(lineX, y, thirdBoxWidthButton, buttonHeight), "flip vert"))
                {
                    Plugin.FlipVertically(ItemId, decalIndex, decalInfo);
                    SyncTransformHandle();
                }
                lineX += thirdBoxWidthButton + smallMargin;

                if (GUI.Button(new Rect(lineX, y, thirdBoxWidthButton, buttonHeight), "flip dir"))
                {
                    Plugin.FlipDirection(ItemId, decalIndex, decalInfo);
                    SyncTransformHandle();
                }
            }
            y += buttonHeight + bigMargin;
        }

        private void DrawDecalEditUI_Texture(int x, ref int y, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            var textureData = Plugin.GetTextureData(decalInfo.Texture);

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditTextureUVOffsetIcon))
            {
                SetupTransformHandle(HandleType.TextureOffset, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.TextureUVx.Get(decalInfo.TextureUV.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.TextureUVy.Get(decalInfo.TextureUV.y), CamoEditorStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - halfBoxWidthButton, y, fourthBoxWidthButton, buttonHeight), "reset"))
            {
                Plugin.ResetTextureUVOffset(ItemId, decalIndex, decalInfo);
                SyncTransformHandle();
            }
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditTextureUVAngleIcon))
            {
                SetupTransformHandle(HandleType.TextureAngle, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.TextureAngle.Get(decalInfo.TextureAngle), CamoEditorStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - halfBoxWidthButton, y, fourthBoxWidthButton, buttonHeight), "reset"))
            {
                Plugin.ResetTextureAngle(ItemId, decalIndex, decalInfo);
                SyncTransformHandle();
            }
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditTextureUVTilingIcon))
            {
                SetupTransformHandle(HandleType.TextureTiling, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.TextureUVz.Get(decalInfo.TextureUV.z), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.TextureUVw.Get(decalInfo.TextureUV.w), CamoEditorStyle.LabelStyleName);
            }
            {
                var valueX = x + boxWidth - halfBoxWidthButton;
                if (GUI.Button(new Rect(valueX, y, fourthBoxWidthButton, buttonHeight), "reset"))
                {
                    Plugin.ResetTextureUVScale(ItemId, decalIndex, decalInfo);
                    SyncTransformHandle();
                }
                valueX += fourthBoxWidthButton + smallMargin;

                if (GUI.Button(new Rect(valueX, y, fourthBoxWidthButton, buttonHeight), "fix UV"))
                {
                    Plugin.FixTextureUV(ItemId, decalIndex, decalInfo);
                    SyncTransformHandle();
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
                        Plugin.ApplyColor(ItemId, decalIndex);
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
                    Plugin.ApplyColor(ItemId, decalIndex);
                }
                GUI.Label(new Rect(valueX, opacityY, longFieldWidth, buttonHeight), Strings.ColorA.Get(decalInfo.ColorHSVA.w), CamoEditorStyle.LabelStyleValue);


                GUI.Label(new Rect(labelX, maxAngleY, nameWidth, buttonHeight), "Max Angle:", CamoEditorStyle.LabelStyleName);
                var newMaxAngle = GUI.HorizontalSlider(new Rect(sliderX, maxAngleY + 11, sliderWidth, buttonHeight), decalInfo.MaxAngle, 0f, 1f);
                if (newMaxAngle != decalInfo.MaxAngle)
                {
                    decalInfo.MaxAngle = newMaxAngle;
                    Plugin.ApplyMaxAngle(ItemId, decalIndex);
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

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            DecalTypeMenu = (DecalTextureType)GUI.Toolbar(new Rect(x, y, boxWidth, buttonHeight), (int)DecalTypeMenu, CamoEditorResources.DecalTypesToolbar);
            y += buttonHeight + smallMargin;

            DrawAllTextures(x, y, decalIndex, decalInfo, decal, DecalTypeMenu, maxTextureIconsVisibleHeight);
        }

        private void DrawDecalEditUI_Mask(int x, ref int y, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            var maskData = Plugin.GetTextureData(decalInfo.Mask);

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditMaskUVOffsetIcon))
            {
                SetupTransformHandle(HandleType.MaskOffset, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.MaskUVx.Get(decalInfo.MaskUV.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.MaskUVy.Get(decalInfo.MaskUV.y), CamoEditorStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - halfBoxWidthButton, y, fourthBoxWidthButton, buttonHeight), "reset"))
            {
                Plugin.ResetMaskUVOffset(ItemId, decalIndex, decalInfo);
                SyncTransformHandle();
            }
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditMaskUVAngleIcon))
            {
                SetupTransformHandle(HandleType.MaskAngle, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.MaskAngle.Get(decalInfo.MaskAngle), CamoEditorStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - halfBoxWidthButton, y, fourthBoxWidthButton, buttonHeight), "reset"))
            {
                Plugin.ResetMaskAngle(ItemId, decalIndex, decalInfo);
                SyncTransformHandle();
            }
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditMaskUVTilingIcon))
            {
                SetupTransformHandle(HandleType.MaskTiling, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.MaskUVz.Get(decalInfo.MaskUV.z), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.MaskUVw.Get(decalInfo.MaskUV.w), CamoEditorStyle.LabelStyleName);
            }
            {
                var valueX = x + boxWidth - halfBoxWidthButton;
                if (GUI.Button(new Rect(valueX, y, fourthBoxWidthButton, buttonHeight), "reset"))
                {
                    Plugin.ResetMaskUVScale(ItemId, decalIndex, decalInfo);
                    SyncTransformHandle();
                }
            }
            y += buttonHeight + mediumMargin;

            {
                GUI.Button(new Rect(x, y, iconSize, iconSize), maskData.Preview);

                var labelX = x + iconSize + smallMargin + 12;
                GUI.Label(new Rect(labelX, y + 1, 256, buttonHeight), decalInfo.Mask, CamoEditorStyle.TextureNameStyle);

                y += iconSize + bigMargin;
            }

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            DrawAllTextures(x, y, decalIndex, decalInfo, decal, DecalTextureType.Mask, maxMaskIconsVisibleHeight);
        }

        private void DrawDecalEditUI_Mask_Erase(int x, ref int y, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            var maskData = Plugin.GetTextureData(decalInfo.Mask);

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditMaskUVOffsetIcon))
            {
                SetupTransformHandle(HandleType.MaskOffset, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.MaskUVx.Get(decalInfo.MaskUV.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.MaskUVy.Get(decalInfo.MaskUV.y), CamoEditorStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - halfBoxWidthButton, y, fourthBoxWidthButton, buttonHeight), "reset"))
            {
                Plugin.ResetMaskUVOffset(ItemId, decalIndex, decalInfo);
                SyncTransformHandle();
            }
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditMaskUVAngleIcon))
            {
                SetupTransformHandle(HandleType.MaskAngle, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.MaskAngle.Get(decalInfo.MaskAngle), CamoEditorStyle.LabelStyleName);
            }
            if (GUI.Button(new Rect(x + boxWidth - halfBoxWidthButton, y, fourthBoxWidthButton, buttonHeight), "reset"))
            {
                Plugin.ResetMaskAngle(ItemId, decalIndex, decalInfo);
                SyncTransformHandle();
            }
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditMaskUVTilingIcon))
            {
                SetupTransformHandle(HandleType.MaskTiling, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.MaskUVz.Get(decalInfo.MaskUV.z), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.MaskUVw.Get(decalInfo.MaskUV.w), CamoEditorStyle.LabelStyleName);
            }
            {
                var valueX = x + boxWidth - halfBoxWidthButton;
                if (GUI.Button(new Rect(valueX, y, fourthBoxWidthButton, buttonHeight), "reset"))
                {
                    Plugin.ResetMaskUVScale(ItemId, decalIndex, decalInfo);
                    SyncTransformHandle();
                }
            }
            y += buttonHeight + bigMargin;

            {
                var sliderWidth = 212;

                var labelX = x;
                var sliderX = labelX + nameWidth + smallMargin - 42;
                var valueX = sliderX + sliderWidth + smallMargin;

                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "Max Alpha:", CamoEditorStyle.LabelStyleName);
                var newMaxAngle = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), decalInfo.MaxAngle, 0f, 1f);
                if (newMaxAngle != decalInfo.MaxAngle)
                {
                    decalInfo.MaxAngle = newMaxAngle;
                    Plugin.ApplyMaxAngle(ItemId, decalIndex);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.MaxAngle.Get(decalInfo.MaxAngle), CamoEditorStyle.LabelStyleValue);

                y += buttonHeight + bigMargin;
            }

            {
                GUI.Button(new Rect(x, y, iconSize, iconSize), maskData.Preview);

                var labelX = x + iconSize + smallMargin + 12;
                GUI.Label(new Rect(labelX, y + 1, 256, buttonHeight), decalInfo.Mask, CamoEditorStyle.TextureNameStyle);

                y += iconSize + bigMargin;
            }

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            DrawAllTextures(x, y, decalIndex, decalInfo, decal, DecalTextureType.Mask, maxEraseMaskIconsVisibleHeight);
        }

        public void SetupTransformHandle(HandleType handleType)
        {
            if (CurrentlyEditedDecalIndex.Some(out var decalIndex))
            {
                var (decalInfo, decal) = Plugin.GetDecal(ItemId, InstanceID, decalIndex);
                SetupTransformHandle(handleType, decalIndex, decalInfo, decal);
            }
        }

        private void SetupTransformHandle(HandleType handleType, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
            if (TransformHandle)
            {
                ForceOnEndedDraggingHandle();
                DestroyTransformHandle();
            }

            var handle = CreateTransformHandle(handleType, decalIndex, decalInfo, decal);
            var cameraProvider = new DefaultCameraProvider(Camera);
            TransformHandle = RuntimeTransformHandle.Create(handle, decal.DecalTransform.parent, cameraProvider, 1 << LayerMaskClass.WeaponPreview);
			TransformHelperClass.SetLayersRecursively(TransformHandle.gameObject, LayerMaskClass.WeaponPreview);
        }

        public ITransformHandle CreateTransformHandle(HandleType handleType, int decalIndex, DecalInfo decalInfo, Decal decal)
        {
     		if (handleType == HandleType.Position)
			{
                return new PositionHandle(Plugin, ItemId, decalIndex, decalInfo, decal, CamoEditorResources.PositionHandleShader);
			}
			if (handleType == HandleType.Rotation)
			{
                return new RotationHandle(Plugin, ItemId, decalIndex, decalInfo, decal, CamoEditorResources.RotationHandleShader);
			}
			if (handleType == HandleType.Scale)
			{
                return new ScaleHandle(Plugin, ItemId, decalIndex, decalInfo, decal, CamoEditorResources.ScaleHandleShader);
			}
            if (handleType == HandleType.TextureOffset)
            {
                return new TextureOffsetHandle(Plugin, ItemId, decalIndex, decalInfo, decal, CamoEditorResources.PositionHandleShader);
            }
            if (handleType == HandleType.TextureAngle)
            {
                return new TextureAngleHandle(Plugin, ItemId, decalIndex, decalInfo, decal, CamoEditorResources.RotationHandleShader);
            }
            if (handleType == HandleType.TextureTiling)
            {
                return new TextureTilingHandle(Plugin, ItemId, decalIndex, decalInfo, decal, CamoEditorResources.ScaleHandleShader);
            }
            if (handleType == HandleType.MaskOffset)
            {
                return new MaskOffsetHandle(Plugin, ItemId, decalIndex, decalInfo, decal, CamoEditorResources.PositionHandleShader);
            }
            if (handleType == HandleType.MaskAngle)
            {
                return new MaskAngleHandle(Plugin, ItemId, decalIndex, decalInfo, decal, CamoEditorResources.RotationHandleShader);
            }
            if (handleType == HandleType.MaskTiling)
            {
                return new MaskTilingHandle(Plugin, ItemId, decalIndex, decalInfo, decal, CamoEditorResources.ScaleHandleShader);
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

        private void DrawAllTextures(int x, int y, int decalIndex, DecalInfo decalInfo, Decal decal, DecalTextureType decalTextureType, int maxIconsVisibleHeight)
        {
            var texturesDirectory = Plugin.GetTexturesDirectory(decalTextureType);

            var (totalHeight, visibleHeight) = CalculateTexturesDirectoryHeight(texturesDirectory, maxIconsVisibleHeight);
            var totalRect = new Rect(x, y, boxWidth, totalHeight);
            var visibleRect = new Rect(x, y, boxWidth + 16, visibleHeight);

            ref var scrollPosition = ref GetScrollPosition(decalTextureType);
            DrawScrollBar(x + boxWidth + 5, y, totalHeight, visibleHeight, scrollPosition);
            scrollPosition = GUI.BeginScrollView(visibleRect, scrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

            DrawAllTextures(ref x, ref y, decalIndex, decalInfo, decal, texturesDirectory, drawName: false);

            GUI.EndScrollView();
        }

        private void DrawAllTextures(ref int x, ref int y, int decalIndex, DecalInfo decalInfo, Decal decal, TexturesDirectory texturesDirectory, bool drawName = true)
        {
            if (drawName)
            {
                if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), texturesDirectory.Name, CamoEditorStyle.DirectoryButtonStyle))
                {
                    texturesDirectory.IsClosed = !texturesDirectory.IsClosed;
                }

                var iconSize = 20;
                var iconMargin = (buttonHeight - iconSize) / 2;
                var icon = texturesDirectory.IsClosed ? CamoEditorResources.MoveUpIcon : CamoEditorResources.MoveDownIcon;
                GUI.DrawTexture(new Rect(x + boxWidth - smallMargin - buttonHeight + iconMargin, y + iconMargin, iconSize, iconSize), icon);

                y += buttonHeight + smallMargin;
            }

            if (texturesDirectory.IsClosed)
            {
                return;
            }

            foreach (var subDirectory in texturesDirectory.Directories)
            {
                DrawAllTextures(ref x, ref y, decalIndex, decalInfo, decal, subDirectory);
            }

            var textures = texturesDirectory.Textures;
            for (var i = 0; i < textures.Length; i++)
            {
                var textureName = textures[i];
                var textureData = Plugin.GetTextureData(textureName);

                var ix = i % iconColumns;
                var iy = i / iconColumns;

                var xi = x + ix * (iconSize + smallMargin);
                var yi = y + iy * (iconSize + smallMargin);

                if (GUI.Button(new Rect(xi, yi, iconSize, iconSize), textureData.Preview))
                {
                    var e = Event.current;
                    if (e.button == 0) // left click
                    {
                        if (textureData.Type == DecalTextureType.Camo && decalInfo.Texture != textureName)
                        {
                            Plugin.ChangeTexture(ItemId, decalIndex, decalInfo, textureName);
                            Plugin.FixTextureUV(ItemId, decalIndex, decalInfo);
                            SyncTransformHandle();
                        }
                        if (textureData.Type == DecalTextureType.Sticker && decalInfo.Texture != textureName)
                        {
                            Plugin.ChangeTexture(ItemId, decalIndex, decalInfo, textureName);
                            Plugin.FixScale(ItemId, decalIndex, decalInfo);
                            SyncTransformHandle();
                        }
                        if (textureData.Type == DecalTextureType.Mask && decalInfo.Mask != textureName)
                        {
                            Plugin.ChangeMask(ItemId, decalIndex, decalInfo, textureName);
                        }
                    }
                    if (e.button == 1) // right click
                    {
                        Plugin.ToggleFavouriteTexture(textureName);
                    }
                }
                if (textureData.Format == DecalTextureFormat.Video)
                {
                    GUI.DrawTexture(new Rect(xi + smallMargin, yi + smallMargin, smallIconSize, smallIconSize), CamoEditorResources.PlayIcon);
                }
                if (Plugin.IsFavouriteTexture(textureName))
                {
                    GUI.DrawTexture(new Rect(xi + iconSize - 3 - smallIconSize, yi + iconSize - 3 - smallIconSize, smallIconSize, smallIconSize), CamoEditorResources.FavouriteIcon);
                }
            }

            var totalRows = DivideIntCeil(textures.Length, iconColumns);
            y += (iconSize + smallMargin) * totalRows;
        }

        public void Destroy()
        {
            if (RuntimeGizmos)
            {
                GameObject.Destroy(RuntimeGizmos);
            }

            DestroyTransformHandle();
        }

        public static int DivideIntCeil(int left, int right)
        {
            return (left + right - 1) / right;
        }

        public static int DivideIntRound(int left, int right)
        {
            return (left + right / 2) / right;
        }

        public static (int totalHeight, int visibleHeight) CalculateScrollViewTotalAndVisibleHeight(int totalCount, int maxCount, int itemHeight, int separatorHeight)
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
        public static void DrawScrollBar(int x, int y, int totalHeight, int visibleHeight, Vector2 scrollPosition, bool drawBackground = true)
        {
            if (totalHeight > visibleHeight)
            {
                var handleHeight = visibleHeight * visibleHeight / (float)totalHeight;
                var handlePositionT = scrollPosition.y / (float)totalHeight;
                var handlePosition = handlePositionT * visibleHeight;
                if (drawBackground)
                {
                    DrawColor(new Rect(x, y, scrollBarWidth, visibleHeight), separatorColor);
                }
                DrawColor(new Rect(x, y + handlePosition, scrollBarWidth, handleHeight), scrollBarHandleColor);
            }
        }

        public void DrawDecalProjectionBox()
        {
            if (CurrentlyEditedDecalIndex.Some(out var currentlyEditedDecalIndex) && RuntimeGizmos)
            {
                var (decalInfo, decal) = Plugin.GetDecal(ItemId, InstanceID, currentlyEditedDecalIndex);
                var decalsRoot = Plugin.GetDecalsRoot(ItemId, InstanceID);
				switch (decalInfo.MirrorMode)
				{
					case DecalMirrorMode.Disabled:
					{
						DrawDecal(decal, RuntimeGizmos);
						break;
					}
					case DecalMirrorMode.Enabled:
					{
						DrawDecal(decal, RuntimeGizmos);
						DrawDecalMirrored(decal, decalsRoot, RuntimeGizmos);
						break;
					}
					case DecalMirrorMode.EnabledNoFlip:
					{
						DrawDecal(decal, RuntimeGizmos);
						DrawDecalMirrored(decal, decalsRoot, RuntimeGizmos);
						break;
					}
				}
            }
        }

		private void DrawDecal(Decal decal, RuntimeGizmos runtimeGizmos)
		{
			DrawDecal(decal.DecalTransform.localToWorldMatrix, runtimeGizmos);
		}

		private void DrawDecalMirrored(Decal decal, Transform decalRoot, RuntimeGizmos runtimeGizmos)
		{
			var localPosition = decal.DecalTransform.localPosition;
			var localRotation = decal.DecalTransform.localRotation;
			var localScale = decal.DecalTransform.localScale;

            // horizontal flip is irrelevant in wireframe
			Plugin.MirrorLeftRight(ref localPosition, ref localRotation, ref localScale);

			var localMatrix = Matrix4x4.TRS(localPosition, localRotation, localScale);
			var localToWorldMatrix = decalRoot.localToWorldMatrix * localMatrix;

			DrawDecal(localToWorldMatrix, runtimeGizmos);
		}

        private void DrawDecal(in Matrix4x4 localToWorldMatrix, RuntimeGizmos runtimeGizmos)
        {
			// its easier to accurately place decal when
			// its transform handle is located on the face
			// of projector volume, instead of geometric center.

			var offset = new Vector3(0, -0.5f, 0);
			var resultMatrix = localToWorldMatrix * Matrix4x4.Translate(offset);
            RuntimeGizmos.Cubes.Add(resultMatrix);
        }
    }
}
