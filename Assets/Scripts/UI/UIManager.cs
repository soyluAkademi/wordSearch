using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI LevelNameTxt;
    [SerializeField] private TextMeshProUGUI LevelTxt;
    [SerializeField] private TextMeshProUGUI BolumIcıLevelTxt;

    private void OnEnable()
    {
        // Evente abone ol
        LevelManager.OnLevelLoaded += UpdateLevelUI;
        WordManager.OnQuestionProgressUpdated += UpdateQuestionProgressUI;
    }

    private void OnDisable()
    {
        // Abonelikten çık
        LevelManager.OnLevelLoaded -= UpdateLevelUI;
        WordManager.OnQuestionProgressUpdated -= UpdateQuestionProgressUI;
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
