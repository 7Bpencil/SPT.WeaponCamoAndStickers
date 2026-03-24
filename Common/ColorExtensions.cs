//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using UnityEngine;

namespace SevenBoldPencil.Common
{
    public static class ColorExtensions
    {
		public static Color WithAlpha(this Color color, float alpha)
		{
			return new(color.r, color.g, color.b, alpha);
		}
    }
}
