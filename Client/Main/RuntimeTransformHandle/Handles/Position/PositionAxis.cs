using SevenBoldPencil.Common;
using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class PositionAxis : HandleBase
    {
        private Vector3 _axis;
        private float _offsetLength;

        public PositionAxis Initialize(RuntimeTransformHandle transformHandle, PositionHandle positionHandle, Vector3 axis, Color color, Shader handleShader)
        {
            _transformHandle = transformHandle;
            _axis = axis;
            _defaultColor = color.WithAlpha(0.5f);

            InitializeMaterial(handleShader);

            transform.SetParent(positionHandle.transform, false);

            {
                var o = new GameObject("Arm");
                o.transform.SetParent(transform, false);
                o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
                o.AddComponent<MeshRenderer>().material = _material;
                o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateCone(2f, .02f, .02f, 8, 1);
                o.AddComponent<MeshCollider>().sharedMesh = MeshUtils.CreateCone(2f, .1f, .02f, 8, 1);
            }

            {
                var o = new GameObject("Tip");
                o.transform.SetParent(transform, false);
                o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _axis);
                o.transform.localPosition = axis * 2;
                o.AddComponent<MeshRenderer>().material = _material;
                o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateCone(.4f, .2f, .0f, 8, 1);
                o.AddComponent<MeshCollider>();
            }

            return this;
        }

		public override bool CanInteract(Vector3 hitPoint)
		{
			return true;
		}

        public override void Interact()
        {
            var raxis = Target.TransformDirection(_axis);
            var position = Target.position;
            var ray = new Ray(position, raxis);
            var cameraRay = _transformHandle.GetCameraRay();
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = raxis * _offsetLength;
            var newPosition = hitPoint - offset;

            Target.position = newPosition;
            TransformHandle.position = newPosition;
        }

        public override void StartInteraction()
        {
            var raxis = Target.TransformDirection(_axis);
            var position = Target.position;
            var ray = new Ray(position, raxis);
            var cameraRay = _transformHandle.GetCameraRay();
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = hitPoint - position;

            _offsetLength = offset.magnitude;
        }

        public override void EndInteraction()
        {

        }
    }
}
