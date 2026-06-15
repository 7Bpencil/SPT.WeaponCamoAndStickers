//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SevenBoldPencil.Common;
using UnityEngine;
using UnityEngine.Rendering;

namespace SevenBoldPencil.Decorator
{
	public class Decorator : MonoBehaviour
	{
		public Transform DecoratorTransform;
		public GameObject Prefab;

		public void Init(DecoratorInfo info, Transform root)
		{
			gameObject.layer = root.gameObject.layer;

			DecoratorTransform = transform;
			ChangeRoot(info, root);
		}

		public void ChangeRoot(DecoratorInfo info, Transform root)
		{
            DecoratorTransform.parent = root;
			DecoratorTransform.localPosition = info.LocalPosition;
			DecoratorTransform.localEulerAngles = info.LocalEulerAngles;
			DecoratorTransform.localScale = info.LocalScale;
		}

		public void Set(GameObject prefab)
		{
			TransformHelperClass.SetLayersRecursively(prefab, gameObject.layer);

			var prefabTransform = prefab.transform;
			prefabTransform.parent = DecoratorTransform;
			prefabTransform.localPosition = Vector3.zero;
			prefabTransform.localEulerAngles = Vector3.zero;
			prefabTransform.localScale = Vector3.one;

			Prefab = prefab;

			ReplaceShadersToNative(prefab);
		}

        private static void ReplaceShadersToNative(GameObject obj)
        {
            foreach (var rend in obj.GetComponentsInChildren<Renderer>())
            {
                if (rend == null) continue;
                foreach (var mat in rend.sharedMaterials)
                {
                    if (mat == null || mat.shader == null) continue;
                    var nativeShader = Shader.Find(mat.shader.name);
                    if (nativeShader != null && mat.shader != nativeShader)
                    {
                        mat.shader = nativeShader;
                    }
                }
            }
        }

	}
}
