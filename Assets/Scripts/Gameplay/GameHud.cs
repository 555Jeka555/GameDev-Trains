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
        private Image _timerBackground;

        // Color scheme matching menu
        private static readonly Color AccentColor = new(0.95f, 0.6f, 0.1f, 1f);
        private static readonly Color PanelColor = new(0.08f, 0.08f, 0.12f, 0.9f);
        private static readonly Color TextColor = new(0.95f, 0.95f, 0.9f, 1f);

        public static GameHud Create(Camera targetCamera)
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = targetCamera;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<GameHud>();
            hud._camera = targetCamera;
            hud.BuildUI();
            return hud;
        }

        private void BuildUI()
        {
            // Timer with background panel
            var timerPanel = CreatePanel("TimerPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0, -60f), new Vector2(320f, 70f));
            _timerBackground = timerPanel.GetComponent<Image>();
            
            _timerText = CreateText("TimerText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60f), 36);
            _timerText.fontStyle = FontStyle.Bold;
            
            // Status text with shadow effect
            _statusText = CreateText("StatusText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 48);
            _statusText.fontStyle = FontStyle.Bold;
            
            // Add shadow to status
            var shadow = _statusText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(3f, -3f);

            _menuButton = CreateStyledButton("MenuButton", new Vector2(0f, 1f), new Vector2(0f, 1f), 
                new Vector2(100f, -60f), new Vector2(160f, 70f), "☰ МЕНЮ");
            _menuButton.onClick.AddListener(() => _menuCallback?.Invoke());
            
            BuildFinishArrow();
        }

        private GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = PanelColor;
            return go;
        }

        private void BuildFinishArrow()
        {
            var go = new GameObject("FinishArrow");
            go.transform.SetParent(transform, false);
            _arrowRect = go.AddComponent<RectTransform>();
            _arrowRect.sizeDelta = new Vector2(50f, 100f);
            _arrowImage = go.AddComponent<Image>();
            _arrowImage.color = AccentColor;
            _arrowImage.sprite = GetArrowSprite();
            _arrowRect.pivot = new Vector2(0.5f, 0.2f);
            _arrowRect.gameObject.SetActive(false);
            
            // Add glow effect
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.8f, 0.3f, 0.5f);
            outline.effectDistance = new Vector2(4f, 4f);
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
            text.color = TextColor;
            text.text = "";
            return text;
        }

        private Button CreateStyledButton(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Vector2 size, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = PanelColor;
            
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = PanelColor;
            colors.highlightedColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextColor;
            text.text = label;
            text.fontStyle = FontStyle.Bold;

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
            var icon = planningPhase ? "⏱️" : "🚂";
            var label = planningPhase ? "ПЛАН" : "ПУТЬ";
            _timerText.text = $"{icon} {label}: {Mathf.Max(0f, seconds):0.0}с";
            
            // Change background color based on phase
            if (_timerBackground != null)
            {
                _timerBackground.color = planningPhase 
                    ? new Color(0.1f, 0.4f, 0.15f, 0.9f)  // Green for planning
                    : PanelColor;
            }
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

