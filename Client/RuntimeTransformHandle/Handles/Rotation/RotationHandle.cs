using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class RotationHandle : MonoBehaviour
    {
        public RotationHandle Initialize(RuntimeTransformHandle transformHandle, Shader handleShader)
        {
            transform.SetParent(transformHandle.transform, false);

            var axisX = new GameObject("RotationAxis.X (YZ)").AddComponent<RotationAxis>().Initialize(transformHandle, this, Vector3.up, Vector3.forward, Vector3.right, Color.red, handleShader);
            var axisY = new GameObject("RotationAxis.Y (XZ)").AddComponent<RotationAxis>().Initialize(transformHandle, this, Vector3.right, Vector3.forward, Vector3.up, Color.green, handleShader);
            var axisZ = new GameObject("RotationAxis.Z (XY)").AddComponent<RotationAxis>().Initialize(transformHandle, this, Vector3.right, Vector3.up, Vector3.forward, Color.blue, handleShader);

            return this;
        }
    }
}
