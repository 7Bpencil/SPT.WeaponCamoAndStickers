using RuntimeHandle;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	public class PositionAxisHandle_Tranform(Transform target) : IPositionAxisHandle
	{
		private readonly Transform _target = target;

		public void OnStartInteraction()
		{

		}

		public void SetPosition(Vector3 position)
		{
            _target.position = position;
		}
	}

    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class PositionHandle(Plugin plugin, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal, Shader handleShader) : ITransformHandle
    {
		private readonly Plugin _plugin = plugin;
		private readonly string _itemId = itemId;
		private readonly int _decalIndex = decalIndex;
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private readonly Shader _handleShader = handleShader;

        public void Init(Transform transformHandle, Camera transformHandleCamera, Transform root)
        {
			var axisHandle = new PositionAxisHandle_Tranform(_decal.DecalTransform);

            var axisX = new GameObject("PositionAxis.X").AddComponent<PositionAxis>().Initialize(transformHandle, root, axisHandle, Vector3.right, Color.red, _handleShader);
            var axisY = new GameObject("PositionAxis.Y").AddComponent<PositionAxis>().Initialize(transformHandle, root, axisHandle, Vector3.up, Color.green, _handleShader);
            var axisZ = new GameObject("PositionAxis.Z").AddComponent<PositionAxis>().Initialize(transformHandle, root, axisHandle, Vector3.forward, Color.blue, _handleShader);

            var planeXY = new GameObject("PositionPlane.XY").AddComponent<PositionPlane>().Initialize(transformHandle, root, axisHandle, Vector3.right, Vector3.up, Vector3.forward, Color.blue, _handleShader);
            var planeYZ = new GameObject("PositionPlane.YZ").AddComponent<PositionPlane>().Initialize(transformHandle, root, axisHandle, Vector3.up, Vector3.forward, Vector3.right, Color.red, _handleShader);
            var planeXZ = new GameObject("PositionPlane.XZ").AddComponent<PositionPlane>().Initialize(transformHandle, root, axisHandle, Vector3.right, Vector3.forward, Vector3.up, Color.green, _handleShader);
        }

        public void Reset(Transform transformHandle)
		{
            transformHandle.localPosition = _decal.DecalTransform.localPosition;
            transformHandle.localRotation = _decal.DecalTransform.localRotation;
		}

		public void OnInteractionEnd()
		{
            _decalInfo.LocalPosition = _decal.DecalTransform.localPosition;
            _plugin.ApplyLocalPosition(_itemId, _decalIndex);
		}
    }
}
