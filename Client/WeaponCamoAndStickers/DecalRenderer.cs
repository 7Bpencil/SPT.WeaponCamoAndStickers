// Copyright (c) 2026 7Bpencil
// SPDX-License-Identifier: Apache-2.0

using EFT.CameraControl;
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
        private Dictionary<int, string> InstanceIdToItemId;
		private Dictionary<Camera, HashSet<string>> DecalCameras;
		private Dictionary<Camera, CommandBuffer> CommandBuffers;

		public DecalRenderer(
			Dictionary<string, ItemsWithDecals> itemsWithDecals,
	        Dictionary<int, string> instanceIdToItemId,
			Dictionary<Camera, HashSet<string>> decalCameras)
		{
			Cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
			ItemsWithDecals = itemsWithDecals;
			InstanceIdToItemId = instanceIdToItemId;
			DecalCameras = decalCameras;
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
			if (CameraManager.Instance.Camera && CameraManager.Instance.Camera == currentCamera)
			{
				return true;
			}
			if (currentCamera.CompareTag("OpticCamera"))
			{
				return true;
			}
			if (DecalCameras.ContainsKey(currentCamera))
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
			if (CameraManager.Instance.Camera && CameraManager.Instance.Camera == currentCamera)
			{
				DrawAllDecals(currentCamera, buffer);
				return;
			}
			if (currentCamera.CompareTag("OpticCamera"))
			{
				DrawAllDecals(currentCamera, buffer);
				return;
			}
			if (DecalCameras.TryGetValue(currentCamera, out var previewItems))
			{
				foreach (var previewItemId in previewItems)
				{
					DrawDecalsOnItem(previewItemId, currentCamera, buffer);
				}
				return;
			}
		}

		private void DrawAllDecals(Camera currentCamera, CommandBuffer buffer)
		{
			// ItemsWithDecals is a global database, that can have thousands of items,
			// but often only a dozen are in the world at once.
			// InstanceIdToItemId lists only actually spawned instances,
			// so iterating over it is more optimal

			foreach (var (instanceID, itemId) in InstanceIdToItemId)
			{
				if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals) &&
					itemsWithDecals.Items.TryGetValue(instanceID, out var itemWithDecals))
				{
					// TODO some simple culling
					var decalsInfo = itemsWithDecals.DecalsInfo;
					DrawDecalsOnItem(itemWithDecals, decalsInfo, currentCamera, buffer);
				}
			}
		}

		private void DrawDecalsOnItem(string itemId, Camera currentCamera, CommandBuffer buffer)
		{
			if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
			{
				var decalsInfo = itemsWithDecals.DecalsInfo;
				foreach (var itemWithDecals in itemsWithDecals.Items.Values)
				{
					DrawDecalsOnItem(itemWithDecals, decalsInfo, currentCamera, buffer);
				}
			}
		}

		private void DrawDecalsOnItem(ItemWithDecals itemWithDecals, List<DecalInfo> decalsInfo, Camera currentCamera, CommandBuffer buffer)
		{
			if (itemWithDecals.DecalsHost is WeaponDecalsHost weaponDecalsHost)
			{
				DrawDecalsOnItem_Mesh(weaponDecalsHost, itemWithDecals, decalsInfo, currentCamera, buffer);
			}
			else
			{
				DrawDecalsOnItem_Box(itemWithDecals, decalsInfo, currentCamera, buffer);
			}
		}

		private void DrawDecalsOnItem_Box(ItemWithDecals itemWithDecals, List<DecalInfo> decalsInfo, Camera currentCamera, CommandBuffer buffer)
		{
			var decals = itemWithDecals.Decals;
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
							DrawDecal_Box(decal, buffer);
							break;
						}
						case DecalMirrorMode.Enabled:
						{
							DrawDecal_Box(decal, buffer);
							DrawDecalMirrored_Box(decal, true, buffer);
							break;
						}
						case DecalMirrorMode.EnabledNoFlip:
						{
							DrawDecal_Box(decal, buffer);
							DrawDecalMirrored_Box(decal, false, buffer);
							break;
						}
					}
				}
			}
		}

		private void DrawDecal_Box(Decal decal, CommandBuffer buffer)
		{
			DrawDecal_Box(decal.DecalTransform.localToWorldMatrix, decal.DecalMaterial, buffer);
		}

		private void DrawDecalMirrored_Box(Decal decal, bool flipHorizontally, CommandBuffer buffer)
		{
			var localPosition = decal.DecalTransform.localPosition;
			var localRotation = decal.DecalTransform.localRotation;
			var localScale = decal.DecalTransform.localScale;
			Plugin.MirrorLeftRight(ref localPosition, ref localRotation, ref localScale, flipHorizontally);

			var localMatrix = Matrix4x4.TRS(localPosition, localRotation, localScale);
			var localToWorldMatrix = decal.DecalRoot.localToWorldMatrix * localMatrix;

			DrawDecal_Box(localToWorldMatrix, decal.DecalMaterial, buffer);
		}

		private void DrawDecal_Box(in Matrix4x4 localToWorldMatrix, Material material, CommandBuffer buffer)
		{
			// its easier to accurately place decal when
			// its transform handle is located on the face
			// of projector volume, instead of geometric center.

			var offset = new Vector3(0, -0.5f, 0);
			var resultMatrix = localToWorldMatrix * Matrix4x4.Translate(offset);
			buffer.DrawMesh(Cube, resultMatrix, material);
		}

		private void DrawDecalsOnItem_Mesh(WeaponDecalsHost weaponDecalsHost, ItemWithDecals itemWithDecals, List<DecalInfo> decalsInfo, Camera currentCamera, CommandBuffer buffer)
		{
			var decals = itemWithDecals.Decals;
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
							DrawDecal_Mesh(decal, decalInfo.ExcludedPaths, buffer, weaponDecalsHost);
							break;
						}
						case DecalMirrorMode.Enabled:
						{
							DrawDecal_Mesh(decal, decalInfo.ExcludedPaths, buffer, weaponDecalsHost);
							DrawDecalMirrored_Mesh(decal, true, decalInfo.ExcludedPaths, buffer, weaponDecalsHost);
							break;
						}
						case DecalMirrorMode.EnabledNoFlip:
						{
							DrawDecal_Mesh(decal, decalInfo.ExcludedPaths, buffer, weaponDecalsHost);
							DrawDecalMirrored_Mesh(decal, false, decalInfo.ExcludedPaths, buffer, weaponDecalsHost);
							break;
						}
					}
				}
			}
		}

		private void DrawDecal_Mesh(Decal decal, HashSet<string> excludedPaths, CommandBuffer buffer, WeaponDecalsHost weaponDecalsHost)
		{
			var localPosition = decal.DecalTransform.localPosition;
			var localRotation = decal.DecalTransform.localRotation;
			var localScale = decal.DecalTransform.localScale;
			DrawDecal_Mesh(new(localPosition, localRotation, localScale), decal.DecalMaterial, excludedPaths, buffer, weaponDecalsHost);
		}

		private void DrawDecalMirrored_Mesh(Decal decal, bool flipHorizontally, HashSet<string> excludedPaths, CommandBuffer buffer, WeaponDecalsHost weaponDecalsHost)
		{
			var localPosition = decal.DecalTransform.localPosition;
			var localRotation = decal.DecalTransform.localRotation;
			var localScale = decal.DecalTransform.localScale;
			Plugin.MirrorLeftRight(ref localPosition, ref localRotation, ref localScale, flipHorizontally);
			DrawDecal_Mesh(new(localPosition, localRotation, localScale), decal.DecalMaterial, excludedPaths, buffer, weaponDecalsHost);
		}

		public readonly record struct TransformLocal
		(
			Vector3 Position,
			Quaternion Rotation,
			Vector3 Scale
		);

		private void DrawDecal_Mesh(in TransformLocal decalTransformLocal, Material decalMaterial, HashSet<string> excludedPaths, CommandBuffer buffer, WeaponDecalsHost weaponDecalsHost)
		{
			// its easier to accurately place decal when
			// its transform handle is located on the face
			// of projector volume, instead of geometric center.

			// TODO rootToDecal can be precomputed on closing the editor
			var newLocalPosition = decalTransformLocal.Position + decalTransformLocal.Rotation * Vector3.Scale(decalTransformLocal.Scale, new(0, -0.5f, 0));
			var decalToRoot = Matrix4x4.TRS(newLocalPosition, decalTransformLocal.Rotation, decalTransformLocal.Scale);
			var rootToDecal = decalToRoot.inverse;

			foreach (var (renderer, rendererData) in weaponDecalsHost.Renderers)
			{
				if (renderer.isVisible && !excludedPaths.Contains(rendererData.Path))
				{
					var fromObject = rootToDecal * rendererData.RendererToRoot;
					if (DecalOverlapsMesh(fromObject, rendererData.MeshBounds))
					{
						buffer.SetGlobalMatrix("_DecalFromObject", fromObject);
						buffer.DrawRenderer(renderer, decalMaterial);
					}
				}
			}
		}

		public static bool DecalOverlapsMesh(in Matrix4x4 fromObject, in Bounds meshBounds)
		{
		    var c = fromObject.MultiplyPoint3x4(meshBounds.center);
		    var e = meshBounds.extents;

		    var ex = Mathf.Abs(fromObject.m00) * e.x + Mathf.Abs(fromObject.m01) * e.y + Mathf.Abs(fromObject.m02) * e.z;
		    var ey = Mathf.Abs(fromObject.m10) * e.x + Mathf.Abs(fromObject.m11) * e.y + Mathf.Abs(fromObject.m12) * e.z;
		    var ez = Mathf.Abs(fromObject.m20) * e.x + Mathf.Abs(fromObject.m21) * e.y + Mathf.Abs(fromObject.m22) * e.z;

		    return Mathf.Abs(c.x) <= 0.5f + ex &&
		           Mathf.Abs(c.y) <= 0.5f + ey &&
		           Mathf.Abs(c.z) <= 0.5f + ez;
		}

	}
}
