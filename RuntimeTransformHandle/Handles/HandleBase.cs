using System;
using System.IO;
using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public abstract class HandleBase : MonoBehaviour
    {
        protected RuntimeTransformHandle _transformHandle;
        protected Color _defaultColor;
        protected Material _material;

		public Transform Target => _transformHandle.target;
		public Transform TransformHandle => _transformHandle.transform;

        protected void InitializeMaterial(Shader shader)
        {
            _material = new Material(shader);
            _material.color = _defaultColor;
        }

        public void SetDefaultColor()
        {
            _material.color = _defaultColor;
        }

        public void SetColor(Color color)
        {
            _material.color = color;
        }

        public virtual void StartInteraction(Vector3 p_hitPoint)
        {

        }

        public virtual bool CanInteract(Vector3 p_hitPoint)
        {
            return true;
        }

        public virtual void Interact(Vector3 p_previousPosition)
        {

        }

        public virtual void EndInteraction()
        {
            SetDefaultColor();
        }

        public static Vector3 GetVectorFromAxes(HandleAxes p_axes)
        {
            return p_axes switch
            {
                HandleAxes.X => new Vector3(1,0,0),
                HandleAxes.Y => new Vector3(0,1,0),
                HandleAxes.Z => new Vector3(0,0,1),
                HandleAxes.XY => new Vector3(1,1,0),
                HandleAxes.XZ => new Vector3(1,0,1),
                HandleAxes.YZ => new Vector3(0,1,1),
                HandleAxes.XYZ => new Vector3(1,1,1),
				_ => throw new ArgumentException($"Unknown HandleAxes: {p_axes}")
            };
        }
    }
}
