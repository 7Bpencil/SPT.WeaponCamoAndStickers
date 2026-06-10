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
	public class PositionAxisHandler_TextureOffset : IPositionAxisHandler
	{
        private readonly Vector3 _axis1;
        private readonly Vector3 _axis2;
		private readonly Vector4 _uvAxis1;
		private readonly Vector4 _uvAxis2;
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private Vector3 _startLocalPosition;
		private Vector4 _startUV;

		public PositionAxisHandler_TextureOffset(Vector3 axis1, Vector3 axis2, Vector4 uvAxis1, Vector4 uvAxis2, DecalInfo decalInfo, Decal decal)
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
			_startLocalPosition = UVTools.GetHandleLocalPosition(_decalInfo.TextureUV);
			_startUV = _decalInfo.TextureUV;
		}

		public void SetPosition(Vector3 position)
		{
			var newLocalPosition = _decal.DecalTransform.InverseTransformPoint(position);
			var delta = newLocalPosition - _startLocalPosition;
			var uvOffset1 = delta.Sum(_axis1);
			var uvOffset2 = delta.Sum(_axis2);

			var newUV = Vector4.Scale(_startUV, _uvAxis1 + _uvAxis2) - (_uvAxis1 * uvOffset1 + _uvAxis2 * uvOffset2);
			var otherUV = Vector4.Scale(_startUV, UVTools.InverseMask(_uvAxis1 + _uvAxis2));

			_decalInfo.TextureUV = otherUV + newUV;
			_decal.ChangeTextureUV(_decalInfo.TextureUV);
		}
	}

    public class TextureOffsetHandle : MonoBehaviour
    {
        public TextureOffsetHandle Initialize(
            RuntimeTransformHandle transformHandle,
            Shader handleShader,
            DecalInfo decalInfo,
            Decal decal)
        {
			var positionHandleTransform = transform;
            positionHandleTransform.SetParent(transformHandle.handleTransform, false);

            var positionHandlerX = new PositionAxisHandler_TextureOffset(Vector3.right, Vector3.forward, new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), decalInfo, decal);
            var positionHandlerZ = new PositionAxisHandler_TextureOffset(Vector3.forward, Vector3.right, new Vector4(0, 1, 0, 0), new Vector4(1, 0, 0, 0), decalInfo, decal);

            var axisX = new GameObject("TextureOffsetAxis.X").AddComponent<PositionAxis>().Initialize(transformHandle, positionHandleTransform, positionHandlerX, Vector3.right, Color.red, handleShader);
            var axisZ = new GameObject("TextureOffsetAxis.Z").AddComponent<PositionAxis>().Initialize(transformHandle, positionHandleTransform, positionHandlerZ, Vector3.forward, Color.blue, handleShader);
            var planeXZ = new GameObject("TextureOffsetPlane.XZ").AddComponent<PositionPlane>().Initialize(transformHandle, positionHandleTransform, positionHandlerX, Vector3.right, Vector3.forward, Vector3.up, Color.green, handleShader);

            return this;
        }

        public void ResetHandleTransform(Transform transformHandle, DecalInfo decalInfo, Decal decal)
        {
			transformHandle.position = UVTools.GetHandlePosition(decal, decalInfo.TextureUV);
            transformHandle.localRotation *= UVTools.GetHandleLocalRotation(decalInfo.LocalScale, decalInfo.TextureAngle);
        }
    }
}
