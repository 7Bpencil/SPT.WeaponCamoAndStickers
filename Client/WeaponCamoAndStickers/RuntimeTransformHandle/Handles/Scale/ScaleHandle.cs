using SevenBoldPencil.Common;
using SevenBoldPencil.WeaponCamoAndStickers;
using UnityEngine;

namespace RuntimeHandle
{
	public class ScaleAxisHandler_Transform : IScaleAxisHandler
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector3 _axis;
        private Vector3 _startLocalScale;

		public ScaleAxisHandler_Transform(DecalInfo decalInfo, Decal decal, Vector3 axis)
		{
			_decalInfo = decalInfo;
			_decal = decal;
			_axis = axis;
		}

		public void OnStartInteraction()
		{
            _startLocalScale = _decal.DecalTransform.localScale;
		}

		public void SetScale(float scale)
		{
			var newLocalScale = ScaleHandle.CalculateScale(_startLocalScale, _axis, scale);
			_decalInfo.LocalScale = newLocalScale;
			_decal.ChangeLocalScale(newLocalScale);
		}
	}

	public class ScalePlaneHandler_Transform : IScalePlaneHandler
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector3 _scaleMask;
        private Vector3 _startLocalScale;

		public ScalePlaneHandler_Transform(DecalInfo decalInfo, Decal decal, Vector3 scaleMask)
		{
			_decalInfo = decalInfo;
			_decal = decal;
			_scaleMask = scaleMask;
		}

		public void OnStartInteraction()
		{
            _startLocalScale = _decal.DecalTransform.localScale;
		}

		public void SetScale(float scale)
		{
			var newLocalScale = ScaleHandle.CalculateScale(_startLocalScale, _scaleMask, scale);
			_decalInfo.LocalScale = newLocalScale;
			_decal.ChangeLocalScale(newLocalScale);
		}
	}

    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class ScaleHandle : ITransformHandle
    {
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;

		public ScaleHandle(DecalInfo decalInfo, Decal decal)
		{
			_decalInfo = decalInfo;
			_decal = decal;
		}

        public void Init(RuntimeTransformHandle transformHandle, Shader handleShader, Transform root)
        {
			var scaleHandlerX = new ScaleAxisHandler_Transform(_decalInfo, _decal, Vector3.right);
			var scaleHandlerY = new ScaleAxisHandler_Transform(_decalInfo, _decal, Vector3.up);
			var scaleHandlerZ = new ScaleAxisHandler_Transform(_decalInfo, _decal, Vector3.forward);
			var scaleHandlerXZ = new ScalePlaneHandler_Transform(_decalInfo, _decal, Vector3.right + Vector3.forward);

            var axisX = new GameObject("ScaleAxis.X").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandlerX, Vector3.right, Color.red, handleShader);
            var axisY = new GameObject("ScaleAxis.Y").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandlerY, Vector3.up, Color.green, handleShader);
            var axisZ = new GameObject("ScaleAxis.Z").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandlerZ, Vector3.forward, Color.blue, handleShader);
            var planeXZ = new GameObject("ScalePlane.XZ").AddComponent<ScalePlane>().Initialize(transformHandle, root, scaleHandlerXZ, axisX, axisZ, Vector3.up, Color.green, handleShader);
        }

        public void Reset(Transform transformHandle)
        {
            transformHandle.localPosition = _decal.DecalTransform.localPosition;
            transformHandle.localRotation = _decal.DecalTransform.localRotation;
        }

		public static Vector3 CalculateScale(Vector3 startScale, Vector3 mask, float scale)
		{
			return Vector3.Scale(startScale, Vector3.one + mask * (scale - 1));
		}
    }
}
