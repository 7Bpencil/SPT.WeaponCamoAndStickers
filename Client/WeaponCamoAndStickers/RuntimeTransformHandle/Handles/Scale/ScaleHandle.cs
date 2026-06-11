using SevenBoldPencil.Common;
using SevenBoldPencil.WeaponCamoAndStickers;
using UnityEngine;

namespace RuntimeHandle
{
	public class ScaleAxisHandle_Transform : IScaleAxisHandle
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector3 _axis;
        private Vector3 _startLocalScale;

		public ScaleAxisHandle_Transform(DecalInfo decalInfo, Decal decal, Vector3 axis)
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

	public class ScalePlaneHandle_Transform : IScalePlaneHandle
	{
		private readonly DecalInfo _decalInfo;
		private readonly Decal _decal;
		private readonly Vector3 _scaleMask;
        private Vector3 _startLocalScale;

		public ScalePlaneHandle_Transform(DecalInfo decalInfo, Decal decal, Vector3 scaleMask)
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

        public void Init(Transform transformHandle, Camera transformHandleCamera, Shader handleShader, Transform root)
        {
			var scaleHandleX = new ScaleAxisHandle_Transform(_decalInfo, _decal, Vector3.right);
			var scaleHandleY = new ScaleAxisHandle_Transform(_decalInfo, _decal, Vector3.up);
			var scaleHandleZ = new ScaleAxisHandle_Transform(_decalInfo, _decal, Vector3.forward);
			var scaleHandleXZ = new ScalePlaneHandle_Transform(_decalInfo, _decal, Vector3.right + Vector3.forward);

            var axisX = new GameObject("ScaleAxis.X").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleX, Vector3.right, Color.red, handleShader);
            var axisY = new GameObject("ScaleAxis.Y").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleY, Vector3.up, Color.green, handleShader);
            var axisZ = new GameObject("ScaleAxis.Z").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleZ, Vector3.forward, Color.blue, handleShader);
            var planeXZ = new GameObject("ScalePlane.XZ").AddComponent<ScalePlane>().Initialize(transformHandle, root, scaleHandleXZ, axisX, axisZ, Vector3.up, Color.green, handleShader);
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
