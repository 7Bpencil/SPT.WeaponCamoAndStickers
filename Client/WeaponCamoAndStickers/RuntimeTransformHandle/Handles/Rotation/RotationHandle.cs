using UnityEngine;

namespace RuntimeHandle
{
	public class RotationAxisHandle_Transform : IRotationAxisHandle
	{
		private readonly Transform _target;
		private readonly Vector3 _perp;
		private Quaternion _startLocalRotation;

		public RotationAxisHandle_Transform(Transform target, Vector3 perp)
		{
			_target = target;
			_perp = perp;
		}

		public Vector3 GetPosition()
		{
			return _target.position;
		}

		public Quaternion GetRotation()
		{
            return _target.rotation;
		}

		public void OnStartInteraction()
		{
			_startLocalRotation = _target.localRotation;
		}

		public void SetAngle(float angle)
		{
			_target.localRotation = _startLocalRotation * Quaternion.AngleAxis(angle, _perp);
		}
	}

    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class RotationHandle : ITransformHandle
    {
		private readonly Transform _target;

		public RotationHandle(Transform target)
		{
			_target = target;
		}

        public void Init(RuntimeTransformHandle transformHandle, Shader handleShader, Transform root)
        {
			var rotationHandleX = new RotationAxisHandle_Transform(_target, Vector3.right);
			var rotationHandleY = new RotationAxisHandle_Transform(_target, Vector3.up);
			var rotationHandleZ = new RotationAxisHandle_Transform(_target, Vector3.forward);

            var axisX = new GameObject("RotationAxis.X (YZ)").AddComponent<RotationAxis>().Initialize(transformHandle, root, rotationHandleX, Vector3.right, Color.red, handleShader);
            var axisY = new GameObject("RotationAxis.Y (XZ)").AddComponent<RotationAxis>().Initialize(transformHandle, root, rotationHandleY, Vector3.up, Color.green, handleShader);
            var axisZ = new GameObject("RotationAxis.Z (XY)").AddComponent<RotationAxis>().Initialize(transformHandle, root, rotationHandleZ, Vector3.forward, Color.blue, handleShader);
        }

        public void Reset(Transform transformHandle)
		{
            transformHandle.localPosition = _target.localPosition;
            transformHandle.localRotation = _target.localRotation;
		}
    }
}
