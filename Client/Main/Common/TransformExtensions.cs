//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using System.Collections.Generic;
using UnityEngine;

namespace SevenBoldPencil.Common
{
	public static class TransformExtensions
	{
        public static string GetHierarchyPath(this Transform transform)
        {
            var names = new List<string>();
            var current = transform;

            while (current)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }
	}
}
