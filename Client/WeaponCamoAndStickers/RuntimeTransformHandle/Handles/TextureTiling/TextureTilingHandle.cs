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
    public class ScaleAxisHandler_TextureTiling : IScaleAxisHandler
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector2 _uvAxis;
		private Vector4 _startUV;

        public ScaleAxisHandler_TextureTiling(DecalInfo decalInfo, Decal decal, Vector2 uvAxis)
        {
    		_decalInfo = decalInfo;
    		_decal = decal;
    		_uvAxis = uvAxis;
        }

		public void OnStartInteraction()
		{
			_startUV = _decalInfo.TextureUV;
		}

		public void SetScale(float scale)
		{
			var uv = UVTools.ScaleUV(_startUV, _uvAxis, scale);
			_decalInfo.TextureUV = uv;
			_decal.ChangeTextureUV(_decalInfo.TextureUV);
		}
	}

	public class ScalePlaneHandler_TextureTiling : IScalePlaneHandler
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector2 _uvScaleMask;
		private Vector4 _startUV;

		public ScalePlaneHandler_TextureTiling(DecalInfo decalInfo, Decal decal, Vector2 uvScaleMask)
		{
			_decalInfo = decalInfo;
			_decal = decal;
			_uvScaleMask = uvScaleMask;
		}

		public void OnStartInteraction()
		{
			_startUV = _decalInfo.TextureUV;
		}

		public void SetScale(float scale)
		{
			var uv = UVTools.ScaleUV(_startUV, _uvScaleMask, scale);
			_decalInfo.TextureUV = uv;
			_decal.ChangeTextureUV(_decalInfo.TextureUV);
		}
	}

    public class TextureTilingHandle : MonoBehaviour
    {
        public TextureTilingHandle Initialize(
            RuntimeTransformHandle transformHandle,
            Shader handleShader,
            DecalInfo decalInfo,
            Decal decal)
        {
			var scaleHandleTransform = transform;
            scaleHandleTransform.SetParent(transformHandle.handleTransform, false);

            var scaleHandlerX = new ScaleAxisHandler_TextureTiling(decalInfo, decal, Vector2.right);
            var scaleHandlerZ = new ScaleAxisHandler_TextureTiling(decalInfo, decal, Vector2.up);
			var scaleHandlerXZ = new ScalePlaneHandler_TextureTiling(decalInfo, decal, Vector2.right + Vector2.up);

            var axisX = new GameObject("TextureTilingAxis.X").AddComponent<ScaleAxis>().Initialize(transformHandle, scaleHandleTransform, scaleHandlerX, Vector3.right, Color.red, handleShader);
            var axisZ = new GameObject("TextureTilingAxis.Z").AddComponent<ScaleAxis>().Initialize(transformHandle, scaleHandleTransform, scaleHandlerZ, Vector3.forward, Color.blue, handleShader);
            var planeXZ = new GameObject("TextureTilingPlane.XZ").AddComponent<ScalePlane>().Initialize(transformHandle, scaleHandleTransform, scaleHandlerXZ, axisX, axisZ, Vector3.up, Color.green, handleShader);

            return this;
        }

        public void ResetHandleTransform(Transform transformHandle, DecalInfo decalInfo, Decal decal)
        {
			transformHandle.position = UVTools.GetHandlePosition(decal, decalInfo.TextureUV);
            transformHandle.localRotation *= UVTools.GetHandleLocalRotation(decalInfo.LocalScale, decalInfo.TextureAngle);
        }
    }
}
