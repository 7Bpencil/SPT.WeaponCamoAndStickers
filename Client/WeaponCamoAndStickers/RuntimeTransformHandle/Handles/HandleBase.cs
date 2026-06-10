using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public abstract class HandleBase : MonoBehaviour
    {
        protected RuntimeTransformHandle _transformHandle;
        protected Color _defaultColor;
        protected Material _material;

		public Transform Target => _transformHandle.targetTransform;
		public Transform TransformHandle => _transformHandle.handleTransform;

        protected void InitializeMaterial(Shader shader)
        {
            _material = new Material(shader);
            _material.color = _defaultColor;
        }

        public void SetDefaultColor()
        {
            _material.color = _defaultColor;
        }

        public void SetInteractionColor()
        {
            _material.color = Color.yellow;
        }

        public abstract bool CanInteract(Vector3 hitPoint);

        public abstract void StartInteraction(Ray cameraRay);

        public abstract void Interact(Ray cameraRay);

        public abstract void EndInteraction();

		public void OnDestroy()
		{
			if (_material)
			{
				Destroy(_material);
			}
		}

        public static (Vector3 position, Vector3 hitPoint, Vector3 raxis) GetAxisHitPoint(Ray cameraRay, Transform TransformHandle, Vector3 _axis)
        {
            var raxis = TransformHandle.TransformDirection(_axis);
            var position = TransformHandle.position;
            var ray = new Ray(position, raxis);
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            return (position, hitPoint, raxis);
        }

        public static (Vector3 position, Vector3 hitPoint) GetPlaneHitPoint(Ray cameraRay, Transform TransformHandle, Vector3 _perp)
        {
            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            return (position, hitPoint);
        }

    }
}
