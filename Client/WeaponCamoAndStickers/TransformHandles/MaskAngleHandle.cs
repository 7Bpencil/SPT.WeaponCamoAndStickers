//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using RuntimeHandle;
using SevenBoldPencil.Common;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	public class RotationAxisHandle_MaskAngle(DecalInfo decalInfo, Decal decal, Vector3 perp) : IRotationAxisHandle
	{
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private readonly Vector3 _perp = perp;
		private float _startAngle;

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

    public class MaskAngleHandle(Plugin plugin, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal, Shader handleShader) : ITransformHandle
    {
		private readonly Plugin _plugin = plugin;
		private readonly string _itemId = itemId;
		private readonly int _decalIndex = decalIndex;
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private readonly Shader _handleShader = handleShader;

        public void Init(Transform transformHandle, Camera transformHandleCamera, Transform root)
        {
            var rotationHandleY = new RotationAxisHandle_MaskAngle(_decalInfo, _decal, Vector3.up);
            var axisY = new GameObject("MaskAngleAxis.Y (XZ)").AddComponent<RotationAxis>().Initialize(transformHandle, transformHandleCamera, root, rotationHandleY, Vector3.up, Color.green, _handleShader);
        }

        public void Reset(Transform transformHandle)
        {
			var rotationOffset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.MaskAngle);
			transformHandle.position = UVTools.GetHandlePosition(_decal, _decalInfo.MaskUV);
            transformHandle.localRotation = _decal.DecalTransform.localRotation * rotationOffset;
        }

		public void OnInteractionEnd()
		{
            _plugin.ApplyMaskAngle(_itemId, _decalIndex);
		}
    }
}
