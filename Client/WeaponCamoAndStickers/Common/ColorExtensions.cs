//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using System;
using System.Globalization;
using UnityEngine;

namespace SevenBoldPencil.Common
{
    public static class ColorExtensions
    {
		public static Color WithAlpha(this Color color, float alpha)
		{
			return new(color.r, color.g, color.b, alpha);
		}

		public static Vector4 WithAlpha(this Vector3 color, float alpha)
		{
			return new(color.x, color.y, color.z, alpha);
		}

		public static Color HSVtoRGBA(this Vector3 hsv)
		{
            return Color.HSVToRGB(hsv.x, hsv.y, hsv.z);
		}

		public static Vector3 RGBAtoHSV(this Color color)
		{
			Color.RGBToHSV(color, out var h, out var s, out var v);
			return new(h, s, v);
		}

		public static Color HSVAtoRGBA(this Vector4 hsva)
		{
            return Color.HSVToRGB(hsva.x, hsva.y, hsva.z).WithAlpha(hsva.w);
		}

		public static string HSVtoHexRGB(this Vector3 hsv)
		{
			var rgb = Color.HSVToRGB(hsv.x, hsv.y, hsv.z);
			var rgb32 = (Color32)rgb; // we lose precision, but people dont care anyway
			return $"#{rgb32.r:X2}{rgb32.g:X2}{rgb32.b:X2}";
		}

		public static Option<Vector3> HexRGBtoHSV(string hexRGB)
		{
			if (hexRGB.Length < 6)
			{
				return default;
			}

			var start = 0;
			if (hexRGB[0] == '#')
			{
				start = 1;
			}
			if (hexRGB.Length != start + 6)
			{
				return default;
			}

			byte r, g, b;
			if (!byte.TryParse(hexRGB.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r))
			{
				return default;
			}
			if (!byte.TryParse(hexRGB.AsSpan(start + 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g))
			{
				return default;
			}
			if (!byte.TryParse(hexRGB.AsSpan(start + 4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
			{
				return default;
			}

			var color32 = new Color32(r, g, b, 0);
			Color.RGBToHSV(color32, out var h, out var s, out var v);
			return new(new(h, s, v));
		}
    }
}
