using UnityEngine;
using UnityEngine.UI;

namespace RailSim.Gameplay
{
    public class GameHud : MonoBehaviour
    {
        private Text _timerText;
        private Text _statusText;
        private Button _menuButton;
        private RectTransform _arrowRect;
        private Image _arrowImage;
        private Camera _camera;
        private Vector3? _finishWorldPosition;
        private System.Action _menuCallback;
        private Sprite _arrowSprite;

        public static GameHud Create(Camera targetCamera)
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = targetCamera;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<GameHud>();
            hud._camera = targetCamera;
            hud.BuildUI();
            return hud;
        }

        private void BuildUI()
        {
            _timerText = CreateText("TimerText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40f), 32);
            _statusText = CreateText("StatusText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 40);
            _menuButton = CreateButton("MenuButton", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -80f), "МЕНЮ");
            _menuButton.onClick.AddListener(() => _menuCallback?.Invoke());
            BuildFinishArrow();
        }

        private void BuildFinishArrow()
        {
            var go = new GameObject("FinishArrow");
            go.transform.SetParent(transform, false);
            _arrowRect = go.AddComponent<RectTransform>();
            _arrowRect.sizeDelta = new Vector2(60f, 120f);
            _arrowImage = go.AddComponent<Image>();
            _arrowImage.color = new Color(1f, 0.8f, 0.2f, 0.85f);
            _arrowImage.sprite = GetArrowSprite();
            _arrowRect.pivot = new Vector2(0.5f, 0.2f);
            _arrowRect.gameObject.SetActive(false);
        }

        private Sprite GetArrowSprite()
        {
            if (_arrowSprite != null)
            {
                return _arrowSprite;
            }

            var texture = Texture2D.whiteTexture;
            _arrowSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width,
                0,
                SpriteMeshType.FullRect);
            return _arrowSprite;
        }

        private Text CreateText(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(600f, 100f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "";
            return text;
        }

        private Button CreateButton(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(200f, 80f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.45f, 0.9f, 0.9f);
            var button = go.AddComponent<Button>();

            var text = CreateText(name + "_Label", Vector2.zero, Vector2.one, Vector2.zero, 26);
            text.transform.SetParent(go.transform, false);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            text.text = label;

            return button;
        }

        private void Update()
        {
            UpdateArrow();
        }

        private void UpdateArrow()
        {
            if (_arrowRect == null || !_finishWorldPosition.HasValue || _camera == null)
            {
                if (_arrowRect != null)
                {
                    _arrowRect.gameObject.SetActive(false);
                }
                return;
            }

            var world = _finishWorldPosition.Value;
            var viewport = _camera.WorldToViewportPoint(world);
            if (viewport.z < 0f)
            {
                _arrowRect.gameObject.SetActive(false);
                return;
            }

            var onScreen = viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
            var screenPoint = new Vector2(viewport.x * Screen.width, viewport.y * Screen.height);

            if (!onScreen)
            {
                viewport.x = Mathf.Clamp(viewport.x, 0.05f, 0.95f);
                viewport.y = Mathf.Clamp(viewport.y, 0.05f, 0.95f);
                screenPoint = new Vector2(viewport.x * Screen.width, viewport.y * Screen.height);
                var direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                _arrowRect.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                _arrowRect.rotation = Quaternion.identity;
            }

            _arrowRect.position = screenPoint;
            _arrowRect.gameObject.SetActive(true);
        }

        public void SetMenuCallback(System.Action callback)
        {
            _menuCallback = callback;
        }

        public void UpdateTimer(float seconds, bool planningPhase)
        {
            var label = planningPhase ? "ПОДГОТОВКА" : "МАРШРУТ";
            _timerText.text = $"{label}: {Mathf.Max(0f, seconds):00.0}s";
        }

        public void UpdateStatus(string status)
        {
            _statusText.text = status;
        }

        public void SetFinishTarget(Vector3? worldPosition)
        {
            _finishWorldPosition = worldPosition;
            if (!_finishWorldPosition.HasValue && _arrowRect != null)
            {
                _arrowRect.gameObject.SetActive(false);
            }
        }
    }
}

