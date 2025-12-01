using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;
using UnityEngine.UI;
using RailSim.Rendering;

namespace RailSim.Gameplay
{
    public class GameMenu : MonoBehaviour
    {
        private CanvasGroup _group;
        private Text _titleText;
        private Button _restartButton;
        private Button _nextLevelButton;
        private Button _menuButton;
        private Button _settingsButton;
        private Text _messageText;
        private RailGameController _controller;
        private string _levelName;
        private RectTransform _levelButtonContainer;
        private RectTransform _settingsPanel;
        private readonly List<Button> _levelButtons = new();
        private readonly List<Button> _themeButtons = new();
        private Image _panelImage;
        private LevelDefinition[] _levelDefinitions;
        private bool _showingSettings;

        // Beautiful color scheme
        private static readonly Color PanelColor = new(0.08f, 0.08f, 0.12f, 0.95f);
        private static readonly Color AccentColor = new(0.95f, 0.6f, 0.1f, 1f);
        private static readonly Color SuccessColor = new(0.2f, 0.75f, 0.3f, 1f);
        private static readonly Color ButtonColor = new(0.15f, 0.15f, 0.22f, 1f);
        private static readonly Color ButtonHoverColor = new(0.25f, 0.25f, 0.35f, 1f);
        private static readonly Color TextColor = new(0.95f, 0.95f, 0.9f, 1f);
        private static readonly Color SubtextColor = new(0.7f, 0.7f, 0.75f, 1f);
        private static readonly Color CompletedColor = new(0.3f, 0.8f, 0.4f, 1f);

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

            // Main panel with rounded corners effect - INCREASED SIZE
            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, 1000f);
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

            // Train icon/emoji - MOVED HIGHER
            var iconText = CreateText(panel.transform, "Icon", 72, new Vector2(0, 420), TextAnchor.MiddleCenter);
            iconText.text = "🚂";

            _titleText = CreateText(panel.transform, "Title", 48, new Vector2(0, 340), TextAnchor.MiddleCenter);
            _titleText.text = "ПОЕЗДА";
            _titleText.color = AccentColor;
            _titleText.fontStyle = FontStyle.Bold;

            // Message text ABOVE the level buttons
            _messageText = CreateText(panel.transform, "Message", 22, new Vector2(0, 260), TextAnchor.MiddleCenter);
            _messageText.color = SubtextColor;
            _messageText.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 80f);

            // Level buttons container - positioned below message
            _levelButtonContainer = new GameObject("LevelButtons").AddComponent<RectTransform>();
            _levelButtonContainer.SetParent(panel.transform, false);
            _levelButtonContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _levelButtonContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _levelButtonContainer.anchoredPosition = new Vector2(0, -60f);
            _levelButtonContainer.sizeDelta = new Vector2(600f, 400f);

            _nextLevelButton = CreateStyledButton(panel.transform, "➡️ ДАЛЕЕ", new Vector2(0, -200), true, SuccessColor);
            _nextLevelButton.gameObject.SetActive(false);
            
            _restartButton = CreateStyledButton(panel.transform, "🔄 РЕСТАРТ", new Vector2(0, -320), false);
            _restartButton.onClick.AddListener(() => _controller.RequestRestart());
            _restartButton.gameObject.SetActive(false);
            
            _menuButton = CreateStyledButton(panel.transform, "🏠 МЕНЮ", new Vector2(0, -320), false);
            _menuButton.onClick.AddListener(() => _controller.ReturnToMenu());
            _menuButton.gameObject.SetActive(false);
            
            _settingsButton = CreateStyledButton(panel.transform, "⚙️ НАСТРОЙКИ", new Vector2(0, -380), false);
            _settingsButton.onClick.AddListener(ToggleSettings);
            _settingsButton.gameObject.SetActive(false);
            
            // Settings panel
            CreateSettingsPanel(panel.transform);
        }

        private void CreateSettingsPanel(Transform parent)
        {
            var settingsGo = new GameObject("SettingsPanel");
            settingsGo.transform.SetParent(parent, false);
            _settingsPanel = settingsGo.AddComponent<RectTransform>();
            _settingsPanel.anchorMin = new Vector2(0.5f, 0.5f);
            _settingsPanel.anchorMax = new Vector2(0.5f, 0.5f);
            _settingsPanel.sizeDelta = new Vector2(800f, 900f);
            _settingsPanel.anchoredPosition = Vector2.zero;
            
            var bg = settingsGo.AddComponent<Image>();
            bg.color = PanelColor;
            
            // Accent line at top (like main menu)
            var accentLine = new GameObject("AccentLine");
            accentLine.transform.SetParent(_settingsPanel, false);
            var accentRect = accentLine.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.sizeDelta = new Vector2(0f, 6f);
            accentRect.anchoredPosition = Vector2.zero;
            var accentImage = accentLine.AddComponent<Image>();
            accentImage.color = AccentColor;
            
            // Settings icon
            var iconText = CreateText(_settingsPanel, "SettingsIcon", 72, new Vector2(0, 380), TextAnchor.MiddleCenter);
            iconText.text = "⚙️";
            
            // Title
            var title = CreateText(_settingsPanel, "SettingsTitle", 48, new Vector2(0, 300), TextAnchor.MiddleCenter);
            title.text = "НАСТРОЙКИ";
            title.color = AccentColor;
            title.fontStyle = FontStyle.Bold;
            
            // Theme label
            var themeLabel = CreateText(_settingsPanel, "ThemeLabel", 26, new Vector2(0, 220), TextAnchor.MiddleCenter);
            themeLabel.text = "Выберите тему фона:";
            themeLabel.color = SubtextColor;
            
            // Theme buttons
            var themeNames = AnimatedBackground.GetThemeNames();
            for (var i = 0; i < themeNames.Length; i++)
            {
                var themeIndex = i;
                var btn = CreateStyledButton(_settingsPanel, themeNames[i], new Vector2(0, 140 - i * 80), false);
                btn.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 70f);
                btn.onClick.AddListener(() => SelectTheme(themeIndex));
                _themeButtons.Add(btn);
            }
            
            // Back button
            var backBtn = CreateStyledButton(_settingsPanel, "← НАЗАД", new Vector2(0, -280), true);
            backBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 80f);
            backBtn.onClick.AddListener(HideSettings);
            
            _settingsPanel.gameObject.SetActive(false);
        }

        private void SelectTheme(int index)
        {
            if (AnimatedBackground.Instance != null)
            {
                AnimatedBackground.Instance.SetTheme((AnimatedBackground.BackgroundTheme)index);
            }
            UpdateThemeButtons();
        }

        private void UpdateThemeButtons()
        {
            var currentTheme = AnimatedBackground.Instance?.CurrentTheme ?? AnimatedBackground.BackgroundTheme.Forest;
            for (var i = 0; i < _themeButtons.Count; i++)
            {
                var img = _themeButtons[i].GetComponent<Image>();
                img.color = i == (int)currentTheme ? SuccessColor : ButtonColor;
                var txt = _themeButtons[i].GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.color = i == (int)currentTheme ? new Color(0.1f, 0.1f, 0.1f, 1f) : TextColor;
                }
            }
        }

        private void ToggleSettings()
        {
            _showingSettings = !_showingSettings;
            if (_showingSettings)
            {
                ShowSettings();
            }
            else
            {
                HideSettings();
            }
        }

        private void ShowSettings()
        {
            _showingSettings = true;
            _settingsPanel.gameObject.SetActive(true);
            _levelButtonContainer.gameObject.SetActive(false);
            UpdateThemeButtons();
        }

        private void HideSettings()
        {
            _showingSettings = false;
            _settingsPanel.gameObject.SetActive(false);
            _levelButtonContainer.gameObject.SetActive(true);
        }

        public void SetLevelDefinitions(LevelDefinition[] definitions)
        {
            _levelDefinitions = definitions;
            RefreshLevelButtons();
        }

        private void RefreshLevelButtons()
        {
            foreach (var button in _levelButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _levelButtons.Clear();

            if (_levelDefinitions == null || _levelDefinitions.Length == 0)
            {
                _levelButtonContainer.gameObject.SetActive(false);
                return;
            }

            _levelButtonContainer.gameObject.SetActive(true);
            var spacing = 100f;
            for (var i = 0; i < _levelDefinitions.Length; i++)
            {
                var entry = _levelDefinitions[i];
                var stars = LevelProgress.GetStars(entry.levelIndex);
                
                // Show 3 stars - earned ones gold, unearned gray
                var starDisplay = "";
                for (var s = 0; s < 3; s++)
                {
                    starDisplay += s < stars ? "⭐" : "☆";
                }
                
                var icon = stars == 0 ? (i == 0 ? "📖 " : "🗺️ ") : "";
                var label = $"{starDisplay} {icon}{entry.displayName.ToUpperInvariant()}";
                
                var button = CreateStyledButton(_levelButtonContainer, label, 
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -i * spacing), false,
                    stars >= 3 ? CompletedColor : (stars > 0 ? new Color(0.7f, 0.6f, 0.2f, 1f) : null));
                button.onClick.AddListener(() => _controller.RequestStart(entry));
                _levelButtons.Add(button);
            }

            var height = Mathf.Max(spacing * _levelDefinitions.Length, 120f);
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

        private Button CreateStyledButton(Transform parent, string label, Vector2 offset, bool isAccent, Color? customColor = null)
        {
            return CreateStyledButton(parent, label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, isAccent, customColor);
        }

        private Button CreateStyledButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, bool isAccent, Color? customColor = null)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = new Vector2(480f, 90f);
            rect.anchoredPosition = offset;

            var baseColor = customColor ?? (isAccent ? AccentColor : ButtonColor);
            var image = go.AddComponent<Image>();
            image.color = baseColor;

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = isAccent || customColor.HasValue 
                ? new Color(Mathf.Min(1f, baseColor.r + 0.1f), Mathf.Min(1f, baseColor.g + 0.1f), Mathf.Min(1f, baseColor.b + 0.1f), 1f)
                : ButtonHoverColor;
            colors.pressedColor = isAccent || customColor.HasValue
                ? new Color(baseColor.r * 0.8f, baseColor.g * 0.8f, baseColor.b * 0.8f, 1f)
                : new Color(0.12f, 0.12f, 0.18f, 1f);
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
            text.color = (isAccent || customColor.HasValue) ? new Color(0.1f, 0.1f, 0.1f, 1f) : TextColor;
            text.text = label;
            text.fontStyle = FontStyle.Bold;

            return button;
        }

        public void ShowMainMenu()
        {
            SetVisible(true);
            _showingSettings = false;
            _settingsPanel.gameObject.SetActive(false);
            var totalStars = LevelProgress.GetTotalStars();
            _titleText.text = "🚂 ПОЕЗДА";
            _messageText.text = totalStars > 0 
                ? $"Всего звёзд: {new string('⭐', Mathf.Min(totalStars, 10))} ({totalStars})\nПереключай стрелки и доведи поезд до финиша!"
                : "Переключай стрелки и доведи поезд до финиша!";
            RefreshLevelButtons();
            _levelButtonContainer.gameObject.SetActive(true);
            _restartButton.gameObject.SetActive(false);
            _nextLevelButton.gameObject.SetActive(false);
            _menuButton.gameObject.SetActive(false);
            _settingsButton.gameObject.SetActive(true);
        }

        public void ShowWinMenu(LevelDefinition nextLevel, int stars = 1, float time = 0f)
        {
            SetVisible(true);
            _showingSettings = false;
            _settingsPanel.gameObject.SetActive(false);
            var starText = new string('⭐', stars);
            _titleText.text = $"🎉 {starText}";
            _levelButtonContainer.gameObject.SetActive(false);
            _restartButton.gameObject.SetActive(false);
            _settingsButton.gameObject.SetActive(false);
            
            if (nextLevel != null)
            {
                _messageText.text = $"Время: {time:0.0}с\nСледующий: {nextLevel.displayName}";
                _nextLevelButton.gameObject.SetActive(true);
                _nextLevelButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
                _nextLevelButton.onClick.RemoveAllListeners();
                _nextLevelButton.onClick.AddListener(() => _controller.RequestNextLevel(nextLevel));
                
                // Update button text
                var buttonText = _nextLevelButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = $"➡️ {nextLevel.displayName.ToUpperInvariant()}";
                }
                
                _menuButton.gameObject.SetActive(true);
                _menuButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -160);
            }
            else
            {
                _messageText.text = $"Время: {time:0.0}с\nВсе уровни пройдены! 🏆";
                _nextLevelButton.gameObject.SetActive(false);
                _menuButton.gameObject.SetActive(true);
                _menuButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
            }
        }

        public void ShowRestartMenu(string message)
        {
            SetVisible(true);
            _showingSettings = false;
            _settingsPanel.gameObject.SetActive(false);
            _titleText.text = "💥 НЕУДАЧА";
            _messageText.text = message;
            _levelButtonContainer.gameObject.SetActive(false);
            _nextLevelButton.gameObject.SetActive(false);
            _menuButton.gameObject.SetActive(false);
            _restartButton.gameObject.SetActive(true);
            _settingsButton.gameObject.SetActive(false);
            _restartButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
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

