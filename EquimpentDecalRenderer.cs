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
	public class EquipmentDecalRenderer
	{
		public Mesh Cube;
		public Shader DecalShader;
		public bool bool_0; // another IsDirty flag?
		public int int_2 = Shader.PropertyToID("_NormalsCopy");
		public CommandBuffer commandBuffer_1;
		public List<EquipmentDecal> Decals;
		public Dictionary<Camera, CommandBuffer> dictionary_2;

		public EquipmentDecalRenderer(Shader decalShader)
		{
			Cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
			DecalShader = decalShader;
			Decals = new(10);
			dictionary_2 = new();
			Camera.onPreCull += OnPreCullCameraRender;
			Camera.onPreRender += OnPreCameraRender;
		}

		public EquipmentDecal CreateDecal(Vector3 localPosition, Vector3 localRotation, float size, float depth, Transform owner)
		{
            var decalGO = new GameObject("Equipment Decal");
            var decalTransform = decalGO.transform;
            var decal = decalGO.AddComponent<EquipmentDecal>();

            decalTransform.parent = owner;
			decalTransform.localScale = new Vector3(size, depth, size);
			decalTransform.localPosition = localPosition;
			decalTransform.localEulerAngles = localRotation;

			decal.Init(DecalShader);

            Decals.Add(decal);

			return decal;
		}

		public void OnPreCullCameraRender(Camera currentCamera)
		{
			if (method_12(currentCamera))
			{
				// this code feels wrong
				// we have single commandBuffer_1 attached to multiple cameras
				// but then we null it, and create new one to assing it only to new camera? eh

				bool_0 = false;
				commandBuffer_1 = null;
				if (dictionary_2.ContainsKey(currentCamera))
				{
					commandBuffer_1 = dictionary_2[currentCamera];
				}
				else
				{
					commandBuffer_1 = new CommandBuffer();
					commandBuffer_1.name = "Deferred decals Dynamic (Weapon Camo)";
					currentCamera.AddCommandBuffer(CameraEvent.BeforeLighting, commandBuffer_1);
					dictionary_2.Add(currentCamera, commandBuffer_1);
					bool_0 = true;
				}
			}
		}

		public void OnPreCameraRender(Camera currentCamera)
		{
			if (method_12(currentCamera) && dictionary_2.ContainsKey(currentCamera))
			{
				method_9(commandBuffer_1, currentCamera, method_10);
			}
		}

		public bool method_12(Camera currentCamera)
		{
			if (currentCamera && EFTHardSettings.Instance.DEFERRED_DECALS_ENABLED)
			{
				if (!currentCamera.isActiveAndEnabled)
				{
					return false;
				}

				var isOpticCamera = currentCamera.CompareTag("OpticCamera");
				if (currentCamera.renderingPath != RenderingPath.DeferredShading && !isOpticCamera)
				{
					return false;
				}
				if (!((CameraClass.Instance.Camera && CameraClass.Instance.Camera == currentCamera) || isOpticCamera))
				{
					return false;
				}
				if (Decals.Count == 0)
				{
					return false;
				}

				return true;
			}
			return false;
		}

		public void method_9(CommandBuffer buffer, Camera currentCamera, Action<Camera, CommandBuffer> drawFunc)
		{
			buffer.Clear();
			buffer.GetTemporaryRT(int_2, -1, -1);
			buffer.Blit(BuiltinRenderTextureType.GBuffer2, int_2);
			buffer.SetRenderTarget
			(
				new RenderTargetIdentifier[4]
				{
					BuiltinRenderTextureType.GBuffer0,
					BuiltinRenderTextureType.GBuffer1,
					BuiltinRenderTextureType.GBuffer2,
					currentCamera.allowHDR ? BuiltinRenderTextureType.CameraTarget : BuiltinRenderTextureType.GBuffer3
				},
				BuiltinRenderTextureType.CameraTarget
			);
			drawFunc(currentCamera, buffer);
			buffer.ReleaseTemporaryRT(int_2);
		}

		public void method_10(Camera currentCamera, CommandBuffer buffer)
		{
			foreach (var decal in Decals)
			{
				// TODO clear dead decals
				if (decal && decal.enabled)
				{
					buffer.DrawMesh(Cube, decal.DecalTransform.localToWorldMatrix, decal.DecalMaterial);
				}
			}
		}

		public void Clear()
		{
			// This object lives for an entirety of application
			// (decals can be shown in inventory, in battle, in loading screens which show character, etc)
			// So clearing it is not neccessary?

			Camera.onPreCull -= OnPreCullCameraRender;
			Camera.onPreRender -= OnPreCameraRender;
			method_8();
			method_4();
		}

		public void method_8()
		{
			foreach (var item in dictionary_2)
			{
				if (item.Key)
				{
					item.Key.RemoveCommandBuffer(CameraEvent.BeforeLighting, item.Value);
				}
			}
			dictionary_2.Clear();
		}

		public void method_4()
		{
			foreach (var decal in Decals)
			{
				if (decal)
				{
					UnityEngine.Object.DestroyImmediate(decal.gameObject);
				}
			}
			Decals.Clear();
		}
	}
}
