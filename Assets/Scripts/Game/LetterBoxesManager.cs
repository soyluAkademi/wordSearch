using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class LetterBoxesManager : MonoBehaviour
{
    [SerializeField] private GameObject letterBoxPrefab;
    public GameObject LetterBoxPrefab => letterBoxPrefab;
    [SerializeField] private Transform container; 

    private List<GameObject> activeBoxes = new List<GameObject>();
    public List<GameObject> ActiveBoxes => activeBoxes;

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
                Destroy(box);
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

        if (letterBoxPrefab == null)
        {
            // Prefab atanmamış, işlem iptal
            yield break;
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
        for (int i = 0; i < count; i++)
        {
            GameObject box = Instantiate(letterBoxPrefab, container);
            
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