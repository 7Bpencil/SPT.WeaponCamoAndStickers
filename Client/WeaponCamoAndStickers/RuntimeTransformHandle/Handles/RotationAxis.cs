using UnityEngine;

namespace RuntimeHandle
{
	public interface IRotationAxisHandle
	{
		public Quaternion GetRotation();
		public void OnStartInteraction();
		public void SetAngle(float angle);
	}

    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class RotationAxis : HandleBase
    {
        public static readonly int _CameraPosition = Shader.PropertyToID("_CameraPosition");
        public static readonly int _CameraDistance = Shader.PropertyToID("_CameraDistance");

		private Transform _transformHandle;
		private Transform _transformHandleCamera;
		private Transform _rotationHandle;
		private IRotationAxisHandle _handle;
        private Vector3 _perp;
		private Vector3 _startOffsetLocalSpace;

        public RotationAxis Initialize(
			Transform transformHandle,
			Camera transformHandleCamera,
			Transform rotationHandle,
			IRotationAxisHandle handle,
			Vector3 perp,
			Color color,
			Shader handleShader)
        {
			_transformHandle = transformHandle;
			_transformHandleCamera = transformHandleCamera.transform;
			_rotationHandle = rotationHandle;
			_handle = handle;
            _perp = perp;

            Init(handleShader, color);

            transform.SetParent(_rotationHandle, false);

			{
	            var o = new GameObject("Arc");
	            o.transform.SetParent(transform, false);
	            o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _perp);
	            o.AddComponent<MeshRenderer>().material = _material;
	            o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateTorus(2f, .04f, 32, 6);
	            o.AddComponent<MeshCollider>().sharedMesh = MeshUtils.CreateTorus(2f, .1f, 32, 6);
			}

            return this;
        }

        public void Update()
        {
			var cameraPosition = _transformHandleCamera.position;
			var cameraDistance = (cameraPosition - _transformHandle.position).magnitude;
            _material.SetVector(_CameraPosition, cameraPosition);
            _material.SetFloat(_CameraDistance, cameraDistance);
        }

        public override void Interact(Ray cameraRay)
        {
			var (position, hitPoint) = GetPlaneHitPoint(cameraRay, _transformHandle, _perp);
			var offset = hitPoint - position;
			var offsetLocalSpace = _transformHandle.InverseTransformDirection(offset);
			var angle = Vector3.SignedAngle(_startOffsetLocalSpace, offsetLocalSpace, _perp);

			_handle.SetAngle(angle);
			_rotationHandle.rotation = _handle.GetRotation();
        }

        public override bool CanInteract(Vector3 hitPoint)
        {
			var cameraPosition = _transformHandleCamera.position;
            var cameraDistance = (_transformHandle.position - cameraPosition).magnitude;
            var pointDistance = (hitPoint - cameraPosition).magnitude;
            return pointDistance <= cameraDistance;
        }

        public override void StartInteraction(Ray cameraRay)
        {
            _transformHandle.rotation = _handle.GetRotation();
			_rotationHandle.localRotation = Quaternion.identity;

			var (position, hitPoint) = GetPlaneHitPoint(cameraRay, _transformHandle, _perp);
            var offset = hitPoint - position;

			_startOffsetLocalSpace = _transformHandle.InverseTransformDirection(offset);
			_handle.OnStartInteraction();
        }

        public override void EndInteraction()
        {
            _transformHandle.rotation = _handle.GetRotation();
			_rotationHandle.localRotation = Quaternion.identity;
        }
    }
}
