using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class WordManager : MonoBehaviour
{
    // [SerializeField] WordData[] _wordDatas; // REMOVED
    private List<QuestionData> _questions = new List<QuestionData>();

    [SerializeField] private float animationDuration = 0.3f;
    
    private int _currentQuestion = 0;
    
    string currentQuestion;
    string currentAnswer;
    
    public string CurrentAnswer => currentAnswer;

    [SerializeField] private GameObject letterPrefab;
    [SerializeField] private Transform letterParent;
    
    [SerializeField] private Transform hintLettersHolder;
    
    // Pool List
    private List<GameObject> _pooledHintLetters = new List<GameObject>();
    private Color _defaultHintColor = Color.black;
    
   private LetterBoxesManager letterBoxesManager;

    private Coroutine _creationCoroutine; // Aktif coroutine'i takip etmek için
    
    private RadialLayout _radialLayout;
    WordConnectManager _wordConnectManager;

    private void Awake()
    {
        _radialLayout=FindAnyObjectByType<RadialLayout>();
        _wordConnectManager=FindAnyObjectByType<WordConnectManager>();
        
        LoadQuestions();
    }

    private void LoadQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("questions");
        if (jsonFile != null)
        {
            LevelData data = JsonUtility.FromJson<LevelData>(jsonFile.text);
            if (data != null && data.questions != null)
            {
                _questions = data.questions;
            }
        }
        else
        {
            Debug.LogError("questions.json could not be found in Resources folder!");
        }
    }

    [SerializeField] private TextMeshProUGUI questionTxt;

    private Vector3 _initialPanelPos;

    private void Start()
    {
        if (hintLettersHolder != null) 
        {
            _initialPanelPos = hintLettersHolder.localPosition;
            
            // Pool Initializasyonu: Editörde eklenmiş olanları listeye al ve deaktif et
            bool colorCaptured = false;
            foreach(Transform child in hintLettersHolder)
            {
                if(child != null)
                {
                    if (!colorCaptured)
                    {
                        var txt = child.Find("lineLetterTxt")?.GetComponent<TextMeshProUGUI>();
                        if (txt != null) 
                        {
                             _defaultHintColor = txt.color;
                             colorCaptured = true;
                        }
                    }

                    child.gameObject.SetActive(false);
                    _pooledHintLetters.Add(child.gameObject);
                }
            }
        }

        // Load Question Progress
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string key = sceneName + "_CurrentQuestion";
        if (PlayerPrefs.HasKey(key))
        {
            _currentQuestion = PlayerPrefs.GetInt(key);
        }
        else
        {
            _currentQuestion = 0;
        }

        StartCoroutine(InitLevel());
    }

    private void SyncProgressToGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentQuestionNumber = _currentQuestion + 1;
        }
    }

    // ... (InitLevel and others unchanged)

    public void ShakeAndClear()
    {
        if (hintLettersHolder != null)
        {
            // Eski tween varsa durdur
            hintLettersHolder.DOKill();
            hintLettersHolder.localPosition = _initialPanelPos;

            // Titretme
            hintLettersHolder.DOShakePosition(0.5f, 30f, 20, 90, false, true)
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
        if (hintLettersHolder != null)
        {
            hintLettersHolder.DOKill();
            hintLettersHolder.localPosition = _initialPanelPos;
        }

        foreach (var obj in activeLineWords)
        {
            if (obj != null) 
            {
                // Destroy yerine Deactivate
                obj.transform.DOKill();
                obj.SetActive(false);
                obj.transform.SetParent(hintLettersHolder); // Garanti olsun
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localScale = Vector3.one;
            }
        }
        activeLineWords.Clear();

        // Panelde kalan (manuel temizlik gerektiren) varsa temizle - opsiyonel
        if (hintLettersHolder != null)
        {
            foreach (Transform child in hintLettersHolder)
            {
                if (child.gameObject != null) 
                {
                    child.DOKill();
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    // Chapter Names
    private string[] _chapterNames = { 
        "SEYYAH", "KEŞŞAF", "ÖNCÜ", "REHBER", "KAŞİF", 
        "MUHAFIZ", "SEFİR", "FATİH", "ÜSTAD", "BİLGE" 
    };

    public static event System.Action<string, int> OnLevelInfoUpdated; // ChapterName, LevelNumber
    public static event System.Action<int, int> OnQuestionProgressUpdated;

    public bool IsInteractionLocked { get; private set; }

    public void SetInteractionLock(bool state)
    {
        IsInteractionLocked = state;
    }

    private bool _areBoxesReady = false;

    private IEnumerator InitLevel()
    {
        IsInteractionLocked = true;
        _areBoxesReady = false; // Reset flag at start
        SyncProgressToGameManager();

        if (HintManager.Instance != null)
        {
            HintManager.Instance.InitializeHintButtons(_currentQuestion + 1);
        }

        // Eğer önceki oluşturma işlemi hala devam ediyorsa durdur
        if (_creationCoroutine != null) StopCoroutine(_creationCoroutine);

        // Girişleri önce açalım ki sıkıntı olmasın (veya level yüklenince açabiliriz)
        if (_wordConnectManager != null) _wordConnectManager.IsInteractable = true;

        // Temizlik işlemleri
        ClearLevel();
        if (successParticle != null) successParticle.SetActive(false); // Reset particle on level init
        
        // Destroy işlemlerinin tamamlanması için bir frame bekle
        yield return null;

        // --- Logic: Chapter & Level Calculation ---
        // 10 Chapters, 10 Levels/Chapter, 15 Questions/Level
        // Total 1500 Questions
        
        int totalIndex = _currentQuestion; // 0-based index
        
        // Calculate Chapter (Every 150 questions)
        int chapterIndex = totalIndex / 150;
        if (chapterIndex >= _chapterNames.Length) chapterIndex = _chapterNames.Length - 1; // Clamp or loop
        string currentChapterName = _chapterNames[chapterIndex];

        // Calculate Level within Chapter (Every 15 questions, 1-10)
        // (totalIndex % 150) gives index within current chapter (0-149)
        // / 15 gives 0-9
        // + 1 gives 1-10
        int levelNumber = ((totalIndex % 150) / 15) + 1;

        // Calculate Question within Level (1-15)
        int questionNumber = (totalIndex % 15) + 1;

        // Reset Level Score if start of new level
        if (questionNumber == 1 && GameManager.Instance != null)
        {
            GameManager.Instance.ResetLevelScore();
        }

        // Update UI Events
        OnLevelInfoUpdated?.Invoke(currentChapterName, levelNumber);
        
        if (_questions != null)
        {
             // We pass 'questionNumber' (1-15) and '15' constant
            OnQuestionProgressUpdated?.Invoke(questionNumber, 15);
        }
        
        // Data Retrieval (Looping the actual content if we run out but index keeps going)
        // safeIndex ensures we don't crash if JSON has fewer questions than 1500
        int safeIndex = _currentQuestion;
        if (_questions != null && _questions.Count > 0)
        {
            safeIndex = _currentQuestion % _questions.Count;
        }
        
        // Use safeIndex for getting data
        if (_questions != null && _questions.Count > safeIndex)
        {
             currentQuestion = _questions[safeIndex].question;
             currentAnswer = _questions[safeIndex].answer;
        }
        else
        {
             currentQuestion = "";
             currentAnswer = "";
        }
       
        if (string.IsNullOrEmpty(currentAnswer))
        {
             // Cevap boş ise inspector kontrol edilmeli
        }
        else
        {
             // Cevap başarıyla yüklendi
        }

        SetQuestionText(); // Soruyu ekrana yazdır
        SpawnLetters();
        
        // Yeni objelerin oluşması ve yerleşmesi için güvenli bir bekleme
        yield return null;
        
        _radialLayout.ArrangeElements();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartTimer();
        }

        // Wait for entry animations (Precise Callback from LetterBoxesManager)
        if (!string.IsNullOrEmpty(currentAnswer))
        {
            // If LetterBoxesManager is missing, we might hang forever if we don't have a fallback.
            // But SpawnLetters handles callback.
            // Safety: if answer is empty, we skip.
             yield return new WaitUntil(() => _areBoxesReady);
        }
        else
        {
            // No answer, no boxes to wait for
        }

        IsInteractionLocked = false;
        //_creationCoroutine = StartCoroutine(CreateLineWords());
    }

    private void ClearLevel()
    {
        // Harfleri temizle
        if (letterParent != null)
        {
             for (int i = letterParent.childCount - 1; i >= 0; i--) 
             {
                 Transform child = letterParent.GetChild(i);
                 // Recursive olarak tüm alt objelerdeki tweenleri öldür
                 foreach(var t in child.GetComponentsInChildren<Transform>())
                 {
                     t.DOKill();
                 }
                 Destroy(child.gameObject);
             }
        }

        // Line kelime kutularını temizle
        if (hintLettersHolder != null)
        {
            foreach (Transform child in hintLettersHolder)
            {
                // Recursive olarak tüm alt objelerdeki tweenleri öldür
                foreach(var t in child.GetComponentsInChildren<Transform>())
                {
                    t.DOKill();
                }
                // Destroy yerine Deactive
                child.gameObject.SetActive(false);
                child.localPosition = Vector3.zero;
                child.localScale = Vector3.one;
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
        
        // Save Progress
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt(sceneName + "_CurrentQuestion", _currentQuestion);
        PlayerPrefs.Save();

        // Döngüsel reset: Eğer sorular biterse başa dön
        // GetCurrentQuestion içinde de kontrol var ama _currentQuestion arttığı için index sınır dışına çıkabilir
        // O yüzden burada kontrol ediyoruz.
        if (_questions != null && _currentQuestion >= _questions.Count)
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
        char[] chars = currentAnswer.ToUpper().ToCharArray();
        
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
            // Kutuları oluştur
            _areBoxesReady = false;
            letterBoxesManager.CreateBoxes(currentAnswer.Length, () => {
                _areBoxesReady = true;
            });
        }
        else
        {
            // LetterBoxesManager bulunamazsa manuel atama gerektirir
            _areBoxesReady = true; // Fallback
        }
    }
    
    // Helper to get current
    string GetCurrentQuestion()
    {
        if (_questions == null || _questions.Count == 0) return "";
        
        if (_currentQuestion >= _questions.Count) 
            _currentQuestion = 0; // Veya hata fırlatılabilir/uyarı verilebilir, şimdilik güvenli olsun
            
        return _questions[_currentQuestion].question;
    }

    string GetCurrentAnswer()
    {
        if (_questions == null || _questions.Count == 0) return "";

        if (_currentQuestion >= _questions.Count) 
            _currentQuestion = 0;

        return _questions[_currentQuestion].answer;
    }
    
    private List<GameObject> activeLineWords = new List<GameObject>();

    public void AddLetter(string letter)
    {
        if (hintLettersHolder == null) return;

        // POOL'dan çekme mantığı
        GameObject lineWordObj = null;

        // 1. Havuzda inaktif olanı bul
        foreach(var obj in _pooledHintLetters)
        {
            if(!obj.activeSelf)
            {
                lineWordObj = obj;
                break;
            }
        }

        // Önemli: Eğer havuz yetmezse (10 tane yetmedi diyelim),
        // ya yeni üretip havuza ekleriz, ya da return deriz.
        // Kullanıcı "10 tane ekledim" dediği için muhtemelen yetecektir ama
        // yine de null check yapalım. Yetersizse instantiate edip havuza ekleyelim (fallback).
        if(lineWordObj == null)
        {
            // Havuz doluysa veya eleman yoksa islem yapma (Prefab kaldirildi)
            return;
        }

        lineWordObj.SetActive(true);
        
        // Text bileşenini bul ve yaz
        Transform txtTrans = lineWordObj.transform.Find("lineLetterTxt");
        if (txtTrans != null)
        {
            TextMeshProUGUI tmPro = txtTrans.GetComponent<TextMeshProUGUI>();
            if (tmPro != null)
            {
                tmPro.text = letter.ToUpper();
                tmPro.color = _defaultHintColor;
                tmPro.fontSize = 60f; // Reset to initial size
            }
        }
        
        // CanvasGroup animasyonları
        CanvasGroup canvasGroup = lineWordObj.GetComponent<CanvasGroup>();
        if(canvasGroup == null) canvasGroup = lineWordObj.AddComponent<CanvasGroup>();
        
        canvasGroup.alpha = 0f;
        lineWordObj.transform.localScale = Vector3.zero;
        lineWordObj.transform.localPosition = Vector3.zero; // Pos reset
        
        lineWordObj.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        canvasGroup.DOFade(1f, animationDuration);

        activeLineWords.Add(lineWordObj);
    }

    public void RemoveLastLetter()
    {
        if (activeLineWords.Count > 0)
        {
            GameObject lastObj = activeLineWords[activeLineWords.Count - 1];
            
            // Destroy yerine deactivate animasyonu
            lastObj.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => 
            {
                lastObj.SetActive(false);
                lastObj.transform.localPosition = Vector3.zero;
            });
            
            activeLineWords.RemoveAt(activeLineWords.Count - 1);
        }
    }

    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private Sprite greenSprite;

    public void MoveLettersToBoxes(System.Action onComplete = null)
    {
        if (letterBoxesManager == null || letterBoxesManager.ActiveBoxes.Count == 0)
        {
            return;
        }

        if (activeLineWords.Count != letterBoxesManager.ActiveBoxes.Count)
        {
            // Seçilen harf sayısı ile hedef kutu sayısı uyuşmuyor olabilir.
            // Bu durumda güvenli işlem yapılır.
        }

        int count = Mathf.Min(activeLineWords.Count, letterBoxesManager.ActiveBoxes.Count);
        
        // DOTween Sequence oluşturuyoruz
        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < count; i++)
        {
            // Hareket edecek harf (bizim paneldeki)
            GameObject letterObj = activeLineWords[i];
            
            // Hedef kutu
            GameObject targetBox = letterBoxesManager.ActiveBoxes[i];

            // Önemli: Harfi panelden çıkarıp ana canvas veya dünya pozisyonunda serbest hareket etmesini sağlayabiliriz
            // ya da direkt dünya pozisyonuna git diyebiliriz. DOMove dünya pozisyonu kullanır, bu güvenlidir.
            
            // Animasyon: Hedefe git
            // Join ile hepsi hafif gecikmeli başlasın diye delay ekliyoruz
            // Animasyon: Hedefe git
            // Join ile hepsi hafif gecikmeli başlasın diye delay ekliyoruz
            float delay = i * 0.1f;
            
            // 1. Hareket: Hedefe git (Y ekseninde 10 birim yukarı)
            Vector3 targetPos = targetBox.transform.position + new Vector3(0,0.05f, 0);
            seq.Insert(delay, letterObj.transform.DOMove(targetPos, moveDuration).SetEase(Ease.OutQuad));
            
            // 2. Büyüme: Giderken 1.5 katına ulaşsın
            seq.Insert(delay, letterObj.transform.DOScale(Vector3.one * 1.5f, moveDuration).SetEase(Ease.OutQuad));

            // 3. Varış: Normale (1.0) geri dön (Hızlıca)
            float arriveTime = delay + moveDuration;
            seq.Insert(arriveTime, letterObj.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));

            // --- USER REQ: TargetBox Animation ---
            // "tam yerleşmeden önce" -> arriveTime civarı
            // Scale up -> back to 1
            // Sprite change
            // Alpha change
            
            float boxEffectTime = arriveTime - 0.15f; // Biraz önce baslasin

            // Scale Effect (1.0 -> 1.5 -> 1.0)
            seq.Insert(boxEffectTime, targetBox.transform.DOScale(Vector3.one * 2f, 0.15f).SetEase(Ease.OutQuad));
            seq.Insert(boxEffectTime + 0.15f, targetBox.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack));

            // Alpha & Sprite Effect
            CanvasGroup boxCG = targetBox.GetComponent<CanvasGroup>();
            Image boxImage = targetBox.GetComponent<Image>();

            if (boxCG != null)
            {
                // Fade out yarım (büyürken silikleşsin)
                seq.Insert(boxEffectTime, boxCG.DOFade(0.3f, 0.15f));
                
                // Sprite Swap (tam 1.5 olduğunda, geri dönerken)
                seq.InsertCallback(boxEffectTime + 0.15f, () => 
                {
                    if (boxImage != null && greenSprite != null)
                    {
                        boxImage.sprite = greenSprite;
                    }
                });

                // Fade back in (küçülürken netleşsin)
                seq.Insert(boxEffectTime + 0.15f, boxCG.DOFade(1f, 0.15f));
            }

            // --- USER REQ: Text Color Animation ---
            // Yerleşirken harf rengi beyaza dönsün
            Transform textTrans = letterObj.transform.Find("lineLetterTxt");
            if (textTrans != null)
            {
                TextMeshProUGUI txtParams = textTrans.GetComponent<TextMeshProUGUI>();
                if (txtParams != null)
                {
                    // Kutu efektiyle eş zamanlı renk değişimi
                    seq.Insert(boxEffectTime, txtParams.DOColor(Color.white, 0.3f));
                    
                    // --- USER REQ: Font Size Animation ---
                    // 60 -> 80
                    // seq.Insert(boxEffectTime, DOTween.To(()=> txtParams.fontSize, x=> txtParams.fontSize = x, 80f, 0.3f));
                    // Or simpler:
                    seq.Insert(boxEffectTime, DOTween.To(() => txtParams.fontSize, x => txtParams.fontSize = x, 80f, 0.3f));
                }
            }
        }

        seq.OnComplete(() =>
        {
            // Animasyon tamamlandı
            onComplete?.Invoke();
        });
    }

    [SerializeField] private GameObject successParticle;

    public void TriggerLevelCompletion()
    {
        // Add Gold for completion
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGold(5);
        }

        Debug.Log("Question Complete!");

        if (successParticle != null)
        {
            successParticle.SetActive(false); // Force reset
            successParticle.SetActive(true);
        }
        TriggerLevelTransition();
    }

    public void TriggerLevelTransition()
    {
        IsInteractionLocked = true;
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        // --- USER REQ: Önce 1 Saniye Bekle ---
        yield return new WaitForSeconds(1f);

        float elementDelay = 0.1f; // Her eleman arasındaki bekleme

        // İndeksleri al
        // İndeksleri al
        int btnIdx = (letterParent != null) ? letterParent.childCount - 1 : -1;
        
        // DÜZELTME: hintLettersHolder.childCount yerine activeLineWords kullanıyoruz
        // Çünkü pool sistemi var, childCount tüm pool objelerini (inaktifler dahil) içerir.
        // activeLineWords ise sadece ekranda görünen (kutulara giden) harflerdir.
        int hintIdx = (activeLineWords != null) ? activeLineWords.Count - 1 : -1;
        
        int boxIdx = (letterBoxesManager != null && letterBoxesManager.ActiveBoxes != null) ? letterBoxesManager.ActiveBoxes.Count - 1 : -1;

        // Herhangi bir listede eleman kaldığı sürece devam et
        while (btnIdx >= 0 || hintIdx >= 0 || boxIdx >= 0)
        {
            // 1. Buton (Sondan)
            if (btnIdx >= 0 && letterParent != null)
            {
                Transform child = letterParent.GetChild(btnIdx);
                if (child != null)
                {
                    CanvasGroup cg = child.GetComponent<CanvasGroup>();
                    if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();
                    cg.DOFade(0f, 0.2f);
                    child.DOScale(Vector3.zero, 0.2f);
                }
                btnIdx--;
            }

            // 2. Hint Letter (Sondan) - activeLineWords üzerinden
            if (hintIdx >= 0 && activeLineWords != null)
            {
                if (hintIdx < activeLineWords.Count)
                {
                    GameObject childObj = activeLineWords[hintIdx];
                    if (childObj != null && childObj.activeSelf)
                    {
                        CanvasGroup cg = childObj.GetComponent<CanvasGroup>();
                        if (cg == null) cg = childObj.AddComponent<CanvasGroup>();
                        cg.DOFade(0f, 0.2f);
                        childObj.transform.DOScale(Vector3.zero, 0.2f);
                    }
                }
                hintIdx--;
            }

            // 3. Letter Box (Sondan)
            if (boxIdx >= 0 && letterBoxesManager != null && letterBoxesManager.ActiveBoxes != null)
            {
                if (boxIdx < letterBoxesManager.ActiveBoxes.Count)
                {
                    GameObject box = letterBoxesManager.ActiveBoxes[boxIdx];
                    if (box != null)
                    {
                        // Sprite Revert REMOVED as per user request (keep green)
                        /*
                        Image img = box.GetComponent<Image>();
                        if (img != null && letterBoxesManager.DefaultBoxSprite != null)
                        {
                            img.sprite = letterBoxesManager.DefaultBoxSprite;
                        }
                        */

                        box.transform.DOScale(Vector3.one * 1.5f, 0.2f);

                        CanvasGroup cg = box.GetComponent<CanvasGroup>();
                        if (cg == null) cg = box.gameObject.AddComponent<CanvasGroup>();
                        cg.DOFade(0f, 0.2f); // Büyürken kaybolsun
                    }
                }
                boxIdx--;
            }

            // Hepsinden birer tane işlem yaptıktan sonra bekle
            yield return new WaitForSeconds(elementDelay);
        }
        
        // İşlemler bitince sıradaki soru (animasyonların tamamlanması için minik bir ek bekleme opsiyonel)
        yield return new WaitForSeconds(0.2f);

        // --- NEW: Tarihi Ilkler Popup Check ---
        if (TarihiIlklerManager.Instance != null)
        {
            bool processed = false;
            // Coroutine wait wrapper
            TarihiIlklerManager.Instance.CheckAndShowFact(_currentQuestion, () => 
            {
                processed = true;
            });
            
            // Wait until popup is closed (if it was shown)
            yield return new WaitUntil(() => processed);
        }

        // --- NEW: Reklam Goster Popup Check ---
        if (ReklamGosterManager.Instance != null)
        {
            bool offerProcessed = false;
            ReklamGosterManager.Instance.CheckAndShowOffer(_currentQuestion, () => 
            {
                offerProcessed = true;
            });
            
            yield return new WaitUntil(() => offerProcessed);
        }

        // Check for Hint Unlock before proceeding
        Action onCompleteLevel = () => 
        {
            // Level / Chapter Transition Check
            // _currentQuestion (0-based) is the one just finished.
            // So we are about to go to (_currentQuestion + 1).

            // 1. Check End Game (1500 Questions)
            if (_currentQuestion + 1 >= 1500)
            {
                if (GecisVeBitisManager.Instance != null)
                {
                    int totalScore = 0;
                    int highScore = 0;
                    if (GameManager.Instance != null)
                    {
                        totalScore = GameManager.Instance.TotalScore;
                        highScore = GameManager.Instance.HighScore;
                    }

                    GecisVeBitisManager.Instance.ShowEndGame(totalScore, highScore);
                }
                return; // Stop progression
            }

            // 2. Check Level Transition (Every 15)
            if ((_currentQuestion + 1) % 15 == 0)
            {
                // Show Transition Panel
                if (GecisVeBitisManager.Instance != null && _chapterNames != null && _chapterNames.Length > 0)
                {
                    // Calculate Next Level Info
                    int nextIndex = _currentQuestion + 1;
                    
                    // Chapter
                    int chapterIndex = nextIndex / 150;
                    if (chapterIndex >= _chapterNames.Length) chapterIndex = _chapterNames.Length - 1;
                    string nextChapterName = _chapterNames[chapterIndex];

                    // Calculate Scores
                    int levelScore = 0;
                    int totalScore = 0;
                    if (GameManager.Instance != null)
                    {
                        levelScore = GameManager.Instance.CurrentLevelScore;
                        totalScore = GameManager.Instance.TotalScore;
                    }

                    // Level (1-10)
                    int level = ((nextIndex % 150) / 15) + 1;
                    
                    GecisVeBitisManager.Instance.ShowTransition(nextChapterName, level, levelScore, totalScore, () => 
                    {
                        NextQuestion();
                    });
                }
                else
                {
                    NextQuestion();
                }
            }
            else
            {
                NextQuestion();
            }
        };

        if (HintManager.Instance != null)
        {
            // _currentQuestion is 0-based index. 
            // So if we just finished question index 2 (which is 3rd question),
            // currentQuestion + 1 will be 3.
            HintManager.Instance.CheckUnlockCondition(_currentQuestion + 1, onCompleteLevel);
        }
        else
        {
            onCompleteLevel.Invoke();
        }
    }
    public void PlayWinAnimation(System.Action onComplete)
    {
        if (letterBoxesManager == null || letterBoxesManager.ActiveBoxes.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        Sequence seq = DOTween.Sequence();
        
        for (int i = 0; i < letterBoxesManager.ActiveBoxes.Count; i++)
        {
            GameObject box = letterBoxesManager.ActiveBoxes[i];
            if (box == null) continue;

            float delay = i * 0.1f;
            
            // Scale Up
            seq.Insert(delay, box.transform.DOScale(Vector3.one * 1.5f, 0.2f).SetEase(Ease.OutBack));
            
            // Color/Sprite Change
            seq.InsertCallback(delay + 0.1f, () => 
            {
                Image img = box.GetComponent<Image>();
                if (img != null && greenSprite != null)
                {
                    img.sprite = greenSprite;
                }
                
                // Optional: Text Color to white if not already
                var txt = box.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.color = Color.white;
            });

            // Scale Back
            seq.Insert(delay + 0.2f, box.transform.DOScale(Vector3.one, 0.2f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    // --- TEST MANAGER HELPER ---
    public void JumpToQuestion(int questionIndex)
    {
        // 1. Stop any ongoing initialization
        if (_creationCoroutine != null) StopCoroutine(_creationCoroutine);
        StopAllCoroutines(); // Safer to clear any transition/animations

        // 2. Clear existing UI elements
        if (letterParent != null)
        {
            // Foreach with Destroy is unsafe for transforms as it modifies the collection
            for (int i = letterParent.childCount - 1; i >= 0; i--)
            {
                Destroy(letterParent.GetChild(i).gameObject);
            }
        }
        
        if (letterBoxesManager != null)
        {
            // Assuming letterBoxesManager has a way to clear boxes, or we just rely on InitLevel to recreate them.
            // But InitLevel usually re-uses or clears. Let's look at InitLevel logic if needed.
            // For now, rely on InitLevel's internal cleanup.
        }

        // 3. Set index
        _currentQuestion = questionIndex;
        // Save it so it persists if we restart app (optional, but good for testing loop)
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt(sceneName + "_CurrentQuestion", _currentQuestion);
        PlayerPrefs.Save();

        // 4. Restart Level
        StartCoroutine(InitLevel());
    }
}



