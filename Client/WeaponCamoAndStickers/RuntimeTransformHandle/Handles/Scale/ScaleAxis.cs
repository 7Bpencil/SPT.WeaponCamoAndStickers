using SevenBoldPencil.Common;
using SevenBoldPencil.WeaponCamoAndStickers;
using UnityEngine;

namespace RuntimeHandle
{
	public interface IScaleAxisHandle
	{
		public void OnStartInteraction();
		public void SetScale(float scale);
	}

    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class ScaleAxis : HandleBase
    {
        private const float SIZE = 2;

		private IScaleAxisHandle _handle;
        private Vector3 _axis;
		private Transform _arm;
		private Transform _tip;
        private float _startOffsetLength;

		public Vector3 Axis => _axis;

        public ScaleAxis Initialize(
			RuntimeTransformHandle transformHandle,
			Transform scaleHandle,
			IScaleAxisHandle handle,
			Vector3 axis,
			Color color,
			Shader handleShader)
        {
			_handle = handle;
            _axis = axis;

            Init(transformHandle, handleShader, color);

            transform.SetParent(scaleHandle, false);

            {
                var o = new GameObject("Arm");
                o.transform.SetParent(transform, false);
                o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
                o.AddComponent<MeshRenderer>().material = _material;
                o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateCone(axis.magnitude * SIZE, .02f, .02f, 8, 1);
                o.AddComponent<MeshCollider>().sharedMesh = MeshUtils.CreateCone(axis.magnitude * SIZE, .1f, .02f, 8, 1);
				_arm = o.transform;
            }

            {
                var o = new GameObject("Tip");
                o.transform.SetParent(transform, false);
                o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
                o.transform.localPosition = axis * SIZE;
                o.AddComponent<MeshRenderer>().material = _material;
                o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateBox(.25f, .25f, .25f);
                o.AddComponent<MeshCollider>();
				_tip = o.transform;
            }

            return this;
        }

        public void SetHandleVisualScale(float scale)
        {
            _arm.localScale = new Vector3(1, scale, 1);
            _tip.localPosition = _axis * (SIZE * scale);
        }

		public override bool CanInteract(Vector3 hitPoint)
		{
			return true;
		}

        public override void Interact(Ray cameraRay)
        {
            var (position, hitPoint, _) = GetAxisHitPoint(cameraRay, TransformHandle, _axis);
            var offset = hitPoint - position;
			var offsetLength = offset.magnitude;
            var scale = offsetLength / _startOffsetLength;

			_handle.SetScale(scale);

			SetHandleVisualScale(scale);
        }

        public override void StartInteraction(Ray cameraRay)
        {
            var (position, hitPoint, _) = GetAxisHitPoint(cameraRay, TransformHandle, _axis);
            var offset = hitPoint - position;

            _startOffsetLength = offset.magnitude;
			_handle.OnStartInteraction();

			SetHandleVisualScale(1);
        }

        public override void EndInteraction()
		{
			SetHandleVisualScale(1);
		}
    }
}
