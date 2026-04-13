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
    public class MaskOffsetPlane : HandleBase
    {
        private Vector3 _axis1;
        private Vector3 _axis2;
        private Vector3 _perp;
        private GameObject _handle;

		private Vector4 _uvAxis1;
		private Vector4 _uvAxis2;
		private DecalInfo _decalInfo;
		private Decal _decal;

        private Vector3 _offsetLocalSpace;
		private Vector3 _startLocalPosition;
		private Vector4 _startUV;

        public MaskOffsetPlane Initialize(
			RuntimeTransformHandle transformHandle,
			MaskOffsetHandle positionHandle,
			Vector3 axis1,
			Vector3 axis2,
			Vector3 perp,
			Color color,
			Shader handleShader,
			Vector4 uvAxis1,
			Vector4 uvAxis2,
			DecalInfo decalInfo,
            Decal decal)
        {
            _transformHandle = transformHandle;
            _defaultColor = color.WithAlpha(0.5f);
            _axis1 = axis1;
            _axis2 = axis2;
            _perp = perp;

			_uvAxis1 = uvAxis1;
			_uvAxis2 = uvAxis2;
			_decalInfo = decalInfo;
			_decal = decal;

            InitializeMaterial(handleShader);

            transform.SetParent(positionHandle.transform, false);

            _handle = new GameObject("Plane");
            _handle.transform.SetParent(transform, false);
            _handle.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _perp);
            _handle.transform.localPosition = _axis1 + _axis2;
            _handle.AddComponent<MeshRenderer>().material = _material;
            _handle.AddComponent<MeshFilter>().mesh = MeshUtils.CreateBox(0.02f, 0.25f, 0.25f);
            _handle.AddComponent<MeshCollider>();

			TransformHandle.position = UVTools.GetHandlePosition(_decal, _decalInfo.MaskUV);

            return this;
        }

		public override bool CanInteract(Vector3 hitPoint)
		{
			return true;
		}

        public override void Interact()
        {
            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            var cameraRay = _transformHandle.GetCameraRay();
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = TransformHandle.TransformDirection(_offsetLocalSpace);
            var newPosition = hitPoint - offset;

			var newLocalPosition = Target.InverseTransformPoint(newPosition);
			var delta = newLocalPosition - _startLocalPosition;
			var uvOffset1 = delta.Sum(_axis1);
			var uvOffset2 = delta.Sum(_axis2);

			var newUV = Vector4.Scale(_startUV, _uvAxis1 + _uvAxis2) - (_uvAxis1 * uvOffset1 + _uvAxis2 * uvOffset2);
			var otherUV = Vector4.Scale(_startUV, UVTools.InverseMask(_uvAxis1 + _uvAxis2));

			_decalInfo.MaskUV = otherUV + newUV;
			_decal.ChangeMaskUV(_decalInfo.MaskUV);

            TransformHandle.position = newPosition;
        }

        public override void StartInteraction()
        {
            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            var cameraRay = _transformHandle.GetCameraRay();
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = hitPoint - position;

            _offsetLocalSpace = TransformHandle.InverseTransformDirection(offset);
			_startLocalPosition = UVTools.GetHandleLocalPosition(_decalInfo.MaskUV);
			_startUV = _decalInfo.MaskUV;
        }

        public override void EndInteraction()
        {

        }
    }
}
