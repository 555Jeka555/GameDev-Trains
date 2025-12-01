using RailSim.Core;
using RailSim.Gameplay;
using UnityEngine;

namespace RailSim.Rendering
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class TrainView : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Rigidbody2D _rigidbody;
        private CircleCollider2D _collider;
        private TrainRuntime _runtime;

        public TrainRuntime Runtime => _runtime;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = PrimitiveSpriteFactory.Square;
            _renderer.drawMode = SpriteDrawMode.Sliced;
            _renderer.size = new Vector2(0.9f, 0.35f);
            _renderer.sortingOrder = 3;

            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.isKinematic = true;
            _rigidbody.gravityScale = 0f;

            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _collider.radius = 0.35f;
        }

        public void Bind(TrainRuntime runtime)
        {
            _runtime = runtime;
            var color = runtime.Blueprint.kind == TrainKind.Passenger
                ? new Color(0.2f, 0.6f, 1f)
                : new Color(0.9f, 0.6f, 0.1f);
            _renderer.color = color;
        }

        private void LateUpdate()
        {
            if (_runtime == null)
            {
                return;
            }

            transform.position = _runtime.GetWorldPosition();
            var direction = _runtime.GetDirection();
            if (direction.sqrMagnitude > 0.001f)
            {
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}

