using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class ReklamGosterManager : MonoBehaviour
{
    public static ReklamGosterManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject contentContainer; // The popup content
    [SerializeField] private Button exitBtn;
    [SerializeField] private Button chestBtn;

    private System.Action _onCompleteCallback;

    [Header("Debug Info (Read-Only)")]
    [Tooltip("Shows which question in each level triggers the popup (Level: Question Index)")]
    public List<string> debugTriggerPoints = new List<string>();

    private void OnValidate()
    {
        CalculateDebugInfo();
    }

    [ContextMenu("Refresh Debug Info")]
    public void CalculateDebugInfo()
    {
        debugTriggerPoints.Clear();
        // Show for first 50 levels as example
        for (int lvl = 1; lvl <= 50; lvl++)
        {
            if (lvl < 2) continue; // Skip level 1 (if desired, or start from 1 if 1 is even? 1 is odd so it skips anyway)
            if (lvl % 2 != 0) continue; // Skip ODD levels (Process EVEN only)

            int qIdx = GetTargetQuestionForLevel(lvl);
            debugTriggerPoints.Add($"Level {lvl}: Question {qIdx}");
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (panel != null) panel.SetActive(false);
        if (exitBtn != null) exitBtn.onClick.AddListener(OnExitClicked);
        if (chestBtn != null) chestBtn.onClick.AddListener(ShowOfferDirectly);
    }

    public void ShowOfferDirectly()
    {
        // Direct call, no callback needed usually, or handle if needed
        _onCompleteCallback = null; 
        ShowOffer();
    }

    public void CheckAndShowOffer(int currentQuestionIndex, System.Action onComplete)
    {
        // currentQuestionIndex is 0-based index of the COMPLETED question.
        
        // Calculate Absolute Level (1-based)
        int absoluteLevel = (currentQuestionIndex / 15) + 1;
        
        // Calculate Question Number within Current Level (1-15)
        int questionInLevel = (currentQuestionIndex % 15) + 1;

        // Logic Requirements:
        // 1. EVEN levels (2, 4, 6...).
        // 2. Random question between 5 and 12.

        bool isEvenLevel = (absoluteLevel % 2 == 0);
        
        // Ensure we are at least at level 2 (first even level)
        // absoluteLevel 1 is odd, so it's skipped by isEvenLevel check.
        
        if (isEvenLevel)
        {
            int targetQuestion = GetTargetQuestionForLevel(absoluteLevel);

            if (questionInLevel == targetQuestion)
            {
                // Condition met!
                _onCompleteCallback = onComplete;
                ShowOffer();
                return;
            }
        }

        // If condition not met, proceed
        onComplete?.Invoke();
    }

    private int GetTargetQuestionForLevel(int level)
    {
        // Deterministic random based on level
        System.Random rng = new System.Random(level * 54321); // Different seed than TarihiIlkler
        // Returns integer between 5 and 12 (inclusive 5, exclusive 13)
        return rng.Next(5, 13); 
    }

    private void ShowOffer()
    {
        if (panel == null || contentContainer == null)
        {
            Debug.LogError("ReklamGosterManager UI references missing!");
            _onCompleteCallback?.Invoke();
            return;
        }

        // Animations
        panel.SetActive(true);

        // Prep container
        contentContainer.transform.localScale = Vector3.one; // Ensure scale is 1
        CanvasGroup cg = contentContainer.GetComponent<CanvasGroup>();
        if (cg == null) cg = contentContainer.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Animate In - Fade Only
        cg.DOFade(1f, 0.5f);
    }

    private void OnExitClicked()
    {
        if (contentContainer != null)
        {
            // Animate Out - Fade Only
            CanvasGroup cg = contentContainer.GetComponent<CanvasGroup>();
            if(cg != null) cg.DOFade(0f, 0.3f);
        }

        // Wait for animation then close
        DOVirtual.DelayedCall(0.35f, () => 
        {
            if (panel != null) panel.SetActive(false);
            _onCompleteCallback?.Invoke();
            _onCompleteCallback = null;
        });
    }
}
