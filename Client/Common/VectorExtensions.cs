//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using UnityEngine;

namespace SevenBoldPencil.Common
{
    public static class VectorExtensions
    {
		public static Vector3 WithScaledX(this Vector3 v, float scale)
		{
			return new(v.x * scale, v.y, v.z);
		}

		public static Vector3 WithScaledY(this Vector3 v, float scale)
		{
			return new(v.x, v.y * scale, v.z);
		}

		public static Vector3 WithScaledZ(this Vector3 v, float scale)
		{
			return new(v.x, v.y, v.z * scale);
		}

		public static float Sum(this Vector3 v)
		{
			return v.x + v.y + v.z;
		}

		public static float Sum(this Vector3 v, Vector3 mask)
		{
			return Vector3.Scale(v, mask).Sum();
		}

		public static float Sum(this Vector4 v)
		{
			return v.x + v.y + v.z + v.w;
		}

		public static float Sum(this Vector4 v, Vector4 mask)
		{
			return Vector4.Scale(v, mask).Sum();
		}
    }
}
