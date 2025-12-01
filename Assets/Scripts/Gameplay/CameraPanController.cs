using RailSim.InputSystem;
using UnityEngine;

namespace RailSim.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public class CameraPanController : MonoBehaviour
    {
        [SerializeField] private float panSpeed = 0.01f;
        [SerializeField] private Vector2 minBounds = new(-10f, -10f);
        [SerializeField] private Vector2 maxBounds = new(10f, 10f);

        private Camera _camera;
        private GestureInput _boundInput;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
        }

        public void BindInput(GestureInput input)
        {
            if (_boundInput != null)
            {
                _boundInput.PanDelta -= HandlePan;
            }

            _boundInput = input;
            if (_boundInput != null)
            {
                _boundInput.PanDelta += HandlePan;
            }
        }

        private void HandlePan(Vector2 screenDelta)
        {
            var offset = new Vector3(-screenDelta.x * panSpeed, -screenDelta.y * panSpeed, 0f);
            var target = transform.position + offset;
            target.x = Mathf.Clamp(target.x, minBounds.x, maxBounds.x);
            target.y = Mathf.Clamp(target.y, minBounds.y, maxBounds.y);
            transform.position = target;
        }
    }
}

