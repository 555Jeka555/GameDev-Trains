using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RailSim.Gameplay
{
    public class GameMenu : MonoBehaviour
    {
        private CanvasGroup _group;
        private Text _titleText;
        private Button _restartButton;
        private Text _messageText;
        private RailGameController _controller;
        private string _levelName;
        private RectTransform _levelButtonContainer;
        private readonly List<Button> _levelButtons = new();
        private Image _panelImage;

        // Beautiful color scheme
        private static readonly Color PanelColor = new(0.08f, 0.08f, 0.12f, 0.95f);
        private static readonly Color AccentColor = new(0.95f, 0.6f, 0.1f, 1f);
        private static readonly Color ButtonColor = new(0.15f, 0.15f, 0.22f, 1f);
        private static readonly Color ButtonHoverColor = new(0.25f, 0.25f, 0.35f, 1f);
        private static readonly Color TextColor = new(0.95f, 0.95f, 0.9f, 1f);
        private static readonly Color SubtextColor = new(0.7f, 0.7f, 0.75f, 1f);

        public static GameMenu Create(Camera camera, RailGameController controller, string levelName)
        {
            EnsureEventSystemExists();

            var canvasGo = new GameObject("Menu");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var menu = canvasGo.AddComponent<GameMenu>();
            menu._controller = controller;
            menu._levelName = levelName;
            menu.BuildUI();
            return menu;
        }

        private static void EnsureEventSystemExists()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUI()
        {
            _group = gameObject.AddComponent<CanvasGroup>();

            // Background overlay
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(transform, false);
            var overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.5f);

            // Main panel with rounded corners effect
            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(700f, 650f);
            rect.anchoredPosition = Vector2.zero;

            _panelImage = panel.AddComponent<Image>();
            _panelImage.color = PanelColor;

            // Decorative top accent line
            var accentLine = new GameObject("AccentLine");
            accentLine.transform.SetParent(panel.transform, false);
            var accentRect = accentLine.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.sizeDelta = new Vector2(0f, 6f);
            accentRect.anchoredPosition = Vector2.zero;
            var accentImage = accentLine.AddComponent<Image>();
            accentImage.color = AccentColor;

            // Train icon/emoji
            var iconText = CreateText(panel.transform, "Icon", 72, new Vector2(0, 220), TextAnchor.MiddleCenter);
            iconText.text = "🚂";

            _titleText = CreateText(panel.transform, "Title", 52, new Vector2(0, 140), TextAnchor.MiddleCenter);
            _titleText.text = "ПОЕЗДА";
            _titleText.color = AccentColor;
            _titleText.fontStyle = FontStyle.Bold;

            _messageText = CreateText(panel.transform, "Message", 28, new Vector2(0, 70), TextAnchor.MiddleCenter);
            _messageText.color = SubtextColor;

            _levelButtonContainer = new GameObject("LevelButtons").AddComponent<RectTransform>();
            _levelButtonContainer.SetParent(panel.transform, false);
            _levelButtonContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _levelButtonContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _levelButtonContainer.anchoredPosition = new Vector2(0, -60f);
            _levelButtonContainer.sizeDelta = new Vector2(500f, 350f);

            _restartButton = CreateStyledButton(panel.transform, "🔄 РЕСТАРТ", new Vector2(0, -180), true);
            _restartButton.onClick.AddListener(() => _controller.RequestRestart());
            _restartButton.gameObject.SetActive(false);
        }

        public void SetLevelDefinitions(LevelDefinition[] definitions)
        {
            foreach (var button in _levelButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _levelButtons.Clear();

            if (definitions == null || definitions.Length == 0)
            {
                _levelButtonContainer.gameObject.SetActive(false);
                return;
            }

            _levelButtonContainer.gameObject.SetActive(true);
            var spacing = 110f;
            for (var i = 0; i < definitions.Length; i++)
            {
                var entry = definitions[i];
                var icon = i == 0 ? "📖 " : "🗺️ ";
                var button = CreateStyledButton(_levelButtonContainer, icon + entry.displayName.ToUpperInvariant(), 
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -i * spacing), false);
                button.onClick.AddListener(() => _controller.RequestStart(entry));
                _levelButtons.Add(button);
            }

            var height = Mathf.Max(spacing * definitions.Length, 120f);
            _levelButtonContainer.sizeDelta = new Vector2(_levelButtonContainer.sizeDelta.x, height);
        }

        private Text CreateText(Transform parent, string name, int fontSize, Vector2 offset, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600f, 100f);
            rect.anchoredPosition = offset;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = TextColor;
            text.text = "";
            return text;
        }

        private Button CreateStyledButton(Transform parent, string label, Vector2 offset, bool isAccent)
        {
            return CreateStyledButton(parent, label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, isAccent);
        }

        private Button CreateStyledButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, bool isAccent)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = new Vector2(480f, 90f);
            rect.anchoredPosition = offset;

            var image = go.AddComponent<Image>();
            image.color = isAccent ? AccentColor : ButtonColor;

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = isAccent ? AccentColor : ButtonColor;
            colors.highlightedColor = isAccent ? new Color(1f, 0.7f, 0.2f, 1f) : ButtonHoverColor;
            colors.pressedColor = isAccent ? new Color(0.8f, 0.5f, 0.1f, 1f) : new Color(0.12f, 0.12f, 0.18f, 1f);
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
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = isAccent ? new Color(0.1f, 0.1f, 0.1f, 1f) : TextColor;
            text.text = label;
            text.fontStyle = FontStyle.Bold;

            return button;
        }

        public void ShowMainMenu()
        {
            SetVisible(true);
            _titleText.text = "🚂 ПОЕЗДА";
            _messageText.text = "Переключай стрелки и доведи поезд до финиша!";
            _levelButtonContainer.gameObject.SetActive(true);
            _restartButton.gameObject.SetActive(false);
        }

        public void ShowRestartMenu(string message)
        {
            SetVisible(true);
            _messageText.text = message;
            _levelButtonContainer.gameObject.SetActive(false);
            _restartButton.gameObject.SetActive(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_group == null)
            {
                return;
            }

            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }
    }
}

