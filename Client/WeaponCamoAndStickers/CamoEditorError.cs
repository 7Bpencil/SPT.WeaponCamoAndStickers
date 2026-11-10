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

        public void DrawWindow()
		{
            // we copy some styles from GUI.skin which can be accessed only from OnGUI call
            if (CamoEditorStyle == null)
            {
                CamoEditorStyle = new(GUI.skin, CamoEditorResources);
            }

            var originalMatrix = GUI.matrix;
            GUI.matrix = CamoEditor.CalculateUIScaleMatrix();

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
            return openCloseButtonHeight;
		}

		private void DrawErrorMessageWindow(int windowID)
		{
            CamoEditor.DrawColor(new Rect(0, 0, windowWidth, WindowRect.height), backgroundColor);

            GUI.Label(new Rect(bigMargin, 0, boxWidth, WindowRect.height), ErrorMessage, CamoEditorStyle.LabelStyleValue);

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
