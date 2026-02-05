using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

public class WordButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{

    //bu scripti harflerin üzerine ekledik. sürükleyp bırakmadaki kontrolleri yapıyor
    private WordConnectManager manager;
    private TextMeshProUGUI _textMesh;

    // Prefined colors
    private Color _selectedColor;
    private Color _defaultColor;

    public void Init(WordConnectManager mgr)
    {
        manager = mgr;
    }

    private void Awake()
    {
        if (manager == null)
        {
            manager = GetComponentInParent<WordConnectManager>();
        }

        // Initialize colors from hex strings
        ColorUtility.TryParseHtmlString("#FFE5AC", out _selectedColor);
        ColorUtility.TryParseHtmlString("#573F0A", out _defaultColor);


        //textin rengini ayarlamak için
        // Cache the TextMeshPro component from Child 1 if it exists
        if (transform.childCount > 1)
        {
            _textMesh = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        manager.OnButtonDown(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        manager.OnButtonEnter(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        manager.OnButtonUp(eventData);
    }

    public void Toggle(bool state)
    {
        // Toggle visual (Child 0)
        if (transform.childCount > 0)
        {
            Transform visual = transform.GetChild(0);
            CanvasGroup cg = visual.GetComponent<CanvasGroup>();

            visual.DOKill();
            if (cg != null) cg.DOKill();

            if (state)
            {
                visual.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                if (cg != null) cg.DOFade(1f, 0.2f);
            }
            else
            {
                visual.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
                if (cg != null) cg.DOFade(0f, 0.2f);
            }
        }

        // Toggle Text Color (Child 1)
        if (_textMesh != null)
        {
            _textMesh.DOColor(state ? _selectedColor : _defaultColor, 0.2f);
        }
    }
    public string GetLetter()
    {
        return _textMesh != null ? _textMesh.text : "";
    }
}