//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using RuntimeHandle;
using SevenBoldPencil.Common;
using UnityEngine;

namespace SevenBoldPencil.Decorator
{
	public class ScaleAxisHandle_Transform(Transform target, Vector3 scaleMask) : IScaleAxisHandle
	{
		private readonly Transform _target = target;
		private readonly Vector3 _scaleMask = scaleMask;
        private Vector3 _startLocalScale;

		public void OnStartInteraction()
		{
            _startLocalScale = _target.localScale;
		}

		public void SetScale(float scale)
		{
			_target.localScale = ScaleHandle.CalculateScale(_startLocalScale, _scaleMask, scale);
		}
	}

    public class ScaleHandle(Plugin plugin, string itemId, int decoratorIndex, DecoratorInfo decoratorInfo, Decorator decorator, Shader handleShader) : ITransformHandle
    {
		private readonly Plugin _plugin = plugin;
		private readonly string _itemId = itemId;
		private readonly int _decoratorIndex = decoratorIndex;
		private readonly DecoratorInfo _decoratorInfo = decoratorInfo;
		private readonly Decorator _decorator = decorator;
		private readonly Shader _handleShader = handleShader;

        public void Init(Transform transformHandle, Camera transformHandleCamera, Transform root)
        {
			var scaleHandleX = new ScaleAxisHandle_Transform(_decorator.DecoratorTransform, Vector3.right);
			var scaleHandleY = new ScaleAxisHandle_Transform(_decorator.DecoratorTransform, Vector3.up);
			var scaleHandleZ = new ScaleAxisHandle_Transform(_decorator.DecoratorTransform, Vector3.forward);

			var scaleHandleXY = new ScaleAxisHandle_Transform(_decorator.DecoratorTransform, Vector3.right + Vector3.up);
			var scaleHandleYZ = new ScaleAxisHandle_Transform(_decorator.DecoratorTransform, Vector3.up + Vector3.forward);
			var scaleHandleXZ = new ScaleAxisHandle_Transform(_decorator.DecoratorTransform, Vector3.right + Vector3.forward);

			var scaleHandleXYZ = new ScaleAxisHandle_Transform(_decorator.DecoratorTransform, Vector3.right + Vector3.up + Vector3.forward);

            var axisX = new GameObject("ScaleAxis.X").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleX, Vector3.right, Color.red, _handleShader);
            var axisY = new GameObject("ScaleAxis.Y").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleY, Vector3.up, Color.green, _handleShader);
            var axisZ = new GameObject("ScaleAxis.Z").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleZ, Vector3.forward, Color.blue, _handleShader);

            var planeXY = new GameObject("ScalePlane.XY").AddComponent<ScalePlane>().Initialize(transformHandle, root, scaleHandleXY, axisX, axisY, Vector3.forward, Color.blue, _handleShader);
            var planeYZ = new GameObject("ScalePlane.YZ").AddComponent<ScalePlane>().Initialize(transformHandle, root, scaleHandleYZ, axisY, axisZ, Vector3.right, Color.red, _handleShader);
            var planeXZ = new GameObject("ScalePlane.XZ").AddComponent<ScalePlane>().Initialize(transformHandle, root, scaleHandleXZ, axisX, axisZ, Vector3.up, Color.green, _handleShader);

			var globalXYZ = new GameObject("ScaleGlobal.XYZ").AddComponent<ScaleGlobal>().Initialize(transformHandle, transformHandleCamera, root, scaleHandleXYZ, Color.white, _handleShader);
        }

        public void Reset(Transform transformHandle)
        {
            transformHandle.localPosition = _decorator.DecoratorTransform.localPosition;
            transformHandle.localRotation = _decorator.DecoratorTransform.localRotation;
        }

		public static Vector3 CalculateScale(Vector3 startScale, Vector3 mask, float scale)
		{
			return Vector3.Scale(startScale, Vector3.one + mask * (scale - 1));
		}

		public void OnInteractionEnd()
		{
            _decoratorInfo.LocalScale = _decorator.DecoratorTransform.localScale;
            _plugin.ApplyLocalScale(_itemId, _decoratorIndex);
		}
    }
}
