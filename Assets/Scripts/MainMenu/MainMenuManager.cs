using UnityEngine;
using TMPro;

using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button baslaBtn;
    [SerializeField] private TextMeshProUGUI bolumTxt;
    [SerializeField] private TextMeshProUGUI seviyeTxt;
    [SerializeField] private TextMeshProUGUI kelimeAdetTxt;

    // Tarihte Bugün - On This Day
    [Header("Tarihte Bugün")]
    [SerializeField] private TextMeshProUGUI tarihTxt;
    [SerializeField] private TextMeshProUGUI olayTxt;

    [System.Serializable]
    public class HistoryEvent
    {
        public string year; // JSON'da number ama string olarak da okunabilir veya int yapabiliriz. String esnek olur.
        public string date;
        public string description;
        public string event_title; 
    }

    [System.Serializable]
    public class HistoryEventList
    {
        public System.Collections.Generic.List<HistoryEvent> history_events;
    }

    private void Start()
    {
        UpdateMenuUI();
        LoadAndShowHistory();

        if (baslaBtn != null)
        {
            baslaBtn.onClick.AddListener(StartGame);
        }
    }

    private void StartGame()
    {
        SceneManager.LoadScene("GamePlay");
    }

    private void UpdateMenuUI()
    {
        // 1. Bölüm Adı (Varsayılan: "Seyyah Bölümü")
        // "ChapterName" key'i daha önce kullanılmadıysa varsayılanı kullanır.
        string chapterName = PlayerPrefs.GetString("ChapterName", "Seyyah Bölümü");
        if(bolumTxt != null) 
            bolumTxt.text = chapterName;

        // 2. Seviye (Varsayılan: 1)
        int level = PlayerPrefs.GetInt("Level", 1);
        if(seviyeTxt != null) 
            seviyeTxt.text = "Seviye : " + level;

        // 3. Kelime Adeti / Soru İndeksi (Varsayılan: 0)
        // Kullanıcı 5. kelimede kaldıysa QuestionIndex 4 olabilir (0-based ise).
        // Ekrana 5/15 yazdırmak için +1 ekliyoruz.
        int questionIndex = PlayerPrefs.GetInt("QuestionIndex", 0);
        
        // Her seviyede 15 kelime olduğu varsayımıyla (sabit 15)
        if(kelimeAdetTxt != null) 
            kelimeAdetTxt.text = (questionIndex + 1) + "/15";
    }

    private void LoadAndShowHistory()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("tarihte_bugun");
        if (jsonFile == null)
        {
            Debug.LogError("Resources/tarihte_bugun.json dosyası bulunamadı!");
            return;
        }

        // JSON zaten bir obje yapısında olduğu için sarmalaMayısa gerek yok.
        // { "history_events": [ ... ] }
        HistoryEventList historyList = JsonUtility.FromJson<HistoryEventList>(jsonFile.text);

        if (historyList == null || historyList.history_events == null)
        {
            Debug.LogError("JSON verisi okunamadı veya boş.");
            return;
        }

        // Bugünün tarihini al (07 Nisan formatında)
        // Kültür fark etmeksizin gün ve ayı string olarak alıp karşılaştıracağız
        System.Globalization.CultureInfo trCulture = new System.Globalization.CultureInfo("tr-TR");
        string todayDay = System.DateTime.Now.ToString("dd", trCulture);
        string todayMonth = System.DateTime.Now.ToString("MMMM", trCulture);
        string todayString = todayDay + " " + todayMonth; // "14 Şubat"



        // Eşleşen tarihi bul (Büyük/küçük harf duyarsız yapalım)
        HistoryEvent todayEvent = historyList.history_events.Find(x => 
            x.date.Trim().ToLower(trCulture) == todayString.Trim().ToLower(trCulture)
        );

        if (todayEvent != null)
        {
            if (tarihTxt != null)
                tarihTxt.text = todayEvent.year + " - " + todayEvent.event_title;
            
            if (olayTxt != null)
                olayTxt.text = todayEvent.description;
        }
        else
        {
            // Eğer tam eşleşme bulunamazsa varsayılan bir mesaj veya boş bırakılabilir
            if (tarihTxt != null) tarihTxt.text = System.DateTime.Now.ToString("yyyy - dd MMMM", trCulture);
            if (olayTxt != null) olayTxt.text = "Bugün tarihte önemli bir olay bulunamadı.";
        }
    }
}
