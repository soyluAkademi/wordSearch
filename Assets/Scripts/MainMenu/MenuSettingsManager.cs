using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class MenuSettingsManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button ayarlarBtn;
    [SerializeField] private Button exitBtn;
    [SerializeField] private RectTransform ayarlarPanel;
    [SerializeField] private CanvasGroup ayarlarPanelCanvasGroup;
    [SerializeField] private TextMeshProUGUI goldTxt; // NEW

    [Header("Sound & Music Elements")]
    [SerializeField] private Button sesAcKapaBtn;
    [SerializeField] private Button musicAcKapaBtn;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;

    private bool isSoundOn = true;
    private bool isMusicOn = true;

    void Start()
    {
        // Load Settings
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
        isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;

        UpdateToggleVisuals();

        // --- NEW: Gold Logic ---
        // If key doesn't exist, start with 300 (User Request)
        if (!PlayerPrefs.HasKey("PlayerGold"))
        {
            PlayerPrefs.SetInt("PlayerGold", 300);
            PlayerPrefs.Save();
        }
        
        int currentGold = PlayerPrefs.GetInt("PlayerGold", 300);
        if (goldTxt != null)
        {
            goldTxt.text = currentGold.ToString();
        }
        // -----------------------

        // Initial State
        if (ayarlarPanel != null)
        {
            ayarlarPanel.localScale = Vector3.zero;
            if (ayarlarPanelCanvasGroup != null)
            {
                ayarlarPanelCanvasGroup.alpha = 0f;
                ayarlarPanelCanvasGroup.blocksRaycasts = false;
                ayarlarPanelCanvasGroup.interactable = false;
            }
            ayarlarPanel.gameObject.SetActive(false);
        }

        // Button Listeners
        if (ayarlarBtn != null)
            ayarlarBtn.onClick.AddListener(OpenSettingsPanel);

        if (exitBtn != null)
            exitBtn.onClick.AddListener(CloseSettingsPanel);

        if (sesAcKapaBtn != null)
            sesAcKapaBtn.onClick.AddListener(ToggleSound);

        if (musicAcKapaBtn != null)
            musicAcKapaBtn.onClick.AddListener(ToggleMusic);
    }

    public void OpenSettingsPanel()
    {
        if (ayarlarPanel == null) return;

        UpdateToggleVisuals(); // Ensure visuals are up to date when opening

        ayarlarPanel.gameObject.SetActive(true);
        if (ayarlarPanelCanvasGroup != null)
        {
            ayarlarPanelCanvasGroup.blocksRaycasts = true;
            ayarlarPanelCanvasGroup.interactable = true;
        }

        // Kill any existing tweens to prevent conflicts
        ayarlarPanel.DOKill();
        if (ayarlarPanelCanvasGroup != null) ayarlarPanelCanvasGroup.DOKill();

        // Animation
        ayarlarPanel.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        if (ayarlarPanelCanvasGroup != null)
            ayarlarPanelCanvasGroup.DOFade(1f, animationDuration);
    }

    public void CloseSettingsPanel()
    {
        if (ayarlarPanel == null) return;

        if (ayarlarPanelCanvasGroup != null)
        {
            ayarlarPanelCanvasGroup.blocksRaycasts = false;
            ayarlarPanelCanvasGroup.interactable = false;
        }

        // Kill any existing tweens
        ayarlarPanel.DOKill();
        if (ayarlarPanelCanvasGroup != null) ayarlarPanelCanvasGroup.DOKill();

        // Animation
        ayarlarPanel.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack).OnComplete(() =>
        {
            ayarlarPanel.gameObject.SetActive(false);
        });

        if (ayarlarPanelCanvasGroup != null)
            ayarlarPanelCanvasGroup.DOFade(0f, animationDuration);
    }

    private void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        PlayerPrefs.SetInt("SoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.Save();
        UpdateToggleVisuals();
        // Add sound manager integration here later if needed
    }

    private void ToggleMusic()
    {
        isMusicOn = !isMusicOn;
        PlayerPrefs.SetInt("MusicOn", isMusicOn ? 1 : 0);
        PlayerPrefs.Save();
        UpdateToggleVisuals();
        // Add music manager integration here later if needed
    }

    private void UpdateToggleVisuals()
    {
        if (sesAcKapaBtn != null) UpdateButtonVisual(sesAcKapaBtn.transform, isSoundOn);
        if (musicAcKapaBtn != null) UpdateButtonVisual(musicAcKapaBtn.transform, isMusicOn);
    }

    private void UpdateButtonVisual(Transform parent, bool isOn)
    {
        if (parent.childCount < 2) return;

        // Assuming Child 0 is ON Icon, Child 1 is OFF Icon
        // Or user said: "0.ci child açıksa kapanmalı 1.ci açılmalı" -> toggle logic.
        // Let's stick to the visual representation:
        // if isOn == true, we want the "On" state visible.
        // User request: "0.ci child açıksa kapanmalı 1.ci açılmalı" implies a toggle. 
        // Let's assume Child 0 = ON state visual, Child 1 = OFF state visual? 
        // Or usually Child 0 is the button background and Child 1 is the icon?
        // Wait, the user said: "bu butona basınca 0.ci child açıksa kapanmalı 1.ci açılmalı. 1 açıksa kapanmalı 0 açılmalı."
        // This means they toggle.
        // So I will map: 
        // if isSoundOn (True) -> Show Child 0 (On Icon), Hide Child 1 (Off Icon)
        // if isSoundOn (False) -> Hide Child 0 (On Icon), Show Child 1 (Off Icon)
        
        parent.GetChild(0).gameObject.SetActive(isOn);
        parent.GetChild(1).gameObject.SetActive(!isOn);
    }

    private void OnDestroy()
    {
        if (ayarlarPanel != null) ayarlarPanel.DOKill();
        if (ayarlarPanelCanvasGroup != null) ayarlarPanelCanvasGroup.DOKill();
    }
}
