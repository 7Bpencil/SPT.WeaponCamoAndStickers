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
				DrawDecalsOnItem(currentCamera, buffer, itemsWithDecals);
			}
		}

		private void DrawDecalsOnItem(Camera currentCamera, CommandBuffer buffer, string itemId)
		{
			if (ItemsWithDecals.TryGetValue(itemId, out var itemsWithDecals))
			{
				DrawDecalsOnItem(currentCamera, buffer, itemsWithDecals);
			}
		}

		private void DrawDecalsOnItem(Camera currentCamera, CommandBuffer buffer, ItemsWithDecals itemsWithDecals)
		{
			// TODO
			// 1) simple OOB culling to not render decals on parts that are not covered by it
			// 2) render only on root and moving slots (if they are not excluded)? (interesting idea but wont work?)
			// 3) instancing? yes, we cannot change refs per instance, so batch per ref?

			var decalsInfo = itemsWithDecals.DecalsInfo;
			foreach (var itemWithDecals in itemsWithDecals.Items.Values)
			{
				var weaponSlots = itemWithDecals.WeaponSlots;
				var decals = itemWithDecals.Decals;
				for (var i = 0; i < decals.Count; i++)
				{
					var decalInfo = decalsInfo[i];
					var decal = decals[i];
					foreach (var slot in weaponSlots)
					{
						if (!decalInfo.ExcludedSlots.Contains(slot.ID))
						{
							DrawDecal(decal, slot, buffer);
						}
					}
				}
			}
		}

		private void DrawDecal(Decal decal, SlotInfo slot, CommandBuffer buffer)
		{
			if (decal)
			{
				var localToWorldMatrix = GetLocalToWorldMatrix(decal, slot);

				// its easier to accurately place decal when
				// its transform handle is located on the face
				// of projector volume, instead of geometric center.
				var offset = new Vector3(0, -0.5f, 0);
				var resultMatrix = localToWorldMatrix * Matrix4x4.Translate(offset);

				// TODO remove copy
				// we set stencil ref value which is part of render state that
				// cannot be changed by MaterialPropertyBlock, so there's no
				// option other than CopyPropertiesFromMaterial
				var newMaterial = new Material(Plugin.Instance.DecalShader);
	            newMaterial.CopyPropertiesFromMaterial(decal.DecalMaterial);
	            newMaterial.SetFloat(Decal._StencilRef, slot.Stencil);
				buffer.DrawMesh(Cube, resultMatrix, newMaterial);
			}
		}

		public Matrix4x4 GetLocalToWorldMatrix(Decal decal, SlotInfo slot)
		{
			if (slot.ID == ItemWithDecals.RootSlotID)
			{
				return decal.DecalTransform.localToWorldMatrix;
			}
			else
			{
				// calculate decal transform as if it was attached to slot and not weapon root

				var positionSlotSpace = InverseTransformPoint(decal.DecalTransform.localPosition, slot.OriginalLocalPosition, slot.OriginalLocalRotation);
				var rotationSlotSpace = InverseTransformRotation(decal.DecalTransform.localRotation, slot.OriginalLocalRotation);
				var position = slot.Transform.TransformPoint(positionSlotSpace);
				var rotation = TransformRotation(rotationSlotSpace, slot.Transform.rotation);
				var localToWorldMatrix = Matrix4x4.TRS(position, rotation, decal.DecalTransform.lossyScale);
				return localToWorldMatrix;
			}
		}

		public static Vector3 InverseTransformPoint(Vector3 worldPoint, Vector3 position, Quaternion rotation)
		{
		    return Quaternion.Inverse(rotation) * (worldPoint - position);
		}

		public static Quaternion InverseTransformRotation(Quaternion worldRotation, Quaternion rotation)
		{
		    return Quaternion.Inverse(rotation) * worldRotation;
		}

		public static Quaternion TransformRotation(Quaternion localRotation, Quaternion rotation)
		{
		    return rotation * localRotation;
		}

	}
}
