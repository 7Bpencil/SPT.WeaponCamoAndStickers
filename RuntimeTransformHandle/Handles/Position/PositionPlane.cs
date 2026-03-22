using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class PositionPlane : HandleBase
    {
        private Vector3 _axis1;
        private Vector3 _axis2;
        private Vector3 _perp;
        private GameObject _handle;
        private Vector3 _offsetLocalSpace;

        public PositionPlane Initialize(RuntimeTransformHandle transformHandle, PositionHandle positionHandle, Vector3 axis1, Vector3 axis2, Vector3 perp, Color color, Shader handleShader)
        {
            _transformHandle = transformHandle;
            _defaultColor = color;
            _axis1 = axis1;
            _axis2 = axis2;
            _perp = perp;

            InitializeMaterial(handleShader);

            transform.SetParent(positionHandle.transform, false);

            _handle = new GameObject("PositionPlane");
            _handle.transform.SetParent(transform, false);
            _handle.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _perp);
            _handle.transform.localPosition = (_axis1 + _axis2) * .25f;
            _handle.AddComponent<MeshRenderer>().material = _material;
            _handle.AddComponent<MeshFilter>().mesh = MeshUtils.CreateBox(.02f, .5f, 0.5f);
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
            var offset = Target.TransformDirection(_offsetLocalSpace);
            var newPosition = hitPoint - offset;

            Target.position = newPosition;
            TransformHandle.position = newPosition;
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

            _offsetLocalSpace = Target.InverseTransformDirection(offset);
        }

        private void Update()
        {
            var axis1 = _axis1;
            var raxis1 = Target.TransformDirection(axis1);
            var angle1 = Vector3.Angle(_transformHandle.handleCamera.transform.forward, raxis1);
            if (angle1 < 90)
            {
                axis1 = -axis1;
            }

            // Debug.Log(Vector3.Angle(_transformHandle.handleCamera.transform.forward, raxis1));
            // if (Vector3.Angle(_transformHandle.handleCamera.transform.forward, axis1) > 90)
            // {
            //     axis1 = -axis1;
            // }

            var axis2 = _axis2;
            var raxis2 = Target.TransformDirection(axis2);
            var angle2 = Vector3.Angle(_transformHandle.handleCamera.transform.forward, raxis2);
            if (angle2 < 90)
            {
                axis2 = -axis2;
            }

            _handle.transform.localPosition = (axis1 + axis2) * 0.25f;
        }
    }
}
