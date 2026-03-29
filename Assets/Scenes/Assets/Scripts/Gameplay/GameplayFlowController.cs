using System;
using DungeonPrototype.Dragon;
using DungeonPrototype.Environment;
using DungeonPrototype.Mana;
using DungeonPrototype.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonPrototype.Gameplay
{
    public class GameplayFlowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private DragonCompanion dragon;
        [SerializeField] private SimpleFirstPersonController playerController;
        [SerializeField] private GateController gate;

        [Header("Level Rules")]
        [SerializeField] private int crystalsToDrainForObjective = 3;
        [SerializeField] private bool autoDetectCrystalCount = true;

        [Header("Scenes")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string fallbackGameplayScene = "DungeonPrototype_Prototype";

        private Text _objectiveText;
        private GameObject _objectivePanel;
        private GameObject _statusPanel;
        private GameObject _resultPanel;
        private Text _resultTitle;
        private Button _restartButton;
        private Button _menuButton;

        private Image _healthFill;
        private Text _healthLabel;
        private Image _manaFill;
        private Text _manaLabel;

        private Image _damageFlashImage;
        private Text _damagePopupText;
        private float _damageFlashTimer;
        private float _damagePopupTimer;
        private float _lastKnownHealth = -1f;

        private int _depletedCrystals;
        private bool _isFinished;
        private bool _hudUnlocked;

        public bool IsFinished => _isFinished;

        public bool IsObjectiveReady => _depletedCrystals >= crystalsToDrainForObjective;

        public bool IsExitAllowed
        {
            get
            {
                if (!IsObjectiveReady)
                {
                    return false;
                }

                return gate == null || gate.IsOpen;
            }
        }

        private void Awake()
        {
            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<SimpleFirstPersonController>(FindObjectsInactive.Include);
            }

            if (dragon == null)
            {
                dragon = FindFirstObjectByType<DragonCompanion>(FindObjectsInactive.Include);
            }

            if (gate == null)
            {
                gate = FindFirstObjectByType<GateController>(FindObjectsInactive.Include);
            }

            if (autoDetectCrystalCount)
            {
                ManaCrystal[] crystals = FindObjectsByType<ManaCrystal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (crystals.Length > 0)
                {
                    crystalsToDrainForObjective = crystals.Length;
                }
            }

            EnsureUi();
            SetHudVisible(!GameIntroSequence.IsIntroRunning);
            _hudUnlocked = !GameIntroSequence.IsIntroRunning;
            RefreshObjectiveText();
            RefreshStatusBars();

            if (playerHealth != null)
            {
                _lastKnownHealth = playerHealth.CurrentHealth;
            }
        }

        private void OnEnable()
        {
            DungeonPrototype.Core.GameEvents.CrystalDepleted += OnCrystalDepleted;
            DungeonPrototype.Core.GameEvents.PlayerHealthChanged += OnPlayerHealthChanged;
            DungeonPrototype.Core.GameEvents.DragonManaChanged += OnDragonManaChanged;
        }

        private void OnDisable()
        {
            DungeonPrototype.Core.GameEvents.CrystalDepleted -= OnCrystalDepleted;
            DungeonPrototype.Core.GameEvents.PlayerHealthChanged -= OnPlayerHealthChanged;
            DungeonPrototype.Core.GameEvents.DragonManaChanged -= OnDragonManaChanged;
        }

        private void Update()
        {
            if (!_hudUnlocked && !GameIntroSequence.IsIntroRunning)
            {
                _hudUnlocked = true;
                SetHudVisible(true);
                RefreshObjectiveText();
                RefreshStatusBars();
            }

            UpdateDamageFeedback();

            if (_isFinished)
            {
                return;
            }

            RefreshObjectiveText();
        }

        public void CompleteLevel()
        {
            if (_isFinished)
            {
                return;
            }

            _isFinished = true;
            ShowResultPanel("ПОБЕДА", "Дракон напитан, путь открыт.");
        }

        public void FailLevel()
        {
            if (_isFinished)
            {
                return;
            }

            _isFinished = true;
            ShowResultPanel("ПОРАЖЕНИЕ", "Принцесса пала.");
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            string sceneName = string.IsNullOrWhiteSpace(fallbackGameplayScene) ? SceneManager.GetActiveScene().name : fallbackGameplayScene;
            if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (!string.IsNullOrWhiteSpace(mainMenuScene) && Application.CanStreamedLevelBeLoaded(mainMenuScene))
            {
                SceneManager.LoadScene(mainMenuScene);
                return;
            }

            Debug.LogError("MainMenu scene is not in Build Settings.");
        }

        private void OnCrystalDepleted(ManaCrystal crystal)
        {
            if (_isFinished)
            {
                return;
            }

            _depletedCrystals++;
            RefreshObjectiveText();
        }

        private void OnPlayerHealthChanged(float current, float max)
        {
            if (_lastKnownHealth >= 0f)
            {
                float damageTaken = _lastKnownHealth - current;
                if (damageTaken > 0.01f)
                {
                    TriggerDamageFeedback(damageTaken);
                }
            }

            _lastKnownHealth = current;
            UpdateHealthBar(current, max);

            if (current <= 0.001f)
            {
                FailLevel();
            }
        }

        private void OnDragonManaChanged(float current, float max, float delta)
        {
            UpdateManaBar(current, max);
        }

        private void EnsureUi()
        {
            EnsureEventSystemInputCompatibility();

            GameObject canvasGo = new GameObject("GameplayHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 180;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Font font = GetDefaultFont();

            GameObject objectiveRoot = CreateUIObject("ObjectivePanel", canvasGo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, 120f), new Vector2(280f, -80f));
            _objectivePanel = objectiveRoot;
            Image objectiveBg = objectiveRoot.AddComponent<Image>();
            objectiveBg.color = new Color(0.06f, 0.1f, 0.16f, 0.7f);

            GameObject objectiveTextObject = CreateUIObject("ObjectiveText", objectiveRoot.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(0f, 0f));
            _objectiveText = objectiveTextObject.AddComponent<Text>();
            _objectiveText.font = font;
            _objectiveText.fontSize = 24;
            _objectiveText.alignment = TextAnchor.MiddleLeft;
            _objectiveText.color = new Color(0.95f, 0.98f, 1f, 1f);

            GameObject statusRoot = CreateUIObject("StatusPanel", canvasGo.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(560f, 170f), new Vector2(300f, 110f));
            _statusPanel = statusRoot;
            Image statusBg = statusRoot.AddComponent<Image>();
            statusBg.color = new Color(0.05f, 0.09f, 0.14f, 0.75f);

            CreateStatusBar(statusRoot.transform, font, "HP", "HealthBar", new Vector2(0f, -44f), new Color(0.84f, 0.21f, 0.23f, 1f), out _healthFill, out _healthLabel);
            CreateStatusBar(statusRoot.transform, font, "MANA", "ManaBar", new Vector2(0f, -108f), new Color(0.2f, 0.78f, 1f, 1f), out _manaFill, out _manaLabel);

            GameObject flashObj = CreateUIObject("DamageFlash", canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            _damageFlashImage = flashObj.AddComponent<Image>();
            _damageFlashImage.color = new Color(0.95f, 0.07f, 0.08f, 0f);
            _damageFlashImage.raycastTarget = false;

            GameObject popupObj = CreateUIObject("DamagePopup", statusRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(220f, 46f), new Vector2(70f, 8f));
            _damagePopupText = popupObj.AddComponent<Text>();
            _damagePopupText.font = font;
            _damagePopupText.fontSize = 30;
            _damagePopupText.alignment = TextAnchor.MiddleCenter;
            _damagePopupText.color = new Color(1f, 0.55f, 0.55f, 0f);
            _damagePopupText.text = string.Empty;

            _resultPanel = CreateUIObject("ResultPanel", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(620f, 420f), Vector2.zero);
            Image panelImage = _resultPanel.AddComponent<Image>();
            panelImage.color = new Color(0.07f, 0.12f, 0.2f, 0.95f);

            GameObject titleObject = CreateUIObject("ResultTitle", _resultPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 90f), new Vector2(0f, -60f));
            _resultTitle = titleObject.AddComponent<Text>();
            _resultTitle.font = font;
            _resultTitle.fontSize = 58;
            _resultTitle.alignment = TextAnchor.MiddleCenter;
            _resultTitle.color = Color.white;

            GameObject subtitleObject = CreateUIObject("ResultSubtitle", _resultPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 80f), new Vector2(0f, -130f));
            Text subtitleText = subtitleObject.AddComponent<Text>();
            subtitleText.font = font;
            subtitleText.fontSize = 26;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = new Color(0.86f, 0.9f, 1f, 1f);
            subtitleText.text = string.Empty;

            _restartButton = CreateButton(_resultPanel.transform, font, "RestartButton", "Заново", new Vector2(0f, -230f));
            _menuButton = CreateButton(_resultPanel.transform, font, "MenuButton", "В меню", new Vector2(0f, -315f));

            _restartButton.interactable = true;
            _menuButton.interactable = true;
            _restartButton.onClick.AddListener(RestartLevel);
            _menuButton.onClick.AddListener(GoToMainMenu);

            _resultPanel.SetActive(false);
        }

        private void SetHudVisible(bool visible)
        {
            if (_objectivePanel != null)
            {
                _objectivePanel.SetActive(visible);
            }

            if (_statusPanel != null)
            {
                _statusPanel.SetActive(visible);
            }

            if (_damagePopupText != null)
            {
                _damagePopupText.gameObject.SetActive(visible);
            }
        }

        private void RefreshStatusBars()
        {
            if (playerHealth != null)
            {
                UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (dragon != null)
            {
                UpdateManaBar(dragon.CurrentMana, dragon.MaxMana);
            }
        }

        private void RefreshObjectiveText()
        {
            if (_objectiveText == null)
            {
                return;
            }

            string objective = IsObjectiveReady
                ? "Цель: идите к выходу за воротами."
                : $"Цель: истощите кристаллы ({_depletedCrystals}/{crystalsToDrainForObjective}).";

            string gateState = gate == null ? "Ворота: не привязаны" : (gate.IsOpen ? "Ворота: открыты" : "Ворота: закрыты");
            _objectiveText.text = objective + "\n" + gateState;
        }

        private void ShowResultPanel(string title, string subtitle)
        {
            if (_resultPanel == null)
            {
                return;
            }

            _resultPanel.SetActive(true);
            EnsureEventSystemInputCompatibility();

            if (_restartButton != null)
            {
                _restartButton.interactable = true;
            }

            if (_menuButton != null)
            {
                _menuButton.interactable = true;
            }

            if (_resultTitle != null)
            {
                _resultTitle.text = title;
                _resultTitle.color = title == "ПОБЕДА" ? new Color(0.62f, 1f, 0.79f, 1f) : new Color(1f, 0.55f, 0.55f, 1f);
            }

            Text subtitleText = _resultPanel.transform.Find("ResultSubtitle")?.GetComponent<Text>();
            if (subtitleText != null)
            {
                subtitleText.text = subtitle;
            }

            Time.timeScale = 0f;
            AudioListener.pause = true;

            if (playerController != null)
            {
                playerController.enabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void TriggerDamageFeedback(float damage)
        {
            _damageFlashTimer = 0.3f;
            _damagePopupTimer = 0.75f;

            if (_damagePopupText != null)
            {
                int rounded = Mathf.Max(1, Mathf.RoundToInt(damage));
                _damagePopupText.text = "-" + rounded;
                _damagePopupText.rectTransform.anchoredPosition = new Vector2(70f, 8f);
                _damagePopupText.color = new Color(1f, 0.55f, 0.55f, 1f);
            }
        }

        private void UpdateDamageFeedback()
        {
            if (_damageFlashImage != null)
            {
                if (_damageFlashTimer > 0f)
                {
                    _damageFlashTimer -= Time.unscaledDeltaTime;
                    float alpha = Mathf.Clamp01(_damageFlashTimer / 0.3f) * 0.35f;
                    _damageFlashImage.color = new Color(0.95f, 0.07f, 0.08f, alpha);
                }
                else
                {
                    _damageFlashImage.color = new Color(0.95f, 0.07f, 0.08f, 0f);
                }
            }

            if (_damagePopupText != null)
            {
                if (_damagePopupTimer > 0f)
                {
                    _damagePopupTimer -= Time.unscaledDeltaTime;
                    float t = 1f - Mathf.Clamp01(_damagePopupTimer / 0.75f);
                    Vector2 start = new Vector2(70f, 8f);
                    Vector2 end = new Vector2(70f, 46f);
                    _damagePopupText.rectTransform.anchoredPosition = Vector2.Lerp(start, end, t);

                    Color c = _damagePopupText.color;
                    c.a = 1f - t;
                    _damagePopupText.color = c;
                }
                else if (!string.IsNullOrEmpty(_damagePopupText.text))
                {
                    _damagePopupText.text = string.Empty;
                    _damagePopupText.color = new Color(1f, 0.55f, 0.55f, 0f);
                }
            }
        }

        private void UpdateHealthBar(float current, float max)
        {
            float safeMax = Mathf.Max(0.001f, max);
            float normalized = Mathf.Clamp01(current / safeMax);

            if (_healthFill != null)
            {
                _healthFill.fillAmount = normalized;
            }

            if (_healthLabel != null)
            {
                _healthLabel.text = $"HP {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(safeMax)}";
            }
        }

        private void UpdateManaBar(float current, float max)
        {
            float safeMax = Mathf.Max(0.001f, max);
            float normalized = Mathf.Clamp01(current / safeMax);

            if (_manaFill != null)
            {
                _manaFill.fillAmount = normalized;
            }

            if (_manaLabel != null)
            {
                _manaLabel.text = $"MANA {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(safeMax)}";
            }
        }

        private static void CreateStatusBar(
            Transform parent,
            Font font,
            string label,
            string name,
            Vector2 anchoredPos,
            Color fillColor,
            out Image fillImage,
            out Text valueLabel)
        {
            GameObject row = CreateUIObject(name + "Row", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, 54f), anchoredPos);

            GameObject captionObj = CreateUIObject("Caption", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, 40f), new Vector2(50f, 0f));
            Text caption = captionObj.AddComponent<Text>();
            caption.font = font;
            caption.fontSize = 21;
            caption.alignment = TextAnchor.MiddleLeft;
            caption.color = new Color(0.84f, 0.9f, 0.97f, 1f);
            caption.text = label;

            GameObject barBgObj = CreateUIObject("BarBg", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, 30f), new Vector2(240f, 0f));
            Image barBg = barBgObj.AddComponent<Image>();
            barBg.color = new Color(0.11f, 0.14f, 0.2f, 1f);

            GameObject barFillObj = CreateUIObject("BarFill", barBgObj.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            fillImage = barFillObj.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;

            GameObject valueObj = CreateUIObject("Value", row.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(110f, 40f), new Vector2(-56f, 0f));
            valueLabel = valueObj.AddComponent<Text>();
            valueLabel.font = font;
            valueLabel.fontSize = 21;
            valueLabel.alignment = TextAnchor.MiddleRight;
            valueLabel.color = Color.white;
            valueLabel.text = "0/0";
        }

        private static Button CreateButton(Transform parent, Font font, string name, string label, Vector2 anchoredPos)
        {
            GameObject buttonGo = CreateUIObject(name, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(360f, 64f), anchoredPos);
            Image image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.16f, 0.29f, 0.45f, 1f);

            Button button = buttonGo.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.29f, 0.45f, 1f);
            colors.highlightedColor = new Color(0.24f, 0.38f, 0.58f, 1f);
            colors.pressedColor = new Color(0.12f, 0.2f, 0.32f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            GameObject textGo = CreateUIObject("Text", buttonGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            Text text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;

            return button;
        }

        private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            return go;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void EnsureEventSystemInputCompatibility()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            }

            Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                StandaloneInputModule old = eventSystem.GetComponent<StandaloneInputModule>();
                if (old != null)
                {
                    Destroy(old);
                }

                if (eventSystem.GetComponent(inputSystemModuleType) == null)
                {
                    eventSystem.gameObject.AddComponent(inputSystemModuleType);
                }
            }
            else if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
        }
    }
}
