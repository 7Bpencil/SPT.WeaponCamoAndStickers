using RuntimeHandle;
using SevenBoldPencil.Common;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	public class ScaleAxisHandle_Transform(DecalInfo decalInfo, Decal decal, Vector3 scaleMask) : IScaleAxisHandle
	{
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private readonly Vector3 _scaleMask = scaleMask;
        private Vector3 _startLocalScale;

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
    public class ScaleHandle(Plugin plugin, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal, Shader handleShader) : ITransformHandle
    {
		private readonly Plugin _plugin = plugin;
		private readonly string _itemId = itemId;
		private readonly int _decalIndex = decalIndex;
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private readonly Shader _handleShader = handleShader;

        public void Init(Transform transformHandle, Camera transformHandleCamera, Transform root)
        {
			var scaleHandleX = new ScaleAxisHandle_Transform(_decalInfo, _decal, Vector3.right);
			var scaleHandleY = new ScaleAxisHandle_Transform(_decalInfo, _decal, Vector3.up);
			var scaleHandleZ = new ScaleAxisHandle_Transform(_decalInfo, _decal, Vector3.forward);
			var scaleHandleXZ = new ScaleAxisHandle_Transform(_decalInfo, _decal, Vector3.right + Vector3.forward);

            var axisX = new GameObject("ScaleAxis.X").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleX, Vector3.right, Color.red, _handleShader);
            var axisY = new GameObject("ScaleAxis.Y").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleY, Vector3.up, Color.green, _handleShader);
            var axisZ = new GameObject("ScaleAxis.Z").AddComponent<ScaleAxis>().Initialize(transformHandle, root, scaleHandleZ, Vector3.forward, Color.blue, _handleShader);
            var planeXZ = new GameObject("ScalePlane.XZ").AddComponent<ScalePlane>().Initialize(transformHandle, root, scaleHandleXZ, axisX, axisZ, Vector3.up, Color.green, _handleShader);
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

		public void OnInteractionEnd()
		{
            _decalInfo.LocalScale = _decal.DecalTransform.localScale;
            _plugin.ApplyLocalScale(_itemId, _decalIndex);
		}
    }
}
