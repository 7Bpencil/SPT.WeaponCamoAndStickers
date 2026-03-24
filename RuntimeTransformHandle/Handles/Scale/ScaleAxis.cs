using SevenBoldPencil.Common;
using System.IO;
using System.Security.Permissions;
using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class ScaleAxis : HandleBase
    {
        private const float SIZE = 2;

        private Vector3 _axis;
		private Transform _arm;
		private Transform _tip;
        private float _startOffsetLength;
        private Vector3 _startLocalScale;

		public Vector3 Axis => _axis;

        public ScaleAxis Initialize(RuntimeTransformHandle transformHandle, ScaleHandle scaleHandle, Vector3 axis, Color color, Shader handleShader)
        {
            _transformHandle = transformHandle;
            _axis = axis;
            _defaultColor = color.WithAlpha(0.5f);

            InitializeMaterial(handleShader);

            transform.SetParent(scaleHandle.transform, false);

            {
                var o = new GameObject("Arm");
                o.transform.SetParent(transform, false);
                o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
                o.AddComponent<MeshRenderer>().material = _material;
                o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateCone(axis.magnitude * SIZE, .02f, .02f, 8, 1);
                o.AddComponent<MeshCollider>().sharedMesh = MeshUtils.CreateCone(axis.magnitude * SIZE, .1f, .02f, 8, 1);
				_arm = o.transform;
            }

            {
                var o = new GameObject("Tip");
                o.transform.SetParent(transform, false);
                o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
                o.transform.localPosition = axis * SIZE;
                o.AddComponent<MeshRenderer>().material = _material;
                o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateBox(.25f, .25f, .25f);
                o.AddComponent<MeshCollider>();
				_tip = o.transform;
            }

            return this;
        }

        public void SetHandleVisualScale(float scale)
        {
            _arm.localScale = new Vector3(1, scale, 1);
            _tip.localPosition = _axis * (SIZE * scale);
        }

        public override void Interact(Vector3 p_previousPosition)
        {
            base.Interact(p_previousPosition);

            var raxis = Target.TransformDirection(_axis);
            var position = Target.position;
            var ray = new Ray(position, raxis);
            var cameraRay = _transformHandle.GetCameraRay();
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = hitPoint - position;
			var offsetLength = offset.magnitude;
            var scale = offsetLength / _startOffsetLength;

            Target.localScale = ScaleHandle.CalculateScale(_startLocalScale, _axis, scale);

			SetHandleVisualScale(scale);
        }

        public override void StartInteraction(Vector3 p_hitPoint)
        {
            base.StartInteraction(p_hitPoint);

            var raxis = Target.TransformDirection(_axis);
            var position = Target.position;
            var ray = new Ray(position, raxis);
            var cameraRay = _transformHandle.GetCameraRay();
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = hitPoint - position;

            _startOffsetLength = offset.magnitude;
            _startLocalScale = Target.localScale;

			SetHandleVisualScale(1);
        }

        public override void EndInteraction()
		{
            base.EndInteraction();

			SetHandleVisualScale(1);
		}
    }
}
