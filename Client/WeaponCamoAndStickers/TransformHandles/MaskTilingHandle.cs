//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using RuntimeHandle;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	public class ScaleAxisHandle_MaskTiling(DecalInfo decalInfo, Decal decal, Vector2 uvScaleMask) : IScaleAxisHandle
	{
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private readonly Vector2 _uvScaleMask = uvScaleMask;
		private Vector4 _startUV;

		public void OnStartInteraction()
		{
			_startUV = _decalInfo.MaskUV;
		}

		public void SetScale(float scale)
		{
			_decalInfo.MaskUV = UVTools.ScaleUV(_startUV, _uvScaleMask, scale);
			_decal.ChangeMaskUV(_decalInfo.MaskUV);
		}
	}

    public class MaskTilingHandle(Plugin plugin, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal, Shader handleShader) : ITransformHandle
    {
		private readonly Plugin _plugin = plugin;
		private readonly string _itemId = itemId;
		private readonly int _decalIndex = decalIndex;
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private readonly Shader _handleShader = handleShader;

        public void Init(Transform transformHandle, Camera transformHandleCamera, Transform root)
        {
            var scaleHandleX = new ScaleAxisHandle_MaskTiling(_decalInfo, _decal, Vector2.right);
            var scaleHandleZ = new ScaleAxisHandle_MaskTiling(_decalInfo, _decal, Vector2.up);
			var scaleHandleXZ = new ScaleAxisHandle_MaskTiling(_decalInfo, _decal, Vector2.right + Vector2.up);

            var axisX = new GameObject("MaskTilingAxis.X").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleX, Vector3.right, Color.red, _handleShader);
            var axisZ = new GameObject("MaskTilingAxis.Z").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleZ, Vector3.forward, Color.blue, _handleShader);
            var planeXZ = new GameObject("MaskTilingPlane.XZ").AddComponent<ScalePlane>().Initialize(transformHandle, root, scaleHandleXZ, axisX, axisZ, Vector3.up, Color.green, _handleShader);
        }

        public void Reset(Transform transformHandle)
        {
			var rotationOffset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.MaskAngle);
			transformHandle.position = UVTools.GetHandlePosition(_decal, _decalInfo.MaskUV);
            transformHandle.localRotation = _decal.DecalTransform.localRotation * rotationOffset;
        }

		public void OnInteractionEnd()
		{
            _plugin.ApplyMaskUV(_itemId, _decalIndex);
		}
    }
}
