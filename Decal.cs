using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using DeferredDecals;
using EFT;
using EFT.Ballistics;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Systems.Effects;
using UnityEngine;
using UnityEngine.Rendering;

namespace SevenBoldPencil.WeaponCamo
{
	public class Decal : MonoBehaviour
	{
		public static readonly int _UvStartEnd = Shader.PropertyToID("_UvStartEnd");
		public static readonly int _NormalPower = Shader.PropertyToID("_NormalPower");
		public static readonly int _SpecularColor = Shader.PropertyToID("_SpecularColor");
		public static readonly int _Temperature = Shader.PropertyToID("_Temperature");
    	public static readonly int _MaxAngle = Shader.PropertyToID("_MaxAngle");

        public static readonly Vector3 LeftSideDecalRotation = new(0, 0, 90);
        public static readonly Vector3 RightSideDecalRotation = new(0, 0, 270);

		public Material DecalMaterial;
		public Transform DecalTransform;

		public void Init(Shader shader)
		{
			// TODO add ability to change uvStartEnd (tiling)
			var uvStartEnd = new Vector4(0, 0, 1, 1);

			DecalMaterial = new Material(shader);
			DecalMaterial.SetVector(_UvStartEnd, uvStartEnd);
			DecalMaterial.SetFloat(_NormalPower, 3);
			DecalMaterial.SetColor(_SpecularColor, new Color(0, 0, 0, 1));
			DecalMaterial.SetColor(_Temperature, new Color(0.1f, 1, 1, 0));

			DecalTransform = transform;
		}

		public void Set(DecalInfo decalInfo, Transform root, Dictionary<string, Texture2D> loadedDecalTextures)
		{
            DecalTransform.parent = root;
			DecalTransform.localPosition = decalInfo.LocalPosition;
			DecalTransform.localEulerAngles = decalInfo.LocalEulerAngles;
			DecalTransform.localScale = decalInfo.LocalScale;

			if (loadedDecalTextures.TryGetValue(decalInfo.Texture, out var diffuse))
			{
	            DecalMaterial.SetTexture("_MainTex", diffuse);
	            // DecalMaterial.SetTexture("_BumpMap", bump);
			}
            DecalMaterial.color = new Color(1, 1, 1, decalInfo.Opacity);
            DecalMaterial.SetFloat(_MaxAngle, decalInfo.MaxAngle);
		}

        public void ChangeTexture(Texture2D diffuse, Texture2D bump = null)
        {
            DecalMaterial.SetTexture("_MainTex", diffuse);
            DecalMaterial.SetTexture("_BumpMap", bump);
        }

        public void ChangeOpacity(float opacity)
        {
            DecalMaterial.color = new Color(1, 1, 1, opacity);
        }

        public void ChangeMaxAngle(float maxAngle)
        {
            DecalMaterial.SetFloat(_MaxAngle, maxAngle);
        }
	}
}
