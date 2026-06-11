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
	public class RotationAxisHandle_TextureAngle : IRotationAxisHandle
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector3 _perp;
		private float _startAngle;

        public RotationAxisHandle_TextureAngle(DecalInfo decalInfo, Decal decal, Vector3 perp)
        {
    		_decalInfo = decalInfo;
    		_decal = decal;
			_perp = perp;
        }

		public Quaternion GetRotation()
		{
			var rotationOffset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.TextureAngle);
            return _decal.DecalTransform.rotation * rotationOffset;
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

    public class TextureAngleHandle : ITransformHandle
    {
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;

		public TextureAngleHandle(DecalInfo decalInfo, Decal decal)
		{
			_decalInfo = decalInfo;
			_decal = decal;
		}

        public void Init(RuntimeTransformHandle transformHandle, Shader handleShader, Transform root)
        {
            var rotationHandleY = new RotationAxisHandle_TextureAngle(_decalInfo, _decal, Vector3.up);
            var axisY = new GameObject("TextureAngleAxis.Y (XZ)").AddComponent<RotationAxis>().Initialize(transformHandle, root, rotationHandleY, Vector3.up, Color.green, handleShader);
        }

        public void Reset(Transform transformHandle)
        {
			var rotationOffset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.TextureAngle);
			transformHandle.position = UVTools.GetHandlePosition(_decal, _decalInfo.TextureUV);
            transformHandle.localRotation = _decal.DecalTransform.localRotation * rotationOffset;
        }
    }
}
