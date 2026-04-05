using SevenBoldPencil.Common;
using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class RotationAxis : HandleBase
    {
		private Transform _rotationHandle;

        private Vector3 _axis1;
        private Vector3 _axis2;
        private Vector3 _perp;

		private Vector3 _startOffsetLocalSpace;
		private Quaternion _startLocalRotation;

        public RotationAxis Initialize(RuntimeTransformHandle transformHandle, RotationHandle rotationHandle, Vector3 axis1, Vector3 axis2, Vector3 perp, Color color, Shader handleShader)
        {
            _transformHandle = transformHandle;
            _defaultColor = color.WithAlpha(0.5f);

			_rotationHandle = rotationHandle.transform;

            _axis1 = axis1;
            _axis2 = axis2;
            _perp = perp;

            InitializeMaterial(handleShader);

            transform.SetParent(_rotationHandle, false);

            var o = new GameObject("Arc");
            o.transform.SetParent(transform, false);
            o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _perp);
            o.AddComponent<MeshRenderer>().material = _material;
            o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateTorus(2f, .04f, 32, 6);
            o.AddComponent<MeshCollider>().sharedMesh = MeshUtils.CreateTorus(2f, .1f, 32, 6);

            return this;
        }

        public void Update()
        {
            _material.SetVector("_CameraPosition", _transformHandle.handleCamera.transform.position);
            _material.SetFloat("_CameraDistance", (_transformHandle.handleCamera.transform.position - TransformHandle.position).magnitude);
        }

        public override void Interact()
        {
            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            var cameraRay = _transformHandle.GetCameraRay();
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
			var offset = hitPoint - position;
			var offsetLocalSpace = TransformHandle.InverseTransformDirection(offset);
			var angle = Vector3.SignedAngle(_startOffsetLocalSpace, offsetLocalSpace, _perp);
			var newLocalRotation = _startLocalRotation * Quaternion.AngleAxis(angle, _perp);

			Target.localRotation = newLocalRotation;
			_rotationHandle.rotation = Target.rotation;
        }

        public override bool CanInteract(Vector3 p_hitPoint)
        {
            var cameraDistance = (TransformHandle.position - _transformHandle.handleCamera.transform.position).magnitude;
            var pointDistance = (p_hitPoint - _transformHandle.handleCamera.transform.position).magnitude;
            return pointDistance <= cameraDistance;
        }

        public override void StartInteraction()
        {
            TransformHandle.rotation = Target.rotation;
			_rotationHandle.rotation = Quaternion.identity;

            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            var cameraRay = _transformHandle.GetCameraRay();
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = hitPoint - position;

			_startOffsetLocalSpace = TransformHandle.InverseTransformDirection(offset);
			_startLocalRotation = Target.localRotation;
        }

        public override void EndInteraction()
        {
            TransformHandle.rotation = Target.rotation;
			_rotationHandle.localRotation = Quaternion.identity;
        }
    }
}
