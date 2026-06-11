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
	public class RotationAxisHandle_TextureAngle(DecalInfo decalInfo, Decal decal, Vector3 perp) : IRotationAxisHandle
	{
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private readonly Vector3 _perp = perp;
		private float _startAngle;

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

    public class TextureAngleHandle(Plugin plugin, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal, Shader handleShader) : ITransformHandle
    {
		private readonly Plugin _plugin = plugin;
		private readonly string _itemId = itemId;
		private readonly int _decalIndex = decalIndex;
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private readonly Shader _handleShader = handleShader;

        public void Init(Transform transformHandle, Camera transformHandleCamera, Transform root)
        {
            var rotationHandleY = new RotationAxisHandle_TextureAngle(_decalInfo, _decal, Vector3.up);
            var axisY = new GameObject("TextureAngleAxis.Y (XZ)").AddComponent<RotationAxis>().Initialize(transformHandle, transformHandleCamera, root, rotationHandleY, Vector3.up, Color.green, _handleShader);
        }

        public void Reset(Transform transformHandle)
        {
			var rotationOffset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.TextureAngle);
			transformHandle.position = UVTools.GetHandlePosition(_decal, _decalInfo.TextureUV);
            transformHandle.localRotation = _decal.DecalTransform.localRotation * rotationOffset;
        }

		public void OnInteractionEnd()
		{
            _plugin.ApplyTextureAngle(_itemId, _decalIndex);
		}
    }
}
