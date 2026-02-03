using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class WordManager : MonoBehaviour
{
    [SerializeField] WordData[] _wordDatas;
    [SerializeField] private float animationDuration = 0.3f;
    
    private int _currentQuestion = 0;
    
    string currentQuestion;
    string currentAnswer;
    
    public string CurrentAnswer => currentAnswer;

    [SerializeField] private GameObject letterPrefab;
    [SerializeField] private Transform letterParent;
    
    [SerializeField] private GameObject lineWordPrefab;
    [SerializeField] private Transform lineWordPanel;
    
    [SerializeField] private LetterBoxesManager letterBoxesManager;

    private Coroutine _creationCoroutine; // Aktif coroutine'i takip etmek için
    
    private RadialLayout _radialLayout;
    WordConnectManager _wordConnectManager;

    private void Awake()
    {
        _radialLayout=FindAnyObjectByType<RadialLayout>();
        _wordConnectManager=FindAnyObjectByType<WordConnectManager>();
    }

    [SerializeField] private TextMeshProUGUI questionTxt;

    private Vector3 _initialPanelPos;

    private void Start()
    {
        if (lineWordPanel != null) _initialPanelPos = lineWordPanel.localPosition;
        StartCoroutine(InitLevel());
    }

    // ... (InitLevel and others unchanged)

    public void ShakeAndClear()
    {
        if (lineWordPanel != null)
        {
            // Eski tween varsa durdur
            lineWordPanel.DOKill();
            lineWordPanel.localPosition = _initialPanelPos;

            // Titretme
            lineWordPanel.DOShakePosition(0.5f, 30f, 20, 90, false, true)
                .OnComplete(() =>
                {
                    ClearLineWords();
                });
        }
        else
        {
            ClearLineWords();
        }
    }

    public void ClearLineWords()
    {
        // Panelde bir animasyon varsa durdur ve resetle
        if (lineWordPanel != null)
        {
            lineWordPanel.DOKill();
            lineWordPanel.localPosition = _initialPanelPos;
        }

        foreach (var obj in activeLineWords)
        {
            if (obj != null) Destroy(obj);
        }
        activeLineWords.Clear();

        // Panelde kalan (manuel temizlik gerektiren) varsa temizle - opsiyonel
        if (lineWordPanel != null)
        {
            foreach (Transform child in lineWordPanel)
            {
                if (child.gameObject != null) Destroy(child.gameObject);
            }
        }
    }

    private IEnumerator InitLevel()
    {
        // Eğer önceki oluşturma işlemi hala devam ediyorsa durdur
        if (_creationCoroutine != null) StopCoroutine(_creationCoroutine);

        // Temizlik işlemleri
        ClearLevel();
        
        // Destroy işlemlerinin tamamlanması için bir frame bekle
        yield return null;

        currentQuestion = GetCurrentQuestion();
        currentAnswer = GetCurrentAnswer();
       
        if (string.IsNullOrEmpty(currentAnswer))
        {
             Debug.LogError("WordManager: Current Answer is EMPTY! Check Inspector for WordDatas or ensure _wordDatas is not empty.");
        }
        else
        {
             Debug.Log($"WordManager: Current Answer loaded: '{currentAnswer}' (Length: {currentAnswer.Length})");
        }

        SetQuestionText(); // Soruyu ekrana yazdır
        SpawnLetters();
        
        // Yeni objelerin oluşması ve yerleşmesi için güvenli bir bekleme
        yield return null;
        
        _radialLayout.ArrangeElements();
        
        //_creationCoroutine = StartCoroutine(CreateLineWords());
    }

    private void ClearLevel()
    {
        // Harfleri temizle
        if (letterParent != null)
        {
             foreach (Transform child in letterParent) 
             {
                 // Recursive olarak tüm alt objelerdeki tweenleri öldür
                 foreach(var t in child.GetComponentsInChildren<Transform>())
                 {
                     t.DOKill();
                 }
                 Destroy(child.gameObject);
             }
        }

        // Line kelime kutularını temizle
        if (lineWordPanel != null)
        {
            foreach (Transform child in lineWordPanel)
            {
                // Recursive olarak tüm alt objelerdeki tweenleri öldür
                foreach(var t in child.GetComponentsInChildren<Transform>())
                {
                    t.DOKill();
                }
                Destroy(child.gameObject);
            }
        }
        
        if (letterBoxesManager != null)
        {
            letterBoxesManager.ClearBoxes();
        }
    }

    public void NextQuestion()
    {
        _currentQuestion++;
        // Döngüsel reset: Eğer sorular biterse başa dön
        // GetCurrentQuestion içinde de kontrol var ama _currentQuestion arttığı için index sınır dışına çıkabilir
        // O yüzden burada kontrol ediyoruz.
        if (_wordDatas != null && _currentQuestion >= _wordDatas.Length)
        {
            _currentQuestion = 0;
        }

        StartCoroutine(InitLevel());
    }

    private void SetQuestionText()
    {
        if (questionTxt != null)
        {
            questionTxt.text = currentQuestion;
            
            // Başlangıç değerleri
            questionTxt.transform.localScale = Vector3.zero;
            questionTxt.GetComponent<CanvasGroup>().alpha = 0f;

            // Animasyonlar
            questionTxt.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
            questionTxt.GetComponent<CanvasGroup>().DOFade(1f, animationDuration);
        }
    }

    private void SpawnLetters()
    {
        if (string.IsNullOrEmpty(currentAnswer)) return;

        // Harfleri diziye al ve karıştır
        char[] chars = currentAnswer.ToCharArray();
        
        _wordConnectManager.GetWordLength(chars.Length);
        
        
        for (int i = 0; i < chars.Length; i++)
        {
            char temp = chars[i];
            int randIndex = UnityEngine.Random.Range(i, chars.Length);
            chars[i] = chars[randIndex];
            chars[randIndex] = temp;
        }

        foreach (char letter in chars)
        {
            GameObject newObj = Instantiate(letterPrefab, letterParent);
            
            // "harfTxt" objesindeki TextMeshProUGUI'yi bul ve yaz
            TextMeshProUGUI textComp = newObj.transform.Find("harfTxt").GetComponent<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = letter.ToString();
            }
        }
        
        if (letterBoxesManager == null)
        {
            letterBoxesManager = FindAnyObjectByType<LetterBoxesManager>();
        }

        if (letterBoxesManager != null)
        {
            Debug.Log($"WordManager: Requesting {currentAnswer.Length} boxes from LetterBoxesManager.");
            letterBoxesManager.CreateBoxes(currentAnswer.Length);
        }
        else
        {
            Debug.LogError("WordManager: LetterBoxesManager reference is missing and could not be found!");
        }
    }

    string GetCurrentQuestion()
    {
        if (_wordDatas == null || _wordDatas.Length == 0) return "";
        
        if (_currentQuestion >= _wordDatas.Length) 
            _currentQuestion = 0; // Veya hata fırlatılabilir/uyarı verilebilir, şimdilik güvenli olsun
            
        return _wordDatas[_currentQuestion].Question;
    }

    string GetCurrentAnswer()
    {
        if (_wordDatas == null || _wordDatas.Length == 0) return "";

        if (_currentQuestion >= _wordDatas.Length) 
            _currentQuestion = 0;

        return _wordDatas[_currentQuestion].Answer;
    }
    
    private List<GameObject> activeLineWords = new List<GameObject>();

    public void AddLetter(string letter)
    {
        if (lineWordPrefab == null || lineWordPanel == null) return;

        // Prefab'ı oluştur
        GameObject lineWordObj = Instantiate(lineWordPrefab, lineWordPanel);
        
        // Text bileşenini bul ve yaz
        Transform txtTrans = lineWordObj.transform.Find("lineLetterTxt");
        if (txtTrans != null)
        {
            TextMeshProUGUI tmPro = txtTrans.GetComponent<TextMeshProUGUI>();
            if (tmPro != null)
            {
                tmPro.text = letter;
            }
        }
        
        // CanvasGroup animasyonları
        CanvasGroup canvasGroup = lineWordObj.GetComponent<CanvasGroup>();
        if(canvasGroup == null) canvasGroup = lineWordObj.AddComponent<CanvasGroup>();
        
        canvasGroup.alpha = 0f;
        lineWordObj.transform.localScale = Vector3.zero;
        
        lineWordObj.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        canvasGroup.DOFade(1f, animationDuration);

        activeLineWords.Add(lineWordObj);
    }

    public void RemoveLastLetter()
    {
        if (activeLineWords.Count > 0)
        {
            GameObject lastObj = activeLineWords[activeLineWords.Count - 1];
            
            // Animasyonlu yok etme opsiyonel olabilir, şimdilik direkt siliyoruz veya scale down yapılabilir
            lastObj.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => Destroy(lastObj));
            
            activeLineWords.RemoveAt(activeLineWords.Count - 1);
        }
    }


}


