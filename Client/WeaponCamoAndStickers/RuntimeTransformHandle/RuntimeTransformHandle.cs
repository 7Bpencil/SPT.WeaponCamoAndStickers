using UnityEngine;

namespace RuntimeHandle
{
	public interface ITransformHandle
	{
		public void Init(Transform transformHandle, Camera transformHandleCamera, Transform root);
        public void Reset(Transform transformHandle);
		public void OnInteractionEnd();
	}

	public interface ICameraProvider
	{
		public Camera GetCamera();
		public Transform GetCameraTransform();
		public Ray GetCameraRay();
	}

	public class DefaultCameraProvider(Camera camera) : ICameraProvider
	{
		private readonly Camera _camera = camera;
		private readonly Transform _cameraTransform = camera.transform;

		public Camera GetCamera() => _camera;
		public Transform GetCameraTransform() => _cameraTransform;
		public Ray GetCameraRay() => _camera.ScreenPointToRay(Input.mousePosition);
	}

    /**
     * Created by Peter @sHTiF Stefcek 21.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class RuntimeTransformHandle : MonoBehaviour
    {
        private const bool _autoScale = true;
        private const float _autoScaleFactor = 1f / 30f;

		private Transform _transform;
		private ICameraProvider _cameraProvider;
		private RaycastHit[] _raycastHits;
		private int _raycastLayerMask;
		private Transform _root;
        private ITransformHandle _handle;

        private Vector3 _previousMousePosition;
		private bool _previousMouseDown;
        private HandleBase _previousHandle;
        private HandleBase _draggingHandle;

		public bool IsDragging => _draggingHandle;

        public static RuntimeTransformHandle Create(ITransformHandle handler, Transform parent, ICameraProvider cameraProvider, int raycastLayerMask)
        {
			var go = new GameObject("RuntimeTransformHandle");
			var handle = go.AddComponent<RuntimeTransformHandle>();
			var handleTransform = go.transform;

			handleTransform.parent = parent;

			handle._transform = handleTransform;
			handle._cameraProvider = cameraProvider;

			handle._raycastHits = new RaycastHit[5];
			handle._raycastLayerMask = raycastLayerMask;

			handle._root = new GameObject("Root").transform;
            handle._root.SetParent(handleTransform, false);

			handle._handle = handler;
			handle._handle.Init(handleTransform, cameraProvider.GetCamera(), handle._root);
			handle._handle.Reset(handleTransform);

			handle.UpdateAutoScale();

            return handle;
        }

		public void ResetHandleTransform()
		{
			_handle.Reset(_transform);
		}

		public void InvokeOnInteractionEnd()
		{
			_handle.OnInteractionEnd();
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
		            var cameraRay = _cameraProvider.GetCameraRay();
	                _draggingHandle.Interact(cameraRay);
	            }
	            if (hasReleased)
	            {
	                _draggingHandle.EndInteraction();
					_handle.OnInteractionEnd();
	                _draggingHandle = null;
	            }
			}
			else
			{
	            var cameraRay = _cameraProvider.GetCameraRay();
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
				}

	            _previousHandle = handle;
			}

            _previousMousePosition = Input.mousePosition;
			_previousMouseDown = mouseDown;
        }

		private void UpdateAutoScale()
		{
            if (_autoScale)
			{
				var cameraTransform = _cameraProvider.GetCameraTransform();
				var cameraDistance = Vector3.Distance(cameraTransform.position, _transform.position);
                _transform.localScale = Vector3.one * (cameraDistance * _autoScaleFactor);
			}
		}

        private (HandleBase, Vector3) GetHandle(Ray cameraRay)
        {
			var hitsCount = Physics.RaycastNonAlloc(cameraRay, _raycastHits, maxDistance: 10, layerMask: _raycastLayerMask);
            if (hitsCount != 0)
			{
				for (var i = 0; i < hitsCount; i++)
				{
					var hit = _raycastHits[i];
	                var p_handle = hit.collider.gameObject.GetComponentInParent<HandleBase>();
	                if (p_handle)
	                {
	                    return (p_handle, hit.point);
	                }
				}
			}

            return default;
        }

    }
}
