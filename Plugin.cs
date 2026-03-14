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
using Systems.Effects;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamo
{
    public struct Proxy_DeferredDecalRenderer
    {
        private static TypedFieldInfo<DeferredDecalRenderer, float> __decalProjectorHeight = new("_decalProjectorHeight");
		private static TypedFieldInfo<DeferredDecalRenderer, Dictionary<MaterialType, DeferredDecalRenderer.SingleDecal>> __dictionary_1 = new("dictionary_1");
		private static TypedFieldInfo<DeferredDecalRenderer, Dictionary<Camera, DeferredDecalRenderer.DeferredDecalBufferClass>> __dictionary_2 = new("dictionary_2");

        public float _decalProjectorHeight { get { return __decalProjectorHeight.Get(__instance); } set { __decalProjectorHeight.Set(__instance, value); } }
        public Dictionary<MaterialType, DeferredDecalRenderer.SingleDecal> dictionary_1 { get { return __dictionary_1.Get(__instance); } set { __dictionary_1.Set(__instance, value); } }
        public Dictionary<Camera, DeferredDecalRenderer.DeferredDecalBufferClass> dictionary_2 { get { return __dictionary_2.Get(__instance); } set { __dictionary_2.Set(__instance, value); } }

        private DeferredDecalRenderer __instance;

        public Proxy_DeferredDecalRenderer(DeferredDecalRenderer instance)
        {
            __instance = instance;
        }
    }

    [BepInPlugin("7Bpencil.WeaponCamo", "7Bpencil.WeaponCamo", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
		public ManualLogSource LoggerInstance;

        public static ConfigEntry<KeyboardShortcut> SpawnButton;
        public static ConfigEntry<MaterialType> DecalMaterial;

    	public RaycastHit[] RaycastHits;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            SpawnButton = Config.Bind("Main", "Spawn Button", new KeyboardShortcut(KeyCode.F4), "Spawn Button");
            DecalMaterial = Config.Bind("Main", "Decal Material", MaterialType.Concrete, "Decal Material");
    		RaycastHits = new RaycastHit[32];
        }

		public void Update()
		{
			if (Input.GetKeyDown(SpawnButton.Value.MainKey))
			{
                TestDecal();
			}
		}

        public void TestDecal()
        {
            LoggerInstance.LogWarning("TestDecal");

            var player = GamePlayerOwner.MyPlayer;
			var camera = CameraClass.Instance;
            var cameraTransform = camera.Camera.transform;

            var maxDistance = 10;
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
            var effects = Singleton<Effects>.Instance;
            var decalsRenderer = effects.DeferredDecals;
            var _decalsRenderer = new Proxy_DeferredDecalRenderer(decalsRenderer);

            var decalBuffers = _decalsRenderer.dictionary_2;
            var decals = _decalsRenderer.dictionary_1;
            var decalProjectorHeight = _decalsRenderer._decalProjectorHeight;
			var decal = decals[material];
			var position = hit.point + hit.normal * EFTHardSettings.Instance.DECAL_SHIFT;

			decalsRenderer.method_5(position, hit.normal, ballisticCollider, decal, decal.DynamicDecalMaterial, decalProjectorHeight);
			foreach (var (camera, buffer) in decalBuffers)
			{
				buffer.IsDynamicBufferDirty = true;
			}
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
