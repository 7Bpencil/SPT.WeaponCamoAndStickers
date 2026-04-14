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
    public class MaskAngleAxis : HandleBase
    {
        private Vector3 _perp;
		private DecalInfo _decalInfo;
		private Decal _decal;

		private Vector3 _startOffsetLocalSpace;
		private float _startAngle;

        public MaskAngleAxis Initialize(
			RuntimeTransformHandle transformHandle,
			MaskAngleHandle rotationHandle,
			Vector3 perp,
			Color color,
			Shader handleShader,
			DecalInfo decalInfo,
            Decal decal)
        {
            _transformHandle = transformHandle;
            _defaultColor = color.WithAlpha(0.5f);

            _perp = perp;
			_decalInfo = decalInfo;
			_decal = decal;

            InitializeMaterial(handleShader);

            transform.SetParent(rotationHandle.transform, false);

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
			var sign = Mathf.Sign(_decalInfo.LocalScale.x) * Mathf.Sign(_decalInfo.LocalScale.z);

			_decalInfo.MaskAngle = _startAngle + angle * sign;
			_decal.ChangeMaskAngle(_decalInfo.MaskAngle);
        }

        public override bool CanInteract(Vector3 p_hitPoint)
        {
            var cameraDistance = (TransformHandle.position - _transformHandle.handleCamera.transform.position).magnitude;
            var pointDistance = (p_hitPoint - _transformHandle.handleCamera.transform.position).magnitude;
            return pointDistance <= cameraDistance;
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

			_startOffsetLocalSpace = TransformHandle.InverseTransformDirection(offset);
			_startAngle = _decalInfo.MaskAngle;
        }

        public override void EndInteraction()
        {

        }
    }
}
