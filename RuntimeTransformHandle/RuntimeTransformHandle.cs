using SevenBoldPencil.Common;
using SevenBoldPencil.WeaponCamo;
using System;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.Events;

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
        public Camera handleCamera;

        private Vector3 _previousMousePosition;
		private bool _previousMouseDown;
        private HandleBase _previousHandle;
        private HandleBase _draggingHandle;

		public bool IsDragging => _draggingHandle;

        private PositionHandle _positionHandle;
        private RotationHandle _rotationHandle;
        private ScaleHandle _scaleHandle;
        private TextureTilingHandle _textureTilingHandle;

        public Transform target;

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

		public void CreateHandleTextureTiling(DecalInfo decalInfo, Decal decal)
		{
            type = HandleType.TextureTiling;
			_textureTilingHandle = new GameObject("TextureTilingHandle").AddComponent<TextureTilingHandle>().Initialize(this, scaleHandleShader, decalInfo, decal);
		}

        public void DestroyHandles()
        {
            _draggingHandle = null;
			_previousHandle = null;
            if (_positionHandle) Destroy(_positionHandle.gameObject);
            if (_rotationHandle) Destroy(_rotationHandle.gameObject);
            if (_scaleHandle) Destroy(_scaleHandle.gameObject);
            if (_textureTilingHandle) Destroy(_textureTilingHandle.gameObject);
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
                transform.localScale = Vector3.one * (Vector3.Distance(handleCamera.transform.position, transform.position) * autoScaleFactor) / 15f;
			}
		}

		public Ray GetCameraRay()
		{
            return handleCamera.ScreenPointToRay(Input.mousePosition);
		}

        private (HandleBase, Vector3) GetHandle()
        {
            var ray = GetCameraRay();
            var hits = Physics.RaycastAll(ray); // TODO alloc
            if (hits.Length != 0)
			{
	            foreach (var hit in hits)
	            {
	                var p_handle = hit.collider.gameObject.GetComponentInParent<HandleBase>();
	                if (p_handle)
	                {
	                    return (p_handle, hit.point);
	                }
	            }
			}

            return default;
        }

        static public RuntimeTransformHandle Create(Transform target, Camera handleCamera, Shader positionHandleShader, Shader rotationHandleShader, Shader scaleHandleShader)
        {
			var handleGO = new GameObject("RuntimeTransformHandle", typeof(RuntimeTransformHandle));
			var handleTransform = handleGO.transform;
            var handle = handleGO.GetComponent<RuntimeTransformHandle>();

			handleTransform.parent = target.parent;
			handleTransform.localPosition = target.localPosition;
			handleTransform.localRotation = target.localRotation;

            handle.target = target;
			handle.handleCamera = handleCamera;

	        handle.positionHandleShader = positionHandleShader;
	        handle.rotationHandleShader = rotationHandleShader;
	        handle.scaleHandleShader = scaleHandleShader;

			handle.UpdateAutoScale();

            return handle;
        }

    }
}
