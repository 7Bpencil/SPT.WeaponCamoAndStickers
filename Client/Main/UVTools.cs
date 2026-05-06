//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using System;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	// we pretend that origin is in the center and not in the bottom left,
	// this is less confusing for users, also handle is nice in center and not somewhere offscreen
	public class UVTools
	{
		public static Vector3 GetHandlePosition(Decal decal, Vector4 uv)
		{
			return decal.DecalTransform.TransformPoint(GetHandleLocalPosition(uv));
		}

		public static Quaternion GetHandleLocalRotation(Vector3 localScale, float angle)
		{
			var r = Quaternion.AngleAxis(angle, Vector3.up);
            var (signX, signY) = GetSignsBool(localScale);
            if (signX)
            {
                if (signY)
                {
					return r;
                }
                else
                {
					return new(r.w, r.x, r.y, r.z);
                }
            }
            else
            {
                if (signY)
                {
					return new(r.y, r.x, -r.w, r.z);
                }
                else
                {
					return new(r.x, r.w, r.z, -r.y);
                }
            }
		}

		public static Vector3 GetHandleLocalPosition(Vector4 uv)
		{
			return -1f * GetUVOffset(uv);
		}

		public static Vector3 GetUVOffset(Vector4 uv)
		{
			return new Vector3(uv.x, 0, uv.y);
		}

		public static Vector3 GetUVScale(Vector4 uv)
		{
			return new Vector3(uv.z, 0, uv.w);
		}

		public static Vector4 InverseMask(Vector4 mask)
		{
			return new Vector4(1, 1, 1, 1) - mask;
		}

		public static (bool signX, bool signY) GetSignsBool(Vector3 localScale)
		{
			return (Mathf.Sign(localScale.x) > 0, Mathf.Sign(localScale.z) > 0);
		}

		public static Vector4 ScaleUV(Vector4 start, Vector2 mask, float scale)
		{
			var size = new Vector2(start.z, start.w);
			var newSize = Vector2.Scale(size, Vector2.one + mask * (1f / scale - 1));
			return new Vector4(start.x, start.y, newSize.x, newSize.y);
		}

		public static Vector2 Divide(Vector2 left, Vector2 right)
		{
			return new Vector2(left.x / right.x, left.y / right.y);
		}

        public static Vector2 GetRotationVector(float angleDegrees)
        {
			var angleRadians = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(MathF.Cos(angleRadians), MathF.Sin(angleRadians));
        }
	}
}
