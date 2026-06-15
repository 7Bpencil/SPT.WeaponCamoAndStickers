//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using UnityEngine;

namespace RuntimeHandle
{
	public class ScaleGlobal : HandleBase
	{
		private Transform _transformHandle;
		private Transform _transformHandleCameraTransform;
		private IScaleAxisHandle _handle;
		private Transform _arc;
        private float _startOffsetLength;

		public ScaleGlobal Initialize(
			Transform transformHandle,
			Camera transformHandleCamera,
			Transform scaleHandle,
			IScaleAxisHandle handle,
			Color color,
			Shader handleShader)
		{
			_transformHandle = transformHandle;
			_transformHandleCameraTransform = transformHandleCamera.transform;
			_handle = handle;

            Init(handleShader, color);

            transform.SetParent(scaleHandle, false);

			{
	            var o = new GameObject("Arc");
	            o.transform.SetParent(transform, false);
	            o.AddComponent<MeshRenderer>().material = _material;
	            o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateTorus(2.4f, .04f, 32, 6);
	            o.AddComponent<MeshCollider>().sharedMesh = MeshUtils.CreateTorus(2.4f, .1f, 32, 6);
				_arc = o.transform;
			}

			return this;
		}

        public void Update()
        {
			var perp = (_transformHandleCameraTransform.position - _transformHandle.position).normalized;
            _arc.rotation = Quaternion.LookRotation(perp) * Quaternion.Euler(90, 0, 0);
        }

		public override bool CanInteract(Vector3 hitPoint)
		{
			return true;
		}

        public override void Interact(Ray cameraRay)
		{
			var perp = (_transformHandleCameraTransform.position - _transformHandle.position).normalized;
            var (position, hitPoint) = GetPlaneHitPoint(cameraRay, _transformHandle, perp);
            var offset = hitPoint - position;
            var offsetLength = offset.magnitude;
            var scale = offsetLength / _startOffsetLength;

			_handle.SetScale(scale);
		}

        public override void StartInteraction(Ray cameraRay)
		{
			var perp = (_transformHandleCameraTransform.position - _transformHandle.position).normalized;
            var (position, hitPoint) = GetPlaneHitPoint(cameraRay, _transformHandle, perp);
            var offset = hitPoint - position;

            _startOffsetLength = offset.magnitude;
			_handle.OnStartInteraction();
		}

        public override void EndInteraction()
		{

		}
	}
}
