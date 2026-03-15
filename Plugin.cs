using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using DeferredDecals;
using EFT;
using EFT.Ballistics;
using SevenBoldPencil.Common;
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

    [BepInPlugin("7Bpencil.WeaponCamo", "7Bpencil.WeaponCamo", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
    	private static readonly int _MaxAngle = Shader.PropertyToID("_MaxAngle");

        public static Plugin Instance;
		public ManualLogSource LoggerInstance;

        public static ConfigEntry<KeyboardShortcut> SpawnBallisticColliderDecalButton;
        public static ConfigEntry<KeyboardShortcut> SpawnWeaponDecalButton;
        public static ConfigEntry<KeyboardShortcut> MoveForward;
        public static ConfigEntry<KeyboardShortcut> MoveBack;
        public static ConfigEntry<float> MoveStep;
        public static ConfigEntry<float> GizmoCubeSize;
        public static ConfigEntry<MaterialType> DecalMaterial;
        public static ConfigEntry<float> DecalMaxAngle;

        public RuntimeGizmos RuntimeGizmos;
    	public RaycastHit[] RaycastHits;
        public List<DynamicDeferredDecalRenderer> Decals;
        public Vector3 LastDecalNormal;

        public AssetBundle Bundle;
        public Shader DecalDynamicShader;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            SpawnBallisticColliderDecalButton = Config.Bind("Main", "Spawn Ballistic Collider Decal Button", new KeyboardShortcut(KeyCode.F3), "Spawn Ballistic Collider Decal Button");
            SpawnWeaponDecalButton = Config.Bind("Main", "Spawn Weapon Decal Button", new KeyboardShortcut(KeyCode.F4), "Spawn Weapon Decal Button");
            MoveForward = Config.Bind("Main", "Move Forward", new KeyboardShortcut(KeyCode.F1), "Move Forward");
            MoveBack = Config.Bind("Main", "Move Back", new KeyboardShortcut(KeyCode.F2), "Move Back");
            MoveStep = Config.Bind<float>("Main", "Move Step", 0.005f, new ConfigDescription("from 1mm to 10cm, default is 5mm", new AcceptableValueRange<float>(0.001f, 0.1f)));
            GizmoCubeSize = Config.Bind<float>("Main", "Gizmo Cube Size", 0.05f, new ConfigDescription("from 1cm to 10cm, default is 5cm", new AcceptableValueRange<float>(0.01f, 0.1f)));
            DecalMaterial = Config.Bind("Main", "Decal Material", MaterialType.Concrete, "Decal Material");
            DecalMaxAngle = Config.Bind<float>("Main", "Decal Max Angle", 0.8f, new ConfigDescription("angle at which decal starts to cut off", new AcceptableValueRange<float>(0f, 1f)));
            DecalMaxAngle.SettingChanged += (s, e) =>
            {
                PropagateDecalsMaxAngle(DecalMaxAngle.Value);
            };

    		RaycastHits = new RaycastHit[32];
            Decals = new(10);

            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var bundlePath = Path.Combine(assemblyDir, "assets", "bundles", "weaponcamo");
            Bundle = AssetBundle.LoadFromFile(bundlePath);
            DecalDynamicShader = Bundle.LoadAsset<Shader>("Assets/WeaponCamo/Shaders/DecalDynamic.shader");

            new Patch_DeferredDecalRenderer_SingleDecal_Init().Enable();
        }

        public void PropagateDecalsMaxAngle(float newMaxAngle)
        {
            foreach (var decal in Decals)
            {
                decal.DecalMaterial.SetFloat(_MaxAngle, newMaxAngle);
            }
        }

		public void Update()
		{
			if (Input.GetKeyDown(SpawnBallisticColliderDecalButton.Value.MainKey))
			{
                PutDecalOnBallisticCollider();
			}
			if (Input.GetKeyDown(SpawnWeaponDecalButton.Value.MainKey))
			{
                PutDecalOnWeapon();
			}

            if (Decals.Count != 0)
            {
                var lastDecalHelper = Decals.Last().TransformHelper;
                if (Input.GetKeyDown(MoveForward.Value.MainKey))
                {
                    lastDecalHelper.Translate(-LastDecalNormal * MoveStep.Value, Space.World);
                }
                if (Input.GetKeyDown(MoveBack.Value.MainKey))
                {
                    lastDecalHelper.Translate(LastDecalNormal * MoveStep.Value, Space.World);
                }
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

        public void PutDecalOnWeapon()
        {
            LoggerInstance.LogWarning("TestDecalOnWeapon");

            var player = GamePlayerOwner.MyPlayer;
            var pwa = player.ProceduralWeaponAnimation;
            var weaponGO = pwa.HandsContainer.Weapon;
			var camera = CameraClass.Instance;
            var cameraTransform = camera.Camera.transform;

            var start = cameraTransform.position;
            var direction = cameraTransform.forward;
            var normal = -direction;
            DrawDecal(start, normal, weaponGO.transform, DecalMaterial.Value);
        }

        public void PutDecalOnBallisticCollider()
        {
            LoggerInstance.LogWarning("TestDecal");

            var player = GamePlayerOwner.MyPlayer;
			var camera = CameraClass.Instance;
            var cameraTransform = camera.Camera.transform;

            var maxDistance = 100;
            var start = cameraTransform.position;
            var direction = cameraTransform.forward;
            var end = start + direction * maxDistance;
            var hitMask = LayerMasksDataAbstractClass.HitMask;

            // I am pretty sure this method is used for raycasting bullets
        	var hasHit = EFTPhysicsClass.LinecastInBothSides(
                start, end,
                out var bestHit, out var isForwardHit,
                hitMask, hitMask,
                RaycastHits,
                hit => IsHitIgnored(player, hit)
            );

            LoggerInstance.LogWarning($"TestDecal: has hit: {hasHit}");

            if (!hasHit)
            {
                return;
            }

            LoggerInstance.LogWarning($"TestDecal: bestHit: {bestHit.collider.gameObject.name}");

    		TryGetBallisticCollider(bestHit, out var ballisticCollider);
            if (!ballisticCollider)
            {
                return;
            }

            LoggerInstance.LogWarning($"TestDecal: ballisticCollider: {ballisticCollider.gameObject.name}");

			var hitBallisticCollider = ballisticCollider.Get(bestHit.point);
            DrawDecal(bestHit, hitBallisticCollider, DecalMaterial.Value);
        }

        public void DrawDecal(RaycastHit hit, BallisticCollider ballisticCollider, MaterialType material)
        {
			var position = hit.point + hit.normal * EFTHardSettings.Instance.DECAL_SHIFT;
            DrawDecal(position, hit.normal, ballisticCollider.transform, material);
        }

        public void DrawDecal(Vector3 position, Vector3 normal, Transform owner, MaterialType material)
        {
            var effects = Singleton<Effects>.Instance;
            var decalsRenderer = effects.DeferredDecals;
            var _decalsRenderer = new Proxy_DeferredDecalRenderer(decalsRenderer);

            var decalBuffers = _decalsRenderer.dictionary_2;
            var decals = _decalsRenderer.dictionary_1;
            var decalProjectorHeight = _decalsRenderer._decalProjectorHeight;
            if (!decals.TryGetValue(material, out var decal))
            {
                LoggerInstance.LogWarning($"DrawDecal: no decal for {material} material");
                return;
            }

			method_5(_decalsRenderer, position, normal, owner, decal, decal.DynamicDecalMaterial, decalProjectorHeight);
            decalsRenderer.method_13();
        }

		public void method_5(Proxy_DeferredDecalRenderer instance, Vector3 position, Vector3 normal, Transform owner, DeferredDecalRenderer.SingleDecal currentDecal, Material currentMaterial, float projectorHeight)
		{
			DynamicDeferredDecalRenderer dynamicDeferredDecalRenderer = instance.list_0[instance.int_3];
			GameObject gameObject = dynamicDeferredDecalRenderer.gameObject;
			Transform transformHelper = dynamicDeferredDecalRenderer.TransformHelper;
			int cullingGroupSphereIndex = dynamicDeferredDecalRenderer.CullingGroupSphereIndex;
			float num = UnityEngine.Random.Range(currentDecal.DecalSize.x, currentDecal.DecalSize.y);
			float num2 = num * 2f;
			float rad = Mathf.Sqrt(num * num + num * num);
			instance.boundingSphere_0[cullingGroupSphereIndex] = new BoundingSphere(position, rad);
			if (gameObject != null)
			{
				gameObject.transform.localScale = new Vector3(num2, projectorHeight, num2);
				gameObject.transform.up = normal;
				gameObject.transform.position = position;
				if (currentDecal.RandomizeRotation)
				{
					gameObject.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 359f), Space.Self);
				}
				if (transformHelper != null)
				{
					transformHelper.position = position;
					transformHelper.rotation = gameObject.transform.rotation;
					transformHelper.parent = owner;

                    Decals.Add(dynamicDeferredDecalRenderer);
                    LastDecalNormal = normal;
				}
			}
			int num3 = UnityEngine.Random.Range(0, currentDecal.TileSheetColumns);
			int num4 = UnityEngine.Random.Range(0, currentDecal.TileSheetRows);
			Vector4 uvStartEnd = new Vector4((float)num4 * currentDecal.TileUSize, (float)num3 * currentDecal.TileVSize, (float)num4 * currentDecal.TileUSize + currentDecal.TileUSize, (float)num3 * currentDecal.TileVSize + currentDecal.TileVSize);
			dynamicDeferredDecalRenderer.enabled = true;
			dynamicDeferredDecalRenderer.Init(currentMaterial, instance._cube, normal, uvStartEnd, currentDecal.IsTiled, cullingGroupSphereIndex);
			instance.int_3 = (instance.int_3 + 1) % instance._maxDynamicDecals;
		}

    	public bool IsHitIgnored(Player player, RaycastHit hit)
    	{
            if (IgnoreBecauseHitPlayer(player, hit))
            {
                return true;
            }

            var hitBallisticCollider = TryGetBallisticCollider(hit, out _);
            return !hitBallisticCollider;
    	}

        public bool IgnoreBecauseHitPlayer(Player player, RaycastHit hit)
        {
    		if (player == null)
    		{
    			return false;
    		}
    		if (player.HasBodyPartCollider(hit.collider))
    		{
    			return true;
    		}
    		if (player.IsAI)
    		{
    			return hit.collider == player.CharacterController.GetCollider();
    		}

    		return false;
        }

    	public bool TryGetBallisticCollider(RaycastHit hit, out BaseBallistic ballisticCollider)
    	{
    		ballisticCollider = null;
    		if (hit.collider == null)
    		{
    			return false;
    		}
    		if (hit.collider.TryGetComponent<BaseBallistic>(out ballisticCollider))
    		{
    			return true;
    		}
    		if (hit.collider.transform.parent == null)
    		{
    			return false;
    		}
    		if (hit.collider.transform.parent.TryGetComponent<BaseBallistic>(out ballisticCollider))
    		{
    			return true;
    		}
    		return false;
    	}

    }
}
