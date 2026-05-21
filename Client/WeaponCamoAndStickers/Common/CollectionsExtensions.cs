//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using System.Collections.Generic;

namespace SevenBoldPencil.Common
{
	public static class CollectionsExtensions
	{
		public static void Toggle<T>(this HashSet<T> hashSet, T value)
		{
            if (!hashSet.Add(value))
            {
                hashSet.Remove(value);
            }
		}
	}
}
