using RuntimeHandle;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	public class RotationAxisHandle_Transform(Transform target, Vector3 perp) : IRotationAxisHandle
	{
		private readonly Transform _target = target;
		private readonly Vector3 _perp = perp;
		private Quaternion _startLocalRotation;

		public Vector3 GetPosition()
		{
			return _target.position;
		}

		public Quaternion GetRotation()
		{
            return _target.rotation;
		}

		public void OnStartInteraction()
		{
			_startLocalRotation = _target.localRotation;
		}

		public void SetAngle(float angle)
		{
			_target.localRotation = _startLocalRotation * Quaternion.AngleAxis(angle, _perp);
		}
	}

    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class RotationHandle(Plugin plugin, string itemId, int decalIndex, DecalInfo decalInfo, Decal decal, Shader handleShader) : ITransformHandle
    {
		private readonly Plugin _plugin = plugin;
		private readonly string _itemId = itemId;
		private readonly int _decalIndex = decalIndex;
		private readonly DecalInfo _decalInfo = decalInfo;
		private readonly Decal _decal = decal;
		private readonly Shader _handleShader = handleShader;

        public void Init(Transform transformHandle, Camera transformHandleCamera, Transform root)
        {
			var rotationHandleX = new RotationAxisHandle_Transform(_decal.DecalTransform, Vector3.right);
			var rotationHandleY = new RotationAxisHandle_Transform(_decal.DecalTransform, Vector3.up);
			var rotationHandleZ = new RotationAxisHandle_Transform(_decal.DecalTransform, Vector3.forward);

            var axisX = new GameObject("RotationAxis.X (YZ)").AddComponent<RotationAxis>().Initialize(transformHandle, transformHandleCamera, root, rotationHandleX, Vector3.right, Color.red, _handleShader);
            var axisY = new GameObject("RotationAxis.Y (XZ)").AddComponent<RotationAxis>().Initialize(transformHandle, transformHandleCamera, root, rotationHandleY, Vector3.up, Color.green, _handleShader);
            var axisZ = new GameObject("RotationAxis.Z (XY)").AddComponent<RotationAxis>().Initialize(transformHandle, transformHandleCamera, root, rotationHandleZ, Vector3.forward, Color.blue, _handleShader);
        }

        public void Reset(Transform transformHandle)
		{
            transformHandle.localPosition = _decal.DecalTransform.localPosition;
            transformHandle.localRotation = _decal.DecalTransform.localRotation;
		}

		public void OnInteractionEnd()
		{
            _decalInfo.LocalEulerAngles = _decal.DecalTransform.localEulerAngles;
            _plugin.ApplyLocalEulerAngles(_itemId, _decalIndex);
		}
    }
}
