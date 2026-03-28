using UnityEngine;
using UnityEngine.UI;
using DungeonPrototype.Player;

namespace DungeonPrototype.UI
{
    /// <summary>
    /// Manages tutorial prompt visibility based on player presence and interaction state.
    /// Shows context-sensitive messages when player enters trigger zones.
    /// </summary>
    public class TutorialPromptTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [SerializeField] private string enterMessage = "Вытянуть ману";
        [SerializeField] private string idleMessage = "Нажмите E для взаимодействия";
        [SerializeField] private float messageFadeInDuration = 0.3f;

        [Header("UI References")]
        [SerializeField] private Text promptText;
        [SerializeField] private CanvasGroup promptCanvasGroup;

        private bool _playerInZone = false;
        private float _fadeTimer = 0f;
        private bool _isFadingIn = false;
        private bool _isFadingOut = false;

        private void Awake()
        {
            // Auto-discover prompt text if not assigned
            if (promptText == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    Transform tutorialUI = canvas.transform.Find("TutorialUI");
                    if (tutorialUI != null)
                    {
                        promptText = tutorialUI.Find("PromptText")?.GetComponent<Text>();
                    }
                }
            }

            // Auto-discover canvas group
            if (promptCanvasGroup == null)
            {
                if (promptText != null)
                {
                    promptCanvasGroup = promptText.GetComponent<CanvasGroup>();
                    if (promptCanvasGroup == null)
                    {
                        promptCanvasGroup = promptText.gameObject.AddComponent<CanvasGroup>();
                    }
                }
            }

            // Start with hidden text
            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = 0f;
            }

            SetIdleMessage();
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.GetComponent<PlayerHealth>() != null)
            {
                _playerInZone = true;
                ShowPrompt(enterMessage);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (collider.GetComponent<PlayerHealth>() != null)
            {
                _playerInZone = false;
                HidePrompt();
            }
        }

        private void Update()
        {
            if (_isFadingIn)
            {
                _fadeTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_fadeTimer / messageFadeInDuration);

                if (promptCanvasGroup != null)
                {
                    promptCanvasGroup.alpha = progress;
                }

                if (progress >= 1f)
                {
                    _isFadingIn = false;
                    if (promptCanvasGroup != null)
                    {
                        promptCanvasGroup.alpha = 1f;
                    }
                }
            }
            else if (_isFadingOut)
            {
                _fadeTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_fadeTimer / messageFadeInDuration);
                float reverseProgress = 1f - progress;

                if (promptCanvasGroup != null)
                {
                    promptCanvasGroup.alpha = reverseProgress;
                }

                if (progress >= 1f)
                {
                    _isFadingOut = false;
                    if (promptCanvasGroup != null)
                    {
                        promptCanvasGroup.alpha = 0f;
                    }
                }
            }
        }

        public void ShowPrompt(string message)
        {
            if (promptText == null) return;

            promptText.text = message;
            _isFadingOut = false;
            _isFadingIn = true;
            _fadeTimer = 0f;
        }

        public void HidePrompt()
        {
            _isFadingIn = false;
            _isFadingOut = true;
            _fadeTimer = 0f;
        }

        public void SetIdleMessage()
        {
            if (promptText != null)
            {
                promptText.text = idleMessage;
            }
        }

        public bool IsPlayerInZone => _playerInZone;
    }
}
