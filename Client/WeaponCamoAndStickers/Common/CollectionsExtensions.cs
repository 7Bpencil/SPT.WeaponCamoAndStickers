// Copyright (c) 2026 7Bpencil
// SPDX-License-Identifier: Apache-2.0

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
