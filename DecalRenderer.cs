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
	public class DecalRenderer
	{
		public static readonly int int_2 = Shader.PropertyToID("_NormalsCopy");

		public Mesh Cube;
		public Dictionary<string, ItemsWithDecals> ItemsWithDecals;
        public Dictionary<Camera, string> WeaponPreviewCameras;
		public Dictionary<Camera, CommandBuffer> CommandBuffers;

		public DecalRenderer(Dictionary<string, ItemsWithDecals> itemsWithDecals, Dictionary<Camera, string> weaponPreviewCameras)
		{
			Cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
			ItemsWithDecals = itemsWithDecals;
			WeaponPreviewCameras = weaponPreviewCameras;
			CommandBuffers = new();
			Camera.onPreCull += OnPreCullCameraRender;
			Camera.onPreRender += OnPreCameraRender;
		}

		public void OnPreCullCameraRender(Camera currentCamera)
		{
			if (method_12(currentCamera) && !CommandBuffers.ContainsKey(currentCamera))
			{
				var commandBuffer = new CommandBuffer();
				commandBuffer.name = "Deferred decals Dynamic (Weapon Camo)";
				currentCamera.AddCommandBuffer(CameraEvent.BeforeLighting, commandBuffer);
				CommandBuffers.Add(currentCamera, commandBuffer);
			}
		}

		public void OnPreCameraRender(Camera currentCamera)
		{
			if (method_12(currentCamera) && CommandBuffers.TryGetValue(currentCamera, out var commandBuffer))
			{
				method_9(currentCamera, commandBuffer);
			}
		}

		public bool method_12(Camera currentCamera)
		{
			if (!currentCamera)
			{
				return false;
			}
			if (!currentCamera.isActiveAndEnabled)
			{
				return false;
			}
			if (currentCamera.renderingPath != RenderingPath.DeferredShading)
			{
				return false;
			}
			if (ItemsWithDecals.Count == 0)
			{
				return false;
			}

			if (CameraClass.Instance.Camera && CameraClass.Instance.Camera == currentCamera)
			{
				return true;
			}
			if (currentCamera.CompareTag("OpticCamera"))
			{
				return true;
			}
			if (WeaponPreviewCameras.ContainsKey(currentCamera))
			{
				return true;
			}

			return false;
		}

		public void method_9(Camera currentCamera, CommandBuffer buffer)
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
			DrawDecals(currentCamera, buffer);
			buffer.ReleaseTemporaryRT(int_2);
		}

		public void DrawDecals(Camera currentCamera, CommandBuffer buffer)
		{
			if (CameraClass.Instance.Camera && CameraClass.Instance.Camera == currentCamera)
			{
				DrawAllDecals(currentCamera, buffer);
			}
			if (currentCamera.CompareTag("OpticCamera"))
			{
				DrawAllDecals(currentCamera, buffer);
			}
			if (WeaponPreviewCameras.TryGetValue(currentCamera, out var itemId))
			{
				DrawDecalsOnItem(currentCamera, buffer, itemId);
			}
		}

		private void DrawAllDecals(Camera currentCamera, CommandBuffer buffer)
		{
			// TODO some simple culling
			foreach (var (_, itemsWithDecals) in ItemsWithDecals)
			{
				foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
				{
					foreach (var decal in itemWithDecals.Decals)
					{
						if (decal)
						{
							buffer.DrawMesh(Cube, decal.DecalTransform.localToWorldMatrix, decal.DecalMaterial);
						}
					}
				}
			}
		}

		private void DrawDecalsOnItem(Camera currentCamera, CommandBuffer buffer, string itemId)
		{
			if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
			{
				foreach (var (_, itemWithDecals) in itemsWithDecals.Items)
				{
					foreach (var decal in itemWithDecals.Decals)
					{
						if (decal)
						{
							buffer.DrawMesh(Cube, decal.DecalTransform.localToWorldMatrix, decal.DecalMaterial);
						}
					}
				}
			}
		}

	}
}
