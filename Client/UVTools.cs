//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Newtonsoft.Json;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using RuntimeHandle;
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

		public static Vector3 GetHandleLocalPosition(Vector4 uv)
		{
			var (nonScaleOffset, scaleOffset, size) = UVTools.DeconstructUV(uv);
			var offset = GetUVOffset(uv);
			return -1f * new Vector3(nonScaleOffset.x, 0, nonScaleOffset.y);
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

		public static Vector3 InverseScale(Vector3 left, Vector3 right)
		{
			return new Vector3(left.x / right.x, left.y / right.y, left.z / right.z);
		}

		public static Vector4 ScaleUV(Vector4 start, Vector2 mask, float scale)
		{
			var (nonScaleOffset, scaleOffset, size) = DeconstructUV(start);
			var newSize = Vector2.Scale(size, Vector2.one + mask * (1f / scale - 1));
			return ConstructUV(nonScaleOffset, newSize);
		}

		public static (Vector2 nonScaleOffset, Vector2 scaleOffset, Vector2 size) DeconstructUV(Vector4 uv)
		{
			var offset = new Vector2(uv.x, uv.y);
			var size = new Vector2(uv.z, uv.w);
			var scaleOffset = GetUVScaleOffset(size);
			var nonScaleOffset = offset - scaleOffset;
			return (nonScaleOffset, scaleOffset, size);
		}

		public static Vector4 ConstructUV(Vector2 nonScaleOffset, Vector2 size)
		{
			var scaleOffset = GetUVScaleOffset(size);
			var offset = nonScaleOffset + scaleOffset;
			return new Vector4(offset.x, offset.y, size.x, size.y);
		}

		public static Vector2 GetUVScaleOffset(Vector2 size)
		{
			return 0.5f * (Divide(Vector2.one, size) - Vector2.one);
		}

		public static Vector2 Divide(Vector2 left, Vector2 right)
		{
			return new Vector2(left.x / right.x, left.y / right.y);
		}
	}
}
