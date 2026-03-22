using System;
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
        public HandleAxes axes = HandleAxes.XYZ;
        public HandleType type = HandleType.POSITION;

        public bool autoScale = true;
        public float autoScaleFactor = 0.5f;
        public Camera handleCamera;

        private Vector3 _previousMousePosition;
		private bool _previousMouseDown;
        private HandleBase _previousAxis;

        private HandleBase _draggingHandle;
		public bool IsDragging => _draggingHandle;

        private PositionHandle _positionHandle;
        private RotationHandle _rotationHandle;
        private ScaleHandle _scaleHandle;

        public Transform target;

        public Action OnStartedDraggingHandle;
        public Action OnDraggingHandle;
        public Action OnEndedDraggingHandle;

        private Shader positionHandleShader;
        private Shader rotationHandleShader;
        private Shader scaleHandleShader;

        private void CreateHandles()
        {
			if (type == HandleType.POSITION)
			{
				_positionHandle = new GameObject("PositionHandle").AddComponent<PositionHandle>().Initialize(this, positionHandleShader);
			}
			if (type == HandleType.ROTATION)
			{
				_rotationHandle = new GameObject("RotationHandle").AddComponent<RotationHandle>().Initialize(this, rotationHandleShader);
			}
			if (type == HandleType.SCALE)
			{
				_scaleHandle = new GameObject("ScaleHandle").AddComponent<ScaleHandle>().Initialize(this, scaleHandleShader);
			}
        }

        private void Clear()
        {
            _draggingHandle = null;
            if (_positionHandle) Destroy(_positionHandle.gameObject);
            if (_rotationHandle) Destroy(_rotationHandle.gameObject);
            if (_scaleHandle) Destroy(_scaleHandle.gameObject);
        }

        private void Update()
        {
			if (!Physics.autoSyncTransforms)
			{
				// thanks BSG, very cool
				Physics.SyncTransforms();
			}

			UpdateAutoScale();

            var (handle, hitPoint) = GetHandle();

            HandleOverEffect(handle, hitPoint);

			// for some reason Input.GetMouseButtonUp(0) doesnt work here,
			// no idea why, some thing blocks it probably, so do it manually

			var mouseDown = Input.GetMouseButton(0);
			var hasPressed = mouseDown && !_previousMouseDown;
			var hasReleased = !mouseDown && _previousMouseDown;

            if (mouseDown && _draggingHandle)
            {
                _draggingHandle.Interact(_previousMousePosition);
                OnDraggingHandle?.Invoke();
            }

            if (hasPressed && handle && handle.CanInteract(hitPoint))
            {
                _draggingHandle = handle;
                _draggingHandle.StartInteraction(hitPoint);
                OnStartedDraggingHandle?.Invoke();
            }

            if (hasReleased && _draggingHandle)
            {
                _draggingHandle.EndInteraction();
                OnEndedDraggingHandle?.Invoke();
                _draggingHandle = null;
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

        private void HandleOverEffect(HandleBase p_axis, Vector3 p_hitPoint)
        {
            if (!_draggingHandle && _previousAxis && (_previousAxis != p_axis || !_previousAxis.CanInteract(p_hitPoint)))
            {
                _previousAxis.SetDefaultColor();
            }

            if (p_axis && !_draggingHandle && p_axis.CanInteract(p_hitPoint))
            {
                p_axis.SetColor(Color.yellow);
            }

            _previousAxis = p_axis;
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

        public void SetHandleMode(HandleType mode)
        {
            type = mode;
			Clear();
			CreateHandles();
        }

        public void EnableXAxis(bool enable)
        {
            if (enable)
			{
                axes |= HandleAxes.X;
			}
            else
			{
                axes &= ~HandleAxes.X;
			}
        }

        public void EnableYAxis(bool enable)
        {
            if (enable)
			{
                axes |= HandleAxes.Y;
			}
            else
			{
                axes &= ~HandleAxes.Y;
			}
        }

        public void EnableZAxis(bool enable)
        {
            if (enable)
			{
                axes |= HandleAxes.Z;
			}
            else
			{
                axes &= ~HandleAxes.Z;
			}
        }

        public void SetAxes(HandleAxes newAxes)
        {
            axes = newAxes;
        }
    }
}
