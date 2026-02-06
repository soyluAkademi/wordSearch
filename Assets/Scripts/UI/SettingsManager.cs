using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button settingsBtn;
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup settingsPanelCanvasGroup;

    [Header("Sound Settings")]
    [SerializeField] private Button soundBtn;
    [SerializeField] private Image soundImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;

    [Header("Music Settings")]
    [SerializeField] private Button musicBtn;
    [SerializeField] private Image musicImage;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float panelFadeDuration = 0.2f;
    [SerializeField] private float delayBetweenButtons = 0.1f;
    [SerializeField] private Ease openEase = Ease.OutBack;
    [SerializeField] private Ease closeEase = Ease.InBack;

    private List<Vector3> originalPositions = new List<Vector3>();
    private List<CanvasGroup> buttonCanvasGroups = new List<CanvasGroup>();
    private bool isOpen = false;
    private bool isAnimating = false;

    private bool isSoundOn = true;
    private bool isMusicOn = true;

    void Start()
    {
        InitializeButtons();
        settingsBtn.onClick.AddListener(ToggleMenu);

        if (soundBtn != null) soundBtn.onClick.AddListener(ToggleSound);
        if (musicBtn != null) musicBtn.onClick.AddListener(ToggleMusic);

        UpdateSoundUI();
        UpdateMusicUI();
    }

    
    private void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        
        UpdateSoundUI();
    }

    private void UpdateSoundUI()
    {
        if (soundImage != null && soundOnSprite != null && soundOffSprite != null)
        {
            soundImage.sprite = isSoundOn ? soundOnSprite : soundOffSprite;
        }
    }

    private void ToggleMusic()
    {
        isMusicOn = !isMusicOn;
        UpdateMusicUI();
    }

    private void UpdateMusicUI()
    {
        if (musicImage != null && musicOnSprite != null && musicOffSprite != null)
        {
            musicImage.sprite = isMusicOn ? musicOnSprite : musicOffSprite;
        }
    }

    private void InitializeButtons()
    {
        if (buttonsContainer == null)
        {
            Debug.LogError("Buttons Container is not assigned in SettingsManager!");
            return;
        }

        foreach (Transform child in buttonsContainer)
        {
            // Store original position
            originalPositions.Add(child.localPosition);

            // Get or Add CanvasGroup
            CanvasGroup cg = child.GetComponent<CanvasGroup>();
            if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();
            
            // Critical: Ensure this group handles its own blocking, ignoring parent state if needed
            cg.ignoreParentGroups = true;
            
            buttonCanvasGroups.Add(cg);

            // Set initial state (closed)
            child.localPosition = Vector3.zero;
            cg.alpha = 0;
            // Handle interaction via blocksRaycasts only
            cg.blocksRaycasts = false; 
            cg.interactable = true; // Ensure interactable is true
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            if (settingsPanelCanvasGroup != null) settingsPanelCanvasGroup.alpha = 0;
        }
    }

    private void ToggleMenu()
    {
        if (isAnimating) return;
        
        isOpen = !isOpen;
        isAnimating = true;

        if (isOpen)
        {
            StartCoroutine(OpenMenuRoutine());
        }
        else
        {
            StartCoroutine(CloseMenuRoutine());
        }
    }

    private IEnumerator OpenMenuRoutine()
    {
        // 1. Fade In Panel
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            if (settingsPanelCanvasGroup != null)
            {
                settingsPanelCanvasGroup.alpha = 0;
                settingsPanelCanvasGroup.DOFade(1, panelFadeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
                yield return new WaitForSecondsRealtime(panelFadeDuration);
            }
        }

        // 2. Animate Buttons
        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true); // Update even if Time.timeScale is 0

        for (int i = 0; i < buttonsContainer.childCount; i++)
        {
            Transform btn = buttonsContainer.GetChild(i);
            CanvasGroup cg = buttonCanvasGroups[i];

            // Move animation
            sequence.Insert(i * delayBetweenButtons, 
                btn.DOLocalMove(originalPositions[i], animationDuration).SetEase(openEase).SetUpdate(true));
            
            // Fade animation - sync with movement duration
            sequence.Insert(i * delayBetweenButtons, 
                cg.DOFade(1, animationDuration).SetEase(Ease.OutQuad).SetUpdate(true));
        }

        sequence.OnComplete(() =>
        {
            isAnimating = false;
            foreach (var cg in buttonCanvasGroups)
            {
                cg.blocksRaycasts = true;
            }
            
        });
    }

    private IEnumerator CloseMenuRoutine()
    {
        // 1. Animate Buttons Closed
        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        // Lock interaction immediately
        foreach (var cg in buttonCanvasGroups)
        {
            cg.blocksRaycasts = false;
        }

        for (int i = buttonsContainer.childCount - 1; i >= 0; i--)
        {
            Transform btn = buttonsContainer.GetChild(i);
            CanvasGroup cg = buttonCanvasGroups[i];

            // Reverse index for delay calculation if we want reverse order
            float delay = (buttonsContainer.childCount - 1 - i) * delayBetweenButtons;

            sequence.Insert(delay, 
                btn.DOLocalMove(Vector3.zero, animationDuration).SetEase(closeEase).SetUpdate(true));
            
            sequence.Insert(delay, 
                cg.DOFade(0, animationDuration).SetEase(Ease.InQuad).SetUpdate(true));
        }

        // Wait for buttons to finish
        yield return sequence.WaitForCompletion();

        // 2. Fade Out Panel
        if (settingsPanel != null)
        {
            if (settingsPanelCanvasGroup != null)
            {
                settingsPanelCanvasGroup.DOFade(0, panelFadeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
                yield return new WaitForSecondsRealtime(panelFadeDuration);
            }
            settingsPanel.SetActive(false);
        }

        isAnimating = false;
    }

    private void OnDestroy()
    {
        if (settingsBtn != null)
            settingsBtn.onClick.RemoveListener(ToggleMenu);
    }
}
