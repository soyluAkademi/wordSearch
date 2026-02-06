using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

public class WordConnectManager : MonoBehaviour
{
    private int _wordLength;
    private List<WordButton> m_buttons = new List<WordButton>();
    private List<WordButton> m_selectedButtons = new List<WordButton>();
    private bool isDragging = false;
    public bool IsInteractable = true;

    [SerializeField] Button rotateBtn;
    
    [SerializeField] private RectTransform linePrefab;
    [SerializeField] private float maxRadius = 350f; // Çizgi çizme sınırı
    private List<RectTransform> m_lines = new List<RectTransform>();
   
    private Canvas _canvas;
    private ControlManager _controlManager;

    private WordManager _wordManager;
    
    // Legacy list removed


    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        _controlManager = FindAnyObjectByType<ControlManager>();
        _wordManager = FindAnyObjectByType<WordManager>();
    }

    public void GetWordLength(int length)
    {
        _wordLength = length;
        CreateButtons();
        if (rotateBtn != null) rotateBtn.interactable = true;
    }
    
    // ... (CreateButtons, OnButtonDown, etc. are unchanged, assume they exist)

    private void UpdateCurrentLine()
    {
        if (currentLine == null || m_selectedButtons.Count == 0) return;

        WordButton lastBtn = m_selectedButtons[m_selectedButtons.Count - 1];
        Vector3 startPos = transform.InverseTransformPoint(lastBtn.transform.position);
        
        // Converting mouse position to local space
        if (Pointer.current == null) return;
        Vector2 screenPos = Pointer.current.position.ReadValue();

        Vector2 localMousePos;
        Camera cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, screenPos, cam, out localMousePos);
        
        // Yarıçap kontrolü
        if (localMousePos.magnitude > maxRadius)
        {
            localMousePos = localMousePos.normalized * maxRadius;
        }

        Vector3 direction = (Vector3)localMousePos - startPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        currentLine.localPosition = startPos;
        currentLine.localRotation = Quaternion.Euler(0, 0, angle);
        currentLine.sizeDelta = new Vector2(distance, currentLine.sizeDelta.y);
    }

    private void CreateButtons()
    {
        // Referansları temizle ama mevcut GameObject'leri yok etme (zaten önceden varlar)
        m_buttons.Clear();

        // Mevcut çocuk objeleri (butonları) kullan
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            
            // Eğer çocuk obje etkileşime girmemesi gereken bir objeyse atla (opsiyonel koruma)
            // Şu an için tüm çocukların buton olduğunu varsayıyoruz

            WordButton wb = child.GetComponent<WordButton>();
            if (wb == null) wb = child.gameObject.AddComponent<WordButton>();
            
            wb.Init(this);
            m_buttons.Add(wb);
        }

        // Dairesel düzeni ayarla (RadialLayout entegrasyonu)
        // Sadece gerekliyse çağır, mevcut düzenin yenilenmesi gerekebilir
        RadialLayout radialLayout = GetComponent<RadialLayout>();
        if (radialLayout != null)
        {
            radialLayout.ArrangeElements();
        }
    }

    public void OnButtonDown(WordButton btn)
    {
        if (!IsInteractable) return;

        isDragging = true;
        ResetSelection(); // Her yeni başlangıçta temizle
        AddButton(btn);

        //rotate butonu kapatıyoruz
        if (rotateBtn != null) rotateBtn.interactable = false;
    }

    public void OnButtonEnter(WordButton btn)
    {
        if (!IsInteractable) return;

        if (isDragging)
        {
            AddButton(btn);
        }
    }

    public void OnButtonUp(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        isDragging = false;

        // Takip çizgisini hemen yok et
        if (currentLine != null)
        {
            Destroy(currentLine.gameObject);
            currentLine = null;
        }

        bool releasedOnValidButton = false;
        bool releasedOnAnyButton = false;

        if (eventData != null && eventData.pointerCurrentRaycast.gameObject != null)
        {
             GameObject hitObj = eventData.pointerCurrentRaycast.gameObject;
             WordButton wb = hitObj.GetComponent<WordButton>();
             if (wb == null) wb = hitObj.GetComponentInParent<WordButton>();

             if (wb != null)
             {
                 releasedOnAnyButton = true;
                 if (m_selectedButtons.Count > 0 && wb == m_selectedButtons[m_selectedButtons.Count - 1])
                 {
                     releasedOnValidButton = true;
                 }
             }
        }

        // --- USER REQ FIX: Check success condition ---
        // Kelime uzunluğu tutmalı.
        // VE (Son butonda bıraktık VEYA Hiçbir butona (boşluğa) bırakmadık)
        // Yani yanlış bir butona bırakmadığımız sürece sorun yok.
        bool isSuccess = (m_selectedButtons.Count == _wordLength) && (!releasedOnAnyButton || releasedOnValidButton);

        if (!isSuccess)
        {
            ResetSelection(true); // Yanlış/eksik bırakma -> Shake
            if (rotateBtn != null) rotateBtn.interactable = true;
        }
        else
        {
            // Kelimeyi birleştir
            string createdWord = "";
            foreach (var btn in m_selectedButtons)
            {
                createdWord += btn.GetLetter();
            }

            if (_controlManager != null)
            {
                _controlManager.CheckWord(createdWord);
            }
            else
            {
                ResetSelection();
            }
        }
    }

    private RectTransform currentLine;

    private void Update()
    {
        if (isDragging && m_selectedButtons.Count > 0)
        {
            UpdateCurrentLine();
        }
    }

    private void AddButton(WordButton btn)
    {
        int index = m_selectedButtons.IndexOf(btn);

        if (index == -1)
        {
            // Yeni seçim
            if (m_selectedButtons.Count > 0)
            {
                // Önceki butona olan bağlantıyı kesinleştir
                // Çünkü currentLine sadece fare ile takip ediyordu, şimdi kalıcı çizgi lazım.
                // currentLine'ı yeniden kullanabiliriz veya yok edip yenisini oluşturabiliriz. Yenisini oluşturmak daha temiz.
                if (currentLine != null) Destroy(currentLine.gameObject);

                WordButton prevBtn = m_selectedButtons[m_selectedButtons.Count - 1];
                CreateLine(prevBtn.transform.position, btn.transform.position);
            }

            m_selectedButtons.Add(btn);

            if (_wordManager != null)
            {
                _wordManager.AddLetter(btn.GetLetter());
            }

            btn.Toggle(true);

            // Bu yeni butondan başlayan yeni bir takip çizgisi oluştur
            CreateCurrentLine(btn.transform.position);
        }
        else if (index == m_selectedButtons.Count - 2)
        {
            // Geri gelme (Backtracking) işlemi
            RemoveLastSelection();
            
            // Son butonu kaldırdık, şimdi listedeki yeni son butondan (yani geri geldiğimizden)
            // tekrar bir takip çizgisi başlatmamız lazım.
            if (currentLine != null) Destroy(currentLine.gameObject);
            CreateCurrentLine(btn.transform.position);
        }
    }

    private void RemoveLastSelection()
    {
        if (m_selectedButtons.Count > 0)
        {
            // Text temizleme
            if (_wordManager != null)
            {
                _wordManager.RemoveLastLetter();
            }

            WordButton lastBtn = m_selectedButtons[m_selectedButtons.Count - 1];
            lastBtn.Toggle(false);
            m_selectedButtons.RemoveAt(m_selectedButtons.Count - 1);
        }

        if (m_lines.Count > 0)
        {
            RectTransform lastLine = m_lines[m_lines.Count - 1];
            if (lastLine != null) Destroy(lastLine.gameObject);
            m_lines.RemoveAt(m_lines.Count - 1);
        }
    }

    private void CreateCurrentLine(Vector3 startPosWorld)
    {
        if (linePrefab == null) return;

        currentLine = Instantiate(linePrefab, transform);
        currentLine.SetAsFirstSibling();
        
        // Buton girişini engellememek için raycast'i kapat
        Image img = currentLine.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;

        Vector3 startPos = transform.InverseTransformPoint(startPosWorld);
        currentLine.localPosition = startPos;
        currentLine.sizeDelta = new Vector2(0, currentLine.sizeDelta.y);
    }



    private void CreateLine(Vector3 startPosWorld, Vector3 endPosWorld)
    {
        if (linePrefab == null) return;

        Vector3 startPos = transform.InverseTransformPoint(startPosWorld);
        Vector3 endPos = transform.InverseTransformPoint(endPosWorld);

        RectTransform line = Instantiate(linePrefab, transform);
        m_lines.Add(line);
        line.SetAsFirstSibling();
        
        // Disable raycast
        Image img = line.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;

        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        line.localPosition = startPos;
        line.localRotation = Quaternion.Euler(0, 0, angle);
        line.sizeDelta = new Vector2(distance, line.sizeDelta.y);
    }

    public void ResetSelection(bool shake = false, bool clearText = true)
    {
        foreach (var btn in m_selectedButtons)
        {
            btn.Toggle(false);
        }
        m_selectedButtons.Clear();

        foreach (var line in m_lines)
        {
            if (line != null) Destroy(line.gameObject);
        }
        m_lines.Clear();
        
        // Also clear the tracking line
        if (currentLine != null)
        {
            Destroy(currentLine.gameObject);
            currentLine = null;
        }

        if (_wordManager != null)
        {
            if (shake)
                _wordManager.ShakeAndClear();
            else if (clearText) // Sadece clearText true ise yazıları sil
                _wordManager.ClearLineWords();
        }
    }
}