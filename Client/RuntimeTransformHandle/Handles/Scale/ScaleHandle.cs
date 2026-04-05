using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class ScaleHandle : MonoBehaviour
    {
        public ScaleHandle Initialize(RuntimeTransformHandle transformHandle, Shader handleShader)
        {
            transform.SetParent(transformHandle.transform, false);

            var axisX = new GameObject("ScaleAxis.X").AddComponent<ScaleAxis>().Initialize(transformHandle, this, Vector3.right, Color.red, handleShader);
            var axisY = new GameObject("ScaleAxis.Y").AddComponent<ScaleAxis>().Initialize(transformHandle, this, Vector3.up, Color.green, handleShader);
            var axisZ = new GameObject("ScaleAxis.Z").AddComponent<ScaleAxis>().Initialize(transformHandle, this, Vector3.forward, Color.blue, handleShader);
            var planeXZ = new GameObject("ScalePlane.XZ").AddComponent<ScalePlane>().Initialize(transformHandle, this, axisX, axisZ, Vector3.up, Color.green, handleShader);

            return this;
        }

		public static Vector3 CalculateScale(Vector3 startScale, Vector3 mask, float scale)
		{
			return Vector3.Scale(startScale, Vector3.one + mask * (scale - 1));
		}
    }
}
