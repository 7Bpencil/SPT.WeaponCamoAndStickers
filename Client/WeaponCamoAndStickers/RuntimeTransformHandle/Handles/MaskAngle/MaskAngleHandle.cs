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
	public class RotationAxisHandler_MaskAngle : IRotationAxisHandler
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector3 _perp;
		private float _startAngle;

        public RotationAxisHandler_MaskAngle(DecalInfo decalInfo, Decal decal, Vector3 perp)
        {
    		_decalInfo = decalInfo;
    		_decal = decal;
			_perp = perp;
        }

		public Quaternion GetRotation()
		{
			var rotationOffset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.MaskAngle);
            return _decal.DecalTransform.rotation * rotationOffset;
		}

		public void OnStartInteraction()
		{
			_startAngle = _decalInfo.MaskAngle;
		}

		public void SetAngle(float angle)
		{
			_decalInfo.MaskAngle = _startAngle + angle;
			_decal.ChangeMaskAngle(_decalInfo.MaskAngle);
		}
	}

    public class MaskAngleHandle : ITransformHandle
    {
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;

		public MaskAngleHandle(DecalInfo decalInfo, Decal decal)
		{
			_decalInfo = decalInfo;
			_decal = decal;
		}

        public void Init(RuntimeTransformHandle transformHandle, Shader handleShader, Transform root)
        {
            var rotationHandlerY = new RotationAxisHandler_MaskAngle(_decalInfo, _decal, Vector3.up);
            var axisY = new GameObject("MaskAngleAxis.Y (XZ)").AddComponent<RotationAxis>().Initialize(transformHandle, root, rotationHandlerY, Vector3.up, Color.green, handleShader);
        }

        public void Reset(Transform transformHandle)
        {
			var rotationOffset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.MaskAngle);
			transformHandle.position = UVTools.GetHandlePosition(_decal, _decalInfo.MaskUV);
            transformHandle.localRotation = _decal.DecalTransform.localRotation * rotationOffset;
        }
    }
}
