//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.Common;
using SevenBoldPencil.WeaponCamoAndStickers;
using System;
using System.Collections.Generic;
using UnityEngine;

// TODO we need presets both for item template and different color swatches

using BigPlugin = SevenBoldPencil.WeaponCamoAndStickers.Plugin;
using BigCamoEditor = SevenBoldPencil.WeaponCamoAndStickers.CamoEditor;

namespace SevenBoldPencil.ChangeEquipmentColor
{
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
        public bool IsColorPickerOpened;
        public DecalTextureType DecalTypeMenu;
        public Vector2 CamosScrollPosition;
        public Vector2 StickersScrollPosition;
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
        public static readonly Rect colorPickerRect = new(0, 80, hsCircleDiameter + bigMargin * 2, hsCircleDiameter + bigMargin * 2);
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
            var uiScale = baseUIScale * BigPlugin.UIScale.Value;
            GUI.matrix = Matrix4x4.Scale(new(uiScale, uiScale, 1f));

            if (IsOpened)
            {
                if (CurrentlyEditedOverride.HasValue)
                {
                    WindowRect.height = CalculateMaterialEditWindowHeight();
                    WindowRect = GUI.Window(1, WindowRect, DrawMaterialEditUI, GUIContent.none);

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
            var (_, visibleHeight) = BigCamoEditor.CalculateScrollViewTotalAndVisibleHeight(totalMaterialsCount, maxMaterialsCount, buttonHeight, smallMargin);
            return
                bigMargin +
                visibleHeight + bigMargin; // materials
        }

        private int CalculateMaterialEditWindowHeight()
        {
            var texturesDirectory = BigPlugin.Instance.GetTexturesDirectory(DecalTypeMenu);
            var (_, visibleHeight) = BigCamoEditor.CalculateTexturesDirectoryHeight(texturesDirectory, maxEraseMaskIconsVisibleHeight);
            return
                bigMargin +
                buttonHeight + bigMargin + // back button
                buttonHeight + smallMargin + // hue
                buttonHeight + smallMargin + // saturation
                buttonHeight + smallMargin + // value
                buttonHeight + smallMargin + // glossness
                buttonHeight + smallMargin + // specularness
                buttonHeight + smallMargin + // texture uv x
                buttonHeight + smallMargin + // texture uv y
                buttonHeight + bigMargin + // texture uv scale
                iconSize + bigMargin + // icon
                smallMargin + bigMargin + // separator
                buttonHeight + smallMargin + // toolbar camos/stickers
                visibleHeight + bigMargin; // icons grid
        }

        private void DrawOpenedWindow(int windowID)
		{
            DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var materialsInfoOption = Plugin.GetMaterialsInfo(ItemId);

            var x = bigMargin;
            var y = bigMargin;

            {
                var materialsY = y;

                var (totalHeight, visibleHeight) = BigCamoEditor.CalculateScrollViewTotalAndVisibleHeight(ItemWithDecals.Overrides.Count, maxMaterialsCount, buttonHeight, smallMargin);
                var totalRect = new Rect(x, materialsY, boxWidth, totalHeight);
                var visibleRect = new Rect(x, materialsY, boxWidth + 16, visibleHeight);

                BigCamoEditor.DrawScrollBar(x + boxWidth + 5, materialsY, totalHeight, visibleHeight, MaterialsScrollPosition);
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
            y += buttonHeight + bigMargin;

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
                    Plugin.ApplyColor(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.ColorHSV.x:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "Saturation:", CamoEditorStyle.LabelStyleName);
                var newSaturation = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.ColorHSV.y, 0f, 1f);
                if (newSaturation != materialInfo.ColorHSV.y)
                {
                    materialInfo.ColorHSV.y = newSaturation;
                    Plugin.ApplyColor(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.ColorHSV.y:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "Value:", CamoEditorStyle.LabelStyleName);
                var newValue = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.ColorHSV.z, 0f, 1f);
                if (newValue != materialInfo.ColorHSV.z)
                {
                    materialInfo.ColorHSV.z = newValue;
                    Plugin.ApplyColor(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.ColorHSV.z:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "Gloss:", CamoEditorStyle.LabelStyleName);
                var newGlossness = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.Glossness, 0.01f, 10f);
                if (newGlossness != materialInfo.Glossness)
                {
                    materialInfo.Glossness = newGlossness;
                    Plugin.ApplyGlossness(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.Glossness:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "Specular:", CamoEditorStyle.LabelStyleName);
                var newSpecularness = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.Specularness, 0.01f, 10f);
                if (newSpecularness != materialInfo.Specularness)
                {
                    materialInfo.Specularness = newSpecularness;
                    Plugin.ApplySpecularness(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.Specularness:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "UV x:", CamoEditorStyle.LabelStyleName);
                var newUVz = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.TextureUV.z, -1f, 1f);
                if (newUVz != materialInfo.TextureUV.z)
                {
                    materialInfo.TextureUV.z = newUVz;
                    Plugin.ApplyTextureUV(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.TextureUV.z:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "UV y:", CamoEditorStyle.LabelStyleName);
                var newUVw = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.TextureUV.w, -1f, 1f);
                if (newUVw != materialInfo.TextureUV.w)
                {
                    materialInfo.TextureUV.w = newUVw;
                    Plugin.ApplyTextureUV(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.TextureUV.w:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "UV scale:", CamoEditorStyle.LabelStyleName);
                var newUVx = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), materialInfo.TextureUV.x, 0.5f, 2f);
                if (newUVx != materialInfo.TextureUV.x)
                {
                    materialInfo.TextureUV.x = newUVx;
                    materialInfo.TextureUV.y = newUVx;
                    Plugin.ApplyTextureUV(ItemId, materialName);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), $"{materialInfo.TextureUV.x:F3}", CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + bigMargin;
            }

            if (string.IsNullOrWhiteSpace(materialInfo.Texture))
            {
                GUI.Button(new Rect(x, y, iconSize, iconSize), "default");
            }
            else
            {
                var textureData = BigPlugin.Instance.GetTextureData(materialInfo.Texture);
                GUI.Button(new Rect(x, y, iconSize, iconSize), textureData.Preview);

                var labelX = x + iconSize + smallMargin + 12;
                GUI.Label(new Rect(labelX, y + 1, 256, buttonHeight), materialInfo.Texture, CamoEditorStyle.TextureNameStyle);
            }
            y += iconSize + bigMargin;

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            DecalTypeMenu = (DecalTextureType)GUI.Toolbar(new Rect(x, y, boxWidth, buttonHeight), (int)DecalTypeMenu, CamoEditorResources.DecalTypesToolbar);
            y += buttonHeight + smallMargin;

            DrawAllTextures(x, y, materialName, materialInfo, DecalTypeMenu, maxEraseMaskIconsVisibleHeight);

			GUI.DragWindow();
        }

        private void DrawAllTextures(int x, int y, string materialName, MaterialInfo materialInfo, DecalTextureType decalTextureType, int maxIconsVisibleHeight)
        {
            var texturesDirectory = BigPlugin.Instance.GetTexturesDirectory(decalTextureType);

            var (totalHeight, visibleHeight) = BigCamoEditor.CalculateTexturesDirectoryHeight(texturesDirectory, maxIconsVisibleHeight);
            var totalRect = new Rect(x, y, boxWidth, totalHeight);
            var visibleRect = new Rect(x, y, boxWidth + 16, visibleHeight);

            BigCamoEditor.DrawScrollBar(x + boxWidth + 5, y, totalHeight, visibleHeight, CamosScrollPosition);
            CamosScrollPosition = GUI.BeginScrollView(visibleRect, CamosScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

            DrawAllTextures(ref x, ref y, materialName, materialInfo, texturesDirectory, drawName: false);

            GUI.EndScrollView();
        }

        public void DrawAllTextures(ref int x, ref int y, string materialName, MaterialInfo materialInfo, TexturesDirectory texturesDirectory, bool drawName = true)
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
                DrawAllTextures(ref x, ref y, materialName, materialInfo, subDirectory);
            }

            var textures = texturesDirectory.Textures;
            for (var i = 0; i < textures.Length; i++)
            {
                var textureName = textures[i];
                var textureData = BigPlugin.Instance.GetTextureData(textureName);

                var ix = i % iconColumns;
                var iy = i / iconColumns;

                var xi = x + ix * (iconSize + smallMargin);
                var yi = y + iy * (iconSize + smallMargin);

                if (GUI.Button(new Rect(xi, yi, iconSize, iconSize), textureData.Preview))
                {
                    Plugin.ChangeTexture(ItemId, materialName, materialInfo, textureName);
                    // if (textureData.Type == DecalTextureType.Camo && decalInfo.Texture != textureName)
                    // {
                    //     Plugin.ChangeTexture(ItemId, decalIndex, decalInfo, textureName);
                    //     Plugin.FixTextureUV(ItemId, decalIndex, decalInfo);
                    //     SyncTransformHandle(decalInfo, decal);
                    // }
                    // if (textureData.Type == DecalTextureType.Sticker && decalInfo.Texture != textureName)
                    // {
                    //     Plugin.ChangeTexture(ItemId, decalIndex, decalInfo, textureName);
                    //     Plugin.FixScale(ItemId, decalIndex, decalInfo);
                    //     SyncTransformHandle(decalInfo, decal);
                    // }
                    // if (textureData.Type == DecalTextureType.Mask && decalInfo.Mask != textureName)
                    // {
                    //     Plugin.ChangeMask(ItemId, decalIndex, decalInfo, textureName);
                    // }
                }
                if (textureData.Format == DecalTextureFormat.Video)
                {
                    GUI.DrawTexture(new Rect(xi + smallMargin, yi + smallMargin, 16, 16), CamoEditorResources.PlayIcon);
                }
            }

            var totalRows = BigCamoEditor.DivideIntCeil(textures.Length, iconColumns);
            y += (iconSize + smallMargin) * totalRows;
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
            GUI.DrawTexture(new Rect(0, 0, mainIconWidth, openCloseButtonHeight), CamoEditorResources.MainIconMaterial, ScaleMode.StretchToFill);

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

        private void DrawColorPickerWindow(int windowID)
        {
            var materialName = CurrentlyEditedOverride.Value;
            var materialInfo = Plugin.GetMaterialInfo(ItemId, materialName);

            DrawColor(new Rect(0, 0, colorPickerRect.width, colorPickerRect.height), backgroundColor);

            var x = bigMargin;
            var y = bigMargin;

            var hsCircleRect = new Rect(x, y, hsCircleDiameter, hsCircleDiameter);
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
                Plugin.ApplyColor(ItemId, materialName);
            }
            y += hsCircleDiameter + bigMargin;
        }

        public void Destroy()
        {

        }

    }
}
