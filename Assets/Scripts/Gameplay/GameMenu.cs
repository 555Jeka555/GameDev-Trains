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

        public static GameMenu Create(Camera camera, RailGameController controller, string levelName)
        {
            EnsureEventSystemExists();

            var canvasGo = new GameObject("Menu");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = camera;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
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

            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600f, 500f);
            rect.anchoredPosition = Vector2.zero;

            var image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.7f);

            _titleText = CreateText(panel.transform, "Title", 48, new Vector2(0, 160), TextAnchor.MiddleCenter);
            _titleText.text = _levelName;

            _messageText = CreateText(panel.transform, "Message", 28, new Vector2(0, 120), TextAnchor.MiddleCenter);

            _levelButtonContainer = new GameObject("LevelButtons").AddComponent<RectTransform>();
            _levelButtonContainer.SetParent(panel.transform, false);
            _levelButtonContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _levelButtonContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _levelButtonContainer.anchoredPosition = new Vector2(0, -40f);
            _levelButtonContainer.sizeDelta = new Vector2(440f, 300f);

            _restartButton = CreateButton(panel.transform, "Рестарт", new Vector2(0, -140));
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
            var spacing = 90f;
            for (var i = 0; i < definitions.Length; i++)
            {
                var entry = definitions[i];
                var button = CreateButton(_levelButtonContainer, entry.displayName, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -i * spacing));
                button.onClick.AddListener(() => _controller.RequestStart(entry));
                button.GetComponentInChildren<Text>().text = entry.displayName.ToUpperInvariant();
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
            rect.sizeDelta = new Vector2(520f, 120f);
            rect.anchoredPosition = offset;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = "";
            return text;
        }

        private Button CreateButton(Transform parent, string label, Vector2 offset)
        {
            return CreateButton(parent, label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset);
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = new Vector2(360f, 100f);
            rect.anchoredPosition = offset;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.5f, 0.9f, 0.9f);
            var button = go.AddComponent<Button>();

            var text = CreateText(go.transform, label + "Text", 32, Vector2.zero, TextAnchor.MiddleCenter);
            text.text = label.ToUpperInvariant();

            return button;
        }

        public void ShowMainMenu()
        {
            SetVisible(true);
            _messageText.text = "Выбери уровень и подготовь маршрут за 10 секунд.";
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

