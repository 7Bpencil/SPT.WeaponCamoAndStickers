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
	public class PositionAxisHandle_TextureOffset(Vector3 axis1, Vector3 axis2, Vector4 uvAxis1, Vector4 uvAxis2, DecalInfo decalInfo, Decal decal) : IPositionAxisHandle
	{
        private readonly Vector3 _axis1 = axis1;
        private readonly Vector3 _axis2 = axis2;
		private readonly Vector4 _uvAxis1 = uvAxis1;
		private readonly Vector4 _uvAxis2 = uvAxis2;
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private Vector3 _startLocalPosition;
		private Vector4 _startUV;

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

    public class TextureOffsetHandle(Plugin plugin, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal, Shader handleShader) : ITransformHandle
    {
		private readonly Plugin _plugin = plugin;
		private readonly string _itemId = itemId;
		private readonly int _decalIndex = decalIndex;
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private readonly Shader _handleShader = handleShader;

        public void Init(Transform transformHandle, Camera transformHandleCamera, Transform root)
        {
            var positionHandleX = new PositionAxisHandle_TextureOffset(Vector3.right, Vector3.forward, new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), _decalInfo, _decal);
            var positionHandleZ = new PositionAxisHandle_TextureOffset(Vector3.forward, Vector3.right, new Vector4(0, 1, 0, 0), new Vector4(1, 0, 0, 0), _decalInfo, _decal);

            var axisX = new GameObject("TextureOffsetAxis.X").AddComponent<PositionAxis>().Initialize(transformHandle, root, positionHandleX, Vector3.right, Color.red, _handleShader);
            var axisZ = new GameObject("TextureOffsetAxis.Z").AddComponent<PositionAxis>().Initialize(transformHandle, root, positionHandleZ, Vector3.forward, Color.blue, _handleShader);
            var planeXZ = new GameObject("TextureOffsetPlane.XZ").AddComponent<PositionPlane>().Initialize(transformHandle, root, positionHandleX, Vector3.right, Vector3.forward, Vector3.up, Color.green, _handleShader);
        }

        public void Reset(Transform transformHandle)
        {
			var rotationOffset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.TextureAngle);
			transformHandle.parent = _decal.DecalRoot;
			transformHandle.position = UVTools.GetHandlePosition(_decal, _decalInfo.TextureUV);
            transformHandle.localRotation = _decal.DecalTransform.localRotation * rotationOffset;
        }

		public void OnInteractionEnd()
		{
            _plugin.ApplyTextureUV(_itemId, _decalIndex);
		}
    }
}
