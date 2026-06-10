using UnityEngine;

namespace RuntimeHandle
{
	public class RotationAxisHandler_Transform : IRotationAxisHandler
	{
		private readonly Transform _target;
		private readonly Vector3 _perp;
		private Quaternion _startLocalRotation;

		public RotationAxisHandler_Transform(Transform target, Vector3 perp)
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
    public class RotationHandle : MonoBehaviour
    {
        public RotationHandle Initialize(RuntimeTransformHandle transformHandle, Shader handleShader)
        {
			var rotationHandleTransform = transform;
            rotationHandleTransform.SetParent(transformHandle.transform, false);

			var rotationHandlerX = new RotationAxisHandler_Transform(transformHandle.targetTransform, Vector3.right);
			var rotationHandlerY = new RotationAxisHandler_Transform(transformHandle.targetTransform, Vector3.up);
			var rotationHandlerZ = new RotationAxisHandler_Transform(transformHandle.targetTransform, Vector3.forward);

            var axisX = new GameObject("RotationAxis.X (YZ)").AddComponent<RotationAxis>().Initialize(transformHandle, rotationHandleTransform, rotationHandlerX, Vector3.right, Color.red, handleShader);
            var axisY = new GameObject("RotationAxis.Y (XZ)").AddComponent<RotationAxis>().Initialize(transformHandle, rotationHandleTransform, rotationHandlerY, Vector3.up, Color.green, handleShader);
            var axisZ = new GameObject("RotationAxis.Z (XY)").AddComponent<RotationAxis>().Initialize(transformHandle, rotationHandleTransform, rotationHandlerZ, Vector3.forward, Color.blue, handleShader);

            return this;
        }
    }
}
