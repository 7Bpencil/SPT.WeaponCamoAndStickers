using SevenBoldPencil.Common;
using UnityEngine;

namespace RuntimeHandle
{
	public interface IRotationAxisHandler
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
		private Transform _rotationHandle;
		private IRotationAxisHandler _handler;
        private Vector3 _perp;
		private Vector3 _startOffsetLocalSpace;

        public RotationAxis Initialize(
			RuntimeTransformHandle transformHandle,
			Transform rotationHandle,
			IRotationAxisHandler handler,
			Vector3 perp,
			Color color,
			Shader handleShader)
        {
            _transformHandle = transformHandle;
            _defaultColor = color.WithAlpha(0.5f);

			_rotationHandle = rotationHandle;
			_handler = handler;
            _perp = perp;

            InitializeMaterial(handleShader);

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
			var cameraPosition = _transformHandle.handleCamera.transform.position;
            _material.SetVector("_CameraPosition", cameraPosition);
            _material.SetFloat("_CameraDistance", (cameraPosition - TransformHandle.position).magnitude);
        }

        public override void Interact(Ray cameraRay)
        {
            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
			var offset = hitPoint - position;
			var offsetLocalSpace = TransformHandle.InverseTransformDirection(offset);
			var angle = Vector3.SignedAngle(_startOffsetLocalSpace, offsetLocalSpace, _perp);

			_handler.SetAngle(angle);
			_rotationHandle.rotation = _handler.GetRotation();
        }

        public override bool CanInteract(Vector3 hitPoint)
        {
			var cameraPosition = _transformHandle.handleCamera.transform.position;
            var cameraDistance = (TransformHandle.position - cameraPosition).magnitude;
            var pointDistance = (hitPoint - cameraPosition).magnitude;
            return pointDistance <= cameraDistance;
        }

        public override void StartInteraction(Ray cameraRay)
        {
            TransformHandle.rotation = _handler.GetRotation();
			_rotationHandle.localRotation = Quaternion.identity;

            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = hitPoint - position;

			_startOffsetLocalSpace = TransformHandle.InverseTransformDirection(offset);
			_handler.OnStartInteraction();
        }

        public override void EndInteraction()
        {
            TransformHandle.rotation = _handler.GetRotation();
			_rotationHandle.localRotation = Quaternion.identity;
        }
    }
}
