using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using DeferredDecals;
using EFT;
using EFT.Ballistics;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Systems.Effects;
using UnityEngine;
using UnityEngine.Rendering;

namespace SevenBoldPencil.WeaponCamo
{
    public struct DecalInfo
    {
        public string Texture;
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale;
        public float Opacity;
        public float MaxAngle;
    }

    public class CamoEditor
    {
        public string ItemId;
        public Transform DecalsRoot;
        public List<DecalUI> Decals;
        public bool IsVisible;
        public bool IsDecalTextureSelectionScreenVisible;
        public int CurrentlyEditedDecal; // TODO this should be Option<int>
    }

    public class DecalUI
    {
        public Decal Decal;
        public string Texture;
        public float Opacity;
        public float MaxAngle;
        public string Size;
        public string Depth;
    }

    [BepInPlugin("7Bpencil.WeaponCamo", "7Bpencil.WeaponCamo", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string DefaultTextureName = "default";

        public static Plugin Instance;

		public ManualLogSource LoggerInstance;

        public static ConfigEntry<KeyboardShortcut> ShowHideCamoEditor;

        public RuntimeGizmos RuntimeGizmos;
        public DecalRenderer DecalRenderer;
        public Option<CamoEditor> CamoEditor;
        public bool IsCamoEditorWaitingForWeaponPreview;

        public string AssemblyDir;
        public string DecalTexturesDir;
        public AssetBundle Bundle;
        public List<string> LoadedDecalTexturesList;
        public Dictionary<string, Texture2D> LoadedDecalTextures;
        public Dictionary<string, List<DecalInfo>> ItemsWithDecalsInfo;
        public Dictionary<string, List<Decal>> ItemsWithDecals;
        public Dictionary<string, string> Clones;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            ShowHideCamoEditor = Config.Bind("Main", "Show/Hide Camo Editor", new KeyboardShortcut(KeyCode.F4), "Show/Hide Camo Editor");

            AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DecalTexturesDir = Path.Combine(AssemblyDir, "assets", "images");
			var bundlePath = Path.Combine(AssemblyDir, "assets", "bundles", "weaponcamo");
            Bundle = AssetBundle.LoadFromFile(bundlePath);
            var decalDynamicShader = Bundle.LoadAsset<Shader>("Assets/WeaponCamo/Shaders/DecalDynamic.shader");
            (LoadedDecalTexturesList, LoadedDecalTextures) = LoadTexturesFromDirectory(DecalTexturesDir, Bundle);
            ItemsWithDecalsInfo = new();
            ItemsWithDecals = new();
            Clones = new();

            DecalRenderer = new(decalDynamicShader);

            new Patch_WeaponPreview_Class3271_method_1().Enable();
            new Patch_WeaponModdingScreen_Show().Enable();
            new Patch_WeaponModdingScreen_Close().Enable();
            new Patch_WeaponPrefab_InitHotObjects().Enable();
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
            //
            // TODO
            // maybe 3D gizmos (similar to unity editor)
            // is the best way to move/rotate/scale decal?
            //
            // TODO
            // we can save items as jsons in assets/items/ each item separate file with name=item.Id
            // we can save presets as jsons in assets/presets/ each preset separate file with name=preset name (with support for subfolders)
            // show presets per weapon template (Item.TemplateId?)
            //
            // TODO
            // trying weapon in hideout clones it to new weapon with different id,
            // so we need to track clone (UpdateHideoutPlayerInventory)
            // - have separate dict for clones Dictionary<string, string>
            //   clones do not need editor, so shits easier
            // - when clone is created check if original item has decals
            // - when clone is destroyed no need to do anything really
        }

        // TODO
        // add support for adding/removing images on running game
        // add support for drawing sub folders as file tree
        public (List<string>, Dictionary<string, Texture2D>) LoadTexturesFromDirectory(string directoryPath, AssetBundle bundle)
        {
            var filePaths = Directory.GetFiles(directoryPath);
            var resultList = new List<string>(filePaths.Length + 1);
            var resultDict = new Dictionary<string, Texture2D>();

            {
                var defaultTexture = bundle.LoadAsset<Texture2D>($"Assets/WeaponCamo/Images/{DefaultTextureName}.png");
                resultList.Add(DefaultTextureName);
                resultDict.Add(DefaultTextureName, defaultTexture);
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
            if (CameraClass.Exist && !RuntimeGizmos)
            {
    			var camera = CameraClass.Instance.Camera;
                if (camera)
                {
                    RuntimeGizmos = camera.gameObject.AddComponent<RuntimeGizmos>();
                }
            }
		}

        public void LateUpdate()
        {
            if (RuntimeGizmos)
            {
                foreach (var decal in DecalRenderer.Decals)
                {
                    if (decal)
                    {
                        var decalTransform = decal.DecalTransform;
                        var position = decalTransform.position;
                        var scale = decalTransform.lossyScale;

                        RuntimeGizmos.Cubes.Add(new RuntimeGizmos.Cube()
                        {
                            Position = position,
                            Rotation = decalTransform.rotation,
                            Scale = scale,
                        });
                        RuntimeGizmos.Lines.Add(new RuntimeGizmos.Line()
                        {
                            Start = position,
                            End = position + decalTransform.up * scale.y,
                        });
                    }
                }
            }
        }

		private const float windowHeaderHeight = 15f;
		private const float boxHeaderHeight = 20f;
		private const float marginX = 15f;
		private const float marginY = 15f;
        private const float startX = marginX;
		private const float startY = windowHeaderHeight + marginY + separatorY;
		private const float height = 30f;
		private const float separatorX = 3f;
		private const float separatorY = 3f;
        private const float buttonWidth = (60 - separatorX) / 2;
        private const float fieldWidth = 40;
        private const float nameWidth = 120;
        private const float sideButtonWidth = 60;
        private const float longButtonWidth = 60;
        private const float longFieldWidth = 60;
        private const int propertiesCount = 10;

        private const float maxPropertyWidth = nameWidth + separatorX + (longButtonWidth + separatorX) * 4 + longFieldWidth;
        private const float boxWidth = maxPropertyWidth + marginX * 2;
        private const float addDecalButtonWidth = boxWidth;
        private const float windowWidth = boxWidth + marginX * 2;
        private const float boxHeight = boxHeaderHeight + separatorY + (height + separatorY) * propertiesCount;

		private Rect windowRect;

        private GUIStyle labelStyleName = new()
        {
            alignment = TextAnchor.MiddleLeft,
            normal = new GUIStyleState()
            {
                textColor = Color.white
            }
        };

        private GUIStyle labelStyleValue = new()
        {
            alignment = TextAnchor.MiddleCenter,
            normal = new GUIStyleState()
            {
                textColor = Color.white
            }
        };

        public void OnGUI()
        {
            if (CamoEditor.Some(out var camoEditor) && camoEditor.IsVisible)
            {
    			// if width == 0, then windowRect has not been initialized, so init it
    			if (windowRect.width == 0f)
    			{
    				windowRect.width = windowWidth;
    				windowRect.x = 50;
    				windowRect.y = 50;
    			}

    			var windowHeight = startY + height + marginY + (boxHeight + marginY) * DecalRenderer.Decals.Count;

    			windowRect.height = windowHeight;
                windowRect = GUI.Window(1, windowRect, WindowFunction, $"Camo Editor");
            }
        }

        private void WindowFunction(int windowID)
		{
			var x = startX;
			var y = startY;

            var camoEditor = CamoEditor.Value;
            if (camoEditor.IsDecalTextureSelectionScreenVisible)
            {
                DrawDecalTextureSelectorUI(x, y, camoEditor);
            }
            else
            {
                DrawDecalModifierUI(x, y, camoEditor);
            }

			GUI.DragWindow();
        }

        private void DrawDecalTextureSelectorUI(float x, float y, CamoEditor camoEditor)
        {
            var columns = 3;
            var iconSize = (boxWidth - ((columns - 1) * separatorX)) / columns;

            if (GUI.Button(new Rect(x, y, addDecalButtonWidth, height), "Back"))
            {
                camoEditor.IsDecalTextureSelectionScreenVisible = false;
            }
            y += height + marginY;

            for (var i = 0; i < LoadedDecalTexturesList.Count; i++)
            {
                var textureName = LoadedDecalTexturesList[i];
                var texture = LoadedDecalTextures[textureName];
                var xi = i % columns;
                var yi = i / columns;
                if (GUI.Button(new Rect(x + xi * (iconSize + separatorX), y + yi * (iconSize + separatorX), iconSize, iconSize), texture))
                {
                    var decalUI = camoEditor.Decals[camoEditor.CurrentlyEditedDecal];
                    if (decalUI.Texture != textureName)
                    {
                        decalUI.Texture = textureName;
                        decalUI.Decal.ChangeTexture(texture);
                    }
                }
            }
        }

        private void DrawDecalModifierUI(float x, float y, CamoEditor camoEditor)
        {
            if (GUI.Button(new Rect(x, y, addDecalButtonWidth, height), "Add New Decal"))
            {
                var decalInfo = new DecalInfo()
                {
                    Texture = DefaultTextureName,
                    LocalPosition = typicalRifleCenter,
                    LocalEulerAngles = Decal.LeftSideDecalRotation,
                    LocalScale = new Vector3(defaultDecalSize, defaultDecalDepth, defaultDecalSize),
                    Opacity = 1f,
                    MaxAngle = 0.8f,
                };
                var decal = DecalRenderer.CreateDecal(decalInfo, camoEditor.DecalsRoot, LoadedDecalTextures);
                if (ItemsWithDecals.TryGetValue(camoEditor.ItemId, out var decals))
                {
                    decals.Add(decal);
                }
                else
                {
                    var newDecals = new List<Decal>();
                    newDecals.Add(decal);
                    ItemsWithDecals.Add(camoEditor.ItemId, newDecals);
                }

                var decalUI = new DecalUI()
                {
                    Decal = decal,
                    Texture = decalInfo.Texture,
                    Opacity = decalInfo.Opacity,
                    MaxAngle = decalInfo.MaxAngle,
                    Size = defaultDecalSize.ToString(),
                    Depth = defaultDecalDepth.ToString(),
                };

                camoEditor.Decals.Add(decalUI);

            }
            y += height + marginY;

            for (var i = 0; i < camoEditor.Decals.Count; i++)
            {
                DrawDecalUI(x, ref y, i, camoEditor.Decals[i], camoEditor);
                y += marginY;
            }
        }

        private void DrawDecalUI(float x, ref float y, int decalIndex, DecalUI decalUI, CamoEditor camoEditor)
        {
            var decal = decalUI.Decal;
            var localPosition = decal.DecalTransform.localPosition;
            var localRotation = decal.DecalTransform.localEulerAngles;

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), "Decal");
            y += boxHeaderHeight + separatorY;

            {
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Texture:", labelStyleName);
                lineX += nameWidth + separatorX;

                var buttonWidth = boxWidth - nameWidth - separatorX - marginX * 2;
                if (GUI.Button(new Rect(lineX, y, buttonWidth, height), decalUI.Texture))
                {
                    camoEditor.IsDecalTextureSelectionScreenVisible = true;
                    camoEditor.CurrentlyEditedDecal = decalIndex;
                }
            }
            y += height + separatorY;

            {
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Side:", labelStyleName);
                lineX += nameWidth + separatorX;

                if (GUI.Button(new Rect(lineX, y, sideButtonWidth, height), "Left"))
                {
                    decal.ChangeSide(true);
                }
                lineX += sideButtonWidth + separatorX;

                if (GUI.Button(new Rect(lineX, y, sideButtonWidth, height), "Right"))
                {
                    decal.ChangeSide(false);
                }
                lineX += sideButtonWidth + separatorX;
            }
            y += height + separatorY;

            {
                var sliderWidth = 200;
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Opacity:", labelStyleName);
                lineX += nameWidth + separatorX;

                var newOpacity = GUI.HorizontalSlider(new Rect(lineX, y, sliderWidth, height), decalUI.Opacity, 0f, 1f);
                if (newOpacity != decalUI.Opacity)
                {
                    decalUI.Opacity = newOpacity;
                    decal.ChangeOpacity(newOpacity);
                }
                lineX += sliderWidth + separatorX;

                GUI.Label(new Rect(lineX, y, longFieldWidth, height), $"{decalUI.Opacity:F3}", labelStyleValue);
                lineX += longFieldWidth + separatorX;

                y += height + separatorY;
            }

            {
                var sliderWidth = 200;
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "MaxAngle:", labelStyleName);
                lineX += nameWidth + separatorX;

                var newMaxAngle = GUI.HorizontalSlider(new Rect(lineX, y, sliderWidth, height), decalUI.MaxAngle, 0f, 1f);
                if (newMaxAngle != decalUI.MaxAngle)
                {
                    decalUI.MaxAngle = newMaxAngle;
                    decal.ChangeMaxAngle(newMaxAngle);
                }
                lineX += sliderWidth + separatorX;

                GUI.Label(new Rect(lineX, y, longFieldWidth, height), $"{decalUI.MaxAngle:F3}", labelStyleValue);
                lineX += longFieldWidth + separatorX;

                y += height + separatorY;
            }

            void DrawChangePositionLine(float x, float y, string name, Vector3 direction, float value)
            {
                var lineX = x + marginX;

                void DrawButton(float value, string valueStr)
                {
                    if (GUI.Button(new Rect(lineX, y, longButtonWidth, height), valueStr))
                    {
                        decal.ChangeLocalPosition(direction, value);
                    }
                    lineX += longButtonWidth + separatorX;
                }

                GUI.Label(new Rect(lineX, y, nameWidth, height), name, labelStyleName);
                lineX += nameWidth + separatorX;

                DrawButton(-0.005f, "5mm");
                DrawButton(-0.001f, "1mm");

                GUI.Label(new Rect(lineX, y, longFieldWidth, height), $"{value:F3}", labelStyleValue);
                lineX += longFieldWidth + separatorX;

                DrawButton(0.001f, "1mm");
                DrawButton(0.005f, "5mm");
            }

            DrawChangePositionLine(x, y, "Forward/Backward:", new Vector3(1f, 0f, 0f), localPosition.y);
            y += height + separatorY;

            DrawChangePositionLine(x, y, "Down/Up:", new Vector3(0f, 0f, 1f), localPosition.z);
            y += height + separatorY;

            DrawChangePositionLine(x, y, "Left/Right", new Vector3(0f, 1f, 0f), localPosition.x);
            y += height + separatorY;

            {
                var lineX = x + marginX;

                void DrawButton(float x, float y, float value, string valueStr)
                {
                    if (GUI.Button(new Rect(lineX, y, buttonWidth, height), valueStr))
                    {
                        decal.ChangeLocalRotation(value);
                    }
                    lineX += buttonWidth + separatorX;
                }

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Rotation:", labelStyleName);
                lineX += nameWidth + separatorX;

                DrawButton(lineX, y, -5, "5°");
                DrawButton(lineX, y, -1, "1°");

                GUI.Label(new Rect(lineX, y, fieldWidth, height), $"{localRotation.x:N0}", labelStyleValue);
                lineX += fieldWidth + separatorX;

                DrawButton(lineX, y, 1, "1°");
                DrawButton(lineX, y, 5, "5°");
            }
            y += height + separatorY;

            {
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Size:", labelStyleName);
                lineX += nameWidth + separatorX;

                decalUI.Size = GUI.TextField(new Rect(lineX, y, longFieldWidth, height), decalUI.Size, 6);
                lineX += longFieldWidth + separatorX;

                if (GUI.Button(new Rect(lineX, y, buttonWidth, height), "Set"))
                {
                    if (float.TryParse(decalUI.Size, out var newSize))
                    {
                        decalUI.Size = newSize.ToString();
                        decal.ChangeSize(newSize);
                    }
                }
                lineX += buttonWidth + separatorX;
            }
            y += height + separatorY;

            {
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Depth:", labelStyleName);
                lineX += nameWidth + separatorX;

                decalUI.Depth = GUI.TextField(new Rect(lineX, y, longFieldWidth, height), decalUI.Depth, 6);
                lineX += longFieldWidth + separatorX;

                if (GUI.Button(new Rect(lineX, y, buttonWidth, height), "Set"))
                {
                    if (float.TryParse(decalUI.Depth, out var newDepth))
                    {
                        decalUI.Depth = newDepth.ToString();
                        decal.ChangeDepth(newDepth);
                    }
                }
                lineX += buttonWidth + separatorX;
            }
            y += height + separatorY;
		}

        private readonly Vector3 typicalRifleCenter = new Vector3(0f, -0.35f, -0.003f);
        private readonly float defaultDecalSize = 0.2f;
        private readonly float defaultDecalDepth = 0.1f;

        public void SpawnItemDecals(string itemId, Transform root)
        {
            var decalsItemId = itemId;
            if (Clones.TryGetValue(itemId, out var originalItemId))
            {
                Logger.LogInfo($"SpawnItemDecals: {itemId} clone of {decalsItemId}");
                decalsItemId = originalItemId;
            }
            if (!ItemsWithDecalsInfo.TryGetValue(decalsItemId, out var decalsInfo))
            {
                Logger.LogInfo($"SpawnItemDecals: {decalsItemId}, no decals info");
                return;
            }
            if (ItemsWithDecals.ContainsKey(itemId))
            {
                Logger.LogInfo($"SpawnItemDecals: {itemId}, already spawned");
                return;
            }

            Logger.LogInfo($"SpawnItemDecals: {itemId}, success");

            var decals = new List<Decal>(decalsInfo.Count);
            foreach (var decalInfo in decalsInfo)
            {
                var decal = DecalRenderer.CreateDecal(decalInfo, root, LoadedDecalTextures);
                decals.Add(decal);
            }

            ItemsWithDecals.Add(itemId, decals);
        }

        public void OnItemDecalsDestroyed(string itemId)
        {
			Logger.LogInfo($"OnItemDecalsDestroyed: {itemId}");
            ItemsWithDecals.Remove(itemId);
        }

        public void OnCloneItem(string originalId, string cloneId)
        {
            // TODO maybe add only if has decals?
            Clones.Add(cloneId, originalId);
        }

        public void SetupCamoEditor(string itemId, Transform root)
        {
            LoggerInstance.LogInfo($"SetupCamoEditor: {itemId}");

            IsCamoEditorWaitingForWeaponPreview = false;

            // this method is called after SpawnItemDecals, right?
            // so we can just get item and all its decals from dicts, right?

            var decalsUI = BuildItemDecalsUI(itemId);

            CamoEditor = new(new CamoEditor()
            {
                ItemId = itemId,
                DecalsRoot = root,
                Decals = decalsUI,
                IsVisible = true,
                IsDecalTextureSelectionScreenVisible = false,
            });
        }

        public List<DecalUI> BuildItemDecalsUI(string itemId)
        {
            if (GetItemDecals(itemId).Some(out var itemDecals))
            {
                var decals = itemDecals.decals;
                var decalsInfo = itemDecals.decalsInfo;
                var decalsUI = new List<DecalUI>(decals.Count);
                for (var i = 0; i < decals.Count; i++)
                {
                    var decal = decals[i];
                    var decalInfo = decalsInfo[i];
                    var decalUI = new DecalUI()
                    {
                        Decal = decal,
                        Texture = decalInfo.Texture,
                        Opacity = decalInfo.Opacity,
                        MaxAngle = decalInfo.MaxAngle,
                        Size = decalInfo.LocalScale.x.ToString(),
                        Depth = decalInfo.LocalScale.y.ToString(),
                    };
                    decalsUI.Add(decalUI);
                }

                return decalsUI;
            }

            // this item doesn't have any decals,
            // but maybe player wants to put some!
            return new();
        }

        public Option<(List<Decal> decals, List<DecalInfo> decalsInfo)> GetItemDecals(string itemId)
        {
            if (!ItemsWithDecals.TryGetValue(itemId, out var decals))
            {
                return default;
            }
            if (!ItemsWithDecalsInfo.TryGetValue(itemId, out var decalsInfo))
            {
                return default;
            }
            if (decals.Count != decalsInfo.Count)
            {
                LoggerInstance.LogError($"GetItemDecals: {itemId}, mismatched decals?");
                return default;
            }

            return new((decals, decalsInfo));
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
            var decalsUI = camoEditor.Decals;
            if (ItemsWithDecalsInfo.TryGetValue(itemId, out var decalsInfo))
            {
                if (decalsUI.Count == 0)
                {
                    // remove decals, with file
                    ItemsWithDecalsInfo.Remove(itemId);
                    RemoveDecalsFile(itemId);
                    LoggerInstance.LogInfo($"CloseCamoEditor: {itemId} remove decals");
                }
                else
                {
                    // rewrite existing decals
                    decalsInfo.Clear();
                    WriteDecalsToList(decalsUI, decalsInfo);
                    WriteDecalsToFile(itemId, decalsInfo);
                    LoggerInstance.LogInfo($"CloseCamoEditor: {itemId} rewrite decals");
                }
            }
            else
            {
                if (decalsUI.Count == 0)
                {
                    // do nothing
                    LoggerInstance.LogInfo($"CloseCamoEditor: {itemId} no decals on item");
                }
                else
                {
                    // write decals to new file
                    decalsInfo = new(decalsUI.Count);
                    WriteDecalsToList(decalsUI, decalsInfo);
                    ItemsWithDecalsInfo.Add(itemId, decalsInfo);
                    WriteDecalsToFile(itemId, decalsInfo);
                    LoggerInstance.LogInfo($"CloseCamoEditor: {itemId} write decals to new file");
                }
            }

            CamoEditor = default;
            // TODO we need to catch moments when weapon is spawned/despawned
        }

        public void WriteDecalsToList(List<DecalUI> decalsUI, List<DecalInfo> decalsInfo)
        {
            foreach (var decalUI in decalsUI)
            {
                var decal = decalUI.Decal;
                var decalInfo = new DecalInfo()
                {
                    Texture = decalUI.Texture,
                    LocalPosition = decal.DecalTransform.localPosition,
                    LocalEulerAngles = decal.DecalTransform.localEulerAngles,
                    LocalScale = decal.DecalTransform.localScale,
                    Opacity = decalUI.Opacity,
                    MaxAngle = decalUI.MaxAngle,
                };
                decalsInfo.Add(decalInfo);
            }
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
