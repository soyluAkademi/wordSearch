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

    void Start()
    {
        InitializeButtons();
        settingsBtn.onClick.AddListener(ToggleMenu);
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
            buttonCanvasGroups.Add(cg);

            // Set initial state (closed)
            child.localPosition = Vector3.zero;
            cg.alpha = 0;
            // Handle interaction via blocksRaycasts only, keeping interactable true to avoid disabled visuals
            cg.blocksRaycasts = false; 
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
                settingsPanelCanvasGroup.DOFade(1, panelFadeDuration).SetEase(Ease.OutQuad);
                yield return new WaitForSeconds(panelFadeDuration);
            }
        }

        // 2. Animate Buttons
        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < buttonsContainer.childCount; i++)
        {
            Transform btn = buttonsContainer.GetChild(i);
            CanvasGroup cg = buttonCanvasGroups[i];

            // Move animation
            sequence.Insert(i * delayBetweenButtons, 
                btn.DOLocalMove(originalPositions[i], animationDuration).SetEase(openEase));
            
            // Fade animation - sync with movement duration
            sequence.Insert(i * delayBetweenButtons, 
                cg.DOFade(1, animationDuration).SetEase(Ease.OutQuad));
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
                btn.DOLocalMove(Vector3.zero, animationDuration).SetEase(closeEase));
            
            sequence.Insert(delay, 
                cg.DOFade(0, animationDuration).SetEase(Ease.InQuad));
        }

        // Wait for buttons to finish
        yield return sequence.WaitForCompletion();

        // 2. Fade Out Panel
        if (settingsPanel != null)
        {
            if (settingsPanelCanvasGroup != null)
            {
                settingsPanelCanvasGroup.DOFade(0, panelFadeDuration).SetEase(Ease.OutQuad);
                yield return new WaitForSeconds(panelFadeDuration);
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
