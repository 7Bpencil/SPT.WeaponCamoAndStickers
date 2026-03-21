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
    public class ItemsWithDecals
    {
        // yes, there can be multiple items with same Id,
        // for example when you open item preview of weapon you already hold in hands,
        // or when hideout shooting range clones weapon (we pretend that they have the same Id)
        public Dictionary<int, ItemWithDecals> Items;
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
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale;
        public float Opacity;
        public float MaxAngle;
    }

    public class CamoEditor
    {
        public Camera Camera;
        public RuntimeGizmos RuntimeGizmos;
        public string ItemId;
        public int InstanceID;
        public Transform DecalsRoot;
        public bool IsVisible;
        public bool IsDecalTextureSelectionScreenVisible;
        public int CurrentlyEditedDecal; // TODO this should be Option<int>
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
        public bool IsCamoEditorWaitingForWeaponPreview;

        public string AssemblyDir;
        public string DecalTexturesDir;
        public AssetBundle Bundle;
        public List<string> LoadedDecalTexturesList;
        public Dictionary<string, Texture2D> LoadedDecalTextures;
        public Dictionary<string, ItemsWithDecals> ItemsWithDecals;
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
            ItemsWithDecals = new();
            Clones = new();

            DecalRenderer = new(decalDynamicShader);

            new Patch_WeaponPreview_Class3271_method_1().Enable();
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
            //
            // TODO
            // maybe 3D gizmos (similar to unity editor)
            // is the best way to move/rotate/scale decal?
            //
            // TODO
            // we can save items as jsons in assets/items/ each item separate file with name=item.Id
            // we can save presets as jsons in assets/presets/ each preset separate file with name=preset name (with support for subfolders)
            // show presets per weapon template (Item.TemplateId?)
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
		}

        public void LateUpdate()
        {
            if (CamoEditor.Some(out var camoEditor) && camoEditor.RuntimeGizmos)
            {
                if (ItemsWithDecals.TryGetValue(camoEditor.ItemId, out var itemsWithDecals))
                {
                    foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
                    {
                        foreach (var decal in itemWithDecals.Decals)
                        {
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

                var decalsCount = 0;
                if (ItemsWithDecals.TryGetValue(camoEditor.ItemId, out var itemsWithDecals))
                {
                    decalsCount = itemsWithDecals.DecalsInfo.Count;
                }

    			var windowHeight = startY + height + marginY + (boxHeight + marginY) * decalsCount;

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
                    var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
                    var decalInfo = itemsWithDecals.DecalsInfo[camoEditor.CurrentlyEditedDecal];
                    if (decalInfo.Texture != textureName)
                    {
                        decalInfo.Texture = textureName;
                        ChangeDecalOnItems(camoEditor.CurrentlyEditedDecal, itemsWithDecals.Items, decal =>
                        {
                            decal.ChangeTexture(texture);
                        });
                    }
                }
            }
        }

        public void ChangeDecalOnItems(int decalIndex, Dictionary<int, ItemWithDecals> items, Action<Decal> changeDecal)
        {
            foreach (var (_, itemWithDecals) in items)
            {
                var decal = itemWithDecals.Decals[decalIndex];
                changeDecal(decal);
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

                if (ItemsWithDecals.ContainsKey(camoEditor.ItemId))
                {
                    var itemsWithDecals = ItemsWithDecals[camoEditor.ItemId];
                    itemsWithDecals.DecalsInfo.Add(decalInfo);
                    foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
                    {
                        var decal = DecalRenderer.CreateDecal(decalInfo, itemWithDecals.DecalsRoot, LoadedDecalTextures);
                        itemWithDecals.Decals.Add(decal);
                    }
                }
                else
                {
                    var decal = DecalRenderer.CreateDecal(decalInfo, camoEditor.DecalsRoot, LoadedDecalTextures);
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
                }
            }
            y += height + marginY;

            {
                if (ItemsWithDecals.TryGetValue(camoEditor.ItemId, out var itemsWithDecals))
                {
                    var decalsInfo = itemsWithDecals.DecalsInfo;
                    for (var i = 0; i < decalsInfo.Count; i++)
                    {
                        DrawDecalUI(x, ref y, i, decalsInfo[i], itemsWithDecals.Items, camoEditor);
                        y += marginY;
                    }
                }
            }
        }

        private void DrawDecalUI(float x, ref float y, int decalIndex, DecalInfo decalInfo, Dictionary<int, ItemWithDecals> items, CamoEditor camoEditor)
        {
            GUI.Box(new Rect(x, y, boxWidth, boxHeight), "Decal");
            y += boxHeaderHeight + separatorY;

            {
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Texture:", labelStyleName);
                lineX += nameWidth + separatorX;

                var buttonWidth = boxWidth - nameWidth - separatorX - marginX * 2;
                if (GUI.Button(new Rect(lineX, y, buttonWidth, height), decalInfo.Texture))
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
                    decalInfo.LocalEulerAngles = Decal.LeftSideDecalRotation;
                    ChangeDecalOnItems(decalIndex, items, decal =>
                    {
                        decal.DecalTransform.localEulerAngles = Decal.LeftSideDecalRotation;
                    });
                }
                lineX += sideButtonWidth + separatorX;

                if (GUI.Button(new Rect(lineX, y, sideButtonWidth, height), "Right"))
                {
                    decalInfo.LocalEulerAngles = Decal.RightSideDecalRotation;
                    ChangeDecalOnItems(decalIndex, items, decal =>
                    {
                        decal.DecalTransform.localEulerAngles = Decal.RightSideDecalRotation;
                    });
                }
                lineX += sideButtonWidth + separatorX;
            }
            y += height + separatorY;

            {
                var sliderWidth = 200;
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Opacity:", labelStyleName);
                lineX += nameWidth + separatorX;

                var newOpacity = GUI.HorizontalSlider(new Rect(lineX, y, sliderWidth, height), decalInfo.Opacity, 0f, 1f);
                if (newOpacity != decalInfo.Opacity)
                {
                    decalInfo.Opacity = newOpacity;
                    ChangeDecalOnItems(decalIndex, items, decal =>
                    {
                        decal.ChangeOpacity(newOpacity);
                    });
                }
                lineX += sliderWidth + separatorX;

                GUI.Label(new Rect(lineX, y, longFieldWidth, height), $"{decalInfo.Opacity:F3}", labelStyleValue);
                lineX += longFieldWidth + separatorX;

                y += height + separatorY;
            }

            {
                var sliderWidth = 200;
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "MaxAngle:", labelStyleName);
                lineX += nameWidth + separatorX;

                var newMaxAngle = GUI.HorizontalSlider(new Rect(lineX, y, sliderWidth, height), decalInfo.MaxAngle, 0f, 1f);
                if (newMaxAngle != decalInfo.MaxAngle)
                {
                    decalInfo.MaxAngle = newMaxAngle;
                    ChangeDecalOnItems(decalIndex, items, decal =>
                    {
                        decal.ChangeMaxAngle(newMaxAngle);
                    });
                }
                lineX += sliderWidth + separatorX;

                GUI.Label(new Rect(lineX, y, longFieldWidth, height), $"{decalInfo.MaxAngle:F3}", labelStyleValue);
                lineX += longFieldWidth + separatorX;

                y += height + separatorY;
            }

            void DrawChangePositionLine(float x, float y, string name, Vector3 direction, float value, DecalInfo decalInfo)
            {
                var lineX = x + marginX;

                void DrawButton(float value, string valueStr, DecalInfo decalInfo)
                {
                    if (GUI.Button(new Rect(lineX, y, longButtonWidth, height), valueStr))
                    {
                        var localPosition = decalInfo.LocalPosition + direction * value;
                        decalInfo.LocalPosition = localPosition;
                        ChangeDecalOnItems(decalIndex, items, decal =>
                        {
                            decal.DecalTransform.localPosition = localPosition;
                        });
                    }
                    lineX += longButtonWidth + separatorX;
                }

                GUI.Label(new Rect(lineX, y, nameWidth, height), name, labelStyleName);
                lineX += nameWidth + separatorX;

                DrawButton(-0.005f, "5mm", decalInfo);
                DrawButton(-0.001f, "1mm", decalInfo);

                GUI.Label(new Rect(lineX, y, longFieldWidth, height), $"{value:F3}", labelStyleValue);
                lineX += longFieldWidth + separatorX;

                DrawButton(0.001f, "1mm", decalInfo);
                DrawButton(0.005f, "5mm", decalInfo);
            }

            DrawChangePositionLine(x, y, "Forward/Backward:", new Vector3(0f, 1f, 0f), decalInfo.LocalPosition.y, decalInfo);
            y += height + separatorY;

            DrawChangePositionLine(x, y, "Down/Up:", new Vector3(0f, 0f, 1f), decalInfo.LocalPosition.z, decalInfo);
            y += height + separatorY;

            DrawChangePositionLine(x, y, "Left/Right", new Vector3(1f, 0f, 0f), decalInfo.LocalPosition.x, decalInfo);
            y += height + separatorY;

            {
                var lineX = x + marginX;

                void DrawButton(float x, float y, float value, string valueStr, DecalInfo decalInfo)
                {
                    if (GUI.Button(new Rect(lineX, y, buttonWidth, height), valueStr))
                    {
                        // TODO rotation
                        // decalInfo.LocalEuler
                        // var quaternion = Quaternion.Euler(0, value, 0);
                        // localRotation *= quaternion;
                        // var new
                        // decal.ChangeLocalRotation(value);
                    }
                    lineX += buttonWidth + separatorX;
                }

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Rotation:", labelStyleName);
                lineX += nameWidth + separatorX;

                DrawButton(lineX, y, -5, "5°", decalInfo);
                DrawButton(lineX, y, -1, "1°", decalInfo);

                GUI.Label(new Rect(lineX, y, fieldWidth, height), $"{decalInfo.LocalEulerAngles.x:N0}", labelStyleValue);
                lineX += fieldWidth + separatorX;

                DrawButton(lineX, y, 1, "1°", decalInfo);
                DrawButton(lineX, y, 5, "5°", decalInfo);
            }
            y += height + separatorY;

            {
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Size:", labelStyleName);
                lineX += nameWidth + separatorX;

                // decalUI.Size = GUI.TextField(new Rect(lineX, y, longFieldWidth, height), decalInfo.LocalScale.y, 6);
                lineX += longFieldWidth + separatorX;

                if (GUI.Button(new Rect(lineX, y, buttonWidth, height), "Set"))
                {
                    // if (float.TryParse(decalUI.Size, out var newSize))
                    // {
                    //     decalUI.Size = newSize.ToString();
                    //     decal.ChangeSize(newSize);
                    // }
                }
                lineX += buttonWidth + separatorX;
            }
            y += height + separatorY;

            {
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Depth:", labelStyleName);
                lineX += nameWidth + separatorX;

                // decalUI.Depth = GUI.TextField(new Rect(lineX, y, longFieldWidth, height), decalUI.Depth, 6);
                lineX += longFieldWidth + separatorX;

                if (GUI.Button(new Rect(lineX, y, buttonWidth, height), "Set"))
                {
                    // if (float.TryParse(decalUI.Depth, out var newDepth))
                    // {
                    //     decalUI.Depth = newDepth.ToString();
                    //     decal.ChangeDepth(newDepth);
                    // }
                }
                lineX += buttonWidth + separatorX;
            }
            y += height + separatorY;
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
                var decal = DecalRenderer.CreateDecal(decalInfo, decalsRoot, LoadedDecalTextures);
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
                IsDecalTextureSelectionScreenVisible = false,
            });
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

            Destroy(camoEditor.RuntimeGizmos);
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
