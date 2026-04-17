//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.Common;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	public class Decal : MonoBehaviour
	{
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
		public Transform DecalTransform;

		public void Init(Shader shader)
		{
			DecalMaterial = new Material(shader);
			DecalMaterial.SetColor(_Temperature, new Color(0.1f, 1, 1, 0));

			DecalTransform = transform;
		}

		public void Set(DecalInfo decalInfo, Transform root, Texture diffuse, Texture mask)
		{
            DecalTransform.parent = root;
			DecalTransform.localPosition = decalInfo.LocalPosition;
			DecalTransform.localEulerAngles = decalInfo.LocalEulerAngles;
			ChangeLocalScale(decalInfo.LocalScale);

			ChangeTexture(diffuse);
			ChangeTextureUV(decalInfo.TextureUV);
			ChangeTextureAngle(decalInfo.TextureAngle);
			ChangeMask(mask);
			ChangeMaskUV(decalInfo.MaskUV);
			ChangeMaskAngle(decalInfo.MaskAngle);
			ChangeColor(decalInfo.ColorHSVA);
			ChangeMaxAngle(decalInfo.MaxAngle);
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

		public void OnDestroy()
		{
			if (DecalMaterial)
			{
				Destroy(DecalMaterial);
			}
		}
	}
}
