//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.Common;
using SevenBoldPencil.WeaponCamoAndStickers;
using UnityEngine;

namespace RuntimeHandle
{
    public class MaskOffsetAxis : HandleBase
    {
        private Vector3 _axis;

		private Vector4 _uvAxis;
		private DecalInfo _decalInfo;
		private Decal _decal;

        private float _startOffsetLength;
		private Vector3 _startLocalPosition;
		private Vector4 _startUV;

        public MaskOffsetAxis Initialize(
			RuntimeTransformHandle transformHandle,
			MaskOffsetHandle uvHandle,
			Vector3 axis,
			Color color,
			Shader handleShader,
			Vector4 uvAxis,
			DecalInfo decalInfo,
            Decal decal)
        {
            _transformHandle = transformHandle;
            _axis = axis;
            _defaultColor = color.WithAlpha(0.5f);

			_uvAxis = uvAxis;
			_decalInfo = decalInfo;
			_decal = decal;

            InitializeMaterial(handleShader);

            transform.SetParent(uvHandle.transform, false);

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

			TransformHandle.position = UVTools.GetHandlePosition(_decal, _decalInfo.MaskUV);

            return this;
        }

		public override bool CanInteract(Vector3 hitPoint)
		{
			return true;
		}

        public override void Interact()
        {
            var raxis = TransformHandle.TransformDirection(_axis);
            var position = TransformHandle.position;
            var ray = new Ray(position, raxis);
            var cameraRay = _transformHandle.GetCameraRay();
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = raxis * _startOffsetLength;
            var newPosition = hitPoint - offset;

			var newLocalPosition = Target.InverseTransformPoint(newPosition);
			var delta = newLocalPosition - _startLocalPosition;
			var uvOffset = delta.Sum(_axis);

			var newUV = Vector4.Scale(_startUV, _uvAxis) - _uvAxis * uvOffset;
			var otherUV = Vector4.Scale(_startUV, UVTools.InverseMask(_uvAxis));

			_decalInfo.MaskUV = otherUV + newUV;
			_decal.ChangeMaskUV(_decalInfo.MaskUV);

            TransformHandle.position = newPosition;
        }

        public override void StartInteraction()
        {
            var raxis = TransformHandle.TransformDirection(_axis);
            var position = TransformHandle.position;
            var ray = new Ray(position, raxis);
            var cameraRay = _transformHandle.GetCameraRay();
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = hitPoint - position;

            _startOffsetLength = offset.magnitude;
			_startLocalPosition = UVTools.GetHandleLocalPosition(_decalInfo.MaskUV);
			_startUV = _decalInfo.MaskUV;
        }

        public override void EndInteraction()
        {

        }
    }
}
