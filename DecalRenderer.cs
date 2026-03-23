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
		public Mesh Cube;
		public int int_2 = Shader.PropertyToID("_NormalsCopy");
		public CommandBuffer commandBuffer_1;
		public Dictionary<string, ItemsWithDecals> ItemsWithDecals;
		public Dictionary<Camera, CommandBuffer> dictionary_2;

		public DecalRenderer(Dictionary<string, ItemsWithDecals> itemsWithDecals)
		{
			Cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
			ItemsWithDecals = itemsWithDecals;
			dictionary_2 = new();
			Camera.onPreCull += OnPreCullCameraRender;
			Camera.onPreRender += OnPreCameraRender;
		}

		public void OnPreCullCameraRender(Camera currentCamera)
		{
			if (method_12(currentCamera))
			{
				// this code feels wrong
				// we have single commandBuffer_1 attached to multiple cameras
				// but then we null it, and create new one to assing it only to new camera? eh

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
			// TODO limit to cameras that can actually see decals
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

			return true;
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
			// thats the easiest way to deal with decal order
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

	}
}
