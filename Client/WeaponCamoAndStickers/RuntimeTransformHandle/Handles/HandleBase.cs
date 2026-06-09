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

		public static bool CanInteract_Rotation_Axis(Vector3 hitPoint, Transform TransformHandle, Camera camera)
		{
			var cameraPosition = camera.transform.position;
            var cameraDistance = (TransformHandle.position - cameraPosition).magnitude;
            var pointDistance = (hitPoint - cameraPosition).magnitude;
            return pointDistance <= cameraDistance;
		}

        public abstract void StartInteraction(Ray cameraRay);

		public static Vector3 StartInteraction_Position_Axis(Ray cameraRay, Transform TransformHandle, Vector3 _axis)
		{
            var raxis = TransformHandle.TransformDirection(_axis);
            var position = TransformHandle.position;
            var ray = new Ray(position, raxis);
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = hitPoint - position;
			return offset;
		}

		public static Vector3 StartInteraction_Position_Plane(Ray cameraRay, Transform TransformHandle, Vector3 _perp)
		{
            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = hitPoint - position;
			return offset;
		}

		public static Vector3 StartInteraction_Scale_Axis(Ray cameraRay, Transform TransformHandle, Vector3 _axis)
		{
            var raxis = TransformHandle.TransformDirection(_axis);
            var position = TransformHandle.position;
            var ray = new Ray(position, raxis);
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = hitPoint - position;
			return offset;
		}

		public static Vector3 StartInteraction_Scale_Plane(Ray cameraRay, Transform TransformHandle, Vector3 _perp)
		{
            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = hitPoint - position;
			return offset;
		}

		public static Vector3 StartInteraction_Rotation_Axis(Ray cameraRay, Transform TransformHandle, Vector3 _perp)
		{
            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = hitPoint - position;
			return offset;
		}

        public abstract void Interact(Ray cameraRay);

		public static Vector3 Interact_Position_Axis(Ray cameraRay, Transform TransformHandle, Vector3 _axis, float _offsetLength)
		{
            var raxis = TransformHandle.TransformDirection(_axis);
            var position = TransformHandle.position;
            var ray = new Ray(position, raxis);
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = raxis * _offsetLength;
            var newPosition = hitPoint - offset;
			return newPosition;
		}

		public static Vector3 Interact_Position_Plane(Ray cameraRay, Transform TransformHandle, Vector3 _perp, Vector3 _offsetLocalSpace)
		{
            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = TransformHandle.TransformDirection(_offsetLocalSpace);
            var newPosition = hitPoint - offset;
			return newPosition;
		}

		public static float Interact_Scale_Axis(Ray cameraRay, Transform TransformHandle, Vector3 _axis, float _startOffsetLength)
		{
            var raxis = TransformHandle.TransformDirection(_axis);
            var position = TransformHandle.position;
            var ray = new Ray(position, raxis);
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            var offset = hitPoint - position;
			var offsetLength = offset.magnitude;
            var scale = offsetLength / _startOffsetLength;
			return scale;
		}

		public static float Interact_Scale_Plane(Ray cameraRay, Transform TransformHandle, Vector3 _perp, float _startOffsetLength)
		{
            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = hitPoint - position;
            var offsetLength = offset.magnitude;
            var scale = offsetLength / _startOffsetLength;
			return scale;
		}

		public static float Interact_Rotation_Axis(Ray cameraRay, Transform TransformHandle, Vector3 _perp, Vector3 _startOffsetLocalSpace)
		{
            var rperp = TransformHandle.TransformDirection(_perp);
            var position = TransformHandle.position;
            var plane = new Plane(rperp, position);
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
			var offset = hitPoint - position;
			var offsetLocalSpace = TransformHandle.InverseTransformDirection(offset);
			var angle = Vector3.SignedAngle(_startOffsetLocalSpace, offsetLocalSpace, _perp);
			return angle;
		}

        public abstract void EndInteraction();

		public void OnDestroy()
		{
			if (_material)
			{
				Destroy(_material);
			}
		}
    }
}
