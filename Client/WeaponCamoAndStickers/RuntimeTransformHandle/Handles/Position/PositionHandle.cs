using UnityEngine;

namespace RuntimeHandle
{
	public class PositionAxisHandle_Tranform : IPositionAxisHandle
	{
		private readonly Transform _target;

		public PositionAxisHandle_Tranform(Transform target)
		{
			_target = target;
		}

		public void OnStartInteraction()
		{

		}

		public void SetPosition(Vector3 position)
		{
            _target.position = position;
		}
	}

    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class PositionHandle : ITransformHandle
    {
		private readonly Transform _target;

		public PositionHandle(Transform target)
		{
			_target = target;
		}

        public void Init(Transform transformHandle, Camera transformHandleCamera, Shader handleShader, Transform root)
        {
			var axisHandle = new PositionAxisHandle_Tranform(_target);

            var axisX = new GameObject("PositionAxis.X").AddComponent<PositionAxis>().Initialize(transformHandle, root, axisHandle, Vector3.right, Color.red, handleShader);
            var axisY = new GameObject("PositionAxis.Y").AddComponent<PositionAxis>().Initialize(transformHandle, root, axisHandle, Vector3.up, Color.green, handleShader);
            var axisZ = new GameObject("PositionAxis.Z").AddComponent<PositionAxis>().Initialize(transformHandle, root, axisHandle, Vector3.forward, Color.blue, handleShader);

            var planeXY = new GameObject("PositionPlane.XY").AddComponent<PositionPlane>().Initialize(transformHandle, root, axisHandle, Vector3.right, Vector3.up, Vector3.forward, Color.blue, handleShader);
            var planeYZ = new GameObject("PositionPlane.YZ").AddComponent<PositionPlane>().Initialize(transformHandle, root, axisHandle, Vector3.up, Vector3.forward, Vector3.right, Color.red, handleShader);
            var planeXZ = new GameObject("PositionPlane.XZ").AddComponent<PositionPlane>().Initialize(transformHandle, root, axisHandle, Vector3.right, Vector3.forward, Vector3.up, Color.green, handleShader);
        }

        public void Reset(Transform transformHandle)
		{
            transformHandle.localPosition = _target.localPosition;
            transformHandle.localRotation = _target.localRotation;
		}
    }
}
