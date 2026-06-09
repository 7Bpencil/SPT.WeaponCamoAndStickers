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

        public override void Interact(Ray cameraRay)
        {
			var angle = Interact_Rotation_Axis(cameraRay, TransformHandle, _perp, _startOffsetLocalSpace);

			_decalInfo.MaskAngle = _startAngle + angle;
			_decal.ChangeMaskAngle(_decalInfo.MaskAngle);
        }

        public override bool CanInteract(Vector3 hitPoint)
        {
			return CanInteract_Rotation_Axis(hitPoint, TransformHandle, _transformHandle.handleCamera);
        }

        public override void StartInteraction(Ray cameraRay)
        {
			var offset = StartInteraction_Rotation_Axis(cameraRay, TransformHandle, _perp);

			_startOffsetLocalSpace = TransformHandle.InverseTransformDirection(offset);
			_startAngle = _decalInfo.MaskAngle;
        }

        public override void EndInteraction()
        {

        }
    }
}
