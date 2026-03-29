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
        private ScaleAxis _axisX;
        private ScaleAxis _axisY;
        private ScaleAxis _axisZ;

        public ScaleHandle Initialize(RuntimeTransformHandle transformHandle, Shader handleShader)
        {
            _transformHandle = transformHandle;

            transform.SetParent(_transformHandle.transform, false);

            if (_transformHandle.axes == HandleAxes.X ||
                _transformHandle.axes == HandleAxes.XY ||
                _transformHandle.axes == HandleAxes.XZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _axisX = new GameObject("ScaleAxis.X").AddComponent<ScaleAxis>().Initialize(_transformHandle, this, Vector3.right, Color.red, handleShader);
            }

            if (_transformHandle.axes == HandleAxes.Y ||
                _transformHandle.axes == HandleAxes.XY ||
                _transformHandle.axes == HandleAxes.YZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _axisY = new GameObject("ScaleAxis.Y").AddComponent<ScaleAxis>().Initialize(_transformHandle, this, Vector3.up, Color.green, handleShader);
            }

            if (_transformHandle.axes == HandleAxes.Z ||
                _transformHandle.axes == HandleAxes.XZ ||
                _transformHandle.axes == HandleAxes.YZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                _axisZ = new GameObject("ScaleAxis.Z").AddComponent<ScaleAxis>().Initialize(_transformHandle, this, Vector3.forward, Color.blue, handleShader);
            }

            if (_transformHandle.axes == HandleAxes.XZ ||
                _transformHandle.axes == HandleAxes.XYZ)
            {
                new GameObject("ScalePlane.XZ").AddComponent<ScalePlane>().Initialize(_transformHandle, this, _axisX, _axisZ, Vector3.up, Color.green, handleShader);
            }

            return this;
        }

		public static Vector3 CalculateScale(Vector3 startScale, Vector3 mask, float scale)
		{
			return Vector3.Scale(startScale, Vector3.one + mask * (scale - 1));
		}
    }
}
