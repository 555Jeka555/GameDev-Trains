using UnityEngine;

namespace RailSim.Rendering
{
    public static class PrimitiveSpriteFactory
    {
        private static Sprite _square;

        public static Sprite Square
        {
            get
            {
                if (_square != null)
                {
                    return _square;
                }

                var texture = Texture2D.whiteTexture;
                _square = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    texture.width,
                    0,
                    SpriteMeshType.FullRect);
                return _square;
            }
        }
    }
}

