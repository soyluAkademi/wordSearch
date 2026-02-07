using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class LetterBoxesManager : MonoBehaviour
{
    // Pool
    private List<GameObject> _pooledBoxes = new List<GameObject>();
    private Sprite _defaultSprite;
    public Sprite DefaultBoxSprite => _defaultSprite;
    [SerializeField] private Transform container; 

    private List<GameObject> activeBoxes = new List<GameObject>();
    public List<GameObject> ActiveBoxes => activeBoxes;

    private void Awake()
    {
        if (container == null) container = transform;

        foreach(Transform child in container)
        {
            if(child != null)
            {
                child.gameObject.SetActive(false);
                _pooledBoxes.Add(child.gameObject);
                
                // İlk kutudan varsayılan sprite'ı al
                if(_defaultSprite == null)
                {
                    var img = child.GetComponent<Image>();
                    if(img != null) _defaultSprite = img.sprite;
                }
            }
        }
    }

    public void CreateBoxes(int count)
    {
        ClearBoxes();
        StartCoroutine(SpawnBoxesRoutine(count));
    }

    public void ClearBoxes()
    {
        foreach (var box in activeBoxes)
        {
            if (box != null)
            {
                box.transform.DOKill();
                box.SetActive(false);
                box.transform.localScale = Vector3.one; 
                
                // Rengi/Sprite'ı resetle (WordManager değiştirmiş olabilir)
                var img = box.GetComponent<Image>();
                if(img != null && _defaultSprite != null) img.sprite = _defaultSprite;
                
                // Text içeriğini temizle
                var txt = box.GetComponentInChildren<TextMeshProUGUI>();
                if(txt != null) txt.text = "";

                var cg = box.GetComponent<CanvasGroup>();
                if(cg != null) cg.alpha = 1f;
            }
        }
        activeBoxes.Clear();
    }

    private IEnumerator SpawnBoxesRoutine(int count)
    {
        if (container == null)
        {
            container = transform; // Container boşsa kendisini kullan
        }

        if (_pooledBoxes.Count == 0 && container.childCount > 0)
        {
             // Awake'te toplanmamışsa (örn. kapatıp açınca) tekrar topla
             _pooledBoxes.Clear();
             foreach(Transform child in container) _pooledBoxes.Add(child.gameObject);
        }

        if (_pooledBoxes.Count < count)
        {
            // Yeterli kutu yok uyarısı verilebilir
        }

        // Ebat belirleme
        Vector2 targetSize = new Vector2(80, 80); // Default (>10 durumu)
        
        if (count <= 8)
        {
            targetSize = new Vector2(120, 120);
        }
        else if (count == 9 || count == 10)
        {
            targetSize = new Vector2(90, 90);
        }

        // Kutu oluşturma işlemini başlat

        // 1. Önce hepsini oluştur ve görünmez yap (Layout düzgün hesaplansın diye)
        // 1. Havuzdan çek ve ayarla
        for (int i = 0; i < count; i++)
        {
            // Havuzda yeterli eleman yoksa döngüyü kır (veya hata ver)
            if (i >= _pooledBoxes.Count) break;

            GameObject box = _pooledBoxes[i];
            
            box.SetActive(true);

            RectTransform rt = box.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = targetSize;
            }

            activeBoxes.Add(box);

            box.transform.localScale = Vector3.zero;
            CanvasGroup cg = box.GetComponent<CanvasGroup>();
            if (cg == null) cg = box.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        // Layout'un oturması için bir frame bekle
        yield return null;

        // 2. Sırayla animasyonları başlat
        foreach (var box in activeBoxes)
        {
            if (box == null) continue;

            box.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            box.GetComponent<CanvasGroup>().DOFade(1f, 0.3f);

            yield return new WaitForSeconds(0.1f);
        }
    }
}