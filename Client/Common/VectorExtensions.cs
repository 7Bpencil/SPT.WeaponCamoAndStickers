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
    }
}
