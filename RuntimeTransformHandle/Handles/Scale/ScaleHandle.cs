using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class ScaleHandle : MonoBehaviour
    {
        private RuntimeTransformHandle _transformHandle;
        private List<ScaleAxis> _axes;
        private ScaleGlobal _globalAxis;

        public ScaleHandle Initialize(RuntimeTransformHandle transformHandle, Shader handleShader)
        {
            _transformHandle = transformHandle;
            transform.SetParent(_transformHandle.transform, false);

            _axes = new List<ScaleAxis>();

            if (_transformHandle.axes == HandleAxes.X ||
                _transformHandle.axes == HandleAxes.XY ||
                _transformHandle.axes == HandleAxes.XZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _axes.Add(new GameObject("ScaleAxis.X").AddComponent<ScaleAxis>().Initialize(_transformHandle, this, Vector3.right, Color.red, handleShader));
            }

            if (_transformHandle.axes == HandleAxes.Y ||
                _transformHandle.axes == HandleAxes.XY ||
                _transformHandle.axes == HandleAxes.YZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _axes.Add(new GameObject("ScaleAxis.Y").AddComponent<ScaleAxis>().Initialize(_transformHandle, this, Vector3.up, Color.green, handleShader));
            }

            if (_transformHandle.axes == HandleAxes.Z ||
                _transformHandle.axes == HandleAxes.XZ ||
                _transformHandle.axes == HandleAxes.YZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _axes.Add(new GameObject("ScaleAxis.Z").AddComponent<ScaleAxis>().Initialize(_transformHandle, this, Vector3.forward, Color.blue, handleShader));
            }

            if (_transformHandle.axes != HandleAxes.X &&
                _transformHandle.axes != HandleAxes.Y &&
                _transformHandle.axes != HandleAxes.Z)
            {
                _globalAxis = new GameObject("ScaleGlobal").AddComponent<ScaleGlobal>().Initialize(_transformHandle, this, _axes, HandleBase.GetVectorFromAxes(_transformHandle.axes), Color.white, handleShader);
            }

            return this;
        }
    }
}
