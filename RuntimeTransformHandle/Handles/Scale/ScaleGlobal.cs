using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RuntimeHandle
{
    /**
     * Created by Peter @sHTiF Stefcek 20.10.2020
     * Rewritten by 7Bpencil 22.03.2026
     */
    public class ScaleGlobal : HandleBase
    {
        private List<ScaleAxis> _axes;
        private Vector3 _axis;
        private float _offsetLength;
        private Vector3 _startScale;

        public ScaleGlobal Initialize(RuntimeTransformHandle transformHandle, ScaleHandle scaleHandle, List<ScaleAxis> axes, Vector3 axis, Color color, Shader handleShader)
        {
            _transformHandle = transformHandle;
            _axes = axes;
            _axis = axis;
            _defaultColor = color;

            InitializeMaterial(handleShader);

            transform.SetParent(scaleHandle.transform, false);

            var o = new GameObject("ScaleGlobal");
            o.transform.SetParent(transform, false);
            o.AddComponent<MeshRenderer>().material = _material;
            o.AddComponent<MeshFilter>().mesh = MeshUtils.CreateBox(.35f, .35f, .35f);
            o.AddComponent<MeshCollider>();

            return this;
        }

        public override void Interact(Vector3 p_previousPosition)
        {
            base.Interact(p_previousPosition);

            var startPosition = _transformHandle.handleCamera.WorldToScreenPoint(Target.position);
            var offset = Input.mousePosition - startPosition;

            var delta = offset.magnitude / _offsetLength - 1f;
            var newScale = Vector3.Scale(_startScale, _axis * delta + Vector3.one);

            Target.localScale = newScale;

            SetHandleVisualScale(delta);
        }

        public override void StartInteraction(Vector3 p_hitPoint)
        {
            base.StartInteraction(p_hitPoint);

            var startPosition = _transformHandle.handleCamera.WorldToScreenPoint(Target.position);
            var offset = Input.mousePosition - startPosition;

            _offsetLength = offset.magnitude;
            _startScale = Target.localScale;

            SetHandleVisualScale(0);
            SetHandlesInteractionColor();
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            SetHandleVisualScale(0);
            SetHandlesDefaultColor();
        }

        public void SetHandleVisualScale(float delta)
        {
            foreach (var axis in _axes)
            {
                axis.SetHandleVisualScale(delta);
            }
        }

        public void SetHandlesInteractionColor()
        {
            foreach (var axis in _axes)
            {
                axis.SetColor(Color.yellow);
            }
        }

        public void SetHandlesDefaultColor()
        {
            foreach (var axis in _axes)
            {
                axis.SetDefaultColor();
            }
        }
    }
}
