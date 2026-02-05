using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

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
    
    [SerializeField] private GameObject hintLetterPrefab;
    [SerializeField] private Transform hintLettersHolder;
    
   private LetterBoxesManager letterBoxesManager;

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
        if (hintLettersHolder != null) _initialPanelPos = hintLettersHolder.localPosition;
        StartCoroutine(InitLevel());
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
            if (obj != null) Destroy(obj);
        }
        activeLineWords.Clear();

        // Panelde kalan (manuel temizlik gerektiren) varsa temizle - opsiyonel
        if (hintLettersHolder != null)
        {
            foreach (Transform child in hintLettersHolder)
            {
                if (child.gameObject != null) Destroy(child.gameObject);
            }
        }
    }

    public static event System.Action<int, int> OnQuestionProgressUpdated;

    private IEnumerator InitLevel()
    {
        // Eğer önceki oluşturma işlemi hala devam ediyorsa durdur
        if (_creationCoroutine != null) StopCoroutine(_creationCoroutine);

        // Girişleri önce açalım ki sıkıntı olmasın (veya level yüklenince açabiliriz)
        if (_wordConnectManager != null) _wordConnectManager.IsInteractable = true;

        // Temizlik işlemleri
        ClearLevel();
        
        // Destroy işlemlerinin tamamlanması için bir frame bekle
        yield return null;

        currentQuestion = GetCurrentQuestion();
        currentAnswer = GetCurrentAnswer();
       
        if (string.IsNullOrEmpty(currentAnswer))
        {
             // Cevap boş ise inspector kontrol edilmeli
        }
        else
        {
             // Cevap başarıyla yüklendi
        }

        // UI Güncelleme Eventi - Soru değiştiğinde bildir
        // _currentQuestion 0-indexli, UI için +1 ekliyoruz
        if (_wordDatas != null)
        {
            OnQuestionProgressUpdated?.Invoke(_currentQuestion + 1, _wordDatas.Length);
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
        if (hintLettersHolder != null)
        {
            foreach (Transform child in hintLettersHolder)
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
            letterBoxesManager.CreateBoxes(currentAnswer.Length);
        }
        else
        {
            // LetterBoxesManager bulunamazsa manuel atama gerektirir
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
        if (hintLetterPrefab == null || hintLettersHolder == null) return;

        // Prefab'ı oluştur
        GameObject lineWordObj = Instantiate(hintLetterPrefab, hintLettersHolder);
        
        // Text bileşenini bul ve yaz
        Transform txtTrans = lineWordObj.transform.Find("lineLetterTxt");
        if (txtTrans != null)
        {
            TextMeshProUGUI tmPro = txtTrans.GetComponent<TextMeshProUGUI>();
            if (tmPro != null)
            {
                tmPro.text = letter.ToUpper();
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
                }
            }
        }

        seq.OnComplete(() =>
        {
            // Animasyon tamamlandı
            onComplete?.Invoke();
        });
    }

    public void TriggerLevelTransition()
    {
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        // --- USER REQ: Önce 2 Saniye Bekle ---
        yield return new WaitForSeconds(2.0f);

        float elementDelay = 0.1f; // Her eleman arasındaki bekleme

        // İndeksleri al
        int btnIdx = (letterParent != null) ? letterParent.childCount - 1 : -1;
        int hintIdx = (hintLettersHolder != null) ? hintLettersHolder.childCount - 1 : -1;
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

            // 2. Hint Letter (Sondan)
            if (hintIdx >= 0 && hintLettersHolder != null)
            {
                Transform child = hintLettersHolder.GetChild(hintIdx);
                if (child != null)
                {
                    CanvasGroup cg = child.GetComponent<CanvasGroup>();
                    if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();
                    cg.DOFade(0f, 0.2f);
                    child.DOScale(Vector3.zero, 0.2f);
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
                        // Sprite Revert
                        Image img = box.GetComponent<Image>();
                        if (img != null && letterBoxesManager.LetterBoxPrefab != null)
                        {
                            Image prefabImg = letterBoxesManager.LetterBoxPrefab.GetComponent<Image>();
                            if (prefabImg != null) img.sprite = prefabImg.sprite;
                        }

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

        NextQuestion();
    }
}



