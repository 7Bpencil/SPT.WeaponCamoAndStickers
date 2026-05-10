//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

// TODO we need presets both for item template and different color swatches

namespace SevenBoldPencil.ChangeEquipmentColor
{
    public class CamoEditorResources
    {
        public Texture2D MainIcon;
        public Texture2D ClosedIcon;
        public Texture2D OpenedIcon;
        public Texture2D MoveUpIcon;
        public Texture2D MoveDownIcon;
        public Texture2D EditPositionIcon;
        public Texture2D EditRotationIcon;
        public Texture2D EditScaleIcon;
        public Texture2D EditTextureUVOffsetIcon;
        public Texture2D EditTextureUVAngleIcon;
        public Texture2D EditTextureUVTilingIcon;
        public Texture2D EditMaskUVOffsetIcon;
        public Texture2D EditMaskUVAngleIcon;
        public Texture2D EditMaskUVTilingIcon;
        public Texture2D DuplicateIcon;
        public Texture2D CopyIcon;
        public Texture2D PasteIcon;
        public Texture2D DeleteIcon;
        public Texture2D SaveIcon;
        public Texture2D SaveErrorIcon;
        public Texture2D ColorWheelHSV;
        public Texture2D PlayIcon;
        public Texture2D HiddenIcon;
        public Texture2D VisibleIcon;
        public Texture2D MirrorDisabled;
        public Texture2D MirrorEnabled;
        public Texture2D MirrorEnabledNoFilp;
        public Texture2D Reset;

        public CamoEditorResources(AssetBundle bundle)
        {
            MainIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/hsv-circle-icon.png");
            ClosedIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/closed-arrow.png");
            OpenedIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/opened-arrow.png");
            MoveUpIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/up-arrow.png");
            MoveDownIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/down-arrow.png");
            EditPositionIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/Move-Icon.png");
            EditRotationIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/Rotate-Icon.png");
            EditScaleIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/Scale-Icon.png");
            EditTextureUVOffsetIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/UV-Texture-Move-Icon.png");
            EditTextureUVAngleIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/UV-Texture-Rotate-Icon.png");
            EditTextureUVTilingIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/UV-Texture-Scale-Icon.png");
            EditMaskUVOffsetIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/UV-Mask-Move-Icon.png");
            EditMaskUVAngleIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/UV-Mask-Rotate-Icon.png");
            EditMaskUVTilingIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/UV-Mask-Scale-Icon.png");
            DuplicateIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/duplicate.png");
            CopyIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/copy.png");
            PasteIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/paste.png");
            DeleteIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/bin.png");
            SaveIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/diskette.png");
            SaveErrorIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/diskette-error.png");
            ColorWheelHSV = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/hsv-circle.png");
            PlayIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/play-icon.png");
            HiddenIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/hidden.png");
            VisibleIcon = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/visible.png");
            MirrorDisabled = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/mirror-off.png");
            MirrorEnabled = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/mirror-on.png");
            MirrorEnabledNoFilp = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/mirror-on-no-flip.png");
            Reset = bundle.LoadAsset<Texture2D>("Assets/ChangeEquipmentColor/Icons/undo.png");
        }
    }

    public class CamoEditorStyle
    {
        public GUIStyle LabelStyleName;
        public GUIStyle TextureNameStyle;
        public GUIStyle LabelStyleValue;
        public GUIStyle TextFieldStyle;
		public GUIStyle ColorPickerButtonStyle;
        public GUIStyle DirectoryButtonStyle;
        public GUIStyle MaterialNameStyle;

        public CamoEditorStyle(GUISkin currentSkin)
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

            DirectoryButtonStyle = new(currentSkin.button)
            {
                alignment = TextAnchor.MiddleLeft
            };

            MaterialNameStyle = new(currentSkin.label)
            {
                alignment = TextAnchor.MiddleCenter,
            };
        }
    }

    public class CamoEditor
    {
        public Plugin Plugin;
        public CamoEditorResources CamoEditorResources;
        public CamoEditorStyle CamoEditorStyle;
        public string ItemId;
        public int InstanceID;
        public ItemWithDecals ItemWithDecals;
        public Dictionary<string, MaterialInfo> OriginalMaterials;
        public bool IsOpened;
        public Vector2 MaterialsScrollPosition;
        public Option<string> CurrentlyEditedOverride;
		public Rect WindowRect;

        // brace for imGUI shitshow

        public const int iconColumns = 5;
        public const int maxDecalsVisibleWhenPresetsAreNotOpened = 10;
        public const int maxDecalsVisibleWhenPresetsAreOpened = 6;
        public const int maxPresetsVisible = 9;
        public const int maxPresetNameLength = 25;
        public const int maxDecalNameLength = 30;
        public const int maxMaterialsCount = 10;

        public const int smallMargin = 4;
        public const int mediumMargin = 8;
        public const int bigMargin = 14;

        public const int startX = 10;
        public const int startY = 10;
        public const int windowWidth = bigMargin + (iconSize + smallMargin) * iconColumns - smallMargin + bigMargin;
        public const int buttonHeight = 32;
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
                if (CurrentlyEditedOverride.HasValue)
                {
                    WindowRect.height = CalculateMaterialEditWindowHeight();
                    WindowRect = GUI.Window(1, WindowRect, DrawMaterialEditUI, GUIContent.none);

                    var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                    GUI.Window(2, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);
                }
                else
                {
                    WindowRect.height = CalculateMaterialsWindowHeight();
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

            GUI.matrix = originalMatrix;
        }

        private int CalculateMaterialsWindowHeight()
        {
            var totalMaterialsCount = ItemWithDecals.Overrides.Count;
            var (_, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(totalMaterialsCount, maxMaterialsCount, buttonHeight, smallMargin);
            return
                bigMargin +
                visibleHeight + bigMargin; // materials
        }

        private int CalculateMaterialEditWindowHeight()
        {
            return
                bigMargin +
                buttonHeight + mediumMargin + // back button
                buttonHeight + mediumMargin + // material name
                hsCircleDiameter + bigMargin + // color swatches + picker
                buttonHeight + smallMargin + // hue
                buttonHeight + smallMargin + // saturation
                buttonHeight + smallMargin + // value
                buttonHeight + smallMargin + // glossness
                buttonHeight + bigMargin - 7; // specularness
        }

        private void DrawOpenedWindow(int windowID)
		{
            DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var materialsInfoOption = Plugin.GetMaterialsInfo(ItemId);

            var x = bigMargin;
            var y = bigMargin;

            {
                var materialsY = y;

                var (totalHeight, visibleHeight) = CalculateScrollViewTotalAndVisibleHeight(ItemWithDecals.Overrides.Count, maxMaterialsCount, buttonHeight, smallMargin);
                var totalRect = new Rect(x, materialsY, boxWidth, totalHeight);
                var visibleRect = new Rect(x, materialsY, boxWidth + 16, visibleHeight);

                DrawScrollBar(x + boxWidth + 5, materialsY, totalHeight, visibleHeight, MaterialsScrollPosition);
                MaterialsScrollPosition = GUI.BeginScrollView(visibleRect, MaterialsScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

                var overrideButtonWidth = boxWidth - buttonHeight - smallMargin;
                var resetX = x + overrideButtonWidth + smallMargin;
                foreach (var materialName in ItemWithDecals.Overrides.Keys)
                {
                    if (materialsInfoOption.Some(out var materialsInfo) && materialsInfo.Materials.ContainsKey(materialName))
                    {
                        if (GUI.Button(new Rect(x, materialsY, overrideButtonWidth, buttonHeight), materialName))
                        {
                            CurrentlyEditedOverride = new(materialName);
                        }
                        if (GUI.Button(new Rect(resetX, materialsY, buttonHeight, buttonHeight), CamoEditorResources.Reset))
                        {
                            Plugin.ResetMaterial(ItemId, materialName);
                        }
                    }
                    else
                    {
                        if (GUI.Button(new Rect(x, materialsY, boxWidth, buttonHeight), materialName))
                        {
                            Plugin.OverrideMaterial(ItemWithDecals, OriginalMaterials, ItemId, InstanceID, materialName);
                            CurrentlyEditedOverride = new(materialName);
                        }
                    }
                    materialsY += buttonHeight + smallMargin;
                }

                GUI.EndScrollView();

                y += visibleHeight;
                y += bigMargin;
            }

			GUI.DragWindow();
        }

        private void DrawMaterialEditUI(int windowID)
        {
            DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var materialName = CurrentlyEditedOverride.Value;
            var materialInfo = Plugin.GetMaterialInfo(ItemId, materialName);

            var x = bigMargin;
            var y = bigMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                CurrentlyEditedOverride = default;
            }
            y += buttonHeight + mediumMargin;

            GUI.Label(new Rect(x, y, boxWidth, buttonHeight), materialName, CamoEditorStyle.MaterialNameStyle);
            y += buttonHeight + mediumMargin;

            {
                var colorPickerX = x + boxWidth - hsCircleDiameter;
                var hsCircleRect = new Rect(colorPickerX, y, hsCircleDiameter, hsCircleDiameter);
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

                    materialInfo.ColorHSV.x = hue;
                    materialInfo.ColorHSV.y = saturation;
                    Plugin.ApplyOverrides(ItemId, materialName);
                }
                y += hsCircleDiameter + bigMargin;
            }

            {
                var sliderWidth = 224;

                var labelX = x;
                var sliderX = labelX + nameWidth + smallMargin - 42;
                var valueX = sliderX + sliderWidth + smallMargin;

                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "Hue:", CamoEditorStyle.LabelStyleName);
                var newHue = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.ColorHSV.x, 0f, 1f);
                if (newHue != materialInfo.ColorHSV.x)
                {
                    materialInfo.ColorHSV.x = newHue;
                    Plugin.ApplyOverrides(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.ColorHSV.x:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "Saturation:", CamoEditorStyle.LabelStyleName);
                var newSaturation = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.ColorHSV.y, 0f, 1f);
                if (newSaturation != materialInfo.ColorHSV.y)
                {
                    materialInfo.ColorHSV.y = newSaturation;
                    Plugin.ApplyOverrides(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.ColorHSV.y:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "Value:", CamoEditorStyle.LabelStyleName);
                var newValue = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.ColorHSV.z, 0f, 1f);
                if (newValue != materialInfo.ColorHSV.z)
                {
                    materialInfo.ColorHSV.z = newValue;
                    Plugin.ApplyOverrides(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.ColorHSV.z:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "Glossness:", CamoEditorStyle.LabelStyleName);
                var newGlossness = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.Glossness, 0.01f, 10f);
                if (newGlossness != materialInfo.Glossness)
                {
                    materialInfo.Glossness = newGlossness;
                    Plugin.ApplyOverrides(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.Glossness:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "Specularness:", CamoEditorStyle.LabelStyleName);
                var newSpecularness = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.Specularness, 0.01f, 10f);
                if (newSpecularness != materialInfo.Specularness)
                {
                    materialInfo.Specularness = newSpecularness;
                    Plugin.ApplyOverrides(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.Specularness:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;
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

        public void Destroy()
        {

        }

    }
}
