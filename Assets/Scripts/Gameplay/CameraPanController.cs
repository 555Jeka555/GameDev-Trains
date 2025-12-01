using RailSim.InputSystem;
using UnityEngine;

namespace RailSim.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public class CameraPanController : MonoBehaviour
    {
        [SerializeField] private float panSpeed = 0.015f;
        [SerializeField] private float zoomSpeed = 0.01f;
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 15f;
        [SerializeField] private float padding = 5f;

        private Camera _camera;
        private GestureInput _boundInput;
        private Vector2 _minBounds = new(-50f, -50f);
        private Vector2 _maxBounds = new(50f, 50f);

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 8f;
        }

        public void BindInput(GestureInput input)
        {
            if (_boundInput != null)
            {
                _boundInput.PanDelta -= HandlePan;
                _boundInput.PinchDelta -= HandlePinch;
            }

            _boundInput = input;
            if (_boundInput != null)
            {
                _boundInput.PanDelta += HandlePan;
                _boundInput.PinchDelta += HandlePinch;
            }
        }

        public void SetBoundsFromNodes(Vector3[] worldPositions)
        {
            if (worldPositions == null || worldPositions.Length == 0)
            {
                return;
            }

            var min = worldPositions[0];
            var max = worldPositions[0];

            foreach (var pos in worldPositions)
            {
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }

            _minBounds = new Vector2(min.x - padding, min.y - padding);
            _maxBounds = new Vector2(max.x + padding, max.y + padding);

            // Center camera on map
            var center = (min + max) * 0.5f;
            transform.position = new Vector3(center.x, center.y, transform.position.z);

            // Auto-adjust zoom to fit the level
            var mapWidth = max.x - min.x + padding * 2f;
            var mapHeight = max.y - min.y + padding * 2f;
            var aspect = _camera.aspect;
            var requiredSize = Mathf.Max(mapHeight * 0.5f, mapWidth * 0.5f / aspect);
            _camera.orthographicSize = Mathf.Clamp(requiredSize, minZoom, maxZoom);
        }

        public void FocusOn(Vector3 worldPosition)
        {
            var target = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            target.x = Mathf.Clamp(target.x, _minBounds.x, _maxBounds.x);
            target.y = Mathf.Clamp(target.y, _minBounds.y, _maxBounds.y);
            transform.position = target;
        }

        private void HandlePan(Vector2 screenDelta)
        {
            var scaledSpeed = panSpeed * (_camera.orthographicSize / 5f);
            var offset = new Vector3(-screenDelta.x * scaledSpeed, -screenDelta.y * scaledSpeed, 0f);
            var target = transform.position + offset;
            target.x = Mathf.Clamp(target.x, _minBounds.x, _maxBounds.x);
            target.y = Mathf.Clamp(target.y, _minBounds.y, _maxBounds.y);
            transform.position = target;
        }

        private void HandlePinch(float delta)
        {
            var newSize = _camera.orthographicSize - delta * zoomSpeed;
            _camera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
}

