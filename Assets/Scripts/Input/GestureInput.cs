using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailSim.InputSystem
{
    public class GestureInput : MonoBehaviour
    {
        [SerializeField] private float tapThresholdTime = 0.25f;
        [SerializeField] private float tapThresholdDistance = 20f;

        public event Action<Vector2> TapPerformed;
        public event Action<Vector2> PanDelta;

        private bool _pointerActive;
        private Vector2 _startPosition;
        private float _startTime;
        private Vector2 _lastPosition;

        private void Update()
        {
            if (TryGetPointer(out var position, out var phase))
            {
                switch (phase)
                {
                    case PointerPhase.Started:
                        _pointerActive = true;
                        _startPosition = position;
                        _lastPosition = position;
                        _startTime = Time.time;
                        break;
                    case PointerPhase.Moved when _pointerActive:
                        var delta = position - _lastPosition;
                        if (delta.sqrMagnitude > 0.01f)
                        {
                            PanDelta?.Invoke(delta);
                        }
                        _lastPosition = position;
                        break;
                    case PointerPhase.Ended when _pointerActive:
                        var elapsed = Time.time - _startTime;
                        var travel = Vector2.Distance(position, _startPosition);
                        if (elapsed <= tapThresholdTime && travel <= tapThresholdDistance)
                        {
                            TapPerformed?.Invoke(position);
                        }
                        _pointerActive = false;
                        break;
                }
            }
            else
            {
                _pointerActive = false;
            }
        }

        private bool TryGetPointer(out Vector2 position, out PointerPhase phase)
        {
            if (TryReadTouchscreen(out position, out phase))
            {
                return true;
            }

            if (TryReadMouse(out position, out phase))
            {
                return true;
            }

            position = Vector2.zero;
            phase = PointerPhase.None;
            return false;
        }

        private static bool TryReadTouchscreen(out Vector2 position, out PointerPhase phase)
        {
            var touchScreen = Touchscreen.current;
            if (touchScreen == null)
            {
                position = Vector2.zero;
                phase = PointerPhase.None;
                return false;
            }

            var touchControl = touchScreen.primaryTouch;
            position = touchControl.position.ReadValue();

            if (touchControl.press.wasPressedThisFrame)
            {
                phase = PointerPhase.Started;
                return true;
            }

            if (touchControl.press.isPressed)
            {
                phase = PointerPhase.Moved;
                return true;
            }

            if (touchControl.press.wasReleasedThisFrame)
            {
                phase = PointerPhase.Ended;
                return true;
            }

            phase = PointerPhase.None;
            return false;
        }

        private static bool TryReadMouse(out Vector2 position, out PointerPhase phase)
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                position = Vector2.zero;
                phase = PointerPhase.None;
                return false;
            }

            position = mouse.position.ReadValue();
            var button = mouse.leftButton;

            if (button.wasPressedThisFrame)
            {
                phase = PointerPhase.Started;
                return true;
            }

            if (button.isPressed)
            {
                phase = PointerPhase.Moved;
                return true;
            }

            if (button.wasReleasedThisFrame)
            {
                phase = PointerPhase.Ended;
                return true;
            }

            phase = PointerPhase.None;
            return false;
        }
    }

    public enum PointerPhase
    {
        None,
        Started,
        Moved,
        Ended
    }
}

