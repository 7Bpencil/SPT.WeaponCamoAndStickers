//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using RuntimeHandle;
using UnityEngine;
using PositionAxisHandle_Tranform = SevenBoldPencil.WeaponCamoAndStickers.PositionAxisHandle_Tranform;

namespace SevenBoldPencil.Decorator
{
    public class PositionHandle(Plugin plugin, string itemId, int decoratorIndex, DecoratorInfo decoratorInfo, Decorator decorator, Shader handleShader) : ITransformHandle
    {
		private readonly Plugin _plugin = plugin;
		private readonly string _itemId = itemId;
		private readonly int _decoratorIndex = decoratorIndex;
		private readonly DecoratorInfo _decoratorInfo = decoratorInfo;
		private readonly Decorator _decorator = decorator;
		private readonly Shader _handleShader = handleShader;

        public void Init(Transform transformHandle, Camera transformHandleCamera, Transform root)
        {
			var axisHandle = new PositionAxisHandle_Tranform(_decorator.DecoratorTransform);

            var axisX = new GameObject("PositionAxis.X").AddComponent<PositionAxis>().Initialize(transformHandle, root, axisHandle, Vector3.right, Color.red, _handleShader);
            var axisY = new GameObject("PositionAxis.Y").AddComponent<PositionAxis>().Initialize(transformHandle, root, axisHandle, Vector3.up, Color.green, _handleShader);
            var axisZ = new GameObject("PositionAxis.Z").AddComponent<PositionAxis>().Initialize(transformHandle, root, axisHandle, Vector3.forward, Color.blue, _handleShader);

            var planeXY = new GameObject("PositionPlane.XY").AddComponent<PositionPlane>().Initialize(transformHandle, root, axisHandle, Vector3.right, Vector3.up, Vector3.forward, Color.blue, _handleShader);
            var planeYZ = new GameObject("PositionPlane.YZ").AddComponent<PositionPlane>().Initialize(transformHandle, root, axisHandle, Vector3.up, Vector3.forward, Vector3.right, Color.red, _handleShader);
            var planeXZ = new GameObject("PositionPlane.XZ").AddComponent<PositionPlane>().Initialize(transformHandle, root, axisHandle, Vector3.right, Vector3.forward, Vector3.up, Color.green, _handleShader);
        }

        public void Reset(Transform transformHandle)
		{
            transformHandle.localPosition = _decorator.DecoratorTransform.localPosition;
            transformHandle.localRotation = _decorator.DecoratorTransform.localRotation;
		}

		public void OnInteractionEnd()
		{
            _decoratorInfo.LocalPosition = _decorator.DecoratorTransform.localPosition;
            _plugin.ApplyLocalPosition(_itemId, _decoratorIndex);
		}
    }
}
