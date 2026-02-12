using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class HintManager : MonoBehaviour
{
    public static HintManager Instance;

    [Header("Unlock Settings")]
    [SerializeField] private GameObject tekliIpucuInfo;
    [SerializeField] private GameObject cokluIpucuInfo;
    [SerializeField] private GameObject kelimeIpucuInfo;

    [Header("Unlock Question Numbers")]
    [SerializeField] private int unlockQuestionTekli = 3;
    [SerializeField] private int unlockQuestionCoklu = 7;
    [SerializeField] private int unlockQuestionKelime = 12;

    [Header("Hint Buttons (To Unlock)")]
    [SerializeField] private GameObject tekliHintBtnObj;
    [SerializeField] private GameObject cokluHintBtnObj;
    [SerializeField] private GameObject kelimeHintBtnObj;
    
    [SerializeField] float animationDuration = 0.5f;

  

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CheckUnlockCondition(int completedQuestionNumber, Action onComplete)
    {
        // completedQuestionNumber: The question just finished (1-based)
        // So if we finished Q3, we show unlock for Q3 completion.

        GameObject targetInfo = null;
        GameObject targetButton = null;

        if (completedQuestionNumber == unlockQuestionTekli)
        {
            targetInfo = tekliIpucuInfo;
            targetButton = tekliHintBtnObj;
        }
        else if (completedQuestionNumber == unlockQuestionCoklu)
        {
            targetInfo = cokluIpucuInfo;
            targetButton = cokluHintBtnObj;
        }
        else if (completedQuestionNumber == unlockQuestionKelime)
        {
            targetInfo = kelimeIpucuInfo;
            targetButton = kelimeHintBtnObj;
        }

        if (targetInfo != null)
        {
            ShowInfo(targetInfo, targetButton, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private void ShowInfo(GameObject infoObj, GameObject buttonToUnlock, Action onClosed)
    {
        if (infoObj == null)
        {
            onClosed?.Invoke();
            return;
        }

        infoObj.SetActive(true);

        // Child 0 animation (Scale & Alpha)
        Transform child = infoObj.transform.GetChild(0);
        CanvasGroup cg = child.GetComponent<CanvasGroup>();
        if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();

        // Init State
        child.localScale = Vector3.zero;
        cg.alpha = 0f;

        // Animate In
        child.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        cg.DOFade(1f, animationDuration);

        // Button Listener Setup
        Button closeBtn = infoObj.GetComponentInChildren<Button>(); 
        
        // Try to find specific button if generic fails or to be sure
        Transform btnTrans = infoObj.transform.Find("DevamEtBtn"); 
        if (btnTrans == null) btnTrans = infoObj.transform.Find("tekliDevamEtBtn");
        if (btnTrans != null) closeBtn = btnTrans.GetComponent<Button>();

        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(() => 
            {
                // Remove listener to avoid multi-call?
                closeBtn.onClick.RemoveAllListeners(); 
                HideInfo(infoObj, buttonToUnlock, onClosed);
            });
        }
        else
        {
            Debug.LogWarning("No close button found in " + infoObj.name);
            // Fallback
        }
    }

    private void HideInfo(GameObject infoObj, GameObject buttonToUnlock, Action onClosed)
    {
        if (infoObj == null) return;

        Transform child = infoObj.transform.GetChild(0);
        CanvasGroup cg = child.GetComponent<CanvasGroup>();

        // Animate Out
        child.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack);
        if(cg != null) cg.DOFade(0f, animationDuration);
        
        // Wait for animation then deactivate
        DOVirtual.DelayedCall(animationDuration, () => 
        {
            infoObj.SetActive(false);
            
            // Unlock the corresponding button if one was targeted
            if (buttonToUnlock != null)
            {
                AnimateButtonUnlock(buttonToUnlock);
            }
            
            onClosed?.Invoke();
        });
    }

    // Overload or modify existing flow to pass button
    // Refactored ShowInfo below to pass buttonToUnlock to HideInfo
    
    private void AnimateButtonUnlock(GameObject btnObj)
    {
        if(btnObj == null) return;
        
        btnObj.SetActive(true);
        CanvasGroup cg = btnObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = btnObj.AddComponent<CanvasGroup>();
        
        btnObj.transform.localScale = Vector3.zero;
        cg.alpha = 0f;
        
        btnObj.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        cg.DOFade(1f, animationDuration);
    }

    public void InitializeHintButtons(int currentQuestionNumber)
    {
        // Set initial state based on progress
        // If currentQuestionNumber is > unlockQuestionX, it means we passed it.
        // Example: Unlock at 3. If we are at 4, button should be active.
        
        InitializeButton(tekliHintBtnObj, currentQuestionNumber > unlockQuestionTekli);
        InitializeButton(cokluHintBtnObj, currentQuestionNumber > unlockQuestionCoklu);
        InitializeButton(kelimeHintBtnObj, currentQuestionNumber > unlockQuestionKelime);
    }

    private void InitializeButton(GameObject btn, bool isUnlocked)
    {
        if (btn == null) return;

        // Ensure object is active to be manipulated/seen
        btn.SetActive(true);

        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg == null) cg = btn.AddComponent<CanvasGroup>();

        if (isUnlocked)
        {
            // If already fully visible (scale ~1), do not re-animate 
            // to avoid annoying pop-in on every level load if persistence is used.
            if (btn.transform.localScale.x > 0.9f && cg.alpha > 0.9f) return;

            // Unlocked: Animate In (Tween)
            btn.transform.localScale = Vector3.zero;
            cg.alpha = 0f;
            
            btn.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
            cg.DOFade(1f, animationDuration);
        }
        else
        {
            // Locked: Hidden (Instant)
            btn.transform.localScale = Vector3.zero;
            cg.alpha = 0f;
        }
    }
}
