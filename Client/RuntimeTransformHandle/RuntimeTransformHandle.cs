using SevenBoldPencil.WeaponCamoAndStickers;
using System;
using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 21.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class RuntimeTransformHandle : MonoBehaviour
    {
        public HandleType type;

        public bool autoScale = true;
        public float autoScaleFactor = 0.5f;

		private RaycastHit[] raycastHits;
		private int raycastLayerMask;
        private Vector3 _previousMousePosition;
		private bool _previousMouseDown;
        private HandleBase _previousHandle;
        private HandleBase _draggingHandle;

		public bool IsDragging => _draggingHandle;

        private PositionHandle _positionHandle;
        private RotationHandle _rotationHandle;
        private ScaleHandle _scaleHandle;
        private TextureOffsetHandle _textureOffsetHandle;
        private TextureTilingHandle _textureTilingHandle;
		private MaskOffsetHandle _maskOffsetHandle;
        private MaskTilingHandle _maskTilingHandle;

        public Transform targetTransform;
		public Transform handleTransform;
        public Camera handleCamera;

        public Action OnStartedDraggingHandle;
        public Action OnDraggingHandle;
        public Action OnEndedDraggingHandle;

        private Shader positionHandleShader;
        private Shader rotationHandleShader;
        private Shader scaleHandleShader;

        public void CreateHandlePosition()
        {
			type = HandleType.Position;
			_positionHandle = new GameObject("PositionHandle").AddComponent<PositionHandle>().Initialize(this, positionHandleShader);
		}

        public void CreateHandleRotation()
		{
			type = HandleType.Rotation;
			_rotationHandle = new GameObject("RotationHandle").AddComponent<RotationHandle>().Initialize(this, rotationHandleShader);
		}

        public void CreateHandleScale()
		{
			type = HandleType.Scale;
			_scaleHandle = new GameObject("ScaleHandle").AddComponent<ScaleHandle>().Initialize(this, scaleHandleShader);
        }

		public void CreateHandleTextureOffset(DecalInfo decalInfo, Decal decal)
		{
            type = HandleType.TextureOffset;
			_textureOffsetHandle = new GameObject("TextureOffsetHandle").AddComponent<TextureOffsetHandle>().Initialize(this, positionHandleShader, decalInfo, decal);
		}

		public void CreateHandleTextureTiling(DecalInfo decalInfo, Decal decal)
		{
            type = HandleType.TextureTiling;
			_textureTilingHandle = new GameObject("TextureTilingHandle").AddComponent<TextureTilingHandle>().Initialize(this, scaleHandleShader, decalInfo, decal);
		}

		public void CreateHandleMaskOffset(DecalInfo decalInfo, Decal decal)
		{
            type = HandleType.MaskOffset;
			_maskOffsetHandle = new GameObject("MaskOffsetHandle").AddComponent<MaskOffsetHandle>().Initialize(this, scaleHandleShader, decalInfo, decal);
		}

		public void CreateHandleMaskTiling(DecalInfo decalInfo, Decal decal)
		{
            type = HandleType.MaskTiling;
			_maskTilingHandle = new GameObject("MaskTilingHandle").AddComponent<MaskTilingHandle>().Initialize(this, scaleHandleShader, decalInfo, decal);
		}

        public void DestroyHandles()
        {
            _draggingHandle = null;
			_previousHandle = null;
            if (_positionHandle) Destroy(_positionHandle.gameObject);
            if (_rotationHandle) Destroy(_rotationHandle.gameObject);
            if (_scaleHandle) Destroy(_scaleHandle.gameObject);
            if (_textureOffsetHandle) Destroy(_textureOffsetHandle.gameObject);
            if (_textureTilingHandle) Destroy(_textureTilingHandle.gameObject);
            if (_maskOffsetHandle) Destroy(_maskOffsetHandle.gameObject);
            if (_maskTilingHandle) Destroy(_maskTilingHandle.gameObject);
        }

        private void Update()
        {
			if (!Physics.autoSyncTransforms)
			{
				// thanks BSG, very cool
				Physics.SyncTransforms();
			}

			UpdateAutoScale();

			// for some reason Input.GetMouseButtonUp(0) doesnt work here,
			// no idea why, some thing blocks it probably, so do it manually

			var mouseDown = Input.GetMouseButton(0);
			var hasPressed = mouseDown && !_previousMouseDown;
			var hasReleased = !mouseDown && _previousMouseDown;

			if (IsDragging)
			{
	            if (mouseDown)
	            {
	                _draggingHandle.Interact();
	                OnDraggingHandle?.Invoke();
	            }
	            if (hasReleased)
	            {
	                _draggingHandle.EndInteraction();
	                OnEndedDraggingHandle?.Invoke();
	                _draggingHandle = null;
	            }
			}
			else
			{
	            var (handle, hitPoint) = GetHandle();

				var canInteract = handle && handle.CanInteract(hitPoint);
				if (handle != _previousHandle)
				{
					if (canInteract)
					{
		                handle.SetInteractionColor();
					}
					if (_previousHandle)
					{
		                _previousHandle.SetDefaultColor();
					}
				}
				if (hasPressed && canInteract)
				{
	                _draggingHandle = handle;
	                _draggingHandle.StartInteraction();
	                OnStartedDraggingHandle?.Invoke();
				}

	            _previousHandle = handle;
			}

            _previousMousePosition = Input.mousePosition;
			_previousMouseDown = mouseDown;
        }

		public void UpdateAutoScale()
		{
            if (autoScale)
			{
                handleTransform.localScale = Vector3.one * (Vector3.Distance(handleCamera.transform.position, handleTransform.position) * autoScaleFactor) / 15f;
			}
		}

		public Ray GetCameraRay()
		{
            return handleCamera.ScreenPointToRay(Input.mousePosition);
		}

        private (HandleBase, Vector3) GetHandle()
        {
            var ray = GetCameraRay();
			var hitsCount = Physics.RaycastNonAlloc(ray, raycastHits, maxDistance: 10, layerMask: raycastLayerMask);
            if (hitsCount != 0)
			{
				for (var i = 0; i < hitsCount; i++)
				{
					var hit = raycastHits[i];
	                var p_handle = hit.collider.gameObject.GetComponentInParent<HandleBase>();
	                if (p_handle)
	                {
	                    return (p_handle, hit.point);
	                }
				}
			}

            return default;
        }

        static public RuntimeTransformHandle Create(
			Transform target,
			Camera handleCamera,
			Shader positionHandleShader,
			Shader rotationHandleShader,
			Shader scaleHandleShader,
			int raycastLayerMask)
        {
			var handleGO = new GameObject("RuntimeTransformHandle", typeof(RuntimeTransformHandle));
			var handleTransform = handleGO.transform;
            var handle = handleGO.GetComponent<RuntimeTransformHandle>();

			handleTransform.parent = target.parent;
			handleTransform.localPosition = target.localPosition;
			handleTransform.localRotation = target.localRotation;

            handle.targetTransform = target;
			handle.handleTransform = handleTransform;
			handle.handleCamera = handleCamera;

	        handle.positionHandleShader = positionHandleShader;
	        handle.rotationHandleShader = rotationHandleShader;
	        handle.scaleHandleShader = scaleHandleShader;

			handle.raycastHits = new RaycastHit[5];
			handle.raycastLayerMask = raycastLayerMask;

			handle.UpdateAutoScale();

            return handle;
        }

    }
}
