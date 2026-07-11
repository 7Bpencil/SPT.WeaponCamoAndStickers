//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.Common;
using UnityEngine;
using UnityEngine.Rendering;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	public class Decal : MonoBehaviour
	{
		public static readonly int _StencilType = Shader.PropertyToID("_StencilType");
		public static readonly int _StencilPassOperation = Shader.PropertyToID("_StencilPassOperation");
		public static readonly int _MainTex = Shader.PropertyToID("_MainTex");
		public static readonly int _MainTexUV = Shader.PropertyToID("_MainTexUV");
		public static readonly int _MaskTex = Shader.PropertyToID("_MaskTex");
		public static readonly int _MaskTexUV = Shader.PropertyToID("_MaskTexUV");
		public static readonly int _Temperature = Shader.PropertyToID("_Temperature");
    	public static readonly int _MaxAngle = Shader.PropertyToID("_MaxAngle");
    	public static readonly int _AspectRatio = Shader.PropertyToID("_AspectRatio");
    	public static readonly int _MainTexRotation = Shader.PropertyToID("_MainTexRotation");
    	public static readonly int _MaskTexRotation = Shader.PropertyToID("_MaskTexRotation");

		public Material DecalMaterial;
		public LocalKeyword DecalMaterialKeywordErase;
		public Transform DecalTransform;
		public Transform DecalRoot;

		public void Init(DecalInfo info, Transform root, Shader shader)
		{
			DecalMaterial = new Material(shader);
			DecalMaterialKeywordErase = new LocalKeyword(shader, "ERASE");
			DecalTransform = transform;
			DecalRoot = root;

            DecalTransform.parent = DecalRoot;
			DecalTransform.localPosition = info.LocalPosition;
			DecalTransform.localEulerAngles = info.LocalEulerAngles;
			ChangeLocalScale(info.LocalScale);

			ChangeTextureUV(info.TextureUV);
			ChangeTextureAngle(info.TextureAngle);
			ChangeMaskUV(info.MaskUV);
			ChangeMaskAngle(info.MaskAngle);
			ChangeColor(info.ColorHSVA);
			ChangeMaxAngle(info.MaxAngle);
			ChangePaintMode(info.PaintMode);
			ChangeStencilType(info.StencilType);

			DecalMaterial.SetColor(_Temperature, new Color(0.1f, 1, 1, 0));
		}

		public void ChangeRoot(Transform root)
		{
			DecalRoot = root;
            DecalTransform.parent = DecalRoot;
		}

		public void ChangeLocalScale(Vector3 localScale)
		{
			var aspectRatio = Mathf.Abs(localScale.x / localScale.z);
			DecalTransform.localScale = localScale;
            DecalMaterial.SetFloat(_AspectRatio, aspectRatio);
		}

        public void ChangeTexture(Texture diffuse)
        {
            DecalMaterial.SetTexture(_MainTex, diffuse);
        }

		public void ChangeTextureUV(Vector4 uv)
		{
			DecalMaterial.SetVector(_MainTexUV, uv);
		}

		public void ChangeTextureAngle(float angle)
		{
			var rotationVector = UVTools.GetRotationVector(angle);
            DecalMaterial.SetVector(_MainTexRotation, rotationVector);
		}

        public void ChangeMask(Texture mask)
        {
            DecalMaterial.SetTexture(_MaskTex, mask);
        }

		public void ChangeMaskAngle(float angle)
		{
			var rotationVector = UVTools.GetRotationVector(angle);
            DecalMaterial.SetVector(_MaskTexRotation, rotationVector);
		}

		public void ChangeMaskUV(Vector4 uv)
		{
			DecalMaterial.SetVector(_MaskTexUV, uv);
		}

        public void ChangeColor(Vector4 colorHSVA)
        {
            DecalMaterial.color = colorHSVA.HSVAtoRGBA();
        }

        public void ChangeMaxAngle(float maxAngle)
        {
            DecalMaterial.SetFloat(_MaxAngle, maxAngle);
        }

		public void ChangePaintMode(DecalPaintMode paintMode)
		{
			if (paintMode == DecalPaintMode.Paint)
			{
	            DecalMaterial.SetFloat(_StencilPassOperation, (int)StencilOp.Keep);
				DecalMaterial.DisableKeyword(DecalMaterialKeywordErase);
			}
			if (paintMode == DecalPaintMode.Erase)
			{
				// decals are rendered only on fragments with stencil == 2,
				// erase downs that number to 1 (same as hands),
				// preventing other decals from rendering

	            DecalMaterial.SetFloat(_StencilPassOperation, (int)StencilOp.DecrementWrap);
				DecalMaterial.EnableKeyword(DecalMaterialKeywordErase);
			}
		}

		public void ChangeStencilType(byte stencilType)
		{
            DecalMaterial.SetFloat(_StencilType, stencilType);
		}

		public void OnDestroy()
		{
			if (DecalMaterial)
			{
				Destroy(DecalMaterial);
			}
		}
	}
}
