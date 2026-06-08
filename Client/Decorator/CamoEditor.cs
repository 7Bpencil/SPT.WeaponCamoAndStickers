//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using EFT;
using EFT.AssetsManager;
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

namespace SevenBoldPencil.Decorator
{
    public class CamoEditor
    {
        public Plugin Plugin;
        public BigPlugin BigPlugin;
        public CamoEditorResources CamoEditorResources;
        public CamoEditorStyle CamoEditorStyle;
        public string ItemId;
        public int InstanceID;
		public AssetPoolObject AssetPoolObject;
        public bool IsOpened;
		public Vector2 DecoratorsScrollPosition;
        public Option<int> CurrentlyEditedDecoratorIndex;
		public Rect WindowRect = GetDefaultWindowRect();

        // brace for imGUI shitshow

        public const int iconColumns = 5;
        public const int maxDecalsVisible = 10;
        public const int maxDecalsVisibleWhenPresetsAreNotOpened = 10;
        public const int maxDecalsVisibleWhenPresetsAreOpened = 6;
        public const int maxPresetsVisible = 24;
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
        public const int maxEraseMaskIconsVisibleHeight = 16 * (buttonHeight + smallMargin) - smallMargin;
        public const int maxMaterialsVisibleHeight = 27 * (buttonHeight + smallMargin) - smallMargin;
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
        public static readonly int colorPickerY_Color = 253;
        public static readonly int colorPickerY_SpecColor = 505;
        public static readonly int colorPickerY_ReflectColor = 757;
        public static readonly int colorPickerSize = hsCircleDiameter + bigMargin * 2;
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
                WindowRect.height = CalculateWindowHeight();
                WindowRect = GUI.Window(1, WindowRect, DrawOpenedWindow, GUIContent.none);

                var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                GUI.Window(2, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);
            }
            else
            {
                WindowRect = GUI.Window(1, WindowRect, DrawClosedWindow, GUIContent.none);

                var openColorPickerWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
                GUI.Window(2, openColorPickerWindowRect, DrawClosedWindowOpenButton, GUIContent.none);
            }

            GUI.matrix = originalMatrix;
        }

		private int CalculateWindowHeight()
		{
            var totalDecoratorsCount = Plugin.GetDecoratorsCount(ItemId);
            var (_, visibleHeight) = BigCamoEditor.CalculateScrollViewTotalAndVisibleHeight(totalDecoratorsCount, maxDecalsVisible, boxHeight, mediumMargin);
            return
                bigMargin +
                buttonHeight + bigMargin + // show presets button
                smallMargin + bigMargin + // separator
                visibleHeight + mediumMargin + // decorators
                buttonHeight + bigMargin; // add new decorator button
		}

        private void DrawOpenedWindow(int windowID)
		{
            DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var x = bigMargin;
            var y = bigMargin;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Show Presets"))
            {
                // TODO ArePresetsOpened = true;
            }
            y += buttonHeight + bigMargin;

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            if (Plugin.GetDecoratorsInfo(ItemId).Some(out var decoratorsInfo))
			{
                var decoratorsY = y;

				var decoratorsCount = decoratorsInfo.Decorators.Count;
                var (totalHeight, visibleHeight) = BigCamoEditor.CalculateScrollViewTotalAndVisibleHeight(decoratorsCount, maxDecalsVisible, boxHeight, mediumMargin);
                var totalRect = new Rect(x, decoratorsY, boxWidth, totalHeight);
                var visibleRect = new Rect(x, decoratorsY, boxWidth + 16, visibleHeight);

                BigCamoEditor.DrawScrollBar(x + boxWidth + 5, decoratorsY, totalHeight, visibleHeight, DecoratorsScrollPosition);
                DecoratorsScrollPosition = GUI.BeginScrollView(visibleRect, DecoratorsScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

                for (var i = 0; i < decoratorsCount; i++)
                {
                    var decoratorInfo = decoratorsInfo.Decorators[i];
                    DrawDecoratorElementUI(x, decoratorsY, i, decoratorInfo);
                    decoratorsY += boxHeight + mediumMargin;
                }
                y += visibleHeight + mediumMargin;

                GUI.EndScrollView();
			}

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Add Decorator"))
            {
				var newDecoratorIndex = Plugin.AddNewDecorator(ItemId, InstanceID, AssetPoolObject);
                SetCurrentlyEditedDecorator(newDecoratorIndex);
            }

			GUI.DragWindow();
        }

        private void SetCurrentlyEditedDecorator(int decoratorIndex)
		{
            CurrentlyEditedDecoratorIndex = new(decoratorIndex);
		}

		private void DrawDecoratorElementUI(int x, int y, int decoratorIndex, DecoratorInfo decoratorInfo)
		{
            var prefabData = Plugin.GetPrefabData(decoratorInfo.Prefab);

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), default(string));

            var topLineY = y + smallMargin;
            var bottomLineY = topLineY + buttonHeight + smallMargin;
            var textureIconX = x + smallMargin;
            if (GUI.Button(new Rect(textureIconX, topLineY, iconSize, iconSize), prefabData.Preview))
            {
				SetCurrentlyEditedDecorator(decoratorIndex);
            }

            var labelX = textureIconX + iconSize + smallMargin + 2;
            var decoratorName = !string.IsNullOrWhiteSpace(decoratorInfo.Name) ? decoratorInfo.Name : decoratorInfo.Prefab;
            GUI.Label(new Rect(labelX, topLineY + 1, 230, iconSize), decoratorName, CamoEditorStyle.TextureNameStyle);

            var lineX = x + boxWidth - (smallMargin + buttonHeight) * 3;
            if (GUI.Button(new Rect(lineX, bottomLineY, buttonHeight, buttonHeight), CamoEditorResources.DeleteIcon))
            {
                Plugin.Delete(ItemId, decoratorIndex);
            }
            lineX += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(lineX, bottomLineY, buttonHeight, buttonHeight), CamoEditorResources.DuplicateIcon))
            {
				// TODO
                // var newDecalIndex = Plugin.Duplicate(ItemId, decalIndex);
                // SetCurrentlyEditedDecorator(newDecalIndex, textureData.Type);
            }
            lineX += buttonHeight + smallMargin;

            var isVisibleIcon = decoratorInfo.IsVisible ? CamoEditorResources.VisibleIcon : CamoEditorResources.HiddenIcon;
            if (GUI.Button(new Rect(lineX, bottomLineY, buttonHeight, buttonHeight), isVisibleIcon))
            {
				// TODO
                // Plugin.SwitchIsVisible(ItemId, decalIndex, decalInfo);
            }
            lineX += buttonHeight + smallMargin;
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

        public void Destroy()
        {

        }

    }
}
