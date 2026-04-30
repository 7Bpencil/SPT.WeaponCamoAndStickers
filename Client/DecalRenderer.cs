//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.Common;
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
				DrawDecalsOnItem(itemId, currentCamera, buffer);
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
				DrawDecalsOnItem(itemsWithDecals, currentCamera, buffer);
			}
		}

		private void DrawDecalsOnItem(string itemId, Camera currentCamera, CommandBuffer buffer)
		{
			if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
			{
				DrawDecalsOnItem(itemsWithDecals, currentCamera, buffer);
			}
		}

		private void DrawDecalsOnItem(ItemsWithDecals itemsWithDecals, Camera currentCamera, CommandBuffer buffer)
		{
			var decalsInfo = itemsWithDecals.DecalsInfo;
			foreach (var itemWithDecals in itemsWithDecals.Items.Values)
			{
				var decals = itemWithDecals.Decals;
				var weaponRoot = Plugin.GetWeaponRoot(itemWithDecals.WeaponPrefab);
				for (var i = 0; i < decals.Count; i++)
				{
					var decalInfo = decalsInfo[i];
					var decal = decals[i];
					if (decalInfo.IsVisible && decal)
					{
						switch (decalInfo.MirrorMode)
						{
							case DecalMirrorMode.Disabled:
							{
								DrawDecal(decal, buffer);
								break;
							}
							case DecalMirrorMode.Enabled:
							{
								DrawDecal(decal, buffer);
								DrawDecalMirrored(decal, decalInfo, true, weaponRoot, buffer);
								break;
							}
							case DecalMirrorMode.EnabledNoFlip:
							{
								DrawDecal(decal, buffer);
								DrawDecalMirrored(decal, decalInfo, false, weaponRoot, buffer);
								break;
							}
						}
					}
				}
			}
		}

		private void DrawDecal(Decal decal, CommandBuffer buffer)
		{
			DrawDecal(decal.DecalTransform.localToWorldMatrix, decal.DecalMaterial, buffer);
		}

		private void DrawDecalMirrored(Decal decal, DecalInfo decalInfo, bool flipHorizontally, Transform weaponRoot, CommandBuffer buffer)
		{
			var localPosition = decalInfo.LocalPosition;
			var localEulerAngles = decalInfo.LocalEulerAngles;
			var localScale = decalInfo.LocalScale;
			Plugin.MirrorLeftRight(ref localPosition, ref localEulerAngles, ref localScale, flipHorizontally);

			var localMatrix = Matrix4x4.TRS(localPosition, localEulerAngles.ToQuaternion(), localScale);
			var localToWorldMatrix = weaponRoot.localToWorldMatrix * localMatrix;

			DrawDecal(localToWorldMatrix, decal.DecalMaterial, buffer);
		}

		private void DrawDecal(in Matrix4x4 localToWorldMatrix, Material material, CommandBuffer buffer)
		{
			// its easier to accurately place decal when
			// its transform handle is located on the face
			// of projector volume, instead of geometric center.

			var offset = new Vector3(0, -0.5f, 0);
			var resultMatrix = localToWorldMatrix * Matrix4x4.Translate(offset);
			buffer.DrawMesh(Cube, resultMatrix, material);
		}
	}
}
