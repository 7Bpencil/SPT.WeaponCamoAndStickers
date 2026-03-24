//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using UnityEngine;

namespace RuntimeHandle
{
    public class ScalePlane : HandleBase
    {
        private const float SIZE = 2;

        private Vector3 _axis1;
        private Vector3 _axis2;
        private Vector3 _perp;
        private GameObject _handle;
        private float _startOffsetLength;
        private Vector3 _startLocalScale;

        private ScaleAxis _axis1Handle;
        private ScaleAxis _axis2Handle;

        public ScalePlane Initialize(RuntimeTransformHandle transformHandle, ScaleHandle scaleHandle, ScaleAxis axis1, ScaleAxis axis2, Vector3 perp, Color color, Shader handleShader)
        {
            _transformHandle = transformHandle;
            _defaultColor = color;
            _axis1 = axis1.Axis;
            _axis2 = axis2.Axis;
            _perp = perp;

            _axis1Handle = axis1;
            _axis2Handle = axis2;

            InitializeMaterial(handleShader);

            transform.SetParent(scaleHandle.transform, false);

            _handle = new GameObject("ScalePlane");
            _handle.transform.SetParent(transform, false);
            _handle.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _perp);
            _handle.transform.localPosition = _axis1 + _axis2;
            _handle.AddComponent<MeshRenderer>().material = _material;
            _handle.AddComponent<MeshFilter>().mesh = MeshUtils.CreateBox(0.02f, 0.25f, 0.25f);
            _handle.AddComponent<MeshCollider>();

            return this;
        }

        public override void Interact(Vector3 p_previousPosition)
        {
            base.Interact(p_previousPosition);

            var rperp = Target.TransformDirection(_perp);
            var position = Target.position;
            var plane = new Plane(rperp, position);
            var cameraRay = _transformHandle.GetCameraRay();
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = hitPoint - position;
            var offsetLength = offset.magnitude;
            var scale = offsetLength / _startOffsetLength;

            Target.localScale = ScaleHandle.CalculateScale(_startLocalScale, _axis1 + _axis2, scale);

            SetHandlesVisualScale(scale);
        }

        public override void StartInteraction(Vector3 p_hitPoint)
        {
			base.StartInteraction(p_hitPoint);

            var rperp = Target.TransformDirection(_perp);
            var position = Target.position;
            var plane = new Plane(rperp, position);
            var cameraRay = _transformHandle.GetCameraRay();
            plane.Raycast(cameraRay, out var closestT);
            var hitPoint = cameraRay.GetPoint(closestT);
            var offset = hitPoint - position;

            _startOffsetLength = offset.magnitude;
            _startLocalScale = Target.localScale;

            SetHandlesVisualScale(1);
            SetHandlesInteractionColor();
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            SetHandlesVisualScale(1);
            SetHandlesDefaultColor();
        }

        public void SetHandlesVisualScale(float scale)
        {
            _axis1Handle.SetHandleVisualScale(scale);
            _axis2Handle.SetHandleVisualScale(scale);
        }

        public void SetHandlesInteractionColor()
        {
            _axis1Handle.SetColor(Color.yellow);
            _axis2Handle.SetColor(Color.yellow);
        }

        public void SetHandlesDefaultColor()
        {
            _axis1Handle.SetDefaultColor();
            _axis2Handle.SetDefaultColor();
        }
	}
}
