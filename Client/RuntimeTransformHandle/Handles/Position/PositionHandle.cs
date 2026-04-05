using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class PositionHandle : MonoBehaviour
    {
        public PositionHandle Initialize(RuntimeTransformHandle transformHandle, Shader handleShader)
        {
            transform.SetParent(transformHandle.transform, false);

            var axisX = new GameObject("PositionAxis.X").AddComponent<PositionAxis>().Initialize(transformHandle, this, Vector3.right, Color.red, handleShader);
            var axisY = new GameObject("PositionAxis.Y").AddComponent<PositionAxis>().Initialize(transformHandle, this, Vector3.up, Color.green, handleShader);
            var axisZ = new GameObject("PositionAxis.Z").AddComponent<PositionAxis>().Initialize(transformHandle, this, Vector3.forward, Color.blue, handleShader);

            var planeXY = new GameObject("PositionPlane.XY").AddComponent<PositionPlane>().Initialize(transformHandle, this, Vector3.right, Vector3.up, Vector3.forward, Color.blue, handleShader);
            var planeYZ = new GameObject("PositionPlane.YZ").AddComponent<PositionPlane>().Initialize(transformHandle, this, Vector3.up, Vector3.forward, Vector3.right, Color.red, handleShader);
            var planeXZ = new GameObject("PositionPlane.XZ").AddComponent<PositionPlane>().Initialize(transformHandle, this, Vector3.right, Vector3.forward, Vector3.up, Color.green, handleShader);

            return this;
        }
    }
}
