using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DungeonPrototype.Core;
using DungeonPrototype.Dragon;
using DungeonPrototype.Player;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DungeonPrototype
{
    public class GameIntroSequence : MonoBehaviour
    {
        public static bool IsIntroRunning { get; private set; }

        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnAwake = true;

        [Header("Scene References (optional)")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera telepathyCamera;
        [SerializeField] private Transform dragonTarget;
        [SerializeField] private Animator playerHandsAnimator;
        [SerializeField] private GameObject playerHandsObject;
        [SerializeField] private UnityEngine.Rendering.Volume telepathyVolume;

        [Header("UI References (optional)")]
        [SerializeField] private Image blackScreen;
        [SerializeField] private RectTransform eyelidTop;
        [SerializeField] private RectTransform eyelidBottom;
        [SerializeField] private GameObject dragonUIOverlay;
        [SerializeField] private GameObject manaHudRoot;
        [SerializeField] private Slider manaSlider;
        [SerializeField] private Text manaValueText;

        [Header("Timings")]
        [SerializeField] private float blinkDuration = 3.2f;
        [SerializeField] private float introDelay = 1.2f;
        [SerializeField] private float preWakePulseDuration = 2.2f;
        [SerializeField] private float preWakePulseStrength = 0.09f;
        [SerializeField] private float dragonMessageDuration = 3.5f;
        [SerializeField] private float handsFallbackRiseDuration = 1.9f;

        [Header("Telepathy Camera")]
        [SerializeField] private Vector3 telepathyOffset = new Vector3(2.3f, 1.5f, -2.8f);
        [SerializeField] private Vector3 telepathyLookAtOffset = new Vector3(0f, 0.85f, 0f);
        [SerializeField] private float telepathyTransitionDuration = 0.38f;
        [SerializeField] private float telepathyZoomFov = 44f;
        [SerializeField] private AnimationCurve telepathyEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Dragon Framing")]
        [SerializeField] private bool rotateDragonToFaceCamera = true;
        [SerializeField] private float dragonFaceCameraLerp = 8f;
        [SerializeField] private bool keepDragonGrounded = true;
        [SerializeField] private float dragonGroundProbeHeight = 3.5f;
        [SerializeField] private float dragonGroundExtraOffset = 0.02f;

        [Header("Intro Control")]
        [SerializeField] private bool lockPlayerControlsDuringIntro = true;

        [Header("Placeholder Cleanup")]
        [SerializeField] private bool hideRuntimePlaceholders = true;
        [SerializeField] private string[] placeholderNamesToHide =
        {
            "Player",
            "MainGate_Cube",
            "Room2_ManaCrystal",
            "Room3_ManaCrystal"
        };

        private bool _isTelepathyActive;
        private bool _introStarted;
        private bool _isTelepathyTransitioning;
        private bool _controlsLockedByIntro;
        private DragonCompanion _dragonCompanion;
        private SimpleFirstPersonController _playerController;
        private Coroutine _telepathyTransitionRoutine;
        private Quaternion _dragonRotationBeforeTelepathy;
        private bool _hasDragonRotationBeforeTelepathy;

        private void Awake()
        {
            if (autoSetupOnAwake)
            {
                AutoSetup();
            }
        }

        private void Start()
        {
            if (_introStarted)
            {
                return;
            }

            _introStarted = true;
            IsIntroRunning = true;
            StartCoroutine(IntroRoutine());
        }

        private void OnEnable()
        {
            GameEvents.DragonManaChanged += OnDragonManaChanged;
        }

        private void OnDisable()
        {
            GameEvents.DragonManaChanged -= OnDragonManaChanged;

            if (_introStarted)
            {
                IsIntroRunning = false;
            }
        }

        private IEnumerator IntroRoutine()
        {
            try
            {
                SetPlayerControlLock(true);

                if (blackScreen)
                {
                    blackScreen.color = new Color(0f, 0f, 0f, 1f);
                }

                SetEyelidOpen(0f);

                yield return new WaitForSecondsRealtime(Mathf.Max(0f, introDelay));

                if (preWakePulseDuration > 0.01f)
                {
                    yield return StartCoroutine(PlayWakePulse());
                }

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime / Mathf.Max(0.05f, blinkDuration);

                    if (blackScreen)
                    {
                        Color c = blackScreen.color;
                        c.a = Mathf.Lerp(1f, 0f, t);
                        blackScreen.color = c;
                    }

                    SetEyelidOpen(Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }

                if (playerHandsObject)
                {
                    playerHandsObject.SetActive(true);
                }

                if (playerHandsAnimator)
                {
                    playerHandsAnimator.SetTrigger("RiseUp");
                    yield return new WaitForSecondsRealtime(1.2f);
                }
                else if (playerHandsObject)
                {
                    yield return StartCoroutine(RaiseHandsFallback());
                }
                else
                {
                    yield return new WaitForSecondsRealtime(0.5f);
                }

                yield return StartCoroutine(DragonRequestRoutine());
            }
            finally
            {
                SetPlayerControlLock(false);
                IsIntroRunning = false;
                SnapDragonToGround();

                // Fail-safe: never leave player with a permanent black screen.
                if (blackScreen)
                {
                    Color c = blackScreen.color;
                    c.a = 0f;
                    blackScreen.color = c;
                    blackScreen.raycastTarget = false;
                }

                SetEyelidOpen(1f);
            }
        }

        private IEnumerator DragonRequestRoutine()
        {
            try
            {
                ToggleTelepathy(true);

                if (dragonUIOverlay)
                {
                    dragonUIOverlay.SetActive(true);
                }

                if (manaHudRoot)
                {
                    manaHudRoot.SetActive(true);
                }

                yield return new WaitForSecondsRealtime(Mathf.Max(0f, dragonMessageDuration));
            }
            finally
            {
                ToggleTelepathy(false);
                SnapDragonToGround();
                if (dragonUIOverlay)
                {
                    dragonUIOverlay.SetActive(false);
                }

                if (_dragonCompanion != null)
                {
                    OnDragonManaChanged(_dragonCompanion.CurrentMana, _dragonCompanion.MaxMana, 0f);
                }
            }
        }

        private void Update()
        {
            if (_isTelepathyTransitioning)
            {
                return;
            }

            bool tabPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                tabPressed = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                tabPressed = true;
            }
#endif

            if (tabPressed)
            {
                ToggleTelepathy(!_isTelepathyActive);
            }
        }

        private void ToggleTelepathy(bool state)
        {
            if (telepathyCamera == null)
            {
                state = false;
            }

            if (_isTelepathyActive == state && !_isTelepathyTransitioning)
            {
                return;
            }

            if (_telepathyTransitionRoutine != null)
            {
                StopCoroutine(_telepathyTransitionRoutine);
            }

            _telepathyTransitionRoutine = StartCoroutine(TelepathyTransitionRoutine(state));
        }

        private IEnumerator TelepathyTransitionRoutine(bool state)
        {
            _isTelepathyTransitioning = true;

            if (state)
            {
                PositionTelepathyCamera();
            }

            if (telepathyCamera == null)
            {
                _isTelepathyActive = false;
                _isTelepathyTransitioning = false;
                yield break;
            }

            float transitionDuration = Mathf.Max(0.05f, telepathyTransitionDuration);

            if (state)
            {
                Transform telepathyTransform = telepathyCamera.transform;
                Vector3 targetPos = telepathyTransform.position;
                Quaternion targetRot = telepathyTransform.rotation;
                float targetFov = telepathyZoomFov > 1f ? telepathyZoomFov : telepathyCamera.fieldOfView;
                Quaternion dragonStartRotation = Quaternion.identity;
                Quaternion dragonTargetRotation = Quaternion.identity;
                bool shouldRotateDragon = TryBuildDragonFacingRotation(targetPos, out dragonStartRotation, out dragonTargetRotation);

                if (shouldRotateDragon)
                {
                    _dragonRotationBeforeTelepathy = dragonStartRotation;
                    _hasDragonRotationBeforeTelepathy = true;
                }

                if (mainCamera != null)
                {
                    telepathyTransform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);
                    telepathyCamera.fieldOfView = mainCamera.fieldOfView;
                }

                telepathyCamera.gameObject.SetActive(true);

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime / transitionDuration;
                    float eased = telepathyEase.Evaluate(Mathf.Clamp01(t));
                    telepathyTransform.position = Vector3.Lerp(telepathyTransform.position, targetPos, eased);
                    telepathyTransform.rotation = Quaternion.Slerp(telepathyTransform.rotation, targetRot, eased);
                    telepathyCamera.fieldOfView = Mathf.Lerp(telepathyCamera.fieldOfView, targetFov, eased);
                    if (shouldRotateDragon && dragonTarget != null)
                    {
                        float dragonEase = Mathf.Clamp01(eased * 1.35f);
                        dragonTarget.rotation = Quaternion.Slerp(dragonStartRotation, dragonTargetRotation, dragonEase);
                    }
                    if (telepathyVolume)
                    {
                        telepathyVolume.weight = eased;
                    }

                    yield return null;
                }

                telepathyTransform.SetPositionAndRotation(targetPos, targetRot);
                telepathyCamera.fieldOfView = targetFov;
                if (shouldRotateDragon && dragonTarget != null)
                {
                    dragonTarget.rotation = dragonTargetRotation;
                }

                if (mainCamera)
                {
                    mainCamera.gameObject.SetActive(false);
                }

                _isTelepathyActive = true;
            }
            else
            {
                if (mainCamera != null)
                {
                    Transform telepathyTransform = telepathyCamera.transform;
                    Vector3 startPos = telepathyTransform.position;
                    Quaternion startRot = telepathyTransform.rotation;
                    float startFov = telepathyCamera.fieldOfView;
                    Vector3 endPos = mainCamera.transform.position;
                    Quaternion endRot = mainCamera.transform.rotation;
                    float endFov = mainCamera.fieldOfView;
                    Quaternion dragonStartRotation = dragonTarget != null ? dragonTarget.rotation : Quaternion.identity;
                    Quaternion dragonEndRotation = _hasDragonRotationBeforeTelepathy ? _dragonRotationBeforeTelepathy : dragonStartRotation;

                    float t = 0f;
                    while (t < 1f)
                    {
                        t += Time.unscaledDeltaTime / transitionDuration;
                        float eased = telepathyEase.Evaluate(Mathf.Clamp01(t));
                        telepathyTransform.position = Vector3.Lerp(startPos, endPos, eased);
                        telepathyTransform.rotation = Quaternion.Slerp(startRot, endRot, eased);
                        telepathyCamera.fieldOfView = Mathf.Lerp(startFov, endFov, eased);
                        if (_hasDragonRotationBeforeTelepathy && dragonTarget != null)
                        {
                            float dragonEase = Mathf.Clamp01(eased * Mathf.Max(1f, dragonFaceCameraLerp * 0.12f));
                            dragonTarget.rotation = Quaternion.Slerp(dragonStartRotation, dragonEndRotation, dragonEase);
                        }
                        if (telepathyVolume)
                        {
                            telepathyVolume.weight = 1f - eased;
                        }

                        yield return null;
                    }

                    mainCamera.gameObject.SetActive(true);
                }

                telepathyCamera.gameObject.SetActive(false);
                if (telepathyVolume)
                {
                    telepathyVolume.weight = 0f;
                }

                _isTelepathyActive = false;
                _hasDragonRotationBeforeTelepathy = false;
                SnapDragonToGround();
            }

            if (mainCamera != null && telepathyCamera != null && !mainCamera.gameObject.activeSelf && !telepathyCamera.gameObject.activeSelf)
            {
                mainCamera.gameObject.SetActive(true);
                _isTelepathyActive = false;
            }

            _isTelepathyTransitioning = false;
            _telepathyTransitionRoutine = null;

            yield break;

        }

        private void SetPlayerControlLock(bool locked)
        {
            if (!lockPlayerControlsDuringIntro)
            {
                return;
            }

            if (_playerController == null)
            {
                _playerController = FindObjectOfType<SimpleFirstPersonController>(true);
            }

            if (_playerController == null)
            {
                return;
            }

            if (locked)
            {
                _controlsLockedByIntro = true;
                _playerController.SetMovementEnabled(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (_controlsLockedByIntro)
            {
                _playerController.SetMovementEnabled(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                _controlsLockedByIntro = false;
            }
        }

        private void AutoSetup()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Camera[] cameras = FindObjectsOfType<Camera>(true);
                    if (cameras.Length > 0)
                    {
                        mainCamera = cameras[0];
                    }
                }
            }

            if (dragonTarget == null)
            {
                DragonCompanion companion = FindObjectOfType<DragonCompanion>(true);
                if (companion != null)
                {
                    dragonTarget = companion.transform;
                    _dragonCompanion = companion;
                }
                else
                {
                    GameObject dragonByName = GameObject.Find("Dragon");
                    if (dragonByName != null)
                    {
                        dragonTarget = dragonByName.transform;
                    }
                }
            }

            if (_dragonCompanion == null && dragonTarget != null)
            {
                _dragonCompanion = dragonTarget.GetComponent<DragonCompanion>();
            }

            if (playerHandsAnimator == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    playerHandsAnimator = player.GetComponentInChildren<Animator>(true);
                }
            }

            if (playerHandsObject == null && playerHandsAnimator != null)
            {
                playerHandsObject = playerHandsAnimator.gameObject;
            }

            if (telepathyCamera == null)
            {
                GameObject camObj = new GameObject("TelepathyCamera");
                telepathyCamera = camObj.AddComponent<Camera>();

                if (mainCamera != null)
                {
                    telepathyCamera.CopyFrom(mainCamera);
                }

                PositionTelepathyCamera();
            }

            if (telepathyCamera != null)
            {
                telepathyCamera.gameObject.SetActive(false);
            }

            if (telepathyVolume == null)
            {
                telepathyVolume = FindObjectOfType<UnityEngine.Rendering.Volume>(true);
            }

            if (telepathyVolume != null)
            {
                telepathyVolume.weight = 0f;
            }

            EnsureRuntimeUI();
            HideRuntimePlaceholders();

            if (dragonUIOverlay != null)
            {
                dragonUIOverlay.SetActive(false);
            }

            if (manaHudRoot != null)
            {
                manaHudRoot.SetActive(false);
            }

            if (_dragonCompanion != null)
            {
                OnDragonManaChanged(_dragonCompanion.CurrentMana, _dragonCompanion.MaxMana, 0f);
            }

            SnapDragonToGround();
        }

        private void HideRuntimePlaceholders()
        {
            if (!hideRuntimePlaceholders || placeholderNamesToHide == null)
            {
                return;
            }

            for (int i = 0; i < placeholderNamesToHide.Length; i++)
            {
                string objectName = placeholderNamesToHide[i];
                if (string.IsNullOrWhiteSpace(objectName))
                {
                    continue;
                }

                GameObject go = GameObject.Find(objectName);
                if (go == null)
                {
                    continue;
                }

                MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }

        private void EnsureRuntimeUI()
        {
            Canvas canvas = null;
            if (blackScreen != null)
            {
                canvas = blackScreen.canvas;
            }

            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>(true);
            }

            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("IntroCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 300;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            if (blackScreen == null)
            {
                GameObject bgObj = CreateUiElement("BlackScreen", canvas.transform);
                blackScreen = bgObj.AddComponent<Image>();
                blackScreen.color = Color.black;

                RectTransform rect = bgObj.GetComponent<RectTransform>();
                StretchToFullScreen(rect);
            }

            if (eyelidTop == null)
            {
                GameObject topObj = CreateUiElement("EyelidTop", blackScreen.transform);
                Image topImage = topObj.AddComponent<Image>();
                topImage.color = Color.black;
                eyelidTop = topObj.GetComponent<RectTransform>();
                eyelidTop.anchorMin = new Vector2(0f, 0.5f);
                eyelidTop.anchorMax = new Vector2(1f, 1f);
                eyelidTop.offsetMin = Vector2.zero;
                eyelidTop.offsetMax = Vector2.zero;
            }

            if (eyelidBottom == null)
            {
                GameObject bottomObj = CreateUiElement("EyelidBottom", blackScreen.transform);
                Image bottomImage = bottomObj.AddComponent<Image>();
                bottomImage.color = Color.black;
                eyelidBottom = bottomObj.GetComponent<RectTransform>();
                eyelidBottom.anchorMin = new Vector2(0f, 0f);
                eyelidBottom.anchorMax = new Vector2(1f, 0.5f);
                eyelidBottom.offsetMin = Vector2.zero;
                eyelidBottom.offsetMax = Vector2.zero;
            }

            if (dragonUIOverlay == null)
            {
                GameObject panelObj = CreateUiElement("DragonRequestOverlay", blackScreen.transform);
                RectTransform panelRect = panelObj.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0f, 0.7f);
                panelRect.anchorMax = new Vector2(1f, 0.92f);
                panelRect.offsetMin = new Vector2(24f, 0f);
                panelRect.offsetMax = new Vector2(-24f, 0f);

                Image panelImage = panelObj.AddComponent<Image>();
                panelImage.color = new Color(0f, 0f, 0f, 0.65f);

                GameObject textObj = CreateUiElement("DragonRequestText", panelObj.transform);
                Text text = textObj.AddComponent<Text>();
                text.text = "SOBERI MNE MANU IZ KRISTALLOV";
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(0.6f, 1f, 1f, 1f);
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 18;
                text.resizeTextMaxSize = 48;
                text.font = GetSafeBuiltInFont();

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                StretchToFullScreen(textRect);

                dragonUIOverlay = panelObj;
            }

            if (manaHudRoot == null)
            {
                GameObject hudObj = CreateUiElement("DragonManaHUD", blackScreen.transform);
                RectTransform hudRect = hudObj.GetComponent<RectTransform>();
                hudRect.anchorMin = new Vector2(0.03f, 0.03f);
                hudRect.anchorMax = new Vector2(0.4f, 0.14f);
                hudRect.offsetMin = Vector2.zero;
                hudRect.offsetMax = Vector2.zero;

                Image hudBg = hudObj.AddComponent<Image>();
                hudBg.color = new Color(0f, 0f, 0f, 0.55f);
                manaHudRoot = hudObj;

                GameObject titleObj = CreateUiElement("ManaTitle", hudObj.transform);
                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.03f, 0.56f);
                titleRect.anchorMax = new Vector2(0.97f, 0.95f);
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;

                Text titleText = titleObj.AddComponent<Text>();
                titleText.text = "DRAGON MANA";
                titleText.font = GetSafeBuiltInFont();
                titleText.alignment = TextAnchor.MiddleLeft;
                titleText.color = new Color(0.7f, 1f, 1f, 1f);
                titleText.resizeTextForBestFit = true;
                titleText.resizeTextMinSize = 10;
                titleText.resizeTextMaxSize = 30;

                GameObject sliderRoot = CreateUiElement("DragonManaSlider", hudObj.transform);
                RectTransform sliderRect = sliderRoot.GetComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(0.03f, 0.10f);
                sliderRect.anchorMax = new Vector2(0.97f, 0.50f);
                sliderRect.offsetMin = Vector2.zero;
                sliderRect.offsetMax = Vector2.zero;

                Image sliderBackground = sliderRoot.AddComponent<Image>();
                sliderBackground.color = new Color(0.1f, 0.12f, 0.12f, 0.9f);

                GameObject fillArea = CreateUiElement("FillArea", sliderRoot.transform);
                RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
                fillAreaRect.anchorMin = new Vector2(0.02f, 0.15f);
                fillAreaRect.anchorMax = new Vector2(0.98f, 0.85f);
                fillAreaRect.offsetMin = Vector2.zero;
                fillAreaRect.offsetMax = Vector2.zero;

                GameObject fill = CreateUiElement("Fill", fillArea.transform);
                Image fillImage = fill.AddComponent<Image>();
                fillImage.color = new Color(0.2f, 0.95f, 0.95f, 1f);
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(1f, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                manaSlider = sliderRoot.AddComponent<Slider>();
                manaSlider.minValue = 0f;
                manaSlider.maxValue = 1f;
                manaSlider.value = 0f;
                manaSlider.fillRect = fillRect;
                manaSlider.targetGraphic = fillImage;
                manaSlider.direction = Slider.Direction.LeftToRight;

                GameObject valueObj = CreateUiElement("ManaValue", hudObj.transform);
                RectTransform valueRect = valueObj.GetComponent<RectTransform>();
                valueRect.anchorMin = new Vector2(0.45f, 0.56f);
                valueRect.anchorMax = new Vector2(0.98f, 0.95f);
                valueRect.offsetMin = Vector2.zero;
                valueRect.offsetMax = Vector2.zero;

                manaValueText = valueObj.AddComponent<Text>();
                manaValueText.text = "0/0";
                manaValueText.font = GetSafeBuiltInFont();
                manaValueText.alignment = TextAnchor.MiddleRight;
                manaValueText.color = new Color(0.75f, 1f, 1f, 1f);
                manaValueText.resizeTextForBestFit = true;
                manaValueText.resizeTextMinSize = 10;
                manaValueText.resizeTextMaxSize = 26;
            }
        }

        private void OnDragonManaChanged(float current, float max, float delta)
        {
            if (manaSlider != null)
            {
                float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;
                manaSlider.value = normalized;
            }

            if (manaValueText != null)
            {
                int cur = Mathf.RoundToInt(current);
                int mx = Mathf.Max(1, Mathf.RoundToInt(max));
                manaValueText.text = cur + "/" + mx;
            }
        }

        private void PositionTelepathyCamera()
        {
            if (telepathyCamera == null || dragonTarget == null)
            {
                return;
            }

            Vector3 worldOffset = dragonTarget.TransformDirection(telepathyOffset);
            telepathyCamera.transform.position = dragonTarget.position + worldOffset;
            telepathyCamera.transform.LookAt(dragonTarget.position + telepathyLookAtOffset);
        }

        private bool TryBuildDragonFacingRotation(Vector3 cameraWorldPosition, out Quaternion startRotation, out Quaternion targetRotation)
        {
            startRotation = Quaternion.identity;
            targetRotation = Quaternion.identity;

            if (!rotateDragonToFaceCamera || dragonTarget == null)
            {
                return false;
            }

            Vector3 toCamera = cameraWorldPosition - dragonTarget.position;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            startRotation = dragonTarget.rotation;
            targetRotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            return true;
        }

        private void SnapDragonToGround()
        {
            if (!keepDragonGrounded || dragonTarget == null)
            {
                return;
            }

            Vector3 origin = dragonTarget.position + Vector3.up * Mathf.Max(0.5f, dragonGroundProbeHeight);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, Mathf.Max(2f, dragonGroundProbeHeight * 2f));
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            float bestY = float.MinValue;
            bool foundGround = false;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                if (hit.collider.transform.root == dragonTarget.root)
                {
                    continue;
                }

                if (!foundGround || hit.point.y > bestY)
                {
                    bestY = hit.point.y;
                    foundGround = true;
                }
            }

            if (!foundGround)
            {
                return;
            }

            float pivotOffset = 0f;
            Renderer dragonRenderer = dragonTarget.GetComponentInChildren<Renderer>(true);
            if (dragonRenderer != null)
            {
                pivotOffset = Mathf.Max(0f, dragonTarget.position.y - dragonRenderer.bounds.min.y);
            }

            Vector3 pos = dragonTarget.position;
            pos.y = bestY + pivotOffset + dragonGroundExtraOffset;
            dragonTarget.position = pos;
        }

        private IEnumerator RaiseHandsFallback()
        {
            Transform hands = playerHandsObject.transform;
            Vector3 startLocal = hands.localPosition;
            Vector3 endLocal = startLocal + new Vector3(0f, 0.35f, 0f);
            float t = 0f;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.05f, handsFallbackRiseDuration);
                hands.localPosition = Vector3.Lerp(startLocal, endLocal, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
        }

        private IEnumerator PlayWakePulse()
        {
            float t = 0f;
            float duration = Mathf.Max(0.05f, preWakePulseDuration);

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float cycle = Mathf.Sin((t / duration) * Mathf.PI * 6f) * 0.5f + 0.5f;
                float alpha = Mathf.Lerp(1f - preWakePulseStrength, 1f, cycle);

                if (blackScreen)
                {
                    Color c = blackScreen.color;
                    c.a = alpha;
                    blackScreen.color = c;
                }

                float eyelid = Mathf.Lerp(0f, 0.08f, cycle);
                SetEyelidOpen(eyelid);
                yield return null;
            }

            if (blackScreen)
            {
                Color c = blackScreen.color;
                c.a = 1f;
                blackScreen.color = c;
            }
            SetEyelidOpen(0f);
        }

        private static Font GetSafeBuiltInFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            // Fallback for environments where legacy font is missing.
            return Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        private void SetEyelidOpen(float openAmount)
        {
            float shift = Mathf.Lerp(0f, 600f, Mathf.Clamp01(openAmount));

            if (eyelidTop != null)
            {
                eyelidTop.anchoredPosition = new Vector2(0f, shift);
            }

            if (eyelidBottom != null)
            {
                eyelidBottom.anchoredPosition = new Vector2(0f, -shift);
            }
        }

        private static GameObject CreateUiElement(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchToFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
