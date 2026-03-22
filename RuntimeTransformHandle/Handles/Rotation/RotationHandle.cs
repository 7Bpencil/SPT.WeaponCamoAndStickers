using System.Collections.Generic;
using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class RotationHandle : MonoBehaviour
    {
        private RuntimeTransformHandle _parentTransformHandle;
        private List<RotationAxis> _axes;

        public RotationHandle Initialize(RuntimeTransformHandle parentTransformHandle, Shader handleShader)
        {
            _parentTransformHandle = parentTransformHandle;
            transform.SetParent(_parentTransformHandle.transform, false);

            _axes = new List<RotationAxis>();

            if (_parentTransformHandle.axes == HandleAxes.X ||
                _parentTransformHandle.axes == HandleAxes.XY ||
                _parentTransformHandle.axes == HandleAxes.XZ ||
                _parentTransformHandle.axes == HandleAxes.XYZ)
            {
                _axes.Add(new GameObject("RotationAxis.X (YZ)").AddComponent<RotationAxis>().Initialize(_parentTransformHandle, this, Vector3.up, Vector3.forward, Vector3.right, Color.red, handleShader));
            }

            if (_parentTransformHandle.axes == HandleAxes.Y ||
                _parentTransformHandle.axes == HandleAxes.XY ||
                _parentTransformHandle.axes == HandleAxes.YZ ||
                _parentTransformHandle.axes == HandleAxes.XYZ)
            {
                _axes.Add(new GameObject("RotationAxis.Y (XZ)").AddComponent<RotationAxis>().Initialize(_parentTransformHandle, this, Vector3.right, Vector3.forward, Vector3.up, Color.green, handleShader));
            }

            if (_parentTransformHandle.axes == HandleAxes.Z ||
                _parentTransformHandle.axes == HandleAxes.YZ ||
                _parentTransformHandle.axes == HandleAxes.XZ ||
                _parentTransformHandle.axes == HandleAxes.XYZ)
            {
                _axes.Add(new GameObject("RotationAxis.Z (XY)").AddComponent<RotationAxis>().Initialize(_parentTransformHandle, this, Vector3.right, Vector3.up, Vector3.forward, Color.blue, handleShader));
            }

            return this;
        }
    }
}
