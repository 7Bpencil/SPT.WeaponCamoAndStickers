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

namespace SevenBoldPencil.WeaponCamo
{
    public struct Proxy_DeferredDecalRenderer
    {
        private static TypedFieldInfo<DeferredDecalRenderer, float> __decalProjectorHeight = new("_decalProjectorHeight");
		private static TypedFieldInfo<DeferredDecalRenderer, Dictionary<MaterialType, DeferredDecalRenderer.SingleDecal>> __dictionary_1 = new("dictionary_1");
		private static TypedFieldInfo<DeferredDecalRenderer, Dictionary<Camera, DeferredDecalRenderer.DeferredDecalBufferClass>> __dictionary_2 = new("dictionary_2");
		private static TypedFieldInfo<DeferredDecalRenderer, List<DynamicDeferredDecalRenderer>> __list_0 = new("list_0");
		private static TypedFieldInfo<DeferredDecalRenderer, int> __int_3 = new("int_3");
		private static TypedFieldInfo<DeferredDecalRenderer, Mesh> __cube = new("_cube");
		private static TypedFieldInfo<DeferredDecalRenderer, BoundingSphere[]> __boundingSphere_0 = new("boundingSphere_0");
		private static TypedFieldInfo<DeferredDecalRenderer, int> __maxDynamicDecals = new("_maxDynamicDecals");

        public float _decalProjectorHeight { get { return __decalProjectorHeight.Get(__instance); } set { __decalProjectorHeight.Set(__instance, value); } }
        public Dictionary<MaterialType, DeferredDecalRenderer.SingleDecal> dictionary_1 { get { return __dictionary_1.Get(__instance); } set { __dictionary_1.Set(__instance, value); } }
        public Dictionary<Camera, DeferredDecalRenderer.DeferredDecalBufferClass> dictionary_2 { get { return __dictionary_2.Get(__instance); } set { __dictionary_2.Set(__instance, value); } }
        public List<DynamicDeferredDecalRenderer> list_0 { get { return __list_0.Get(__instance); } set { __list_0.Set(__instance, value); } }
        public int int_3 { get { return __int_3.Get(__instance); } set { __int_3.Set(__instance, value); } }
        public Mesh _cube { get { return __cube.Get(__instance); } set { __cube.Set(__instance, value); } }
        public BoundingSphere[] boundingSphere_0 { get { return __boundingSphere_0.Get(__instance); } set { __boundingSphere_0.Set(__instance, value); } }
        public int _maxDynamicDecals { get { return __maxDynamicDecals.Get(__instance); } set { __maxDynamicDecals.Set(__instance, value); } }

        private DeferredDecalRenderer __instance;

        public Proxy_DeferredDecalRenderer(DeferredDecalRenderer instance)
        {
            __instance = instance;
        }
    }

    public class CamoEditorUI
    {
        public List<DecalUI> Decals;
        public bool IsVisible;
        public bool IsDecalTextureSelectionScreenVisible;
        public int CurrentlyEditedDecal;
    }

    public class DecalUI
    {
        public DynamicDeferredDecalRenderer Decal;
        public string Texture;
        public float Opacity;
        public float MaxAngle;
        public string Size;
        public string Depth;
    }

    public static class DecalExtensions
    {
    	public static readonly int _MaxAngle = Shader.PropertyToID("_MaxAngle");
        public static readonly string DefaultTextureName = "default";
        public static readonly Vector3 LeftSideDecalRotation = new(0, 0, 90);
        public static readonly Vector3 RightSideDecalRotation = new(0, 0, 270);

        public static void ChangeTexture(this DynamicDeferredDecalRenderer decal, Texture2D diffuse, Texture2D bump = null)
        {
            decal.DecalMaterial.SetTexture("_MainTex", diffuse);
            decal.DecalMaterial.SetTexture("_BumpMap", bump);
        }

        public static void ChangeSide(this DynamicDeferredDecalRenderer decal, bool isLeft)
        {
            var decalTransfromHelper = decal.TransformHelper;
            if (isLeft)
            {
                decalTransfromHelper.localEulerAngles = LeftSideDecalRotation;
            }
            else
            {
                decalTransfromHelper.localEulerAngles = RightSideDecalRotation;
            }
        }

        public static void ChangeLocalPosition(this DynamicDeferredDecalRenderer decal, Vector3 direction, float step)
        {
            var decalTransfromHelper = decal.TransformHelper;
            decalTransfromHelper.Translate(direction * step, Space.Self);
        }

        public static void ChangeLocalRotation(this DynamicDeferredDecalRenderer decal, float delta)
        {
            var decalTransfromHelper = decal.TransformHelper;
            decalTransfromHelper.Rotate(new Vector3(0, 1, 0) * delta, Space.Self);
        }

        public static void ChangeOpacity(this DynamicDeferredDecalRenderer decal, float opacity)
        {
            decal.DecalMaterial.color = new Color(1, 1, 1, opacity);
        }

        public static void ChangeMaxAngle(this DynamicDeferredDecalRenderer decal, float maxAngle)
        {
            decal.DecalMaterial.SetFloat(_MaxAngle, maxAngle);
        }

        public static void ChangeSize(this DynamicDeferredDecalRenderer decal, float size)
        {
            var decalTransform = decal.transform;
            var depth = decalTransform.localScale.y;
            decalTransform.localScale = new Vector3(size, depth, size);
        }

        public static void ChangeDepth(this DynamicDeferredDecalRenderer decal, float depth)
        {
            var decalTransform = decal.transform;
            var size = decalTransform.localScale.x;
            decalTransform.localScale = new Vector3(size, depth, size);
        }
    }

    [BepInPlugin("7Bpencil.WeaponCamo", "7Bpencil.WeaponCamo", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;

		public ManualLogSource LoggerInstance;

        public static ConfigEntry<KeyboardShortcut> ShowHideCamoEditor;
        public static ConfigEntry<float> GizmoCubeSize;
        public static ConfigEntry<MaterialType> DecalMaterial;

        public RuntimeGizmos RuntimeGizmos;
    	public RaycastHit[] RaycastHits;
        public List<DynamicDeferredDecalRenderer> Decals;
        public CamoEditorUI CamoEditorUI;

        public string AssemblyDir;
        public string DecalTexturesDir;
        public AssetBundle Bundle;
        public Shader DecalDynamicShader;
        public List<string> LoadedDecalTexturesList;
        public Dictionary<string, Texture2D> LoadedDecalTextures;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            ShowHideCamoEditor = Config.Bind("Main", "Show/Hide Camo Editor", new KeyboardShortcut(KeyCode.F4), "Show/Hide Camo Editor");
            GizmoCubeSize = Config.Bind<float>("Main", "Gizmo Cube Size", 0.05f, new ConfigDescription("from 1cm to 10cm, default is 5cm", new AcceptableValueRange<float>(0.01f, 0.1f)));
            DecalMaterial = Config.Bind("Main", "Decal Material", MaterialType.Concrete, "Decal Material");

    		RaycastHits = new RaycastHit[32];
            Decals = new(10);
            CamoEditorUI = new CamoEditorUI()
            {
                Decals = new(10),
                IsVisible = false,
                IsDecalTextureSelectionScreenVisible = false,
            };

            AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DecalTexturesDir = Path.Combine(AssemblyDir, "assets", "images");
			var bundlePath = Path.Combine(AssemblyDir, "assets", "bundles", "weaponcamo");
            Bundle = AssetBundle.LoadFromFile(bundlePath);
            DecalDynamicShader = Bundle.LoadAsset<Shader>("Assets/WeaponCamo/Shaders/DecalDynamic.shader");
            (LoadedDecalTexturesList, LoadedDecalTextures) = LoadTexturesFromDirectory(DecalTexturesDir);

            new Patch_DeferredDecalRenderer_SingleDecal_Init().Enable();
            new Patch_DeferredDecalRenderer_Update().Enable();
            new Patch_DeferredDecalRenderer_OnPreCameraRender().Enable();

            // TODO
            // seems like decals are not drawing on gun (because of stencil?)
            // does it mean we can make decals that will only apply on gun (and not hands and env)?

            // TODO
            // figure out why decal moves slightly during weapon inspect animation

            // TODO
            // add option to collapse decal settings

            // TODO
            // load textures from folder

            // TODO
            // add color picker

            // TODO
            // add support for non square decal textures

            // TODO
            // maybe apply camo texture on top of diffuse texture?
        }

        // TODO
        // add support for adding/removing images on running game
        // add support for drawing sub folders as file tree
        public (List<string>, Dictionary<string, Texture2D>) LoadTexturesFromDirectory(string directoryPath)
        {
            var filePaths = Directory.GetFiles(directoryPath);
            var resultList = new List<string>(filePaths.Length + 1);
            var resultDict = new Dictionary<string, Texture2D>();

            {
                var defaultTexture = Bundle.LoadAsset<Texture2D>($"Assets/WeaponCamo/Images/{DecalExtensions.DefaultTextureName}.png");
                resultList.Add(DecalExtensions.DefaultTextureName);
                resultDict.Add(DecalExtensions.DefaultTextureName, defaultTexture);
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
                    LoggerInstance.LogWarning($"Failed to load decal texture: {name}");
                }
            }

            return (resultList, resultDict);
        }

		public void Update()
		{
            if (Input.GetKeyDown(ShowHideCamoEditor.Value.MainKey))
            {
                CamoEditorUI.IsVisible = !CamoEditorUI.IsVisible;
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
                foreach (var decal in Decals)
                {
                    var decalHelper = decal.TransformHelper;
                    var position = decalHelper.position;
                    var scale = GizmoCubeSize.Value;
                    var scale3 = new Vector3(scale, scale, scale);
                    RuntimeGizmos.Cubes.Add(new RuntimeGizmos.Cube()
                    {
                        Position = position,
                        Rotation = decalHelper.rotation,
                        Scale = scale3,
                    });
                    RuntimeGizmos.Lines.Add(new RuntimeGizmos.Line()
                    {
                        Start = position,
                        End = position + decalHelper.up * scale,
                    });
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
            if (CamoEditorUI.IsVisible)
            {
    			// if width == 0, then windowRect has not been initialized, so init it
    			if (windowRect.width == 0f)
    			{
    				windowRect.width = windowWidth;
    				windowRect.x = 50;
    				windowRect.y = 50;
    			}

    			var windowHeight = startY + height + marginY + (boxHeight + marginY) * Decals.Count;

    			windowRect.height = windowHeight;
                windowRect = GUI.Window(1, windowRect, WindowFunction, $"Camo Editor");
            }
        }

        private void WindowFunction(int windowID)
		{
			var x = startX;
			var y = startY;

            if (CamoEditorUI.IsDecalTextureSelectionScreenVisible)
            {
                DrawDecalTextureSelectorUI(x, y);
            }
            else
            {
                DrawDecalModifierUI(x, y);
            }

			GUI.DragWindow();
        }

        private void DrawDecalTextureSelectorUI(float x, float y)
        {
            var columns = 3;
            var iconSize = (boxWidth - ((columns - 1) * separatorX)) / columns;

            if (GUI.Button(new Rect(x, y, addDecalButtonWidth, height), "Back"))
            {
                CamoEditorUI.IsDecalTextureSelectionScreenVisible = false;
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
                    var decalUI = CamoEditorUI.Decals[CamoEditorUI.CurrentlyEditedDecal];
                    if (decalUI.Texture != textureName)
                    {
                        decalUI.Texture = textureName;
                        decalUI.Decal.ChangeTexture(texture);
                    }
                }
            }
        }

        private void DrawDecalModifierUI(float x, float y)
        {
            if (GUI.Button(new Rect(x, y, addDecalButtonWidth, height), "Add New Decal"))
            {
                PutDecalOnWeapon();
            }
            y += height + marginY;

            for (var i = 0; i < CamoEditorUI.Decals.Count; i++)
            {
                DrawDecalUI(x, ref y, i, CamoEditorUI.Decals[i]);
                y += marginY;
            }
        }

        private void DrawDecalUI(float x, ref float y, int decalIndex, DecalUI decalUI)
        {
            var decal = decalUI.Decal;
            var localPosition = decal.TransformHelper.localPosition;
            var localRotation = decal.TransformHelper.localEulerAngles;

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), "Decal");
            y += boxHeaderHeight + separatorY;

            {
                var lineX = x + marginX;

                GUI.Label(new Rect(lineX, y, nameWidth, height), "Texture:", labelStyleName);
                lineX += nameWidth + separatorX;

                var buttonWidth = boxWidth - nameWidth - separatorX - marginX * 2;
                if (GUI.Button(new Rect(lineX, y, buttonWidth, height), decalUI.Texture))
                {
                    CamoEditorUI.IsDecalTextureSelectionScreenVisible = true;
                    CamoEditorUI.CurrentlyEditedDecal = decalIndex;
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
        private readonly float defaultDecalDepth = 0.1f;
        private readonly float defaultDecalSize = 0.2f;

        public void PutDecalOnWeapon()
        {
            LoggerInstance.LogWarning("TestDecalOnWeapon");

            var player = GamePlayerOwner.MyPlayer;
            var pwa = player.ProceduralWeaponAnimation;
            var weaponGO = pwa.HandsContainer.Weapon;
			var camera = CameraClass.Instance;
            var cameraTransform = camera.Camera.transform;

            var effects = Singleton<Effects>.Instance;
            var decalsRenderer = effects.DeferredDecals;
            var _decalsRenderer = new Proxy_DeferredDecalRenderer(decalsRenderer);

            var decalBuffers = _decalsRenderer.dictionary_2;
            var decals = _decalsRenderer.dictionary_1;
            var material = DecalMaterial.Value;

            if (!decals.TryGetValue(material, out var decal))
            {
                LoggerInstance.LogWarning($"DrawDecal: no decal for {material} material");
                return;
            }

			method_5(_decalsRenderer, typicalRifleCenter, DecalExtensions.LeftSideDecalRotation, defaultDecalSize, weaponGO.transform, decal, decal.DynamicDecalMaterial, defaultDecalDepth);
            decalsRenderer.method_13();
        }

        private static readonly float sqrt2 = Mathf.Sqrt(2f);

		public void method_5(Proxy_DeferredDecalRenderer instance, Vector3 localPosition, Vector3 localRotation, float size, Transform owner, DeferredDecalRenderer.SingleDecal currentDecal, Material currentMaterial, float depth)
		{
			DynamicDeferredDecalRenderer dynamicDeferredDecalRenderer = instance.list_0[instance.int_3];
			Transform decalTransform = dynamicDeferredDecalRenderer.gameObject.transform;
			Transform transformHelper = dynamicDeferredDecalRenderer.TransformHelper;
			int cullingGroupSphereIndex = dynamicDeferredDecalRenderer.CullingGroupSphereIndex;

            // TODO I think Tarkov default decal radius calculation is incorrect

			transformHelper.parent = owner;
            transformHelper.localPosition = localPosition;
            transformHelper.localEulerAngles = localRotation;

            var position = transformHelper.position;
            var rotation = transformHelper.rotation;

			decalTransform.localScale = new Vector3(size, depth, size);
			decalTransform.rotation = rotation;
			decalTransform.position = position;

            // TODO we have to update BoundingSphere with size changes
            var boundingSphereRadius = sqrt2 * size * 0.5f;
			instance.boundingSphere_0[cullingGroupSphereIndex] = new BoundingSphere(position, boundingSphereRadius);

            Decals.Add(dynamicDeferredDecalRenderer);
            CamoEditorUI.Decals.Add(new DecalUI()
            {
                Decal = dynamicDeferredDecalRenderer,
                Texture = DecalExtensions.DefaultTextureName,
                Opacity = 1f,
                MaxAngle = 0.8f,
                Size = size.ToString(),
                Depth = depth.ToString(),
            });

            // TODO add support for tiling?
			var uvStartEnd = new Vector4(0, 0, 1, 1);
			dynamicDeferredDecalRenderer.enabled = true;

			dynamicDeferredDecalRenderer.Init(currentMaterial, default, default, uvStartEnd, true, cullingGroupSphereIndex);
            dynamicDeferredDecalRenderer.ChangeTexture(LoadedDecalTextures[DecalExtensions.DefaultTextureName]);

			instance.int_3 = (instance.int_3 + 1) % instance._maxDynamicDecals;
		}

    }
}
