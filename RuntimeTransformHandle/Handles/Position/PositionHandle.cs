using System.Collections.Generic;
using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class PositionHandle : MonoBehaviour
    {
        private RuntimeTransformHandle _transformHandle;
        private List<PositionAxis> _axes;
        private List<PositionPlane> _planes;

        public PositionHandle Initialize(RuntimeTransformHandle transformHandle, Shader handleShader)
        {
            _transformHandle = transformHandle;

            transform.SetParent(_transformHandle.transform, false);

            _axes = new List<PositionAxis>();

            if (_transformHandle.axes == HandleAxes.X ||
                _transformHandle.axes == HandleAxes.XY ||
                _transformHandle.axes == HandleAxes.XZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _axes.Add(new GameObject("PositionAxis.X").AddComponent<PositionAxis>().Initialize(_transformHandle, this, Vector3.right, Color.red, handleShader));
            }

            if (_transformHandle.axes == HandleAxes.Y ||
                _transformHandle.axes == HandleAxes.XY ||
                _transformHandle.axes == HandleAxes.YZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _axes.Add(new GameObject("PositionAxis.Y").AddComponent<PositionAxis>().Initialize(_transformHandle, this, Vector3.up, Color.green, handleShader));
            }

            if (_transformHandle.axes == HandleAxes.Z ||
                _transformHandle.axes == HandleAxes.XZ ||
                _transformHandle.axes == HandleAxes.YZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _axes.Add(new GameObject("PositionAxis.Z").AddComponent<PositionAxis>().Initialize(_transformHandle, this, Vector3.forward, Color.blue, handleShader));
            }

            _planes = new List<PositionPlane>();

            if (_transformHandle.axes == HandleAxes.XY ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _planes.Add(new GameObject("PositionPlane.XY").AddComponent<PositionPlane>().Initialize(_transformHandle, this, Vector3.right, Vector3.up, Vector3.forward, Color.blue, handleShader));
            }

            if (_transformHandle.axes == HandleAxes.YZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _planes.Add(new GameObject("PositionPlane.YZ").AddComponent<PositionPlane>().Initialize(_transformHandle, this, Vector3.up, Vector3.forward, Vector3.right, Color.red, handleShader));
            }

            if (_transformHandle.axes == HandleAxes.XZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _planes.Add(new GameObject("PositionPlane.XZ").AddComponent<PositionPlane>().Initialize(_transformHandle, this, Vector3.right, Vector3.forward, Vector3.up, Color.green, handleShader));
            }

            return this;
        }
    }
}
