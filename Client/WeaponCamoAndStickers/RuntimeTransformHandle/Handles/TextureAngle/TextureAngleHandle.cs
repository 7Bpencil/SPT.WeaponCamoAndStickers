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
	public class RotationAxisHandler_TextureAngle : IRotationAxisHandler
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector3 _perp;
		private float _startAngle;

        public RotationAxisHandler_TextureAngle(DecalInfo decalInfo, Decal decal, Vector3 perp)
        {
    		_decalInfo = decalInfo;
    		_decal = decal;
			_perp = perp;
        }

		public Quaternion GetRotation()
		{
			var offset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.TextureAngle);
            return _decal.DecalTransform.rotation * offset;
		}

		public void OnStartInteraction()
		{
			_startAngle = _decalInfo.TextureAngle;
		}

		public void SetAngle(float angle)
		{
			_decalInfo.TextureAngle = _startAngle + angle;
			_decal.ChangeTextureAngle(_decalInfo.TextureAngle);
		}
	}

    public class TextureAngleHandle : MonoBehaviour
    {
        public TextureAngleHandle Initialize(
            RuntimeTransformHandle transformHandle,
            Shader handleShader,
            DecalInfo decalInfo,
            Decal decal)
        {
            var rotationHandleTransform = transform;
            rotationHandleTransform.SetParent(transformHandle.transform, false);

            var rotationHandlerY = new RotationAxisHandler_TextureAngle(decalInfo, decal, Vector3.up);

            var axisY = new GameObject("TextureAngleAxis.Y (XZ)").AddComponent<RotationAxis>().Initialize(transformHandle, rotationHandleTransform, rotationHandlerY, Vector3.up, Color.green, handleShader);

            return this;
        }

        public void ResetHandleTransform(Transform transformHandle, DecalInfo decalInfo, Decal decal)
        {
			transformHandle.position = UVTools.GetHandlePosition(decal, decalInfo.TextureUV);
            transformHandle.localRotation *= UVTools.GetHandleLocalRotation(decalInfo.LocalScale, decalInfo.TextureAngle);
        }
    }
}
