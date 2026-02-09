using UnityEngine;
using DG.Tweening;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI LevelNameTxt;
    [SerializeField] private TextMeshProUGUI LevelTxt;
    [SerializeField] private TextMeshProUGUI BolumIcıLevelTxt;
    [SerializeField] private TextMeshProUGUI toplamPuanTxt;
    [SerializeField] private GameObject scoreEffectObject; // Inner prefab
    private Vector3 _initialEffectPos;

    private void Awake()
    {
        if (scoreEffectObject != null)
        {
            _initialEffectPos = scoreEffectObject.transform.localPosition;
        }
    }

    private void OnEnable()
    {
        // Evente abone ol
        // LevelManager.OnLevelLoaded += UpdateLevelUI; // Old system
        WordManager.OnLevelInfoUpdated += UpdateLevelUI; // New system
        WordManager.OnQuestionProgressUpdated += UpdateQuestionProgressUI;
        GameManager.OnScoreUpdated += UpdateScoreUI;
        GameManager.OnScoreAnimationStart += OnScoreAnimStart;
        GameManager.OnScoreAnimationEnd += OnScoreAnimEnd;
    }

    private void OnDisable()
    {
        // Abonelikten çık
        // LevelManager.OnLevelLoaded -= UpdateLevelUI;
        WordManager.OnLevelInfoUpdated -= UpdateLevelUI;
        WordManager.OnQuestionProgressUpdated -= UpdateQuestionProgressUI;
        GameManager.OnScoreUpdated -= UpdateScoreUI;
        GameManager.OnScoreAnimationStart -= OnScoreAnimStart;
        GameManager.OnScoreAnimationEnd -= OnScoreAnimEnd;
    }

    private void OnScoreAnimStart()
    {
        if (scoreEffectObject != null)
        {
            scoreEffectObject.SetActive(true);
            
            // Start from valid initial pos (e.g. -82)
            scoreEffectObject.transform.localPosition = _initialEffectPos;

            // X: Start -> 75 -> Start
            scoreEffectObject.transform.DOKill();
            scoreEffectObject.transform.DOLocalMoveX(75f, 0.4f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.Linear);
        }
    }

    private void OnScoreAnimEnd()
    {
        if (scoreEffectObject != null)
        {
            scoreEffectObject.transform.DOKill();
            scoreEffectObject.transform.localPosition = _initialEffectPos;
            scoreEffectObject.SetActive(false);
        }
    }

    private void UpdateScoreUI(int score)
    {
        if (toplamPuanTxt != null)
        {
            toplamPuanTxt.text = score.ToString();
        }
    }

    private void UpdateQuestionProgressUI(int current, int total)
    {
        if (BolumIcıLevelTxt != null)
        {
            BolumIcıLevelTxt.text = $"{current}/{total}";
        }
    }

    private void UpdateLevelUI(string chapterName, int levelNumber)
    {
        if (LevelNameTxt != null)
        {
            // İstenen format: "KEŞŞAF BÖLÜMÜ"
            LevelNameTxt.text = chapterName + "\nBÖLÜMÜ";
        }

        if (LevelTxt != null)
        {
            LevelTxt.text = "SEVİYE: " + levelNumber;
        }
    }

    // Removed GetTurkishCategoryName as it is no longer needed (names come directly from WordManager)
}
