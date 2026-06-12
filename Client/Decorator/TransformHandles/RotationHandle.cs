//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using RuntimeHandle;
using UnityEngine;
using RotationAxisHandle_Transform = SevenBoldPencil.WeaponCamoAndStickers.RotationAxisHandle_Transform;

namespace SevenBoldPencil.Decorator
{
    public class RotationHandle(Plugin plugin, string itemId, int decoratorIndex, DecoratorInfo decoratorInfo, Decorator decorator, Shader handleShader) : ITransformHandle
    {
		private readonly Plugin _plugin = plugin;
		private readonly string _itemId = itemId;
		private readonly int _decoratorIndex = decoratorIndex;
		private readonly DecoratorInfo _decoratorInfo = decoratorInfo;
		private readonly Decorator _decorator = decorator;
		private readonly Shader _handleShader = handleShader;

        public void Init(Transform transformHandle, Camera transformHandleCamera, Transform root)
        {
			var rotationHandleX = new RotationAxisHandle_Transform(_decorator.DecoratorTransform, Vector3.right);
			var rotationHandleY = new RotationAxisHandle_Transform(_decorator.DecoratorTransform, Vector3.up);
			var rotationHandleZ = new RotationAxisHandle_Transform(_decorator.DecoratorTransform, Vector3.forward);

            var axisX = new GameObject("RotationAxis.X (YZ)").AddComponent<RotationAxis>().Initialize(transformHandle, transformHandleCamera, root, rotationHandleX, Vector3.right, Color.red, _handleShader);
            var axisY = new GameObject("RotationAxis.Y (XZ)").AddComponent<RotationAxis>().Initialize(transformHandle, transformHandleCamera, root, rotationHandleY, Vector3.up, Color.green, _handleShader);
            var axisZ = new GameObject("RotationAxis.Z (XY)").AddComponent<RotationAxis>().Initialize(transformHandle, transformHandleCamera, root, rotationHandleZ, Vector3.forward, Color.blue, _handleShader);
        }

        public void Reset(Transform transformHandle)
		{
            transformHandle.localPosition = _decorator.DecoratorTransform.localPosition;
            transformHandle.localRotation = _decorator.DecoratorTransform.localRotation;
		}

		public void OnInteractionEnd()
		{
            _decoratorInfo.LocalEulerAngles = _decorator.DecoratorTransform.localEulerAngles;
            _plugin.ApplyLocalEulerAngles(_itemId, _decoratorIndex);
		}
    }
}
