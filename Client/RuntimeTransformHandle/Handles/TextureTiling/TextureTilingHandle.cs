//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.WeaponCamoAndStickers;
using UnityEngine;

namespace RuntimeHandle
{
    public class TextureTilingHandle : MonoBehaviour
    {
        public TextureTilingHandle Initialize(
            RuntimeTransformHandle transformHandle,
            Shader handleShader,
            DecalInfo decalInfo,
            Decal decal)
        {
            transform.SetParent(transformHandle.handleTransform, false);

            var axisX = new GameObject("TextureTilingAxis.X").AddComponent<TextureTilingAxis>().Initialize(transformHandle, this, Vector3.right, Color.red, handleShader, new(1, 0), decalInfo, decal);
            var axisZ = new GameObject("TextureTilingAxis.Z").AddComponent<TextureTilingAxis>().Initialize(transformHandle, this, Vector3.forward, Color.blue, handleShader, new(0, 1), decalInfo, decal);
            var planeXZ = new GameObject("TextureTilingPlane.XZ").AddComponent<TextureTilingPlane>().Initialize(transformHandle, this, axisX, axisZ, Vector3.up, Color.green, handleShader, new(1, 0), new(0, 1), decalInfo, decal);

            return this;
        }

        public void ResetHandleTransform(Transform transformHandle, DecalInfo decalInfo, Decal decal)
        {
			transformHandle.position = UVTools.GetHandlePosition(decal, decalInfo.TextureUV);
        }
    }
}
