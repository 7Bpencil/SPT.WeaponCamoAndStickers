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

		public Transform Target => _transformHandle.targetTransform;
		public Transform TransformHandle => _transformHandle.handleTransform;

        protected void InitializeMaterial(Shader shader)
        {
            _material = new Material(shader);
            _material.color = _defaultColor;
        }

        public void SetDefaultColor()
        {
            _material.color = _defaultColor;
        }

        public void SetInteractionColor()
        {
            _material.color = Color.yellow;
        }

        public abstract bool CanInteract(Vector3 hitPoint);

        public abstract void StartInteraction();

        public abstract void Interact();

        public abstract void EndInteraction();

        protected virtual void OnDestroy()
        {
            if (_material) Destroy(_material);
        }
    }
}
