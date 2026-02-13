using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Collections;

public class GecisManager : MonoBehaviour
{
    public static GecisManager Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject gecisBackPanel; 
    [SerializeField] private Image transitionImage; 
    [SerializeField] private Transform particleParent; // Parent of 3 particles
    
    [SerializeField] private Button devamEtBtn;
    
    [SerializeField] private TextMeshProUGUI sonrakiBolumTxt;
    [SerializeField] private TextMeshProUGUI sonrakiLevelTxt;

    private Action _onContinueCallback;
    private CanvasGroup _panelCanvasGroup;

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

    private void Start()
    {
        if (gecisBackPanel != null) 
        {
            _panelCanvasGroup = gecisBackPanel.GetComponent<CanvasGroup>();
            if (_panelCanvasGroup == null) _panelCanvasGroup = gecisBackPanel.AddComponent<CanvasGroup>();
            
            gecisBackPanel.SetActive(false); // Başlangıçta kapalı
        }

        if (transitionImage != null)
            transitionImage.enabled = false;

        // Partikülleri başlangıçta kapat
        if (particleParent != null)
        {
            foreach (Transform child in particleParent)
            {
                child.gameObject.SetActive(false);
            }
        }

        if (devamEtBtn != null)
        {
            devamEtBtn.onClick.RemoveAllListeners();
            devamEtBtn.onClick.AddListener(OnDevamEtClicked);
        }
    }

    public void ShowTransition(string nextChapterName, int nextLevelNum, Action onContinue)
    {
        _onContinueCallback = onContinue;

        if (gecisBackPanel == null)
        {
            _onContinueCallback?.Invoke();
            return;
        }

        // Metinleri güncelle
        if (sonrakiBolumTxt != null) sonrakiBolumTxt.text = nextChapterName;
        if (sonrakiLevelTxt != null) sonrakiLevelTxt.text = nextLevelNum.ToString();

        // Paneli aktif et
        gecisBackPanel.SetActive(true);
        
        // Resimi enabled yap
        if (transitionImage != null)
            transitionImage.enabled = true;

        // Reset Animation State
        gecisBackPanel.transform.localScale = Vector3.zero;
        if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 0f;

        // Animate In
        gecisBackPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).OnComplete(() => 
        {
            // Panel açıldıktan sonra partikülleri oynat
            StartCoroutine(PlayParticles());
        });
        
        if (_panelCanvasGroup != null)
            _panelCanvasGroup.DOFade(1f, 0.5f);
    }

    private IEnumerator PlayParticles()
    {
        if (particleParent != null)
        {
            foreach (Transform child in particleParent)
            {
                child.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void OnDevamEtClicked()
    {
        if (gecisBackPanel == null) return;

        if (devamEtBtn != null) devamEtBtn.interactable = false;

        // Animate Out
        gecisBackPanel.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
        
        if (_panelCanvasGroup != null)
        {
            _panelCanvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
            {
                gecisBackPanel.SetActive(false);
                
                // Resimi disabled yap
                if (transitionImage != null)
                    transitionImage.enabled = false;
                
                // Partikülleri kapat
                if (particleParent != null)
                {
                    foreach (Transform child in particleParent)
                    {
                        child.gameObject.SetActive(false);
                    }
                }

                if (devamEtBtn != null) devamEtBtn.interactable = true;
                
                _onContinueCallback?.Invoke();
            });
        }
        else
        {
             gecisBackPanel.SetActive(false);
             if (transitionImage != null) transitionImage.enabled = false;
             
             // Partikülleri kapat
             if (particleParent != null)
             {
                 foreach (Transform child in particleParent)
                 {
                     child.gameObject.SetActive(false);
                 }
             }

             if (devamEtBtn != null) devamEtBtn.interactable = true;
             _onContinueCallback?.Invoke();
        }
    }
}
