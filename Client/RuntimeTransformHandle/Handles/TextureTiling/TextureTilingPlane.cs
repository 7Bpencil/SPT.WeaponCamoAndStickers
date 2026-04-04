//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.Common;
using SevenBoldPencil.WeaponCamo;
using System;
using UnityEngine;

namespace RuntimeHandle
{
    public class TextureTilingPlane : HandleBase
    {
        private const float SIZE = 2;

        private Vector3 _axis1;
        private Vector3 _axis2;
        private Vector3 _perp;
        private GameObject _handle;

        private TextureTilingAxis _axis1Handle;
        private TextureTilingAxis _axis2Handle;

		private Vector4 _uvAxis1;
		private Vector4 _uvAxis2;
		private DecalInfo _decalInfo;
		private Decal _decal;

        private float _startOffsetLength;
		private Vector4 _startUV;

        public TextureTilingPlane Initialize(
			RuntimeTransformHandle transformHandle,
			TextureTilingHandle uvHandle,
			TextureTilingAxis axis1,
			TextureTilingAxis axis2,
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
            _axis1 = axis1.Axis;
            _axis2 = axis2.Axis;
            _perp = perp;

            _axis1Handle = axis1;
            _axis2Handle = axis2;

			_uvAxis1 = uvAxis1;
			_uvAxis2 = uvAxis2;
			_decalInfo = decalInfo;
			_decal = decal;

            InitializeMaterial(handleShader);

            transform.SetParent(uvHandle.transform, false);

            _handle = new GameObject("ScalePlane");
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
            var offset = hitPoint - position;
            var offsetLength = offset.magnitude;
            var scale = offsetLength / _startOffsetLength;

			var uv = TextureTilingHandle.CalculateUV(_startUV, _uvAxis1 + _uvAxis2, scale);
			_decalInfo.UV = uv;
			_decal.ChangeUV(uv);

            SetHandlesVisualScale(scale);
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

            _startOffsetLength = offset.magnitude;
			_startUV = _decalInfo.UV;

            SetHandlesVisualScale(1);
            SetHandlesInteractionColor();
        }

        public override void EndInteraction()
        {
            SetHandlesVisualScale(1);
            SetHandlesDefaultColor();
        }

        public void SetHandlesVisualScale(float scale)
        {
            _axis1Handle.SetHandleVisualScale(scale);
            _axis2Handle.SetHandleVisualScale(scale);
        }

        public void SetHandlesInteractionColor()
        {
            _axis1Handle.SetInteractionColor();
            _axis2Handle.SetInteractionColor();
        }

        public void SetHandlesDefaultColor()
        {
            _axis1Handle.SetDefaultColor();
            _axis2Handle.SetDefaultColor();
        }
	}
}
