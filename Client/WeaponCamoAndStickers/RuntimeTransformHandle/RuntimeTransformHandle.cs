using SevenBoldPencil.WeaponCamoAndStickers;
using System;
using UnityEngine;

namespace RuntimeHandle
{
	public interface ITransformHandle
	{
		public void Init(Transform transformHandle, Camera transformHandleCamera, Shader handleShader, Transform root);
        public void Reset(Transform transformHandle);
	}

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

		private Transform _root;
        private ITransformHandle _handle;

        public Transform targetTransform;
		public Transform handleTransform;
        public Camera handleCamera;

        public Action OnStartedDraggingHandle;
        public Action OnDraggingHandle;
        public Action OnEndedDraggingHandle;

        private Shader positionHandleShader;
        private Shader rotationHandleShader;
        private Shader scaleHandleShader;

		private void CreateHandle(ITransformHandle handle, Shader handleShader, HandleType handleType)
		{
			type = handleType;
			_root = CreateRoot();
			_handle = handle;
			_handle.Init(handleTransform, handleCamera, handleShader, _root);
		}

		private Transform CreateRoot()
		{
			var root = new GameObject("Root").transform;
            root.SetParent(handleTransform, false);
			return root;
		}

        public void CreateHandlePosition()
        {
			CreateHandle(new PositionHandle(targetTransform), positionHandleShader, HandleType.Position);
		}

        public void CreateHandleRotation()
		{
			CreateHandle(new RotationHandle(targetTransform), rotationHandleShader, HandleType.Rotation);
		}

        public void CreateHandleScale(DecalInfo decalInfo, Decal decal)
		{
			CreateHandle(new ScaleHandle(decalInfo, decal), scaleHandleShader, HandleType.Scale);
        }

		public void CreateHandleTextureOffset(DecalInfo decalInfo, Decal decal)
		{
			CreateHandle(new TextureOffsetHandle(decalInfo, decal), positionHandleShader, HandleType.TextureOffset);
		}

		public void CreateHandleTextureAngle(DecalInfo decalInfo, Decal decal)
		{
			CreateHandle(new TextureAngleHandle(decalInfo, decal), rotationHandleShader, HandleType.TextureAngle);
		}

		public void CreateHandleTextureTiling(DecalInfo decalInfo, Decal decal)
		{
			CreateHandle(new TextureTilingHandle(decalInfo, decal), scaleHandleShader, HandleType.TextureTiling);
		}

		public void CreateHandleMaskOffset(DecalInfo decalInfo, Decal decal)
		{
			CreateHandle(new MaskOffsetHandle(decalInfo, decal), positionHandleShader, HandleType.MaskOffset);
		}

		public void CreateHandleMaskAngle(DecalInfo decalInfo, Decal decal)
		{
			CreateHandle(new MaskAngleHandle(decalInfo, decal), rotationHandleShader, HandleType.MaskAngle);
		}

		public void CreateHandleMaskTiling(DecalInfo decalInfo, Decal decal)
		{
			CreateHandle(new MaskTilingHandle(decalInfo, decal), scaleHandleShader, HandleType.MaskTiling);
		}

		public void ResetHandleTransform()
		{
			_handle.Reset(handleTransform);
		}

        public void DestroyHandles()
        {
            _draggingHandle = null;
			_previousHandle = null;

			Destroy(_root.gameObject);
			_handle = null;
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
		            var cameraRay = GetCameraRay();
	                _draggingHandle.Interact(cameraRay);
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
	            var cameraRay = GetCameraRay();
	            var (handle, hitPoint) = GetHandle(cameraRay);
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
	                _draggingHandle.StartInteraction(cameraRay);
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

        private (HandleBase, Vector3) GetHandle(Ray cameraRay)
        {
			var hitsCount = Physics.RaycastNonAlloc(cameraRay, raycastHits, maxDistance: 10, layerMask: raycastLayerMask);
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

        public static RuntimeTransformHandle Create(
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
