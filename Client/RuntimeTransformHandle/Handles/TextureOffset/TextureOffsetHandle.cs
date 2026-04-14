//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.Common;
using SevenBoldPencil.WeaponCamoAndStickers;
using UnityEngine;

namespace RuntimeHandle
{
    public class TextureOffsetHandle : MonoBehaviour
    {
        public TextureOffsetHandle Initialize(
            RuntimeTransformHandle transformHandle,
            Shader handleShader,
            DecalInfo decalInfo,
            Decal decal)
        {
            transform.SetParent(transformHandle.handleTransform, false);

            var axisX = new GameObject("TextureOffsetAxis.X").AddComponent<TextureOffsetAxis>().Initialize(transformHandle, this, Vector3.right, Vector3.forward, Color.red, handleShader, new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), decalInfo, decal);
            var axisZ = new GameObject("TextureOffsetAxis.Z").AddComponent<TextureOffsetAxis>().Initialize(transformHandle, this, Vector3.forward, Vector3.right, Color.blue, handleShader, new Vector4(0, 1, 0, 0), new Vector4(1, 0, 0, 0), decalInfo, decal);
            var planeXZ = new GameObject("TextureOffsetPlane.XZ").AddComponent<TextureOffsetPlane>().Initialize(transformHandle, this, Vector3.right, Vector3.forward, Vector3.up, Color.green, handleShader, new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), decalInfo, decal);

            return this;
        }

        public void ResetHandleTransform(Transform transformHandle, DecalInfo decalInfo, Decal decal)
        {
			transformHandle.position = UVTools.GetHandlePosition(decal, decalInfo.TextureUV);
            transformHandle.localRotation *= UVTools.GetHandleLocalRotation(decalInfo.TextureAngle);
        }
    }
}
