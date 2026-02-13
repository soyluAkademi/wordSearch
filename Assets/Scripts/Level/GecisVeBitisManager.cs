using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class GecisVeBitisManager : MonoBehaviour
{
    public static GecisVeBitisManager Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject gecisBackPanel; 
    [SerializeField] private Image transitionImage; 
    [SerializeField] private Transform particleParent; // Parent of 3 particles
    
    [SerializeField] private Button devamEtBtn;
    
    [SerializeField] private TextMeshProUGUI sonrakiBolumTxt;
    [SerializeField] private TextMeshProUGUI sonrakiLevelTxt;
    [SerializeField] private TextMeshProUGUI bolumPuaniTxt;
    [SerializeField] private TextMeshProUGUI toplamPuanTxt;

    [Header("End Game UI Elements")]
    [SerializeField] private GameObject bitisBackPanel;
    [SerializeField] private TextMeshProUGUI bitisToplamPuanTxt;
    [SerializeField] private TextMeshProUGUI enYuksekSkorTxt;
    [SerializeField] private Button bitisMainBtn; // Main Menu
    [SerializeField] private Button bitisTekrarOynaBtn; // Restart Game

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

        if (bitisMainBtn != null)
        {
            bitisMainBtn.onClick.RemoveAllListeners();
            bitisMainBtn.onClick.AddListener(() => 
            {
                SceneManager.LoadScene("MainMenu");
            });
        }

        if (bitisTekrarOynaBtn != null)
        {
            bitisTekrarOynaBtn.onClick.RemoveAllListeners();
            bitisTekrarOynaBtn.onClick.AddListener(() => 
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResetGame();
                }
            });
        }
    }

    public void ShowTransition(string nextChapterName, int nextLevelNum, int levelScore, int totalScore, Action onContinue)
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

        // Score Animations
        if (bolumPuaniTxt != null)
        {
            int currentLevelScore = 0;
            DOTween.To(() => currentLevelScore, x => currentLevelScore = x, levelScore, 2f)
                .OnUpdate(() => bolumPuaniTxt.text = currentLevelScore.ToString())
                .SetEase(Ease.OutQuad);
        }

        if (toplamPuanTxt != null)
        {
            int currentTotalScore = 0; // Start from 0 as requested ("sayac gibi akacak")
            // Alternatively start from (totalScore - levelScore) if preferred, but user said "0 dan 300 e" for level
            // and "benzer mantik" for total. Let's do 0 to Total for dramatic effect.
            DOTween.To(() => currentTotalScore, x => currentTotalScore = x, totalScore, 2f)
                .OnUpdate(() => toplamPuanTxt.text = currentTotalScore.ToString())
                .SetEase(Ease.OutQuad);
        }
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

    public void ShowEndGame(int totalScore, int highScore)
    {
        if (bitisBackPanel != null)
        {
            bitisBackPanel.SetActive(true);

            // Reset Animation State
            bitisBackPanel.transform.localScale = Vector3.zero;
            CanvasGroup bgCG = bitisBackPanel.GetComponent<CanvasGroup>();
            if (bgCG == null) bgCG = bitisBackPanel.AddComponent<CanvasGroup>();
            bgCG.alpha = 0f;

            // Animate In within 0.5s
            bitisBackPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                StartCoroutine(PlayParticles()); // Re-use particles or add separate ones
            });
            bgCG.DOFade(1f, 0.5f);

            // Animate Scores
            if (bitisToplamPuanTxt != null)
            {
                int currentTotal = 0;
                DOTween.To(() => currentTotal, x => currentTotal = x, totalScore, 2f)
                    .OnUpdate(() => bitisToplamPuanTxt.text = currentTotal.ToString())
                    .SetEase(Ease.OutQuad);
            }

            if (enYuksekSkorTxt != null)
            {
                int currentHigh = 0;
                DOTween.To(() => currentHigh, x => currentHigh = x, highScore, 2f)
                    .OnUpdate(() => enYuksekSkorTxt.text = currentHigh.ToString())
                    .SetEase(Ease.OutQuad);
            }
        }
    }
}
