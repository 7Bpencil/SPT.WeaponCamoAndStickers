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
using EFT.UI.WeaponModding;
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

// TODO by default give only camos and eft stickers, others are addons

namespace SevenBoldPencil.WeaponCamo
{
    public class ItemsWithDecals
    {
        // yes, there can be multiple items with same Id,
        // for example when you open item preview of weapon you already hold in hands,
        // or when hideout shooting range clones weapon (we pretend that they have the same Id)
        public Dictionary<int, ItemWithDecals> Items; // TODO iterating dict is probably not the best idea, but list in annoying
        public List<DecalInfo> DecalsInfo;
    }

    public class ItemWithDecals
    {
        public Transform DecalsRoot;
        public List<Decal> Decals;
    }

    public class DecalInfo
    {
        public string Texture;
        public Color Color; // TODO make full color picker, I am too tired rn
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale;
        public float MaxAngle;

        public DecalInfo GetCopy()
        {
            // this is enough for now
            return (DecalInfo)MemberwiseClone();
        }
    }

    public class CamoEditor
    {
        public Camera Camera;
        public RuntimeGizmos RuntimeGizmos;
        public string ItemId;
        public int InstanceID;
        public Transform DecalsRoot;
        public bool IsVisible;
        public Option<int> CurrentlyEditedDecalIndex;
        public RuntimeTransformHandle TransformHandle;
		public Rect WindowRect;
    }

    public class CamoEditorResources
    {
        public Shader PositionHandleShader;
        public Shader RotationHandleShader;
        public Shader ScaleHandleShader;
        public Texture2D MoveUpIcon;
        public Texture2D MoveDownIcon;
        public Texture2D EditPositionIcon;
        public Texture2D EditRotationIcon;
        public Texture2D EditScaleIcon;
        public Texture2D DuplicateIcon;
        public Texture2D DeleteIcon;

        public GUIStyle LabelStyleName = new()
        {
            alignment = TextAnchor.MiddleLeft,
            normal = new GUIStyleState()
            {
                textColor = Color.white
            }
        };

        public GUIStyle TextureNameStyle = new()
        {
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            normal = new GUIStyleState()
            {
                textColor = Color.white
            }
        };

        public GUIStyle LabelStyleValue = new()
        {
            alignment = TextAnchor.MiddleCenter,
            normal = new GUIStyleState()
            {
                textColor = Color.white
            }
        };

        public CamoEditorResources(AssetBundle bundle)
        {
            PositionHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamo/Shaders/HandleShader.shader");
            RotationHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamo/Shaders/AdvancedHandleShader.shader");
            ScaleHandleShader = bundle.LoadAsset<Shader>("Assets/WeaponCamo/Shaders/HandleShader.shader");
            MoveUpIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/up-arrow.png");
            MoveDownIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/down-arrow.png");
            EditPositionIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/Move-Icon.png");
            EditRotationIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/Rotate-Icon.png");
            EditScaleIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/Scale-Icon.png");
            DuplicateIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/copy.png");
            DeleteIcon = bundle.LoadAsset<Texture2D>("Assets/WeaponCamo/Icons/bin.png");
        }
    }

    [BepInPlugin("7Bpencil.WeaponCamo", "7Bpencil.WeaponCamo", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string DefaultTextureName = "default";

        public static Plugin Instance;

		public ManualLogSource LoggerInstance;

        public static ConfigEntry<KeyboardShortcut> ShowHideCamoEditor;

        public DecalRenderer DecalRenderer;
        public Option<CamoEditor> CamoEditor;
        public CamoEditorResources CamoEditorResources;
        public bool IsCamoEditorWaitingForWeaponPreview;

        public string AssemblyDir;
        public string DecalTexturesDir;
        public AssetBundle Bundle;
        public Shader DecalShader;
        public List<string> LoadedDecalTexturesList;
        public Dictionary<string, Texture2D> LoadedDecalTextures; // TODO return ERROR texture if tries to get unknown texture
        public Dictionary<string, ItemsWithDecals> ItemsWithDecals;
        public Dictionary<string, string> Clones;
        public Dictionary<Camera, string> WeaponPreviewCameras;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            ShowHideCamoEditor = Config.Bind("Main", "Show/Hide Camo Editor", new KeyboardShortcut(KeyCode.F4), "Show/Hide Camo Editor");

            AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DecalTexturesDir = Path.Combine(AssemblyDir, "assets", "images");
			var bundlePath = Path.Combine(AssemblyDir, "assets", "bundles", "weaponcamo");
            Bundle = AssetBundle.LoadFromFile(bundlePath);
            DecalShader = Bundle.LoadAsset<Shader>("Assets/WeaponCamo/Shaders/DecalDynamic.shader");
            (LoadedDecalTexturesList, LoadedDecalTextures) = LoadTexturesFromDirectory(DecalTexturesDir, Bundle);
            CamoEditorResources = new(Bundle);
            ItemsWithDecals = new();
            Clones = new();
            WeaponPreviewCameras = new();

            DecalRenderer = new(ItemsWithDecals, WeaponPreviewCameras);

            new Patch_WeaponPreview_Class3271_method_1().Enable();
            new Patch_WeaponPreview_Rotate().Enable();
            new Patch_WeaponPreview_Hide().Enable();
            new Patch_WeaponModdingScreen_Show().Enable();
            new Patch_WeaponModdingScreen_Close().Enable();
            new Patch_WeaponPrefab_InitHotObjects().Enable();
            new Patch_WeaponPrefab_ReturnToPool().Enable();
            new Patch_AssetPoolObject_OnDestroy().Enable();
            new Patch_GClass3380_CloneItem().Enable();

            // TODO
            // seems like decals are not drawing on gun (because of stencil?)
            // does it mean we can make decals that will only apply on gun (and not hands and env)?

            // TODO
            // add option to collapse decal settings

            // TODO
            // add color picker

            // TODO
            // add support for non square decal textures

            // TODO
            // maybe apply camo texture on top of diffuse texture?

            // TODO
            // we can save items as jsons in assets/items/ each item separate file with name=item.Id
            // we can save presets as jsons in assets/presets/ each preset separate file with name=preset name (with support for subfolders)
            // show presets per weapon template (Item.TemplateId?)

            // TODO
            // add reset rotation button

            // TODO
            // hear me out: we can place 3D models as decorations on guns and equipment!

            // TODO
            // it doesnt work without Unity Explorer enabled, cursor glitches out

            // TODO
            // are gifs possible?
        }

        // TODO
        // add support for adding/removing images on running game
        // add support for drawing sub folders as file tree
        public (List<string>, Dictionary<string, Texture2D>) LoadTexturesFromDirectory(string directoryPath, AssetBundle bundle)
        {
            var filePaths = Directory.GetFiles(directoryPath, "*", new EnumerationOptions()
            {
                RecurseSubdirectories = true
            });
            var resultList = new List<string>(filePaths.Length + 1);
            var resultDict = new Dictionary<string, Texture2D>();

            {
                resultList.Add(DefaultTextureName);
                resultDict.Add(DefaultTextureName, Texture2D.whiteTexture);
            }

            foreach (var filePath in filePaths)
            {
                var extension = Path.GetExtension(filePath);
                if (!(extension == ".png" || extension == ".jpg"))
                {
                    continue;
                }

                var fileData = File.ReadAllBytes(filePath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, fileData))
                {
                    var name = filePath
                        .Replace(directoryPath, "")
                        .Replace(extension, "")
                        .Remove(0, 1); // remove first slash

                    resultList.Add(name);
                    resultDict.Add(name, texture);
                }
                else
                {
                    LoggerInstance.LogError($"Failed to load decal texture: {name}");
                }
            }

            return (resultList, resultDict);
        }

		public void Update()
		{
            if (Input.GetKeyDown(ShowHideCamoEditor.Value.MainKey) && CamoEditor.Some(out var camoEditor))
            {
                camoEditor.IsVisible = !camoEditor.IsVisible;
            }
		}

        public void LateUpdate()
        {
            if (CamoEditor.Some(out var camoEditor) &&
                camoEditor.CurrentlyEditedDecalIndex.Some(out var currentlyEditedDecalIndex) &&
                camoEditor.RuntimeGizmos &&
                ItemsWithDecals.TryGetValue(camoEditor.ItemId, out var itemsWithDecals) &&
                itemsWithDecals.Items.TryGetValue(camoEditor.InstanceID, out var itemWithDecals))
            {
                var decal = itemWithDecals.Decals[currentlyEditedDecalIndex];
                if (decal)
                {
                    var decalTransform = decal.DecalTransform;
                    var position = decalTransform.position;
                    var scale = decalTransform.lossyScale;

                    camoEditor.RuntimeGizmos.Cubes.Add(new RuntimeGizmos.Cube()
                    {
                        Position = position,
                        Rotation = decalTransform.rotation,
                        Scale = scale,
                    });
                    camoEditor.RuntimeGizmos.Lines.Add(new RuntimeGizmos.Line()
                    {
                        Start = position,
                        End = position + decalTransform.up * scale.y,
                    });
                }
            }
        }

        private const float start = 23;
        private const float windowWidth = margin + (iconSize + iconSeparator) * iconColumns - iconSeparator + margin; // TODO adjust to 5 icons width
        private const float buttonHeight = 30;
        private const float buttonSeparator = 4;
        private const float margin = 14;
        private const float startMarginY = 15 + margin;
        private const float iconSize = 66;
        private const float iconSeparator = 3;
        private const float smallIconSize = (iconSize - iconSeparator) / 2;
        private const int iconColumns = 5;
        private const float boxWidth = windowWidth - margin * 2;
        private const float boxHeight = iconSize + boxMargin * 2;
        private const float boxMargin = 3;
        private const float nameWidth = 120;
        private const float longFieldWidth = 60;
        private const float fixTransformButtonWidth = 110;

        public void OnGUI()
        {
            if (CamoEditor.Some(out var camoEditor) && camoEditor.IsVisible)
            {
    			// if width == 0, then windowRect has not been initialized, so init it
    			ref var windowRect = ref camoEditor.WindowRect;
    			if (windowRect.width == 0f)
    			{
    				windowRect.width = windowWidth;
    				windowRect.x = start;
    				windowRect.y = start;
    			}

				windowRect.height = CalculateWindowHeight(camoEditor);
                windowRect = GUI.Window(1, windowRect, WindowFunction, $"Camo Editor");
            }
        }

        private float CalculateWindowHeight(CamoEditor camoEditor)
        {
            if (camoEditor.CurrentlyEditedDecalIndex.Some(out var currentlyEditedDecalIndex))
            {
                return 600 + iconSize + margin;
            }
            else
            {
                var height = startMarginY;
                if (ItemsWithDecals.TryGetValue(camoEditor.ItemId, out var itemsWithDecals))
                {
                    height += itemsWithDecals.DecalsInfo.Count * (boxHeight + 8);
                }

                height += buttonHeight;
                height += margin;

                return height;
            }
        }

        private void WindowFunction(int windowID)
		{
            // TODO add small handle at the top right side of the window to collapse/expand it
            var camoEditor = CamoEditor.Value;

            if (camoEditor.CurrentlyEditedDecalIndex.Some(out var currentlyEditedDecalIndex))
            {
                DrawDecalEditUI(camoEditor, currentlyEditedDecalIndex);
            }
            else
            {
                DrawDecalsListUI(camoEditor);
            }

			GUI.DragWindow();
        }

        public void DrawDecalsListUI(CamoEditor camoEditor)
        {
            var x = margin;
            var y = startMarginY;

            if (ItemsWithDecals.TryGetValue(camoEditor.ItemId, out var itemsWithDecals))
            {
                // TODO add scroll view
                var decalsInfo = itemsWithDecals.DecalsInfo;
                for (var i = 0; i < decalsInfo.Count; i++)
                {
                    var decalInfo = decalsInfo[i];
                    DrawDecalElementUI(camoEditor, x, y, i, decalInfo);
                    y += boxHeight + 8;
                }
            }

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Add New Decal"))
            {
                var newDecalIndex = AddNewDecal(camoEditor);
                camoEditor.CurrentlyEditedDecalIndex = new(newDecalIndex);
            }
        }

        private void DrawDecalElementUI(CamoEditor camoEditor, float x, float y, int decalIndex, DecalInfo decalInfo)
        {
            var texture = LoadedDecalTextures[decalInfo.Texture];

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), default(string));

            var topLineY = y + boxMargin;
            var bottomLineY = topLineY + smallIconSize + iconSeparator;

            var textureIconX = x + boxMargin;
            if (GUI.Button(new Rect(textureIconX, topLineY, iconSize, iconSize), texture))
            {
                camoEditor.CurrentlyEditedDecalIndex = new(decalIndex);
            }

            var labelX = textureIconX + iconSize + iconSeparator + 2;
            GUI.Label(new Rect(labelX, topLineY + 1, 270, iconSize), decalInfo.Texture, CamoEditorResources.TextureNameStyle);

            var deleteX = x + boxWidth - (iconSeparator + smallIconSize) * 3;
            if (GUI.Button(new Rect(deleteX, bottomLineY, smallIconSize, smallIconSize), CamoEditorResources.DeleteIcon))
            {
                Delete(camoEditor, decalIndex);
            }

            var duplicateX = deleteX + smallIconSize + iconSeparator;
            if (GUI.Button(new Rect(duplicateX, bottomLineY, smallIconSize, smallIconSize), CamoEditorResources.DuplicateIcon))
            {
                var newDecalIndex = Duplicate(camoEditor, decalIndex);
                camoEditor.CurrentlyEditedDecalIndex = new(newDecalIndex);
            }

            var arrowX = duplicateX + smallIconSize + iconSeparator;
            if (GUI.Button(new Rect(arrowX, topLineY, smallIconSize, smallIconSize), CamoEditorResources.MoveUpIcon))
            {
                Swap(camoEditor, decalIndex, decalIndex - 1);
            }
            if (GUI.Button(new Rect(arrowX, bottomLineY, smallIconSize, smallIconSize), CamoEditorResources.MoveDownIcon))
            {
                Swap(camoEditor, decalIndex, decalIndex + 1);
            }
        }

        private void DrawDecalEditUI(CamoEditor camoEditor, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];
            var decal = itemsWithDecals.Items[camoEditor.InstanceID].Decals[decalIndex];
            var texture = LoadedDecalTextures[decalInfo.Texture];

            var x = margin;
            var y = startMarginY;

            if (GUI.Button(new Rect(x, y, boxWidth, buttonHeight), "Back"))
            {
                camoEditor.CurrentlyEditedDecalIndex = default;
                DestroyTransformHandle(camoEditor);
            }
            y += buttonHeight + margin;

            {
                var columnY = y;

                if (GUI.Button(new Rect(x, columnY, smallIconSize, smallIconSize), CamoEditorResources.EditPositionIcon))
                {
                    SetupTransformHandle(camoEditor, HandleType.POSITION, decalIndex, decal);
                }
                {
                    var valueX = x + smallIconSize + iconSeparator + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localPosition.x:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localPosition.y:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localPosition.z:F3}", CamoEditorResources.LabelStyleName);
                }
                columnY += smallIconSize + iconSeparator;


                if (GUI.Button(new Rect(x, columnY, smallIconSize, smallIconSize), CamoEditorResources.EditRotationIcon))
                {
                    SetupTransformHandle(camoEditor, HandleType.ROTATION, decalIndex, decal);
                }
                {
                    var valueX = x + smallIconSize + iconSeparator + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localEulerAngles.x:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localEulerAngles.y:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localEulerAngles.z:F3}", CamoEditorResources.LabelStyleName);
                }
                {
                    if (GUI.Button(new Rect(x + boxWidth - fixTransformButtonWidth, columnY, fixTransformButtonWidth, smallIconSize), "round to degree"))
                    {
                        RoundLocalEulerAnglesToDegree(camoEditor, decalIndex);
                    }
                }
                columnY += smallIconSize + iconSeparator;

                if (GUI.Button(new Rect(x, columnY, smallIconSize, smallIconSize), CamoEditorResources.EditScaleIcon))
                {
                    SetupTransformHandle(camoEditor, HandleType.SCALE, decalIndex, decal);
                }
                {
                    var valueX = x + smallIconSize + iconSeparator + 7;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"X: {decal.DecalTransform.localScale.x:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Y: {decal.DecalTransform.localScale.y:F3}", CamoEditorResources.LabelStyleName);
                    valueX += longFieldWidth + iconSeparator;

                    GUI.Label(new Rect(valueX, columnY, longFieldWidth, buttonHeight), $"Z: {decal.DecalTransform.localScale.z:F3}", CamoEditorResources.LabelStyleName);
                }
                {
                    if (GUI.Button(new Rect(x + boxWidth - fixTransformButtonWidth, columnY, fixTransformButtonWidth, smallIconSize), "fix aspect ratio"))
                    {
                        FixAspectRatio(camoEditor, decalIndex);
                    }
                }
                columnY += smallIconSize + iconSeparator;

                GUI.Button(new Rect(x, columnY, iconSize, iconSize), texture);

                var labelX = x + iconSize + iconSeparator + 12;
                GUI.Label(new Rect(labelX, columnY + 1, 256, smallIconSize), decalInfo.Texture, CamoEditorResources.TextureNameStyle);

                columnY += iconSize + iconSeparator;

                y = columnY;
            }

            {
                var sliderWidth = 212;

                var labelX = x;
                var sliderX = labelX + nameWidth + iconSeparator - 42;
                var valueX = sliderX + sliderWidth + iconSeparator;

                var opacityY = y;
                var maxAngleY = opacityY + buttonHeight + iconSeparator;


                GUI.Label(new Rect(labelX, opacityY, nameWidth, buttonHeight), "Opacity:", CamoEditorResources.LabelStyleName);
                var newAlpha = GUI.HorizontalSlider(new Rect(sliderX, opacityY + 11, sliderWidth, buttonHeight), decalInfo.Color.a, 0f, 1f);
                if (newAlpha != decalInfo.Color.a)
                {
                    decalInfo.Color = decalInfo.Color.WithAlpha(newAlpha);
                    ModfiyDecalOnItems(decalIndex, itemsWithDecals.Items, decal =>
                    {
                        decal.ChangeColor(decalInfo.Color);
                    });
                }
                GUI.Label(new Rect(valueX, opacityY, longFieldWidth, buttonHeight), $"{decalInfo.Color.a:F3}", CamoEditorResources.LabelStyleValue);


                GUI.Label(new Rect(labelX, maxAngleY, nameWidth, buttonHeight), "MaxAngle:", CamoEditorResources.LabelStyleName);
                var newMaxAngle = GUI.HorizontalSlider(new Rect(sliderX, maxAngleY + 11, sliderWidth, buttonHeight), decalInfo.MaxAngle, 0f, 1f);
                if (newMaxAngle != decalInfo.MaxAngle)
                {
                    decalInfo.MaxAngle = newMaxAngle;
                    ModfiyDecalOnItems(decalIndex, itemsWithDecals.Items, decal =>
                    {
                        decal.ChangeMaxAngle(newMaxAngle);
                    });
                }
                GUI.Label(new Rect(valueX, maxAngleY, longFieldWidth, buttonHeight), $"{decalInfo.MaxAngle:F3}", CamoEditorResources.LabelStyleValue);


                y = maxAngleY + buttonHeight + iconSeparator;
            }


            GUI.DrawTexture(new Rect(x, y, boxWidth, 2), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.1f, 0.1f, 0.1f, 1f), 0, 0);
            y += 2 + margin;

            DrawAllTextures(camoEditor, x, y, decalIndex, decalInfo, itemsWithDecals);
        }

        public void SetupTransformHandle(CamoEditor camoEditor, HandleType handleType, int decalIndex, Decal decal)
        {
            if (camoEditor.TransformHandle)
            {
                if (camoEditor.TransformHandle.type == handleType)
                {
                    return;
                }
            }
            else
            {
                camoEditor.TransformHandle = RuntimeTransformHandle.Create
                (
                    decal.DecalTransform,
                    camoEditor.Camera,
                    CamoEditorResources.PositionHandleShader,
                    CamoEditorResources.RotationHandleShader,
                    CamoEditorResources.ScaleHandleShader
                );

                camoEditor.TransformHandle.OnEndedDraggingHandle += () => OnEndedDraggingHandle(camoEditor, decalIndex);
            }

            camoEditor.TransformHandle.SetHandleMode(handleType);
			TransformHelperClass.SetLayersRecursively(camoEditor.TransformHandle.gameObject, LayerMaskClass.WeaponPreview);
        }

        public void OnEndedDraggingHandle(CamoEditor camoEditor, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];
            var decal = itemsWithDecals.Items[camoEditor.InstanceID].Decals[decalIndex];

            decalInfo.LocalPosition = decal.DecalTransform.localPosition;
            decalInfo.LocalEulerAngles = decal.DecalTransform.localEulerAngles;
            decalInfo.LocalScale = decal.DecalTransform.localScale;

            ModfiyDecalOnItems(decalIndex, itemsWithDecals.Items, decal =>
            {
                decal.DecalTransform.localPosition = decalInfo.LocalPosition;
                decal.DecalTransform.localEulerAngles = decalInfo.LocalEulerAngles;
                decal.DecalTransform.localScale = decalInfo.LocalScale;
            });
        }

        public void DestroyTransformHandle(CamoEditor camoEditor)
        {
            if (camoEditor.TransformHandle)
            {
                Destroy(camoEditor.TransformHandle.gameObject);
            }
        }

        private void DrawAllTextures(CamoEditor camoEditor, float x, float y, int decalIndex, DecalInfo decalInfo, ItemsWithDecals itemsWithDecals)
        {
            for (var i = 0; i < LoadedDecalTexturesList.Count; i++)
            {
                var textureName = LoadedDecalTexturesList[i];
                var texture = LoadedDecalTextures[textureName];

                var ix = i % iconColumns;
                var iy = i / iconColumns;

                var xi = x + ix * (iconSize + iconSeparator);
                var yi = y + iy * (iconSize + iconSeparator);

                if (GUI.Button(new Rect(xi, yi, iconSize, iconSize), texture))
                {
                    if (decalInfo.Texture != textureName)
                    {
                        decalInfo.Texture = textureName;
                        ModfiyDecalOnItems(decalIndex, itemsWithDecals.Items, decal =>
                        {
                            decal.ChangeTexture(texture);
                        });
                        FixAspectRatio(camoEditor, decalIndex);
                    }
                }
            }
        }

        public int Duplicate(CamoEditor camoEditor, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];
            var decalInfoDuplicate = decalInfo.GetCopy();
            itemsWithDecals.DecalsInfo.Insert(decalIndex, decalInfoDuplicate);
            foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
            {
                var decal = CreateDecal(decalInfo, itemWithDecals.DecalsRoot);
                itemWithDecals.Decals.Insert(decalIndex, decal);
            }

            return decalIndex + 1;
        }

        public void Delete(CamoEditor camoEditor, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
            itemsWithDecals.DecalsInfo.RemoveAt(decalIndex);
            foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
            {
                var decal = itemWithDecals.Decals[decalIndex];
                itemWithDecals.Decals.RemoveAt(decalIndex);
                Destroy(decal.gameObject);
            }
        }

        public void Swap(CamoEditor camoEditor, int decalIndexA, int decalIndexB)
        {
            var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
            var decalsInfo = itemsWithDecals.DecalsInfo;

            if (decalIndexA < 0 || decalIndexA > decalsInfo.Count - 1 ||
                decalIndexB < 0 || decalIndexB > decalsInfo.Count - 1)
            {
                return;
            }

            (decalsInfo[decalIndexA], decalsInfo[decalIndexB]) = (decalsInfo[decalIndexB], decalsInfo[decalIndexA]);
            foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
            {
                var decals = itemWithDecals.Decals;
                (decals[decalIndexA], decals[decalIndexB]) = (decals[decalIndexB], decals[decalIndexA]);
            }
        }

        public int AddNewDecal(CamoEditor camoEditor)
        {
            var decalInfo = new DecalInfo()
            {
                Texture = DefaultTextureName,
                Color = Color.white,
                LocalPosition = typicalRifleCenter,
                LocalEulerAngles = Decal.LeftSideDecalRotation,
                LocalScale = new Vector3(defaultDecalSize, defaultDecalDepth, defaultDecalSize),
                MaxAngle = 0.8f,
            };

            if (ItemsWithDecals.ContainsKey(camoEditor.ItemId))
            {
                var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
                itemsWithDecals.DecalsInfo.Add(decalInfo);
                foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
                {
                    var decal = CreateDecal(decalInfo, itemWithDecals.DecalsRoot);
                    itemWithDecals.Decals.Add(decal);
                }

                return itemsWithDecals.DecalsInfo.Count - 1;
            }
            else
            {
                var decal = CreateDecal(decalInfo, camoEditor.DecalsRoot);
                var decals = new List<Decal>() { decal };
                var decalsInfo = new List<DecalInfo>() { decalInfo };
                var itemsWithDecals = new ItemsWithDecals() {
                    Items = new Dictionary<int, ItemWithDecals>() {
                        {
                            camoEditor.InstanceID,
                            new ItemWithDecals() {
                                DecalsRoot = camoEditor.DecalsRoot,
                                Decals = decals,
                            }
                        }
                    },
                    DecalsInfo = decalsInfo
                };

                ItemsWithDecals.Add(camoEditor.ItemId, itemsWithDecals);
                WeaponPreviewCameras.Add(camoEditor.Camera, camoEditor.ItemId);

                return 0;
            }
        }

        public void RoundLocalEulerAnglesToDegree(CamoEditor camoEditor, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];

            decalInfo.LocalEulerAngles.x = MathF.Round(decalInfo.LocalEulerAngles.x);
            decalInfo.LocalEulerAngles.y = MathF.Round(decalInfo.LocalEulerAngles.y);
            decalInfo.LocalEulerAngles.z = MathF.Round(decalInfo.LocalEulerAngles.z);

            ModfiyDecalOnItems(decalIndex, itemsWithDecals.Items, decal =>
            {
                decal.DecalTransform.localEulerAngles = decalInfo.LocalEulerAngles;
            });
        }

        public void FixAspectRatio(CamoEditor camoEditor, int decalIndex)
        {
            var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
            var decalInfo = itemsWithDecals.DecalsInfo[decalIndex];
            var texture = LoadedDecalTextures[decalInfo.Texture];
            var textureInverseAspectRatio = texture.height / (float)texture.width;

            // we keep decal width the same and change height to match texture aspect ratio
            decalInfo.LocalScale.z = decalInfo.LocalScale.x * textureInverseAspectRatio;

            ModfiyDecalOnItems(decalIndex, itemsWithDecals.Items, decal =>
            {
                decal.DecalTransform.localScale = decalInfo.LocalScale;
            });
        }

        // notice that we modify decal on all items
        public static void ModfiyDecalOnItems(int decalIndex, Dictionary<int, ItemWithDecals> items, Action<Decal> changeDecal)
        {
            foreach (var (_, itemWithDecals) in items)
            {
                var decal = itemWithDecals.Decals[decalIndex];
                changeDecal(decal);
            }
        }

        private readonly Vector3 typicalRifleCenter = new Vector3(0f, -0.35f, -0.003f);
        private readonly float defaultDecalSize = 0.2f;
        private readonly float defaultDecalDepth = 0.1f;

        public void OnWeaponPrefabCreated(string itemId, WeaponPrefab weaponPrefab)
        {
            itemId = GetOriginalItemId(itemId);
            if (!ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                Logger.LogInfo($"OnWeaponPrefabCreated: {itemId}, no decals info");
                return;
            }

            var instanceID = weaponPrefab.GetInstanceID();
            if (itemsWithDecals.Items.ContainsKey(instanceID))
            {
                Logger.LogError($"OnWeaponPrefabCreated: {itemId}, tried to init multiple times?");
                return;
            }

            var decalsRoot = GetWeaponRoot(weaponPrefab);
            var decalsInfo = itemsWithDecals.DecalsInfo;
            var decals = new List<Decal>(decalsInfo.Count);
            foreach (var decalInfo in decalsInfo)
            {
                var decal = CreateDecal(decalInfo, decalsRoot);
                decals.Add(decal);
            }

            var itemWithDecals = new ItemWithDecals()
            {
                DecalsRoot = decalsRoot,
                Decals = decals,
            };

            itemsWithDecals.Items.Add(instanceID, itemWithDecals);

            Logger.LogInfo($"OnWeaponPrefabCreated: {itemId}, success");
        }

		public Decal CreateDecal(DecalInfo decalInfo, Transform root)
		{
            var decal = new GameObject("Decal", typeof(Decal)).GetComponent<Decal>();
			decal.Init(DecalShader);
			decal.Set(decalInfo, root, LoadedDecalTextures);
			return decal;
		}

		public static Transform GetWeaponRoot(WeaponPrefab weaponPrefab)
		{
			return weaponPrefab.Hierarchy.GetTransform(ECharacterWeaponBones.weapon);
		}

        public void OnWeaponPrefabDestroyed(string itemId, WeaponPrefab weaponPrefab)
        {
            itemId = GetOriginalItemId(itemId);
            if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                var instanceID = weaponPrefab.GetInstanceID();
                if (itemsWithDecals.Items.Remove(instanceID, out var itemWithDecals))
                {
        			Logger.LogInfo($"OnWeaponPrefabDestroyed: {itemId}, {instanceID}");
                    foreach (var decal in itemWithDecals.Decals)
                    {
                        if (decal)
                        {
                            Destroy(decal.gameObject);
                        }
                    }
                }
            }
        }

        public void OnCloneItem(string originalId, string cloneId)
        {
            // when user tries weapon in hideout shooting range,
            // all his gear gets copied to new items to preserve
            // original durability/ammo count/etc,
            // but we have to clone decals ourselves
            if (ItemsWithDecals.ContainsKey(originalId))
            {
                Logger.LogInfo($"OnCloneItem: original: {originalId}, clone: {cloneId}");
                Clones.Add(cloneId, originalId);
            }
        }

        public string GetOriginalItemId(string itemId)
        {
            if (Clones.TryGetValue(itemId, out var originalId))
            {
                return originalId;
            }

            return itemId;
        }

        public void OnWeaponPreviewOpened(Camera weaponPreviewCamera, string itemId)
        {
			Logger.LogWarning($"OnWeaponPreviewOpened: {itemId}");
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                WeaponPreviewCameras.Add(weaponPreviewCamera, itemId);
            }
        }

        public void OnWeaponPreviewClosed(Camera weaponPreviewCamera, string itemId)
        {
			Logger.LogWarning($"OnWeaponPreviewClosed: {itemId}");
            WeaponPreviewCameras.Remove(weaponPreviewCamera);
        }

        public void SetupCamoEditor(Camera editorCamera, string itemId, WeaponPrefab weaponPrefab)
        {
            itemId = GetOriginalItemId(itemId);
            LoggerInstance.LogInfo($"SetupCamoEditor: {itemId}");
            IsCamoEditorWaitingForWeaponPreview = false;
            var instanceID = weaponPrefab.GetInstanceID();
            var decalsRoot = GetWeaponRoot(weaponPrefab);
            var runtimeGizmos = editorCamera.gameObject.AddComponent<RuntimeGizmos>();
            CamoEditor = new(new CamoEditor()
            {
                Camera = editorCamera,
                RuntimeGizmos = runtimeGizmos,
                ItemId = itemId,
                InstanceID = instanceID,
                DecalsRoot = decalsRoot,
                IsVisible = true,
            });
        }

		public bool CanWeaponPreviewRotate(string itemId)
        {
            if (CamoEditor.Some(out var camoEditor) &&
                camoEditor.ItemId == itemId &&
                camoEditor.CurrentlyEditedDecalIndex.Some(out var currentlyEditedDecalIndex) &&
                camoEditor.TransformHandle)
            {
                return !camoEditor.TransformHandle.IsDragging;
            }

            return true;
        }

        public void CloseCamoEditor()
        {
            IsCamoEditorWaitingForWeaponPreview = false;

			// CloseCamoEditor method can be called
			// even when editor is not intialized, this happens in cases:
			// 1) user can quickly tap Modify and hit Escape,
			// which means weapon preview won't be fully loaded,
			// 2) WeaponModdingScreen.Close is called even if user
			// entered customization window on trader guns

            if (!CamoEditor.Some(out var camoEditor))
            {
                LoggerInstance.LogWarning($"CloseCamoEditor: tried to close uninitialized decal editor");
                return;
            }

            var itemId = camoEditor.ItemId;
            if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
            {
                var decalsInfo = itemsWithDecals.DecalsInfo;
                if (decalsInfo.Count == 0)
                {
                    ItemsWithDecals.Remove(itemId);
                    RemoveDecalsFile(itemId);
                    LoggerInstance.LogInfo($"CloseCamoEditor: {itemId} remove decals");
                }
                else
                {
                    WriteDecalsToFile(itemId, decalsInfo);
                    LoggerInstance.LogInfo($"CloseCamoEditor: {itemId} rewrite decals");
                }
            }

            if (camoEditor.RuntimeGizmos)
            {
                Destroy(camoEditor.RuntimeGizmos);
            }
            DestroyTransformHandle(camoEditor);
            CamoEditor = default;
        }

        public void WriteDecalsToFile(string itemId, List<DecalInfo> decalsInfo)
        {
            // TODO
            // dump decalsInfo on disk
            // create if doesnt exist, rewrite if does
            // assets/items/itemId.json
        }

        public void RemoveDecalsFile(string itemId)
        {
            // TODO
        }
    }
}
