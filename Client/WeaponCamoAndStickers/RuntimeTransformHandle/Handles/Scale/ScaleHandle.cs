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

    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class ScaleHandle : MonoBehaviour
    {
        public ScaleHandle Initialize(
			RuntimeTransformHandle transformHandle,
			Shader handleShader,
            DecalInfo decalInfo,
            Decal decal)
        {
			var scaleHandleTransform = transform;
            scaleHandleTransform.SetParent(transformHandle.transform, false);

			var scaleHandlerX = new ScaleAxisHandler_Transform(decalInfo, decal, Vector3.right);
			var scaleHandlerY = new ScaleAxisHandler_Transform(decalInfo, decal, Vector3.up);
			var scaleHandlerZ = new ScaleAxisHandler_Transform(decalInfo, decal, Vector3.forward);

            var axisX = new GameObject("ScaleAxis.X").AddComponent<ScaleAxis>().Initialize(transformHandle, scaleHandleTransform, scaleHandlerX, Vector3.right, Color.red, handleShader);
            var axisY = new GameObject("ScaleAxis.Y").AddComponent<ScaleAxis>().Initialize(transformHandle, scaleHandleTransform, scaleHandlerY, Vector3.up, Color.green, handleShader);
            var axisZ = new GameObject("ScaleAxis.Z").AddComponent<ScaleAxis>().Initialize(transformHandle, scaleHandleTransform, scaleHandlerZ, Vector3.forward, Color.blue, handleShader);
            var planeXZ = new GameObject("ScalePlane.XZ").AddComponent<ScalePlane>().Initialize(transformHandle, this, axisX, axisZ, Vector3.up, Color.green, handleShader, decalInfo, decal);

            return this;
        }

		public static Vector3 CalculateScale(Vector3 startScale, Vector3 mask, float scale)
		{
			return Vector3.Scale(startScale, Vector3.one + mask * (scale - 1));
		}
    }
}
