//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.WeaponCamo;
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
            transform.SetParent(transformHandle.transform, false);

            var axisX = new GameObject("TextureTilingAxis.X").AddComponent<TextureTilingAxis>().Initialize(transformHandle, this, Vector3.right, Color.red, handleShader, new Vector4(0, 0, 1, 0), decalInfo, decal);
            var axisZ = new GameObject("TextureTilingAxis.Z").AddComponent<TextureTilingAxis>().Initialize(transformHandle, this, Vector3.forward, Color.blue, handleShader, new Vector4(0, 0, 0, 1), decalInfo, decal);
            var planeXZ = new GameObject("TextureTilingPlane.XZ").AddComponent<TextureTilingPlane>().Initialize(transformHandle, this, axisX, axisZ, Vector3.up, Color.green, handleShader, new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1), decalInfo, decal);

            return this;
        }

		public static Vector4 CalculateUV(Vector4 start, Vector4 mask, float scale)
		{
			return Vector4.Scale(start, Vector4.one + mask * (1f / scale - 1));
		}
    }
}
