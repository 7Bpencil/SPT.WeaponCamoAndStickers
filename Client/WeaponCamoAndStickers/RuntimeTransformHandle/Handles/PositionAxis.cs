using UnityEngine;

namespace RuntimeHandle
{
	public interface IPositionAxisHandle
	{
		public void OnStartInteraction();
		public void SetPosition(Vector3 position);
	}

    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class PositionAxis : HandleBase
    {
		private Transform _transformHandle;
		private IPositionAxisHandle _handle;
        private Vector3 _axis;
        private float _offsetLength;

        public PositionAxis Initialize(
			Transform transformHandle,
			Transform positionHandle,
			IPositionAxisHandle handle,
			Vector3 axis,
			Color color,
			Shader handleShader)
        {
			_transformHandle = transformHandle;
			_handle = handle;
            _axis = axis;

            Init(handleShader, color);

            transform.SetParent(positionHandle, false);

            {
                var o = new GameObject("Arm");
                o.transform.SetParent(transform, false);
                o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
                o.AddComponent<MeshRenderer>().material = _material;
                o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateCone(2f, .02f, .02f, 8, 1);
                o.AddComponent<MeshCollider>().sharedMesh = MeshUtils.CreateCone(2f, .1f, .02f, 8, 1);
            }

            {
                var o = new GameObject("Tip");
                o.transform.SetParent(transform, false);
                o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _axis);
                o.transform.localPosition = axis * 2;
                o.AddComponent<MeshRenderer>().material = _material;
                o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateCone(.4f, .2f, .0f, 8, 1);
                o.AddComponent<MeshCollider>();
            }

            return this;
        }

		public override bool CanInteract(Vector3 hitPoint)
		{
			return true;
		}

        public override void Interact(Ray cameraRay)
        {
			var (_, hitPoint, raxis) = GetAxisHitPoint(cameraRay, _transformHandle, _axis);
            var offset = raxis * _offsetLength;
            var newPosition = hitPoint - offset;

			_handle.SetPosition(newPosition);
            _transformHandle.position = newPosition;
        }

        public override void StartInteraction(Ray cameraRay)
        {
			var (position, hitPoint, _) = GetAxisHitPoint(cameraRay, _transformHandle, _axis);
            var offset = hitPoint - position;

            _offsetLength = offset.magnitude;
			_handle.OnStartInteraction();
        }

        public override void EndInteraction()
        {

        }
    }
}
