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
        LevelManager.OnLevelLoaded += UpdateLevelUI;
        WordManager.OnQuestionProgressUpdated += UpdateQuestionProgressUI;
        GameManager.OnScoreUpdated += UpdateScoreUI;
        GameManager.OnScoreAnimationStart += OnScoreAnimStart;
        GameManager.OnScoreAnimationEnd += OnScoreAnimEnd;
    }

    private void OnDisable()
    {
        // Abonelikten çık
        LevelManager.OnLevelLoaded -= UpdateLevelUI;
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

    private void UpdateLevelUI(string rawLevelName)
    {
        // Gelen veri formatı: kessaf_8, oncu_2 vb.
        string[] parts = rawLevelName.Split('_');
        
        if (parts.Length > 0)
        {
            if (LevelNameTxt != null)
            {
                string categoryKey = parts[0]; // kessaf, oncu...
                string turkishName = GetTurkishCategoryName(categoryKey);

                // İstenen format: "KEŞŞAF BÖLÜMÜ"
                LevelNameTxt.text = turkishName + "\nBÖLÜMÜ";
            }
        }

        if (parts.Length > 1)
        {
            if (LevelTxt != null)
            {
                // Level numarasını al: "8"
                string levelNumber = parts[1];
                LevelTxt.text = "SEVİYE: " + levelNumber;
            }
        }
    }

    private string GetTurkishCategoryName(string key)
    {
        // key küçük harf veya karışık gelebilir, garantiye alalım
        switch (key.ToLower())
        {
            case "seyyah":
                return "SEYYAH";
            case "resam": // Kullanıcı listesinde yoktu ama örnek olsun
                return "RESSAM";
            case "kessaf":
                return "KEŞŞAF";
            case "oncu":
                return "ÖNCÜ";
            case "kasif":
                return "KAŞİF";
            case "fatih":
                return "FATİH";
            default:
                // Tanımlı değilse direkt büyük harfe çevirip dönderelim
                return key.ToUpper();
        }
    }
}
