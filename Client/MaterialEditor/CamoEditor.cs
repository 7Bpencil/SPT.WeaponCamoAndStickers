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

using BigPlugin = SevenBoldPencil.WeaponCamoAndStickers.Plugin;
using BigCamoEditor = SevenBoldPencil.WeaponCamoAndStickers.CamoEditor;
using CamoEditorStyle = SevenBoldPencil.WeaponCamoAndStickers.CamoEditorStyle;
using CamoEditorResources = SevenBoldPencil.WeaponCamoAndStickers.CamoEditorResources;
using DecalTextureType = SevenBoldPencil.WeaponCamoAndStickers.DecalTextureType;
using TexturesDirectory = SevenBoldPencil.WeaponCamoAndStickers.TexturesDirectory;
using DecalTextureFormat = SevenBoldPencil.WeaponCamoAndStickers.DecalTextureFormat;
using static SevenBoldPencil.WeaponCamoAndStickers.CamoEditorConstants;

namespace SevenBoldPencil.MaterialEditor
{
    public class ColorStringCache
    {
        public StringCache<float> H = new(MaterialStringCache.SimpleFloatFormat);
        public StringCache<float> S = new(MaterialStringCache.SimpleFloatFormat);
        public StringCache<float> V = new(MaterialStringCache.SimpleFloatFormat);
    }

    public class Vector2StringCache
    {
        public StringCache<float> X = new(MaterialStringCache.SimpleFloatFormat);
        public StringCache<float> Y = new(MaterialStringCache.SimpleFloatFormat);
    }

    public class MaterialStringCache
    {
        public ColorStringCache Color = new();
        public ColorStringCache SpecColor = new();
        public ColorStringCache ReflectColor = new();

        public StringCache<float> SpecularCompensation = new(SimpleFloatFormat);
        public StringCache<float> Glossness = new(SimpleFloatFormat);
        public StringCache<float> Specularness = new(SimpleFloatFormat);

        public Vector2StringCache DefVals = new();
        public Vector2StringCache SpecVals = new();

        public StringCache<float> TextureUVx = new(SimpleFloatFormat);
        public StringCache<float> TextureUVy = new(SimpleFloatFormat);
        public StringCache<float> TextureUVscale = new(SimpleFloatFormat);

        public static string SimpleFloatFormat(float v) => $"{v:F3}";
    }

    public record CamoEditorItem
    (
        string Name,
        string ItemId,
        int InstanceID,
        ItemWithMaterials ItemWithMaterials,
        Dictionary<string, MaterialInfo> OriginalMaterials
    );

    public readonly record struct EditedOverride(int ItemIndex, string MaterialName);

    public class CamoEditor
    {
        public Plugin Plugin;
        public BigPlugin BigPlugin;
        public CamoEditorResources CamoEditorResources;
        public CamoEditorStyle CamoEditorStyle;
        public MaterialStringCache Strings = new();
        public List<CamoEditorItem> Items;
        public Dictionary<string, List<int>> ItemsDict;
        public bool IsOpened;
        public bool AreItemPresetsOpened;
        public bool AreMaterialPresetsOpened;
        public WeaponCamoAndStickers.TextField<string> CurrentItemPresetName = new(v => v, BigCamoEditor.TryParsePresetName);
        public WeaponCamoAndStickers.TextField<string> CurrentMaterialPresetName = new(v => v, BigCamoEditor.TryParsePresetName);
        public Vector2 ItemPresetsScrollPosition;
        public Vector2 MaterialPresetsScrollPosition;
        public Vector2 MaterialsScrollPosition;
        public Option<EditedOverride> CurrentlyEditedOverride;
        public HashSet<EditedOverride> LinkedOverrides = new();
        public bool IsColorPickerOpened_Color;
        public bool IsColorPickerOpened_SpecColor;
        public bool IsColorPickerOpened_ReflectColor;
        public bool AreAdvancedSettingsOpened;
        public DecalTextureType DecalTypeMenu = DecalTextureType.Camo;
        public WeaponCamoAndStickers.TextField<Vector3> TextField_Color = new(ColorExtensions.HSVtoHexRGB, ColorExtensions.HexRGBtoHSV);
        public WeaponCamoAndStickers.TextField<Vector3> TextField_SpecColor = new(ColorExtensions.HSVtoHexRGB, ColorExtensions.HexRGBtoHSV);
        public WeaponCamoAndStickers.TextField<Vector3> TextField_ReflectColor = new(ColorExtensions.HSVtoHexRGB, ColorExtensions.HexRGBtoHSV);
        public Vector2 CamosScrollPosition;
        public Vector2 StickersScrollPosition;
		public Rect WindowRect;

        // brace for imGUI shitshow

        public const int maxDecalsVisibleWhenPresetsAreNotOpened = 10;
        public const int maxDecalsVisibleWhenPresetsAreOpened = 6;
        public const int maxItemPresetsVisible = 24;
        public const int maxMaterialPresetsVisible = 22;

        public const int maxEraseMaskIconsVisibleHeight = 16 * (buttonHeight + smallMargin) - smallMargin;
        public const int maxMaterialsVisibleHeight = 25 * (buttonHeight + smallMargin) - smallMargin;

        public static readonly int colorPickerY_Color = 217;
        public static readonly int colorPickerY_SpecColor = 469;
        public static readonly int colorPickerY_ReflectColor = 721;
        public static readonly int colorPickerSize = hsCircleDiameter + bigMargin * 2;

        public static Rect GetDefaultWindowRect_Item()
        {
            return new(startX, startY, mainIconWidth, openCloseButtonHeight);
        }

        public static Rect GetDefaultWindowRect_Clothes()
        {
            return new(startX, 53, mainIconWidth, openCloseButtonHeight);
        }

        public void DrawWindow()
        {
            // we copy some styles from GUI.skin which can be accessed only from OnGUI call
            if (CamoEditorStyle == null)
            {
                CamoEditorStyle = new(GUI.skin);
            }

            var originalMatrix = GUI.matrix;
            GUI.matrix = BigCamoEditor.CalculateUIScale();

            if (IsOpened)
            {
                if (AreItemPresetsOpened)
                {
                    WindowRect.height = CalculateMaterialsWindowHeight_Presets();
                    WindowRect = GUI.Window(1, WindowRect, DrawOpenedWindow_Presets, GUIContent.none);

                    var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                    GUI.Window(2, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);
                }
                else
                {
                    if (CurrentlyEditedOverride.HasValue)
                    {
                        if (AreMaterialPresetsOpened)
                        {
                            WindowRect.height = CalculateMaterialEditWindowHeight_Presets();
                            WindowRect = GUI.Window(1, WindowRect, DrawMaterialEditUI_Presets, GUIContent.none);

                            var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                            GUI.Window(2, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);
                        }
                        else
                        {
                            WindowRect.height = CalculateMaterialEditWindowHeight_Material();
                            WindowRect = GUI.Window(1, WindowRect, DrawMaterialEditUI_Material, GUIContent.none);

                            var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                            GUI.Window(2, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);

                            void DrawColorPicker(int windowID, bool isOpened, int y, UnityEngine.GUI.WindowFunction colorPickerWindow, UnityEngine.GUI.WindowFunction openColorPickerWindow, UnityEngine.GUI.WindowFunction closeColorPickerWindow)
                            {
                                if (isOpened)
                                {
                                    var colorPickerWindowRect = new Rect(WindowRect.xMax, WindowRect.y + y, colorPickerSize, colorPickerSize);
                                    GUI.Window(windowID, colorPickerWindowRect, colorPickerWindow, GUIContent.none);

                                    var closeColorPickerWindowRect = new Rect(colorPickerWindowRect.xMax, colorPickerWindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                                    GUI.Window(windowID + 1, closeColorPickerWindowRect, closeColorPickerWindow, GUIContent.none);
                                }
                                else
                                {
                                    var openColorPickerWindowRect = new Rect(WindowRect.xMax, WindowRect.y + y, openCloseButtonWidth, openCloseButtonHeight);
                                    GUI.Window(windowID, openColorPickerWindowRect, openColorPickerWindow, GUIContent.none);
                                }

                            }

                            if (AreAdvancedSettingsOpened)
                            {
                                DrawColorPicker(3, IsColorPickerOpened_Color, colorPickerY_Color, DrawColorPickerWindow_Color, DrawColorPickerWindowOpenButton_Color, DrawColorPickerWindowCloseButton_Color);
                                DrawColorPicker(5, IsColorPickerOpened_SpecColor, colorPickerY_SpecColor, DrawColorPickerWindow_SpecColor, DrawColorPickerWindowOpenButton_SpecColor, DrawColorPickerWindowCloseButton_SpecColor);
                                DrawColorPicker(7, IsColorPickerOpened_ReflectColor, colorPickerY_ReflectColor, DrawColorPickerWindow_ReflectColor, DrawColorPickerWindowOpenButton_ReflectColor, DrawColorPickerWindowCloseButton_ReflectColor);
                            }
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
            }
            else
            {
                WindowRect = GUI.Window(1, WindowRect, DrawClosedWindow, GUIContent.none);

                var openColorPickerWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                GUI.Window(2, openColorPickerWindowRect, DrawClosedWindowOpenButton, GUIContent.none);
            }

            GUI.matrix = originalMatrix;
        }

        private int CalculateMaterialsWindowHeight_Presets()
        {
            var header =
                bigMargin + buttonHeight + bigMargin + // hide presets button
                smallMargin + bigMargin + // separator
                buttonHeight + mediumMargin; // preset name

            var totalPresets = Plugin.GetItemPresetsCount();
            if (totalPresets > 0)
            {
                var (_, visibleHeight) = BigCamoEditor.CalculateScrollViewTotalAndVisibleHeight(totalPresets, maxItemPresetsVisible, buttonHeight, smallMargin);
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

        private int CalculateMaterialsWindowHeight()
        {
            var materialsHeight = CalculateItemsWithMaterialsWindowHeight();
            var visibleHeight = Math.Min(maxMaterialsVisibleHeight, materialsHeight);
            return
                bigMargin + buttonHeight + bigMargin + // show presets button
                smallMargin + bigMargin + // separator
                visibleHeight + bigMargin; // materials
        }

        private int CalculateItemsWithMaterialsWindowHeight()
        {
            var totalMaterialsHeight = 0;

            foreach (var item in Items)
            {
                totalMaterialsHeight += CalculateItemWithMaterialsWindowHeight(item.ItemWithMaterials.Materials.Count);
            }

            totalMaterialsHeight += (Items.Count - 1) * smallMargin; // add height from all separators

            // we subtract top margin and bottom margin so scroll rect is nicely bound and not touching edges
            return -bigMargin + totalMaterialsHeight + -bigMargin;
        }

        private int CalculateItemWithMaterialsWindowHeight(int materialsCount)
        {
            var materialsHeight = materialsCount * (buttonHeight + smallMargin) - smallMargin;
            return
                smallMargin + buttonHeight + smallMargin + // name
                materialsHeight + bigMargin;
        }

        private int CalculateMaterialEditWindowHeight_Presets()
        {
            var header =
                smallMargin + buttonHeight + smallMargin + // material name
                buttonHeight + mediumMargin + // back button
                buttonHeight + bigMargin + // show/hide presets button
                smallMargin + bigMargin + // separator
                buttonHeight + mediumMargin; // preset name

            var totalPresets = Plugin.GetMaterialPresetsCount();
            if (totalPresets > 0)
            {
                var (_, visibleHeight) = BigCamoEditor.CalculateScrollViewTotalAndVisibleHeight(totalPresets, maxMaterialPresetsVisible, buttonHeight, smallMargin);
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

        private int CalculateMaterialEditWindowHeight_Material()
        {
            var header =
                smallMargin + buttonHeight + smallMargin + // material name
                buttonHeight + mediumMargin + // back button
                buttonHeight + bigMargin + // show/hide presets button
                smallMargin + bigMargin + // separator
                buttonHeight + mediumMargin; // show/hide advanced settings

            if (AreAdvancedSettingsOpened)
            {
                return
                    header +
                    buttonHeight + smallMargin + // compensate specular
                    buttonHeight + smallMargin + // color
                    buttonHeight + smallMargin + // color hue
                    buttonHeight + smallMargin + // color saturation
                    buttonHeight + smallMargin + // color value
                    buttonHeight + smallMargin + // diffuse values x
                    buttonHeight + smallMargin + // diffuse values y
                    buttonHeight + smallMargin + // glossness
                    buttonHeight + smallMargin + // specular color
                    buttonHeight + smallMargin + // specular color hue
                    buttonHeight + smallMargin + // specular color saturation
                    buttonHeight + smallMargin + // specular color value
                    buttonHeight + smallMargin + // specularness
                    buttonHeight + smallMargin + // specular values x
                    buttonHeight + smallMargin + // specular values y
                    buttonHeight + smallMargin + // reflect color
                    buttonHeight + smallMargin + // reflect color hue
                    buttonHeight + smallMargin + // reflect color saturation
                    buttonHeight + smallMargin + // reflect color value
                    buttonHeight + smallMargin + // texture uv x
                    buttonHeight + smallMargin + // texture uv y
                    buttonHeight + bigMargin; // texture uv scale
            }
            else
            {
                var texturesDirectory = BigPlugin.GetTexturesDirectory(DecalTypeMenu);
                var (_, visibleHeight) = BigCamoEditor.CalculateTexturesDirectoryHeight(texturesDirectory, maxEraseMaskIconsVisibleHeight);
                return
                    header +
                    buttonHeight + smallMargin + // compensate specular
                    buttonHeight + mediumMargin + // texture uv scale
                    iconSize + bigMargin + // icon
                    smallMargin + bigMargin + // separator
                    buttonHeight + smallMargin + // toolbar camos/stickers
                    visibleHeight + bigMargin; // icons grid
            }
        }

        private void DrawOpenedWindow_Presets(int windowID)
        {
            BigCamoEditor.DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var x = bigMargin;
            var y = bigMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Hide Presets"))
            {
                AreItemPresetsOpened = false;
            }
            y += buttonHeight + bigMargin;

            BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            BigCamoEditor.DrawPresetNameTextField(ref x, ref y, ref CurrentItemPresetName, SaveItemsIntoPreset, CamoEditorResources, CamoEditorStyle);
            BigCamoEditor.DrawPresets(ref x, ref y, ref CurrentItemPresetName, Plugin.GetItemPresetNames(), ref ItemPresetsScrollPosition, SwitchToItemPreset, Plugin.DeleteItemPreset, CamoEditorResources, CamoEditorStyle, maxItemPresetsVisible);

			GUI.DragWindow();
        }

        private void SaveItemsIntoPreset(string presetName)
        {
            Plugin.SaveItemMaterialsIntoPreset(Items, ItemsDict, presetName);
        }

        private void SwitchToItemPreset(string presetName)
        {
            Plugin.SwitchToItemPreset(Items, ItemsDict, presetName);
        }

        private void DrawOpenedWindow(int windowID)
		{
            BigCamoEditor.DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var x = bigMargin;
            var y = bigMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Show Presets"))
            {
                AreItemPresetsOpened = true;
            }
            y += buttonHeight + bigMargin;

            BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin;

            var scrollRectY = y + bigMargin;
            var totalHeight = CalculateItemsWithMaterialsWindowHeight();
            var visibleHeight = Math.Min(maxMaterialsVisibleHeight, totalHeight);
            var totalRect = new Rect(0, scrollRectY, boxWidth + bigMargin, totalHeight);
            var visibleRect = new Rect(0, scrollRectY, boxWidth + bigMargin + 16, visibleHeight);

            MaterialsScrollPosition = GUI.BeginScrollView(visibleRect, MaterialsScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

            for (var i = 0; i < Items.Count - 1; i++)
            {
                var item = Items[i];
                DrawItemMaterials(ref x, ref y, i, item);
                BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
                y += smallMargin;
            }
            {
                var i = Items.Count - 1;
                var item = Items[i];
                DrawItemMaterials(ref x, ref y, i, item);
            }

            GUI.EndScrollView();

            BigCamoEditor.DrawScrollBar(bigMargin + boxWidth + 5, scrollRectY, totalHeight, visibleHeight, MaterialsScrollPosition, drawBackground:false);

			GUI.DragWindow();
        }

        private void DrawItemMaterials(ref int x, ref int y, int itemIndex, CamoEditorItem item)
        {
            var materialsInfoOption = Plugin.GetMaterialsInfo(item.ItemId);

            y += smallMargin;

            GUI.Label(new Rect(x + 5, y, boxWidth - bigMargin, buttonHeight), item.Name, CamoEditorStyle.LabelStyleName);
            y += buttonHeight + smallMargin;

            var defaultMaterialButtonWidth = boxWidth - buttonHeight - smallMargin;
            var overridenMaterialButtonWidth = defaultMaterialButtonWidth - buttonHeight - smallMargin;
            var materialButtonX = x;
            var resetX = materialButtonX + overridenMaterialButtonWidth + smallMargin;
            var linkX = resetX + buttonHeight + smallMargin;

            foreach (var materialName in item.ItemWithMaterials.Materials.Keys)
            {
                var isOverridenMaterial = materialsInfoOption.Some(out var materialsInfo) && materialsInfo.Materials.ContainsKey(materialName);
                var thisOverride = new EditedOverride(itemIndex, materialName);

                var materialButtonWidth = isOverridenMaterial ? overridenMaterialButtonWidth : defaultMaterialButtonWidth;
                if (GUI.Button(new Rect(materialButtonX, y, materialButtonWidth, buttonHeight), materialName, CamoEditorStyle.DirectoryButtonStyle))
                {
                    CurrentlyEditedOverride = new(thisOverride);
                    ForEveryLinkedItem
                    (
                        thisOverride,
                        (item, materialName) => Plugin.OverrideMaterial(item.ItemWithMaterials, item.OriginalMaterials, item.ItemId, item.InstanceID, materialName)
                    );

                    {
                        // TODO make it smarter? also notice that we init text fields
                        // after Plugin.OverrideMaterial, because they can be unintialized
                        var (_, _, materialInfo, _) = GetEditedMaterialInfo();
                        TextField_Color.SetValue(materialInfo.ColorHSV);
                        TextField_SpecColor.SetValue(materialInfo.SpecColorHSV);
                        TextField_ReflectColor.SetValue(materialInfo.ReflectColorHSV);
                    }
                }

                if (isOverridenMaterial)
                {
                    if (GUI.Button(new Rect(resetX, y, buttonHeight, buttonHeight), CamoEditorResources.Reset))
                    {
                        ForEveryLinkedItem
                        (
                            thisOverride,
                            (item, materialName) => Plugin.ResetMaterial(item.ItemId, materialName)
                        );
                    }
                }

                var isLinked = LinkedOverrides.Contains(thisOverride);
                var linkIcon = isLinked ? CamoEditorResources.LinkOn : CamoEditorResources.CheckboxOff;
                if (GUI.Button(new Rect(linkX, y, buttonHeight, buttonHeight), linkIcon))
                {
                    LinkedOverrides.Toggle(thisOverride);
                }
                y += buttonHeight + smallMargin;
            }

            y -= smallMargin;
            y += bigMargin;
        }

        private void DrawMaterialEditUI_Presets(int windowID)
        {
            BigCamoEditor.DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var (item, materialName, _, _) = GetEditedMaterialInfo();

            var x = bigMargin;
            var y = smallMargin;

            GUI.Label(new Rect(x, y, boxWidth, buttonHeight), materialName, CamoEditorStyle.LabelStyleValue);
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                CurrentlyEditedOverride = default;
            }
            y += buttonHeight + mediumMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Hide Presets"))
            {
                AreMaterialPresetsOpened = false;
            }
            y += buttonHeight + bigMargin;

            BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            BigCamoEditor.DrawPresetNameTextField(ref x, ref y, ref CurrentMaterialPresetName, SaveMaterialIntoPreset, CamoEditorResources, CamoEditorStyle);
            BigCamoEditor.DrawPresets(ref x, ref y, ref CurrentMaterialPresetName, Plugin.GetMaterialPresetNames(), ref MaterialPresetsScrollPosition, SwitchToMaterialPreset, Plugin.DeleteMaterialPreset, CamoEditorResources, CamoEditorStyle, maxMaterialPresetsVisible);

			GUI.DragWindow();
        }

        private void SaveMaterialIntoPreset(string presetName)
        {
            var (item, materialName, _, _) = GetEditedMaterialInfo();
            Plugin.SaveMaterialIntoPreset(item.ItemId, materialName, presetName);
        }

        private void SwitchToMaterialPreset(string presetName)
        {
            ForEveryLinkedItem(Plugin.SwitchToMaterialPreset, presetName);
        }

        private void DrawSlidersColorHSV(ref int x, ref int y, ref Vector3 colorHSV, ref WeaponCamoAndStickers.TextField<Vector3> textField, string name, ColorStringCache strings, Action<string, string, Vector3> action)
        {
            var labelWidth = 23;
            var nameDelta = labelWidth - (nameWidth - 42);
            var sliderWidth = 224 - nameDelta;
            var labelX = x;
            var sliderX = labelX + labelWidth + smallMargin;
            var valueX = sliderX + sliderWidth + smallMargin;

            BigCamoEditor.DrawColor(new Rect(labelX, y + 8, buttonHeight, buttonHeight / 2), colorHSV.HSVtoRGBA());
            GUI.Label(new Rect(labelX + buttonHeight + mediumMargin, y, boxWidth, buttonHeight), name, CamoEditorStyle.LabelStyleName);


            {
                var textFieldX = x + boxWidth - fourthBoxWidthButton;
                GUI.Label(new Rect(textFieldX - 39, y, longFieldWidth, buttonHeight), "RGB:", CamoEditorStyle.LabelStyleName);

                var previousBackgroundColor = GUI.backgroundColor;
                var buttonBackgroundColor = textField.IsValid ? previousBackgroundColor : Color.red;

                GUI.backgroundColor = buttonBackgroundColor;
                var newColorHex = GUI.TextField(new Rect(textFieldX, y, fourthBoxWidthButton, buttonHeight), textField.Value, 7, CamoEditorStyle.RGBHexTextFieldStyle);
                GUI.backgroundColor = previousBackgroundColor;

                if (textField.TrySetValue(newColorHex, out var newColorOption) && newColorOption.Some(out var newColor))
                {
                    colorHSV = newColor;
                    ForEveryLinkedItem(action, colorHSV);
                }
            }


            y += buttonHeight + smallMargin;

            GUI.Label(new Rect(labelX, y, labelWidth, buttonHeight), "H:", CamoEditorStyle.LabelStyleName);
            var newHue = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), colorHSV.x, 0f, 1f);
            if (newHue != colorHSV.x)
            {
                colorHSV.x = newHue;
                textField.SetValue(colorHSV);
                ForEveryLinkedItem(action, colorHSV);
            }
            GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), strings.H.Get(colorHSV.x), CamoEditorStyle.LabelStyleValue);
            y += buttonHeight + smallMargin;


            GUI.Label(new Rect(labelX, y, labelWidth, buttonHeight), "S:", CamoEditorStyle.LabelStyleName);
            var newSaturation = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), colorHSV.y, 0f, 1f);
            if (newSaturation != colorHSV.y)
            {
                colorHSV.y = newSaturation;
                textField.SetValue(colorHSV);
                ForEveryLinkedItem(action, colorHSV);
            }
            GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), strings.S.Get(colorHSV.y), CamoEditorStyle.LabelStyleValue);
            y += buttonHeight + smallMargin;


            GUI.Label(new Rect(labelX, y, labelWidth, buttonHeight), "V:", CamoEditorStyle.LabelStyleName);
            var newValue = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), colorHSV.z, 0f, 1f);
            if (newValue != colorHSV.z)
            {
                colorHSV.z = newValue;
                textField.SetValue(colorHSV);
                ForEveryLinkedItem(action, colorHSV);
            }
            GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), strings.V.Get(colorHSV.z), CamoEditorStyle.LabelStyleValue);
            y += buttonHeight + smallMargin;
        }

        private void DrawSliderFloat(ref int x, ref int y, ref float value, float left, float right, string name, int labelWidth, StringCache<float> strings, Action<string, string, float> action)
        {
            var nameDelta = labelWidth - (nameWidth - 42);
            var sliderWidth = 224 - nameDelta;
            var labelX = x;
            var sliderX = labelX + labelWidth + smallMargin;
            var valueX = sliderX + sliderWidth + smallMargin;

            GUI.Label(new Rect(labelX, y, labelWidth, buttonHeight), name, CamoEditorStyle.LabelStyleName);
            var newValue = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), value, left, right);
            if (newValue != value)
            {
                value = newValue;
                ForEveryLinkedItem(action, value);
            }
            GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), strings.Get(value), CamoEditorStyle.LabelStyleValue);
            y += buttonHeight + smallMargin;
        }

        private void DrawSliderVector2(ref int x, ref int y, ref Vector2 value, float left, float right, string nameX, string nameY, int labelWidth, Vector2StringCache strings, Action<string, string, Vector2> action)
        {
            var nameDelta = labelWidth - (nameWidth - 42);
            var sliderWidth = 224 - nameDelta;
            var labelX = x;
            var sliderX = labelX + labelWidth + smallMargin;
            var valueX = sliderX + sliderWidth + smallMargin;

            GUI.Label(new Rect(labelX, y, labelWidth, buttonHeight), nameX, CamoEditorStyle.LabelStyleName);
            var newValueX = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), value.x, left, right);
            if (newValueX != value.x)
            {
                value.x = newValueX;
                ForEveryLinkedItem(action, value);
            }
            GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), strings.X.Get(value.x), CamoEditorStyle.LabelStyleValue);
            y += buttonHeight + smallMargin;


            GUI.Label(new Rect(labelX, y, labelWidth, buttonHeight), nameY, CamoEditorStyle.LabelStyleName);
            var newValueY = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), value.y, left, right);
            if (newValueY != value.y)
            {
                value.y = newValueY;
                ForEveryLinkedItem(action, value);
            }
            GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), strings.Y.Get(value.y), CamoEditorStyle.LabelStyleValue);
            y += buttonHeight + smallMargin;
        }

        private void DrawMaterialEditUI_Material(int windowID)
        {
            BigCamoEditor.DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var (_, materialName, materialInfo, _) = GetEditedMaterialInfo();
            var colorHSV = materialInfo.ColorHSV;
            var specColorHSV = materialInfo.SpecColorHSV;
            var reflectColorHSV = materialInfo.ReflectColorHSV;
            var glossness = materialInfo.Glossness;
            var specularness = materialInfo.Specularness;
            var specVals = materialInfo.SpecVals;
            var defVals = materialInfo.DefVals;
            var textureUV = materialInfo.TextureUV;
            var compensateSpecular = materialInfo.CompensateSpecular;

            var x = bigMargin;
            var y = smallMargin;

            GUI.Label(new Rect(x, y, boxWidth, buttonHeight), materialName, CamoEditorStyle.LabelStyleValue);
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                CurrentlyEditedOverride = default;
            }
            y += buttonHeight + mediumMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Show Presets"))
            {
                AreMaterialPresetsOpened = true;
            }
            y += buttonHeight + bigMargin;

            BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            var advancedSettingsLabel = AreAdvancedSettingsOpened ? "Hide Advanced Settings" : "Show Advanced Settings";
            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), advancedSettingsLabel))
            {
                AreAdvancedSettingsOpened = !AreAdvancedSettingsOpened;
            }
            y += buttonHeight + mediumMargin;


            var sliderWidth = 224 + 12;
            var labelX = x;
            var sliderX = labelX + nameWidth + smallMargin - 42 - 12;
            var valueX = sliderX + sliderWidth + smallMargin;

            {
                var specularCompensationIcon = compensateSpecular ? CamoEditorResources.CheckboxOn : CamoEditorResources.CheckboxOff;
                if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), specularCompensationIcon))
                {
                    compensateSpecular = !compensateSpecular;
                    ForEveryLinkedItem(Plugin.ChangeCompensateSpecular, compensateSpecular);
                }
                {
                    var buttonLabelX = x + buttonHeight + smallMargin + 7;
                    GUI.Label(new Rect(buttonLabelX, y, boxWidth, buttonHeight), "Compensate missing specular map in alpha channel", CamoEditorStyle.LabelStyleName);
                }
                y += buttonHeight + smallMargin;
            }

            if (AreAdvancedSettingsOpened)
            {
                DrawSlidersColorHSV(ref x, ref y, ref colorHSV, ref TextField_Color, "Color:", Strings.Color, Plugin.ChangeColor);
                DrawSliderVector2(ref x, ref y, ref defVals, 0, 3, "Def Vals X:", "Def Vals Y:", 73, Strings.DefVals, Plugin.ChangeDefVals);
                DrawSliderFloat(ref x, ref y, ref glossness, 0.01f, 10, "Glossness:", 73, Strings.Glossness, Plugin.ChangeGlossness);
                DrawSlidersColorHSV(ref x, ref y, ref specColorHSV, ref TextField_SpecColor, "Specular Color:", Strings.SpecColor, Plugin.ChangeSpecColor);
                DrawSliderFloat(ref x, ref y, ref specularness, 0.01f, 10, "Specularness:", 92, Strings.Specularness, Plugin.ChangeSpecularness);
                DrawSliderVector2(ref x, ref y, ref specVals, 0, 3, "Spec Vals X:", "Spec Vals Y:", 92, Strings.SpecVals, Plugin.ChangeSpecVals);
                DrawSlidersColorHSV(ref x, ref y, ref reflectColorHSV, ref TextField_ReflectColor, "Reflect Color:", Strings.ReflectColor, Plugin.ChangeReflectColor);


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "UV x:", CamoEditorStyle.LabelStyleName);
                var newUVz = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), textureUV.z, -1f, 1f);
                if (newUVz != textureUV.z)
                {
                    textureUV.z = newUVz;
                    ForEveryLinkedItem(Plugin.ChangeTextureUV, textureUV);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.TextureUVx.Get(textureUV.z), CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;


                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "UV y:", CamoEditorStyle.LabelStyleName);
                var newUVw = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), textureUV.w, -1f, 1f);
                if (newUVw != textureUV.w)
                {
                    textureUV.w = newUVw;
                    ForEveryLinkedItem(Plugin.ChangeTextureUV, textureUV);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.TextureUVy.Get(textureUV.w), CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + smallMargin;
            }

            {
                var (leftScale, rightScale) = GetLoopingSliderBounds(textureUV.x);
                GUI.Label(new Rect(labelX, y, nameWidth, buttonHeight), "UV scale:", CamoEditorStyle.LabelStyleName);
                var newUVx = GUI.HorizontalSlider(new Rect(sliderX, y + 11, sliderWidth, buttonHeight), textureUV.x, leftScale, rightScale);
                if (newUVx != textureUV.x)
                {
                    textureUV.x = newUVx;
                    textureUV.y = newUVx;
                    ForEveryLinkedItem(Plugin.ChangeTextureUV, textureUV);
                }
                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.TextureUVscale.Get(textureUV.x), CamoEditorStyle.LabelStyleValue);
                y += buttonHeight + mediumMargin;
            }

            if (!AreAdvancedSettingsOpened)
            {
                if (string.IsNullOrWhiteSpace(materialInfo.Texture))
                {
                    GUI.Button(new Rect(x, y, iconSize, iconSize), "default");
                }
                else
                {
                    var textureData = BigPlugin.GetTextureData(materialInfo.Texture);
                    GUI.Button(new Rect(x, y, iconSize, iconSize), textureData.Preview);

                    var iconLabelX = x + iconSize + smallMargin + 12;
                    GUI.Label(new Rect(iconLabelX, y + 1, 256, buttonHeight), materialInfo.Texture, CamoEditorStyle.TextureNameStyle);
                }
                y += iconSize + bigMargin;

                BigCamoEditor.DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
                y += smallMargin + bigMargin;

                DecalTypeMenu = (DecalTextureType)GUI.Toolbar(new Rect(x, y, boxWidth, buttonHeight), (int)DecalTypeMenu, CamoEditorResources.DecalTypesToolbar);
                y += buttonHeight + smallMargin;

                DrawAllTextures(x, y, materialInfo.Texture, DecalTypeMenu, maxEraseMaskIconsVisibleHeight);
            }

			GUI.DragWindow();
        }

        // when fractional part of value goes over 0.5 slider moves to next page
        // this way we still have precision of delegating entire slider to range of 1,
        // but can to go higher values smoothly
        public static (float left, float right) GetLoopingSliderBounds(float value)
        {
            const float offsetEffective = 0.5f;
            const float offsetTotal = 0.6f;

            var centerPoint = (int)value;
            var valueFraction = value % 1;
            if (valueFraction > offsetEffective)
            {
                centerPoint++;
            }

            centerPoint = Math.Max(centerPoint, 1);
            var left = centerPoint - offsetTotal;
            var right = centerPoint + offsetTotal;

            return (left, right);
        }

        private ref Vector2 GetScrollPosition(DecalTextureType textureType)
        {
            switch (textureType)
            {
                case DecalTextureType.Camo: return ref CamosScrollPosition;
                case DecalTextureType.Sticker: return ref StickersScrollPosition;
                default: throw new ArgumentException();
            }
        }

        private void DrawAllTextures(int x, int y, string currentTextureName, DecalTextureType decalTextureType, int maxIconsVisibleHeight)
        {
            var texturesDirectory = BigPlugin.GetTexturesDirectory(decalTextureType);

            var (totalHeight, visibleHeight) = BigCamoEditor.CalculateTexturesDirectoryHeight(texturesDirectory, maxIconsVisibleHeight);
            var totalRect = new Rect(x, y, boxWidth, totalHeight);
            var visibleRect = new Rect(x, y, boxWidth + 16, visibleHeight);

            ref var scrollPosition = ref GetScrollPosition(decalTextureType);
            BigCamoEditor.DrawScrollBar(x + boxWidth + 5, y, totalHeight, visibleHeight, scrollPosition);
            scrollPosition = GUI.BeginScrollView(visibleRect, scrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

            DrawAllTextures(ref x, ref y, currentTextureName, texturesDirectory, drawName: false);

            GUI.EndScrollView();
        }

        private void DrawAllTextures(ref int x, ref int y, string currentTextureName, TexturesDirectory texturesDirectory, bool drawName = true)
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
                DrawAllTextures(ref x, ref y, currentTextureName, subDirectory);
            }

            var textures = texturesDirectory.Textures;
            for (var i = 0; i < textures.Length; i++)
            {
                var textureName = textures[i];
                var textureData = BigPlugin.GetTextureData(textureName);

                var ix = i % iconColumns;
                var iy = i / iconColumns;

                var xi = x + ix * (iconSize + smallMargin);
                var yi = y + iy * (iconSize + smallMargin);

                if (GUI.Button(new Rect(xi, yi, iconSize, iconSize), textureData.Preview))
                {
                    var e = Event.current;
                    if (e.button == 0) // left click
                    {
                        if (currentTextureName != textureName)
                        {
                            ForEveryLinkedItem(Plugin.ChangeTexture, textureName);
                        }
                    }
                    if (e.button == 1) // right click
                    {
                        BigPlugin.ToggleFavouriteTexture(textureName);
                    }
                }
                if (textureData.Format == DecalTextureFormat.Video)
                {
                    GUI.DrawTexture(new Rect(xi + smallMargin, yi + smallMargin, 16, 16), CamoEditorResources.PlayIcon);
                }
                if (BigPlugin.IsFavouriteTexture(textureName))
                {
                    GUI.DrawTexture(new Rect(xi + iconSize - 3 - smallIconSize, yi + iconSize - 3 - smallIconSize, smallIconSize, smallIconSize), CamoEditorResources.FavouriteIcon);
                }
            }

            var totalRows = BigCamoEditor.DivideIntCeil(textures.Length, iconColumns);
            y += (iconSize + smallMargin) * totalRows;
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
            GUI.DrawTexture(new Rect(0, 0, mainIconWidth, openCloseButtonHeight), CamoEditorResources.MainIconMaterial, ScaleMode.StretchToFill);

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

        private void DrawColorPickerWindowCloseButton_Color(int windowID)
        {
            DrawColorPickerWindowCloseButton_Common(ref IsColorPickerOpened_Color);
        }

        private void DrawColorPickerWindowCloseButton_SpecColor(int windowID)
        {
            DrawColorPickerWindowCloseButton_Common(ref IsColorPickerOpened_SpecColor);
        }

        private void DrawColorPickerWindowCloseButton_ReflectColor(int windowID)
        {
            DrawColorPickerWindowCloseButton_Common(ref IsColorPickerOpened_ReflectColor);
        }

        private void DrawColorPickerWindowCloseButton_Common(ref bool isColorPickerOpened)
        {
            BigCamoEditor.DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.OpenedIconColorWheel, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                isColorPickerOpened = false;
            }
        }

        private void DrawColorPickerWindowOpenButton_Color(int windowID)
        {
            DrawColorPickerWindowOpenButton(ref IsColorPickerOpened_Color);
        }

        private void DrawColorPickerWindowOpenButton_SpecColor(int windowID)
        {
            DrawColorPickerWindowOpenButton(ref IsColorPickerOpened_SpecColor);
        }

        private void DrawColorPickerWindowOpenButton_ReflectColor(int windowID)
        {
            DrawColorPickerWindowOpenButton(ref IsColorPickerOpened_ReflectColor);
        }

        private void DrawColorPickerWindowOpenButton(ref bool isColorPickerOpened)
        {
            BigCamoEditor.DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.ClosedIconColorWheel, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                isColorPickerOpened = true;
            }
        }

        private (CamoEditorItem, string, MaterialInfo, bool) GetEditedMaterialInfo()
        {
            var thisOverride = CurrentlyEditedOverride.Value;
            var item = Items[thisOverride.ItemIndex];
            var materialName = thisOverride.MaterialName;
            var materialInfo = Plugin.GetMaterialInfo(item.ItemId, materialName).Value;
            var isLinked = LinkedOverrides.Contains(thisOverride);
            return (item, materialName, materialInfo, isLinked);
        }

        private void DrawColorPickerWindow_Color(int windowID)
        {
            var (_, _, materialInfo, _) = GetEditedMaterialInfo();
            DrawColorPickerWindow_Common(ref materialInfo.ColorHSV, ref TextField_Color, Plugin.ChangeColor);
        }

        private void DrawColorPickerWindow_SpecColor(int windowID)
        {
            var (_, _, materialInfo, _) = GetEditedMaterialInfo();
            DrawColorPickerWindow_Common(ref materialInfo.SpecColorHSV, ref TextField_SpecColor, Plugin.ChangeSpecColor);
        }

        private void DrawColorPickerWindow_ReflectColor(int windowID)
        {
            var (_, _, materialInfo, _) = GetEditedMaterialInfo();
            DrawColorPickerWindow_Common(ref materialInfo.ReflectColorHSV, ref TextField_ReflectColor, Plugin.ChangeReflectColor);
        }

        private void DrawColorPickerWindow_Common(ref Vector3 colorHSV, ref WeaponCamoAndStickers.TextField<Vector3> textField, Action<string, string, Vector3> changeColorAction)
        {
            BigCamoEditor.DrawColor(new Rect(0, 0, colorPickerSize, colorPickerSize), backgroundColor);

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

                colorHSV.x = hue;
                colorHSV.y = saturation;
                textField.SetValue(colorHSV);
                ForEveryLinkedItem(changeColorAction, colorHSV);
            }
            y += hsCircleDiameter + bigMargin;
        }

        private void ForEveryLinkedItem(EditedOverride thisOverride, Action<CamoEditorItem, string> action)
        {
            if (LinkedOverrides.Contains(thisOverride))
            {
                foreach (var linkedOverride in LinkedOverrides)
                {
                    var linkedItem = Items[linkedOverride.ItemIndex];
                    action(linkedItem, linkedOverride.MaterialName);
                }
            }
            else
            {
                var item = Items[thisOverride.ItemIndex];
                action(item, thisOverride.MaterialName);
            }
        }

        private void ForEveryLinkedItem<T>(Action<string, string, T> action, T value)
        {
            var thisOverride = CurrentlyEditedOverride.Value;
            if (LinkedOverrides.Contains(thisOverride))
            {
                foreach (var linkedOverride in LinkedOverrides)
                {
                    var linkedItem = Items[linkedOverride.ItemIndex];
                    action(linkedItem.ItemId, linkedOverride.MaterialName, value);
                }
            }
            else
            {
                var item = Items[thisOverride.ItemIndex];
                action(item.ItemId, thisOverride.MaterialName, value);
            }
        }

        public void Destroy()
        {

        }

    }
}
