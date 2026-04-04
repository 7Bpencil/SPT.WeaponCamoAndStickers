using SevenBoldPencil.Common;
using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class PositionPlane : HandleBase
    {
        private Vector3 _axis1;
        private Vector3 _axis2;
        private Vector3 _perp;
        private GameObject _handle;
        private Vector3 _offsetLocalSpace;

        public PositionPlane Initialize(RuntimeTransformHandle transformHandle, PositionHandle positionHandle, Vector3 axis1, Vector3 axis2, Vector3 perp, Color color, Shader handleShader)
        {
            _transformHandle = transformHandle;
            _defaultColor = color.WithAlpha(0.5f);
            _axis1 = axis1;
            _axis2 = axis2;
            _perp = perp;

            InitializeMaterial(handleShader);

            transform.SetParent(positionHandle.transform, false);

            _handle = new GameObject("PositionPlane");
            _handle.transform.SetParent(transform, false);
            _handle.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _perp);
            _handle.transform.localPosition = _axis1 + _axis2;
            _handle.AddComponent<MeshRenderer>().material = _material;
            _handle.AddComponent<MeshFilter>().mesh = MeshUtils.CreateBox(0.02f, 0.25f, 0.25f);
            _handle.AddComponent<MeshCollider>();

            return this;
        }

		public override bool CanInteract(Vector3 hitPoint)
		{
			return true;
		}

        public override void Interact()
        {
            var rperp = Target.TransformDirection(_perp);
            var position = Target.position;
            var plane = new Plane(rperp, position);
            var cameraRay = _transformHandle.GetCameraRay();
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = Target.TransformDirection(_offsetLocalSpace);
            var newPosition = hitPoint - offset;

            Target.position = newPosition;
            TransformHandle.position = newPosition;
        }

        public override void StartInteraction()
        {
            var rperp = Target.TransformDirection(_perp);
            var position = Target.position;
            var plane = new Plane(rperp, position);
            var cameraRay = _transformHandle.GetCameraRay();
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = hitPoint - position;

            _offsetLocalSpace = Target.InverseTransformDirection(offset);
        }

        public override void EndInteraction()
        {

        }
    }
}
