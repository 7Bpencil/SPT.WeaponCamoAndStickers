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
	public class ScaleAxisHandler_MaskTiling : IScaleAxisHandler
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector2 _uvAxis;
		private Vector4 _startUV;

        public ScaleAxisHandler_MaskTiling(DecalInfo decalInfo, Decal decal, Vector2 uvAxis)
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
			var uv = UVTools.ScaleUV(_startUV, _uvAxis, scale);
			_decalInfo.MaskUV = uv;
			_decal.ChangeMaskUV(_decalInfo.MaskUV);
		}
	}

	public class ScalePlaneHandler_MaskTiling : IScalePlaneHandler
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector2 _uvScaleMask;
		private Vector4 _startUV;

		public ScalePlaneHandler_MaskTiling(DecalInfo decalInfo, Decal decal, Vector2 uvScaleMask)
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
			var uv = UVTools.ScaleUV(_startUV, _uvScaleMask, scale);
			_decalInfo.MaskUV = uv;
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
            var scaleHandlerX = new ScaleAxisHandler_MaskTiling(_decalInfo, _decal, Vector2.right);
            var scaleHandlerZ = new ScaleAxisHandler_MaskTiling(_decalInfo, _decal, Vector2.up);
			var scaleHandlerXZ = new ScalePlaneHandler_MaskTiling(_decalInfo, _decal, Vector2.right + Vector2.up);

            var axisX = new GameObject("MaskTilingAxis.X").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandlerX, Vector3.right, Color.red, handleShader);
            var axisZ = new GameObject("MaskTilingAxis.Z").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandlerZ, Vector3.forward, Color.blue, handleShader);
            var planeXZ = new GameObject("MaskTilingPlane.XZ").AddComponent<ScalePlane>().Initialize(transformHandle, root, scaleHandlerXZ, axisX, axisZ, Vector3.up, Color.green, handleShader);
        }

        public void Reset(Transform transformHandle)
        {
			var rotationOffset = UVTools.GetHandleLocalRotation(_decalInfo.LocalScale, _decalInfo.MaskAngle);
			transformHandle.position = UVTools.GetHandlePosition(_decal, _decalInfo.MaskUV);
            transformHandle.localRotation = _decal.DecalTransform.localRotation * rotationOffset;
        }
    }
}
