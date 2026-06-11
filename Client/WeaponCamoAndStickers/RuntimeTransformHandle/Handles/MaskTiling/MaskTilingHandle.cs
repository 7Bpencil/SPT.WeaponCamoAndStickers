//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.WeaponCamoAndStickers;
using UnityEngine;

namespace RuntimeHandle
{
	public class ScaleAxisHandle_MaskTiling : IScaleAxisHandle
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector2 _uvAxis;
		private Vector4 _startUV;

        public ScaleAxisHandle_MaskTiling(DecalInfo decalInfo, Decal decal, Vector2 uvAxis)
        {
            _decalInfo = decalInfo;
            _decal = decal;
            _uvAxis = uvAxis;
        }

		public void OnStartInteraction()
		{
			_startUV = _decalInfo.MaskUV;
		}

		public void SetScale(float scale)
		{
			_decalInfo.MaskUV = UVTools.ScaleUV(_startUV, _uvAxis, scale);
			_decal.ChangeMaskUV(_decalInfo.MaskUV);
		}
	}

	public class ScalePlaneHandle_MaskTiling : IScalePlaneHandle
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector2 _uvScaleMask;
		private Vector4 _startUV;

		public ScalePlaneHandle_MaskTiling(DecalInfo decalInfo, Decal decal, Vector2 uvScaleMask)
		{
			_decalInfo = decalInfo;
			_decal = decal;
			_uvScaleMask = uvScaleMask;
		}

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

    public class MaskTilingHandle : ITransformHandle
    {
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;

		public MaskTilingHandle(DecalInfo decalInfo, Decal decal)
		{
			_decalInfo = decalInfo;
			_decal = decal;
		}

        public void Init(RuntimeTransformHandle transformHandle, Shader handleShader, Transform root)
        {
            var scaleHandleX = new ScaleAxisHandle_MaskTiling(_decalInfo, _decal, Vector2.right);
            var scaleHandleZ = new ScaleAxisHandle_MaskTiling(_decalInfo, _decal, Vector2.up);
			var scaleHandleXZ = new ScalePlaneHandle_MaskTiling(_decalInfo, _decal, Vector2.right + Vector2.up);

            var axisX = new GameObject("MaskTilingAxis.X").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleX, Vector3.right, Color.red, handleShader);
            var axisZ = new GameObject("MaskTilingAxis.Z").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleZ, Vector3.forward, Color.blue, handleShader);
            var planeXZ = new GameObject("MaskTilingPlane.XZ").AddComponent<ScalePlane>().Initialize(transformHandle, root, scaleHandleXZ, axisX, axisZ, Vector3.up, Color.green, handleShader);
        }

        public void Reset(Transform transformHandle)
        {
			var rotationOffset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.MaskAngle);
			transformHandle.position = UVTools.GetHandlePosition(_decal, _decalInfo.MaskUV);
            transformHandle.localRotation = _decal.DecalTransform.localRotation * rotationOffset;
        }
    }
}
