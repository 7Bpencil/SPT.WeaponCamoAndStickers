//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	public class DecalRenderer
	{
		public static readonly int int_2 = Shader.PropertyToID("_NormalsCopy");

		private Mesh Cube;
		private Dictionary<string, ItemsWithDecals> ItemsWithDecals;
        private Dictionary<Camera, string> WeaponPreviewCameras;
        private HashSet<Camera> PlayerModelViewCameras;
		private Dictionary<Camera, CommandBuffer> CommandBuffers;

		public DecalRenderer(
			Dictionary<string, ItemsWithDecals> itemsWithDecals,
			Dictionary<Camera, string> weaponPreviewCameras,
			HashSet<Camera> playerModelViewCameras)
		{
			Cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
			ItemsWithDecals = itemsWithDecals;
			WeaponPreviewCameras = weaponPreviewCameras;
			PlayerModelViewCameras = playerModelViewCameras;
			CommandBuffers = new();
			Camera.onPreCull += OnPreCullCameraRender;
			Camera.onPreRender += OnPreCameraRender;
		}

		public void OnPreCullCameraRender(Camera currentCamera)
		{
			if (CanCameraSeeDecals(currentCamera) && !CommandBuffers.ContainsKey(currentCamera))
			{
				var commandBuffer = new CommandBuffer();
				commandBuffer.name = "[WeaponCamoAndStickers] Deferred Decals";
				currentCamera.AddCommandBuffer(CameraEvent.BeforeLighting, commandBuffer);
				CommandBuffers.Add(currentCamera, commandBuffer);
			}
		}

		public void OnPreCameraRender(Camera currentCamera)
		{
			if (CanCameraSeeDecals(currentCamera) && CommandBuffers.TryGetValue(currentCamera, out var commandBuffer))
			{
				SetupBufferAndDrawDecals(currentCamera, commandBuffer);
			}
		}

		public bool CanCameraSeeDecals(Camera currentCamera)
		{
			if (!currentCamera)
			{
				return false;
			}
			if (!currentCamera.isActiveAndEnabled)
			{
				return false;
			}
			if (currentCamera.actualRenderingPath != RenderingPath.DeferredShading)
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
			if (PlayerModelViewCameras.Contains(currentCamera))
			{
				return true;
			}

			return false;
		}

		public void SetupBufferAndDrawDecals(Camera currentCamera, CommandBuffer buffer)
		{
			buffer.Clear();
			buffer.GetTemporaryRT(int_2, -1, -1);
			buffer.Blit(BuiltinRenderTextureType.GBuffer2, int_2);
			buffer.SetRenderTarget
			(
				BuiltinRenderTextureType.GBuffer0,
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
				return;
			}
			if (currentCamera.CompareTag("OpticCamera"))
			{
				DrawAllDecals(currentCamera, buffer);
				return;
			}
			if (WeaponPreviewCameras.TryGetValue(currentCamera, out var itemId))
			{
				DrawDecalsOnItem(currentCamera, buffer, itemId);
				return;
			}
			if (PlayerModelViewCameras.Contains(currentCamera))
			{
				DrawAllDecals(currentCamera, buffer);
				return;
			}
		}

		private void DrawAllDecals(Camera currentCamera, CommandBuffer buffer)
		{
			// TODO some simple culling
			foreach (var itemsWithDecals in ItemsWithDecals.Values)
			{
				foreach (var itemWithDecals in itemsWithDecals.Items.Values)
				{
					foreach (var decal in itemWithDecals.Decals)
					{
						DrawDecal(decal, buffer);
					}
				}
			}
		}

		private void DrawDecalsOnItem(Camera currentCamera, CommandBuffer buffer, string itemId)
		{
			if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
			{
				foreach (var itemWithDecals in itemsWithDecals.Items.Values)
				{
					foreach (var decal in itemWithDecals.Decals)
					{
						DrawDecal(decal, buffer);
					}
				}
			}
		}

		private void DrawDecal(Decal decal, CommandBuffer buffer)
		{
			if (decal)
			{
				// its easier to accurately place decal when
				// its transform handle is located on the face
				// of projector volume, instead of geometric center.

				var offset = new Vector3(0, -0.5f, 0);
				var resultMatrix = decal.DecalTransform.localToWorldMatrix * Matrix4x4.Translate(offset);
				buffer.DrawMesh(Cube, resultMatrix, decal.DecalMaterial);
			}
		}
	}
}
