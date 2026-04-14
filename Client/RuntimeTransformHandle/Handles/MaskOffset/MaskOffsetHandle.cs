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
    public class MaskOffsetHandle : MonoBehaviour
    {
        public MaskOffsetHandle Initialize(
            RuntimeTransformHandle transformHandle,
            Shader handleShader,
            DecalInfo decalInfo,
            Decal decal)
        {
            transform.SetParent(transformHandle.handleTransform, false);

            var axisX = new GameObject("MaskOffsetAxis.X").AddComponent<MaskOffsetAxis>().Initialize(transformHandle, this, Vector3.right, Vector3.forward, Color.red, handleShader, new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), decalInfo, decal);
            var axisZ = new GameObject("MaskOffsetAxis.Z").AddComponent<MaskOffsetAxis>().Initialize(transformHandle, this, Vector3.forward, Vector3.right, Color.blue, handleShader, new Vector4(0, 1, 0, 0), new Vector4(1, 0, 0, 0), decalInfo, decal);
            var planeXZ = new GameObject("MaskOffsetPlane.XZ").AddComponent<MaskOffsetPlane>().Initialize(transformHandle, this, Vector3.right, Vector3.forward, Vector3.up, Color.green, handleShader, new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), decalInfo, decal);

            return this;
        }

        public void ResetHandleTransform(Transform transformHandle, DecalInfo decalInfo, Decal decal)
        {
			transformHandle.position = UVTools.GetHandlePosition(decal, decalInfo.MaskUV);
            transformHandle.localRotation *= UVTools.GetHandleLocalRotation(decalInfo.MaskAngle);
        }
    }
}
