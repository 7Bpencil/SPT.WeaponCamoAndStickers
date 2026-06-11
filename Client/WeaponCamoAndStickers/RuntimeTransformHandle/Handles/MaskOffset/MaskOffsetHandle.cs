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
	public class PositionAxisHandle_MaskOffset : IPositionAxisHandle
	{
        private readonly Vector3 _axis1;
        private readonly Vector3 _axis2;
		private readonly Vector4 _uvAxis1;
		private readonly Vector4 _uvAxis2;
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private Vector3 _startLocalPosition;
		private Vector4 _startUV;

		public PositionAxisHandle_MaskOffset(Vector3 axis1, Vector3 axis2, Vector4 uvAxis1, Vector4 uvAxis2, DecalInfo decalInfo, Decal decal)
		{
	        _axis1 = axis1;
	        _axis2 = axis2;
			_uvAxis1 = uvAxis1;
			_uvAxis2 = uvAxis2;
			_decalInfo = decalInfo;
			_decal = decal;
		}

		public void OnStartInteraction()
		{
			_startLocalPosition = UVTools.GetHandleLocalPosition(_decalInfo.MaskUV);
			_startUV = _decalInfo.MaskUV;
		}

		public void SetPosition(Vector3 position)
		{
			var newLocalPosition = _decal.DecalTransform.InverseTransformPoint(position);
			var delta = newLocalPosition - _startLocalPosition;
			var uvOffset1 = delta.Sum(_axis1);
			var uvOffset2 = delta.Sum(_axis2);

			var newUV = Vector4.Scale(_startUV, _uvAxis1 + _uvAxis2) - (_uvAxis1 * uvOffset1 + _uvAxis2 * uvOffset2);
			var otherUV = Vector4.Scale(_startUV, UVTools.InverseMask(_uvAxis1 + _uvAxis2));

			_decalInfo.MaskUV = otherUV + newUV;
			_decal.ChangeMaskUV(_decalInfo.MaskUV);
		}
	}

    public class MaskOffsetHandle : ITransformHandle
    {
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;

		public MaskOffsetHandle(DecalInfo decalInfo, Decal decal)
		{
			_decalInfo = decalInfo;
			_decal = decal;
		}

        public void Init(Transform transformHandle, Camera transformHandleCamera, Shader handleShader, Transform root)
        {
            var positionHandleX = new PositionAxisHandle_MaskOffset(Vector3.right, Vector3.forward, new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), _decalInfo, _decal);
            var positionHandleZ = new PositionAxisHandle_MaskOffset(Vector3.forward, Vector3.right, new Vector4(0, 1, 0, 0), new Vector4(1, 0, 0, 0), _decalInfo, _decal);

            var axisX = new GameObject("MaskOffsetAxis.X").AddComponent<PositionAxis>().Initialize(transformHandle, root, positionHandleX, Vector3.right, Color.red, handleShader);
            var axisZ = new GameObject("MaskOffsetAxis.Z").AddComponent<PositionAxis>().Initialize(transformHandle, root, positionHandleZ, Vector3.forward, Color.blue, handleShader);
            var planeXZ = new GameObject("MaskOffsetPlane.XZ").AddComponent<PositionPlane>().Initialize(transformHandle, root, positionHandleX, Vector3.right, Vector3.forward, Vector3.up, Color.green, handleShader);
        }

        public void Reset(Transform transformHandle)
        {
			var rotationOffset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.MaskAngle);
			transformHandle.position = UVTools.GetHandlePosition(_decal, _decalInfo.MaskUV);
            transformHandle.localRotation = _decal.DecalTransform.localRotation * rotationOffset;
        }
    }
}
