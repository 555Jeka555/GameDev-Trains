using RailSim.Gameplay;
using UnityEngine;

namespace RailSim.Rendering
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TrackView : MonoBehaviour
    {
        private const float DefaultWidth = 0.18f;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = PrimitiveSpriteFactory.Square;
            _renderer.drawMode = SpriteDrawMode.Sliced;
            _renderer.sortingOrder = 0;
            _renderer.color = new Color(0.18f, 0.18f, 0.18f, 1f);
        }

        public void Bind(RailEdge edge, float width)
        {
            var start = edge.A.WorldPosition;
            var end = edge.B.WorldPosition;
            var center = (start + end) * 0.5f;
            transform.position = center;

            var direction = end - start;
            var length = direction.magnitude;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            var trackWidth = width <= 0f ? DefaultWidth : width;
            _renderer.size = new Vector2(Mathf.Max(0.1f, length), trackWidth);
        }
    }
}

