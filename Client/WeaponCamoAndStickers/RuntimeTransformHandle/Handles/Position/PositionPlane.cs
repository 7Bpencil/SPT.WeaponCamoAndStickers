using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class PositionPlane : HandleBase
    {
		private Transform _transformHandle;
		private IPositionAxisHandle _handle;
        private Vector3 _perp;
        private Vector3 _offsetLocalSpace;

        public PositionPlane Initialize(
			Transform transformHandle,
			Transform positionHandle,
			IPositionAxisHandle handle,
			Vector3 axis1,
			Vector3 axis2,
			Vector3 perp,
			Color color,
			Shader handleShader)
        {
			_transformHandle = transformHandle;
			_handle = handle;
            _perp = perp;

            Init(handleShader, color);

            transform.SetParent(positionHandle, false);

			{
	            var o = new GameObject("PositionPlane");
	            o.transform.SetParent(transform, false);
	            o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _perp);
	            o.transform.localPosition = axis1 + axis2;
	            o.AddComponent<MeshRenderer>().material = _material;
	            o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateBox(0.02f, 0.25f, 0.25f);
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
            var (_, hitPoint) = GetPlaneHitPoint(cameraRay, _transformHandle, _perp);
            var offset = _transformHandle.TransformDirection(_offsetLocalSpace);
            var newPosition = hitPoint - offset;

			_handle.SetPosition(newPosition);
            _transformHandle.position = newPosition;
        }

        public override void StartInteraction(Ray cameraRay)
        {
            var (position, hitPoint) = GetPlaneHitPoint(cameraRay, _transformHandle, _perp);
            var offset = hitPoint - position;

            _offsetLocalSpace = _transformHandle.InverseTransformDirection(offset);
			_handle.OnStartInteraction();
        }

        public override void EndInteraction()
        {

        }
    }
}
