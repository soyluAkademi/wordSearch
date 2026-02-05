using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class RadialLayout : MonoBehaviour
{
    [SerializeField] private float radius = 200f; // Çemberin yarıçapı
    

    [SerializeField] private float animationDelay = 0.1f;
    [SerializeField] private float animationDuration = 0.5f;

    [SerializeField] private RectTransform rotateBtn;
    private void Start()
    {
        //ArrangeElements();
    }

    public void ArrangeElements()
    {
        if (rotateBtn != null)
        {
            rotateBtn.localScale = Vector3.zero;
            CanvasGroup btnCg = rotateBtn.GetComponent<CanvasGroup>();
            if (btnCg != null) btnCg.alpha = 0f;
        }

        List<Transform> activeChildren = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != rotateBtn && child.gameObject.activeSelf)
            {
                activeChildren.Add(child);
            }
        }

        int childCount = activeChildren.Count;
        if (childCount == 0) return;

        float angleStep = 360f / childCount;

        // Pozisyonlama ve Başlangıç Durumu (Gizli)
        for (int i = 0; i < childCount; i++)
        {
            float angle = (i * angleStep) * Mathf.Deg2Rad;
            Vector3 newPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

            RectTransform child = activeChildren[i] as RectTransform;
            if (child != null)
            {
                child.localPosition = newPos;
                child.localScale = Vector3.zero; // Başlangıçta scale 0

                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0f; // Başlangıçta görünmez
                }
            }
        }

        // Animasyonu Başlat
        StartCoroutine(AnimateRoutine(activeChildren));
    }

    private IEnumerator AnimateRoutine(List<Transform> targets)
    {
        foreach (var child in targets)
        {
            if (child == null) continue;

            // Scale Animasyonu
            child.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);

            // Fade Animasyonu
            CanvasGroup cg = child.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOFade(1f, animationDuration);
            }

            yield return new WaitForSeconds(animationDelay);
        }
        
        if (rotateBtn != null)
        {
            rotateBtn.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
            CanvasGroup btnCg = rotateBtn.GetComponent<CanvasGroup>();
            if (btnCg != null) btnCg.DOFade(1f, animationDuration);
        }
    }

    private bool isShuffling = false;

    public void ShuffleElements()
    {
        if (isShuffling) return;

        int childCount = transform.childCount;
        if (childCount < 2) return;

        // Çocukları listeye al (rotateBtn hariç)
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            // Eğer rotateBtn hiyerarşide child ise onu dahil etme
            if (child != rotateBtn)
            {
                children.Add(child);
            }
        }

        if (children.Count < 2) return;

        isShuffling = true;

        // Rastgele 2 veya daha fazla elemanı yer değiştir
        int swapCount = children.Count / 2;
        if (swapCount < 1) swapCount = 1;

        for (int i = 0; i < swapCount; i++)
        {
            int indexA = UnityEngine.Random.Range(0, children.Count);
            int indexB = UnityEngine.Random.Range(0, children.Count);

            if (indexA == indexB) 
                indexB = (indexA + 1) % children.Count;

            Transform temp = children[indexA];
            children[indexA] = children[indexB];
            children[indexB] = temp;
        }

        // Pozisyonları Animasyonla Güncelle
        UpdatePositionsAnimated(children);

        // Animasyon süresi kadar bekle ve kilidi aç
        DOVirtual.DelayedCall(animationDuration, () => isShuffling = false);
    }

    private void UpdatePositionsAnimated(List<Transform> children)
    {
        float angleStep = 360f / children.Count;
        for (int i = 0; i < children.Count; i++)
        {
            float angle = (i * angleStep) * Mathf.Deg2Rad;
            Vector3 targetPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

           

            children[i].DOLocalMove(targetPos, animationDuration).SetEase(Ease.OutBack);
        }
    }
}