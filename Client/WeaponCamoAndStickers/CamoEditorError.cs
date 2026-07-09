//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using UnityEngine;
using static SevenBoldPencil.WeaponCamoAndStickers.CamoEditorConstants;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
    public class CamoEditorError
    {
        public Plugin Plugin;
        public CamoEditorResources CamoEditorResources;
        public CamoEditorStyle CamoEditorStyle;
		public string ErrorMessage;
        public bool IsOpened;
		public Rect WindowRect = WeaponCamoAndStickers.CamoEditor.GetDefaultWindowRect();

		public const int errorMargin = (openCloseButtonHeight - buttonHeight) / 2;

        public void DrawWindow()
		{
            // we copy some styles from GUI.skin which can be accessed only from OnGUI call
            if (CamoEditorStyle == null)
            {
                CamoEditorStyle = new(GUI.skin);
            }

            var originalMatrix = GUI.matrix;
            GUI.matrix = CamoEditor.CalculateUIScale();

            if (IsOpened)
            {
                WindowRect.height = CalculateErrorWindowHeight();
                WindowRect = GUI.Window(1, WindowRect, DrawErrorMessageWindow, GUIContent.none);

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

        private void DrawClosedWindow(int windowID)
        {
            CamoEditor.DrawColor(new Rect(0, 0, mainIconWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(new Rect(0, 0, mainIconWidth, openCloseButtonHeight), CamoEditorResources.MainIcon, ScaleMode.StretchToFill);

			GUI.DragWindow();
        }

        private void DrawClosedWindowOpenButton(int windowID)
        {
            CamoEditor.DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.ClosedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                IsOpened = true;
				WindowRect.width = windowWidth;
            }
        }

		private int CalculateErrorWindowHeight()
		{
            return errorMargin + buttonHeight + errorMargin;
		}

		private void DrawErrorMessageWindow(int windowID)
		{
            CamoEditor.DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            var x = errorMargin;
            var y = errorMargin;

            GUI.Label(new Rect(x, y, boxWidth, buttonHeight), ErrorMessage, CamoEditorStyle.LabelStyleValue);
			y += buttonHeight + errorMargin;

			GUI.DragWindow();
		}

        private void DrawOpenedWindowCloseButton(int windowID)
        {
            CamoEditor.DrawColor(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), backgroundColor);
            GUI.DrawTexture(openCloseButtonIconRect, CamoEditorResources.OpenedIcon, ScaleMode.StretchToFill);
            if (GUI.Button(new Rect(0, 0, openCloseButtonWidth, openCloseButtonHeight), GUIContent.none, GUIStyle.none))
            {
                IsOpened = false;
				WindowRect.width = mainIconWidth;
				WindowRect.height = openCloseButtonHeight;
            }
        }

	}
}
