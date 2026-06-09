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
    public class DecoratorStringCache
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
    }

    public class CamoEditor
    {
        public Plugin Plugin;
        public BigPlugin BigPlugin;
        public CamoEditorResources CamoEditorResources;
        public CamoEditorStyle CamoEditorStyle;
		public DecoratorStringCache Strings = new();
        public string ItemId;
        public int InstanceID;
		public AssetPoolObject AssetPoolObject;
        public bool IsOpened;
		public Vector2 DecoratorsScrollPosition;
        public Option<int> CurrentlyEditedDecoratorIndex;
        public Vector2 PrefabsScrollPosition;
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
				if (CurrentlyEditedDecoratorIndex.Some(out var decoratorIndex))
				{
	                WindowRect.height = CalculateDecoratorEditWindowHeight();
	                WindowRect = GUI.Window(1, WindowRect, DrawDecoratorEditUI, GUIContent.none);

	                var closeButtonWindowRect = new Rect(WindowRect.xMax, WindowRect.y, openCloseButtonWidth, openCloseButtonHeight);
	                GUI.Window(2, closeButtonWindowRect, DrawOpenedWindowCloseButton, GUIContent.none);
				}
				else
				{
	                WindowRect.height = CalculateWindowHeight();
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

		private int CalculateDecoratorEditWindowHeight()
		{
            var decoratorsCount = Plugin.GetTotalDecoratorsCount();
            var totalRows = BigCamoEditor.DivideIntCeil(decoratorsCount, iconColumns);
			var (totalHeight, visibleHeight) = BigCamoEditor.CalculateScrollViewTotalAndVisibleHeight(totalRows, 5, iconSize, smallMargin);
			return
				bigMargin +
                buttonHeight + mediumMargin + // back button
                buttonHeight + mediumMargin + // decal name
                4 * (buttonHeight + smallMargin) - smallMargin + bigMargin + // position, rotation, scale, flip
                smallMargin + bigMargin + // separator
                iconSize + bigMargin + // icon
                smallMargin + bigMargin + // separator
                visibleHeight + bigMargin; // icons grid
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

        private void DrawDecoratorEditUI(int windowID)
		{
            DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var decoratorIndex = CurrentlyEditedDecoratorIndex.Value;
            var (decoratorInfo, decorator) = Plugin.GetDecorator(ItemId, InstanceID, decoratorIndex);

            var x = bigMargin;
            var y = bigMargin;

			DrawDecoratorEditUI_Header(x, ref y, decoratorIndex, decoratorInfo, decorator);
			DrawDecoratorEditUI_Transform(x, ref y, decoratorIndex, decoratorInfo, decorator);
			DrawDecoratorEditUI_Icons(x, ref y, decoratorIndex, decoratorInfo, decorator);

			GUI.DragWindow();
		}

		private void DrawDecoratorEditUI_Header(int x, ref int y, int decoratorIndex, DecoratorInfo decoratorInfo, Decorator decorator)
		{
            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                CurrentlyEditedDecoratorIndex = default;
            }
            y += buttonHeight + mediumMargin;


            decoratorInfo.Name = GUI.TextField(new Rect(x, y, boxWidth, buttonHeight), decoratorInfo.Name, maxDecalNameLength, CamoEditorStyle.TextFieldStyle);
            if (string.IsNullOrWhiteSpace(decoratorInfo.Name))
            {
                GUI.Label(new Rect(x + CamoEditorStyle.TextFieldStyle.contentOffset.x + 3, y, boxWidth, buttonHeight), "enter decorator name (optional)", CamoEditorStyle.LabelStyleName);
            }
            y += buttonHeight + mediumMargin;
		}

		private void DrawDecoratorEditUI_Transform(int x, ref int y, int decoratorIndex, DecoratorInfo decoratorInfo, Decorator decorator)
		{
            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditPositionIcon))
            {
                // SetupTransformHandle(HandleType.Position, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalPositionX.Get(decorator.DecoratorTransform.localPosition.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalPositionY.Get(decorator.DecoratorTransform.localPosition.y), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalPositionZ.Get(decorator.DecoratorTransform.localPosition.z), CamoEditorStyle.LabelStyleName);
            }
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditRotationIcon))
            {
                // SetupTransformHandle(HandleType.Rotation, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalEulerAnglesX.Get(decorator.DecoratorTransform.localEulerAngles.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalEulerAnglesY.Get(decorator.DecoratorTransform.localEulerAngles.y), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalEulerAnglesZ.Get(decorator.DecoratorTransform.localEulerAngles.z), CamoEditorStyle.LabelStyleName);
            }
            y += buttonHeight + smallMargin;

            if (GUI.Button(new Rect(x, y, buttonHeight, buttonHeight), CamoEditorResources.EditScaleIcon))
            {
                // SetupTransformHandle(HandleType.Scale, decalIndex, decalInfo, decal);
            }
            {
                var valueX = x + buttonHeight + smallMargin + 7;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalScaleX.Get(decorator.DecoratorTransform.localScale.x), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalScaleY.Get(decorator.DecoratorTransform.localScale.y), CamoEditorStyle.LabelStyleName);
                valueX += longFieldWidth + smallMargin;

                GUI.Label(new Rect(valueX, y, longFieldWidth, buttonHeight), Strings.LocalScaleZ.Get(decorator.DecoratorTransform.localScale.z), CamoEditorStyle.LabelStyleName);
            }
            y += buttonHeight + smallMargin;

            {
                var lineX = x;
                if (GUI.Button(new Rect(lineX, y, thirdBoxWidthButton, buttonHeight), "flip X"))
                {
                    // Plugin.FlipHorizontally(ItemId, decalIndex, decalInfo);
                    // SyncTransformHandle(decalInfo, decal);
                }
                lineX += thirdBoxWidthButton + smallMargin;

                if (GUI.Button(new Rect(lineX, y, thirdBoxWidthButton, buttonHeight), "flip Y"))
                {
                    // Plugin.FlipVertically(ItemId, decalIndex, decalInfo);
                    // SyncTransformHandle(decalInfo, decal);
                }
                lineX += thirdBoxWidthButton + smallMargin;

                if (GUI.Button(new Rect(lineX, y, thirdBoxWidthButton, buttonHeight), "flip Z"))
                {
                    // Plugin.FlipDirection(ItemId, decalIndex, decalInfo);
                    // SyncTransformHandle(decalInfo, decal);
                }
            }
            y += buttonHeight + bigMargin;
		}

		private void DrawDecoratorEditUI_Icons(int x, ref int y, int decoratorIndex, DecoratorInfo decoratorInfo, Decorator decorator)
		{
            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;

            {
				var prefabData = Plugin.GetPrefabData(decoratorInfo.Prefab);

                GUI.Button(new Rect(x, y, iconSize, iconSize), prefabData.Preview);

                var labelX = x + iconSize + smallMargin + 12;
                GUI.Label(new Rect(labelX, y + 1, 256, buttonHeight), decoratorInfo.Prefab, CamoEditorStyle.TextureNameStyle);

                y += iconSize + bigMargin;
            }

            DrawColor(new Rect(0, y, windowWidth, smallMargin), separatorColor);
            y += smallMargin + bigMargin;


			{
				var decorators = Plugin.GetAllDecorators();

	            var totalRows = BigCamoEditor.DivideIntCeil(decorators.Length, iconColumns);
                var (totalHeight, visibleHeight) = BigCamoEditor.CalculateScrollViewTotalAndVisibleHeight(totalRows, 5, iconSize, smallMargin);

                var totalRect = new Rect(x, y, boxWidth, totalHeight);
                var visibleRect = new Rect(x, y, boxWidth + 16, visibleHeight);

                BigCamoEditor.DrawScrollBar(x + boxWidth + 5, y, totalHeight, visibleHeight, PrefabsScrollPosition);
                PrefabsScrollPosition = GUI.BeginScrollView(visibleRect, PrefabsScrollPosition, totalRect, GUIStyle.none, GUIStyle.none);

	            for (var i = 0; i < decorators.Length; i++)
				{
	                var prefabName = decorators[i];
	                var prefabData = Plugin.GetPrefabData(prefabName);

	                var ix = i % iconColumns;
	                var iy = i / iconColumns;

	                var xi = x + ix * (iconSize + smallMargin);
	                var yi = y + iy * (iconSize + smallMargin);

	                if (GUI.Button(new Rect(xi, yi, iconSize, iconSize), prefabData.Preview))
	                {
	                    var e = Event.current;
	                    if (e.button == 0) // left click
	                    {
							if (decoratorInfo.Prefab != prefabName)
							{
								Plugin.ChangePrefab(ItemId, decoratorIndex, decoratorInfo, prefabName);
							}
	                    }
	                    if (e.button == 1) // right click
	                    {
	                        // Plugin.ToggleFavouriteTexture(textureName);
	                    }
	                }
				}

                GUI.EndScrollView();
			}
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
