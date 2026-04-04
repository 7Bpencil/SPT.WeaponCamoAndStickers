//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.Common;
using SevenBoldPencil.WeaponCamo;
using System;
using System.IO;
using System.Security.Permissions;
using UnityEngine;

namespace RuntimeHandle
{
    public class TextureTilingAxis : HandleBase
    {
        private const float SIZE = 2;

        private Vector3 _axis;
		private Transform _arm;
		private Transform _tip;

		private Vector4 _uvAxis;
		private DecalInfo _decalInfo;
		private Decal _decal;

        private float _startOffsetLength;
		private Vector4 _startUV;

		public Vector3 Axis => _axis;

        public TextureTilingAxis Initialize(
			RuntimeTransformHandle transformHandle,
			TextureTilingHandle uvHandle,
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
            var offset = hitPoint - position;
			var offsetLength = offset.magnitude;
            var scale = offsetLength / _startOffsetLength;

			var uv = TextureTilingHandle.CalculateUV(_startUV, _uvAxis, scale);
			_decalInfo.UV = uv;
			_decal.ChangeUV(uv);

			SetHandleVisualScale(scale);
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

            _startOffsetLength = offset.magnitude;
			_startUV = _decalInfo.UV;

			SetHandleVisualScale(1);
        }

        public override void EndInteraction()
		{
			SetHandleVisualScale(1);
		}
    }
}
