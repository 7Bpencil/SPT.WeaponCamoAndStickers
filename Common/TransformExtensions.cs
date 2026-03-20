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
