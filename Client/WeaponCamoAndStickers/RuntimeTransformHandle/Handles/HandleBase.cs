// Copyright (c) 2020 Peter @sHTiF Stefcek
// Copyright (c) 2026 7Bpencil
// SPDX-License-Identifier: MIT

using UnityEngine;

namespace RuntimeHandle
{
    public abstract class HandleBase : MonoBehaviour
    {
        private Color _defaultColor;
        protected Material _material;

        protected void Init(Shader shader, Color defaultColor)
        {
			_defaultColor = new(defaultColor.r, defaultColor.g, defaultColor.b, 0.5f);
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

        public static (Vector3 position, Vector3 hitPoint, Vector3 raxis) GetAxisHitPoint(Ray cameraRay, Transform transformHandle, Vector3 axis)
        {
            var raxis = transformHandle.TransformDirection(axis);
            var position = transformHandle.position;
            var ray = new Ray(position, raxis);
            var closestT = HandleMathUtils.ClosestPointOnRay(ray, cameraRay);
            var hitPoint = ray.GetPoint(closestT);
            return (position, hitPoint, raxis);
        }

        public static (Vector3 position, Vector3 hitPoint) GetPlaneHitPoint(Ray cameraRay, Transform transformHandle, Vector3 perp)
        {
            var rperp = transformHandle.TransformDirection(perp);
            var position = transformHandle.position;
            var plane = new Plane(rperp, position);
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            return (position, hitPoint);
        }

    }
}
