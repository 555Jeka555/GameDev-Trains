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
            // Container for arrow elements
            var go = new GameObject("FinishArrow");
            go.transform.SetParent(transform, false);
            _arrowRect = go.AddComponent<RectTransform>();
            _arrowRect.sizeDelta = new Vector2(80f, 80f);
            _arrowRect.pivot = new Vector2(0.5f, 0.5f);
            
            // Circle background
            var circleBg = new GameObject("CircleBg");
            circleBg.transform.SetParent(go.transform, false);
            var circleRect = circleBg.AddComponent<RectTransform>();
            circleRect.sizeDelta = new Vector2(70f, 70f);
            var circleImage = circleBg.AddComponent<Image>();
            circleImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            
            // Flag emoji
            var flagGo = new GameObject("Flag");
            flagGo.transform.SetParent(go.transform, false);
            var flagRect = flagGo.AddComponent<RectTransform>();
            flagRect.sizeDelta = new Vector2(60f, 60f);
            var flagText = flagGo.AddComponent<Text>();
            flagText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            flagText.fontSize = 40;
            flagText.alignment = TextAnchor.MiddleCenter;
            flagText.text = "🏁";
            
            // Direction arrow (triangle indicator)
            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(go.transform, false);
            var arrowObjRect = arrowGo.AddComponent<RectTransform>();
            arrowObjRect.sizeDelta = new Vector2(20f, 20f);
            arrowObjRect.anchoredPosition = new Vector2(0f, -45f);
            _arrowImage = arrowGo.AddComponent<Image>();
            _arrowImage.color = AccentColor;
            
            // Pulsing outline
            var outline = circleImage.gameObject.AddComponent<Outline>();
            outline.effectColor = AccentColor;
            outline.effectDistance = new Vector2(3f, 3f);
            
            _arrowRect.gameObject.SetActive(false);
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

            // Check if flag is on screen with some margin
            var margin = 0.1f;
            var onScreen = viewport.x >= margin && viewport.x <= (1f - margin) && 
                          viewport.y >= margin && viewport.y <= (1f - margin);

            // Hide arrow when flag is visible on screen
            if (onScreen)
            {
                _arrowRect.gameObject.SetActive(false);
                return;
            }

            // Position arrow at screen edge pointing towards finish
            var edgeMargin = 0.08f;
            var clampedX = Mathf.Clamp(viewport.x, edgeMargin, 1f - edgeMargin);
            var clampedY = Mathf.Clamp(viewport.y, edgeMargin, 1f - edgeMargin);
            var screenPoint = new Vector2(clampedX * Screen.width, clampedY * Screen.height);
            
            // Rotate arrow to point towards finish
            var direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            
            // Rotate only the direction indicator, not the whole container
            if (_arrowImage != null)
            {
                _arrowImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
                _arrowImage.rectTransform.anchoredPosition = new Vector2(
                    Mathf.Cos((angle + 90f) * Mathf.Deg2Rad) * 45f,
                    Mathf.Sin((angle + 90f) * Mathf.Deg2Rad) * 45f
                );
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

