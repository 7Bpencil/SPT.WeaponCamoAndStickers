using SevenBoldPencil.Common;
using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class RotationAxis : HandleBase
    {
		private Transform _rotationHandle;

        private Vector3 _perp;

		private Vector3 _startOffsetLocalSpace;
		private Quaternion _startLocalRotation;

        public RotationAxis Initialize(RuntimeTransformHandle transformHandle, RotationHandle rotationHandle, Vector3 perp, Color color, Shader handleShader)
        {
            _transformHandle = transformHandle;
            _defaultColor = color.WithAlpha(0.5f);

			_rotationHandle = rotationHandle.transform;

            _perp = perp;

            InitializeMaterial(handleShader);

            transform.SetParent(_rotationHandle, false);

            var o = new GameObject("Arc");
            o.transform.SetParent(transform, false);
            o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _perp);
            o.AddComponent<MeshRenderer>().material = _material;
            o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateTorus(2f, .04f, 32, 6);
            o.AddComponent<MeshCollider>().sharedMesh = MeshUtils.CreateTorus(2f, .1f, 32, 6);

            return this;
        }

        public void Update()
        {
            _material.SetVector("_CameraPosition", _transformHandle.handleCamera.transform.position);
            _material.SetFloat("_CameraDistance", (_transformHandle.handleCamera.transform.position - TransformHandle.position).magnitude);
        }

        public override void Interact(Ray cameraRay)
        {
			var angle = Interact_Rotation_Axis(cameraRay, TransformHandle, _perp, _startOffsetLocalSpace);

			Target.localRotation = _startLocalRotation * Quaternion.AngleAxis(angle, _perp);
			_rotationHandle.rotation = Target.rotation;
        }

        public override bool CanInteract(Vector3 hitPoint)
        {
			return CanInteract_Rotation_Axis(hitPoint, TransformHandle, _transformHandle.handleCamera);
        }

        public override void StartInteraction(Ray cameraRay)
        {
            TransformHandle.rotation = Target.rotation;
			_rotationHandle.rotation = Quaternion.identity;

			var offset = StartInteraction_Rotation_Axis(cameraRay, TransformHandle, _perp);

			_startOffsetLocalSpace = TransformHandle.InverseTransformDirection(offset);
			_startLocalRotation = Target.localRotation;
        }

        public override void EndInteraction()
        {
            TransformHandle.rotation = Target.rotation;
			_rotationHandle.localRotation = Quaternion.identity;
        }
    }
}
