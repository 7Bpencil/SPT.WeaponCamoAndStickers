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
    public class TextureOffsetAxis : HandleBase
    {
        private Vector3 _axis1;
        private Vector3 _axis2;

		private Vector4 _uvAxis1;
		private Vector4 _uvAxis2;
		private DecalInfo _decalInfo;
		private Decal _decal;

        private float _startOffsetLength;
		private Vector3 _startLocalPosition;
		private Vector4 _startUV;

        public TextureOffsetAxis Initialize(
			RuntimeTransformHandle transformHandle,
			TextureOffsetHandle offsetHandle,
			Vector3 axis1,
			Vector3 axis2,
			Color color,
			Shader handleShader,
			Vector4 uvAxis1,
			Vector4 uvAxis2,
			DecalInfo decalInfo,
            Decal decal)
        {
            _transformHandle = transformHandle;
            _axis1 = axis1;
            _axis2 = axis2;
            _defaultColor = color.WithAlpha(0.5f);

			_uvAxis1 = uvAxis1;
			_uvAxis2 = uvAxis2;
			_decalInfo = decalInfo;
			_decal = decal;

            InitializeMaterial(handleShader);

            transform.SetParent(offsetHandle.transform, false);

            {
                var o = new GameObject("Arm");
                o.transform.SetParent(transform, false);
                o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _axis1);
                o.AddComponent<MeshRenderer>().material = _material;
                o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateCone(2f, .02f, .02f, 8, 1);
                o.AddComponent<MeshCollider>().sharedMesh = MeshUtils.CreateCone(2f, .1f, .02f, 8, 1);
            }

            {
                var o = new GameObject("Tip");
                o.transform.SetParent(transform, false);
                o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _axis1);
                o.transform.localPosition = _axis1 * 2;
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
            var raxis = TransformHandle.TransformDirection(_axis1);
            var position = TransformHandle.position;
            var ray = new Ray(position, raxis);
            var cameraRay = _transformHandle.GetCameraRay();
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = raxis * _startOffsetLength;
            var newPosition = hitPoint - offset;

			var newLocalPosition = Target.InverseTransformPoint(newPosition);
			var delta = newLocalPosition - _startLocalPosition;
			var uvOffset1 = delta.Sum(_axis1);
			var uvOffset2 = delta.Sum(_axis2);

			var newUV = Vector4.Scale(_startUV, _uvAxis1 + _uvAxis2) - (_uvAxis1 * uvOffset1 + _uvAxis2 * uvOffset2);
			var otherUV = Vector4.Scale(_startUV, UVTools.InverseMask(_uvAxis1 + _uvAxis2));

			_decalInfo.TextureUV = otherUV + newUV;
			_decal.ChangeTextureUV(_decalInfo.TextureUV);

            TransformHandle.position = newPosition;
        }

        public override void StartInteraction()
        {
            var raxis = TransformHandle.TransformDirection(_axis1);
            var position = TransformHandle.position;
            var ray = new Ray(position, raxis);
            var cameraRay = _transformHandle.GetCameraRay();
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = hitPoint - position;

            _startOffsetLength = offset.magnitude;
			_startLocalPosition = UVTools.GetHandleLocalPosition(_decalInfo.TextureUV);
			_startUV = _decalInfo.TextureUV;
        }

        public override void EndInteraction()
        {

        }
    }
}
