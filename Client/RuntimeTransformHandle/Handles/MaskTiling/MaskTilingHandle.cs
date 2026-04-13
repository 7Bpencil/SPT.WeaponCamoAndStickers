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
    public class MaskTilingHandle : MonoBehaviour
    {
        public MaskTilingHandle Initialize(
            RuntimeTransformHandle transformHandle,
            Shader handleShader,
            DecalInfo decalInfo,
            Decal decal)
        {
            transform.SetParent(transformHandle.transform, false);

            var axisX = new GameObject("MaskTilingAxis.X").AddComponent<MaskTilingAxis>().Initialize(transformHandle, this, Vector3.right, Color.red, handleShader, new(1, 0), decalInfo, decal);
            var axisZ = new GameObject("MaskTilingAxis.Z").AddComponent<MaskTilingAxis>().Initialize(transformHandle, this, Vector3.forward, Color.blue, handleShader, new(0, 1), decalInfo, decal);
            var planeXZ = new GameObject("MaskTilingPlane.XZ").AddComponent<MaskTilingPlane>().Initialize(transformHandle, this, axisX, axisZ, Vector3.up, Color.green, handleShader, new(1, 0), new(0, 1), decalInfo, decal);

            return this;
        }
    }
}
