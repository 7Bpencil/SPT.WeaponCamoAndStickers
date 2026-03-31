//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

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
			// set as repeating, so it stays the same on vertical,
			// but gets cropped/multiplied on horizontal

			DecalMaterial = new Material(shader);
			DecalMaterial.SetFloat(_NormalPower, 3);
			DecalMaterial.SetColor(_SpecularColor, new Color(0, 0, 0, 1));
			DecalMaterial.SetColor(_Temperature, new Color(0.1f, 1, 1, 0));

			DecalTransform = transform;
		}

		public void Set(DecalInfo decalInfo, Transform root, Texture2D diffuse)
		{
            DecalTransform.parent = root;
			DecalTransform.localPosition = decalInfo.LocalPosition;
			DecalTransform.localEulerAngles = decalInfo.LocalEulerAngles;
			DecalTransform.localScale = decalInfo.LocalScale;

			ChangeTexture(diffuse);
            DecalMaterial.SetTexture("_BumpMap", Texture2D.normalTexture); // TODO add support for bump textures

			ChangeColor(decalInfo.ColorHSVA);
			ChangeUV(decalInfo.UV);
			ChangeMaxAngle(decalInfo.MaxAngle);
		}

        public void ChangeTexture(Texture2D diffuse)
        {
            DecalMaterial.SetTexture("_MainTex", diffuse);
        }

        public void ChangeColor(Vector4 colorHSVA)
        {
            DecalMaterial.color = colorHSVA.HSVAtoRGBA();
        }

		public void ChangeUV(Vector4 uv)
		{
			DecalMaterial.SetVector(_UvStartEnd, uv);
		}

        public void ChangeMaxAngle(float maxAngle)
        {
            DecalMaterial.SetFloat(_MaxAngle, maxAngle);
        }
	}
}
