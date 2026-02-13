using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

public class CarkManager : MonoBehaviour
{
    [Header("Atamalar")]
    [Tooltip("Dönecek olan çark görseli (Disk)")]
    [SerializeField] private Transform carkDiski;
    
    [Tooltip("Çevirme Butonu")]
    [SerializeField] private Button cevirButonu;

    [Tooltip("Çark dönerken -2, 2 arası gidip gelecek obje (İbre)")]
    [SerializeField] private Transform kirmiziRotateImg;

    [Header("Panel Ayarları")]
    [Tooltip("Kapatma Butonu")]
    [SerializeField] private Button exitBtn;

    [Tooltip("Animasyonla kapanacak panel görseli")]
    [SerializeField] private Transform carkPanel;

    [Tooltip("Animasyonla kapanacak panelin CanvasGroup'u (Alpha için)")]
    [SerializeField] private CanvasGroup carkPanelCanvasGroup;

    [Tooltip("Tamamen kapanınca deaktif olacak ana obje")]
    [SerializeField] private GameObject carkPanelObje;

    [Header("Ayarlar")]
    [SerializeField] private int turSayisi = 5; // Kaç tam tur atacak
    [SerializeField] private float donmeSuresi = 4f; // Saniye

    private bool _donuyor = false;
    private int _toplamAgirlik = 0;

    [System.Serializable]
    public class CarkDilimi
    {
        public int odulMiktari;
        public float aci; // Çarkın duracağı açı (0, 45, 90...)
        public int agirlik; // Gelme ihtimali (Yüksek sayı = Yüksek ihtimal)

        public CarkDilimi(int odul, float a, int ag)
        {
            odulMiktari = odul;
            aci = a;
            agirlik = ag;
        }
    }

    private List<CarkDilimi> _dilimler;

    private void OnEnable()
    {
        // Panel tekrar açıldığında görünür olduğundan emin ol
        if (carkPanel != null) carkPanel.localScale = Vector3.one;
        if (carkPanelCanvasGroup != null) carkPanelCanvasGroup.alpha = 1f;
    }

    private void Start()
    {
        DilimleriHazirla();

        if (cevirButonu != null)
        {
            cevirButonu.onClick.RemoveAllListeners();
            cevirButonu.onClick.AddListener(CarkiCevir);
        }

        if (exitBtn != null)
        {
            exitBtn.onClick.RemoveAllListeners();
            exitBtn.onClick.AddListener(CarkiKapat);
        }
    }

    private void DilimleriHazirla()
    {
        _dilimler = new List<CarkDilimi>();

        // Açılar ve Ödüller
        // 0 derece -> 15 Altın
        // 45 derece -> 25 Altın
        // ...
        // 315 derece -> 250 Altın (Çok nadir)

        // Kullanıcı isteği: 250 hariç diğerleri eşit ihtimalle gelsin.
        int standartAgirlik = 100;

        _dilimler.Add(new CarkDilimi(15, 0, standartAgirlik));
        _dilimler.Add(new CarkDilimi(25, 45, standartAgirlik));
        _dilimler.Add(new CarkDilimi(35, 90, standartAgirlik));
        _dilimler.Add(new CarkDilimi(50, 135, standartAgirlik));
        _dilimler.Add(new CarkDilimi(60, 180, standartAgirlik));
        _dilimler.Add(new CarkDilimi(75, 225, standartAgirlik));
        _dilimler.Add(new CarkDilimi(100, 270, standartAgirlik));
        _dilimler.Add(new CarkDilimi(250, 315, 5)); // Jackpot (Çok nadir: 5/705 ≈ %0.7)

        _toplamAgirlik = _dilimler.Sum(d => d.agirlik);
    }

    public void CarkiCevir()
    {
        if (_donuyor || carkDiski == null) return;

        _donuyor = true;
        if (cevirButonu != null) cevirButonu.interactable = false;

        // İbre Animasyonu Başlat
        if (kirmiziRotateImg != null)
        {
            kirmiziRotateImg.DOKill();
            // -2 ile 2 arasında hızlıca gidip gel (Yoyo)
            kirmiziRotateImg.localRotation = Quaternion.Euler(0, 0, 2);
            kirmiziRotateImg.DOLocalRotate(new Vector3(0, 0, -2), 0.15f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.Linear);
        }

        // 1. Rastgele bir ödül seç
        CarkDilimi hedefDilim = RastgeleDilimSec();
     

        // 2. Dönme Açısını Hesapla
        // Çarkın mevcut açısını al
        float mevcutZ = carkDiski.eulerAngles.z;

        // Hedef açıya gitmek için ne kadar dönmeli?
        // Unity'de Z ekseni CCW (saat yönü tersine) artar.
        // Hedef dilimin ÜSTE (0 dereceye) gelmesi için: 
        // Eğer dilim 45 derecedeyse (Sağ Üst, 1:30 yönü), bunu 0'a getirmek için sola (CCW, +) 45 derece dönmeliyiz.
        // Yani hedef açı POZİTİF olmalı.

        float hedefAci = hedefDilim.aci;

        // Sapma kaldırıldı, tam açıya gitmeli
        float sapma = 0f;

        // Toplam Dönüş: (Tur Sayısı * 360) + Hedef Açı
        // Saat yönünde dönsün istiyoruz (Negatif yön)
        float gidilecekZ = hedefAci + sapma - (360f * turSayisi);

        // Mevcut açıdan bağımsız, relatif olarak döndürmek yerine direkt açıya gitmesini sağlıyoruz (RotateMode.FastBeyond360)
        
        carkDiski.DORotate(new Vector3(0, 0, gidilecekZ), donmeSuresi, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic) // Hızlı başla, yavaşça dur
            .OnComplete(() => DonmeBitti(hedefDilim));
    }

    private CarkDilimi RastgeleDilimSec()
    {
        int rastgeleDeger = Random.Range(0, _toplamAgirlik);
        int mevcutAgirlik = 0;

        foreach (var dilim in _dilimler)
        {
            mevcutAgirlik += dilim.agirlik;
            if (rastgeleDeger < mevcutAgirlik)
            {
                return dilim;
            }
        }
        return _dilimler[0];
    }

    private void DonmeBitti(CarkDilimi dilim)
    {
        _donuyor = false;
        if (cevirButonu != null) cevirButonu.interactable = true;

        // İbre Animasyonu Durdur
        if (kirmiziRotateImg != null)
        {
            kirmiziRotateImg.DOKill();
            kirmiziRotateImg.localRotation = Quaternion.identity; // 0'a çek
        }

        // Altın Ekle
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGold(dilim.odulMiktari);
            
        }
    }

    public void CarkiKapat()
    {
        // Eğer dönüyorsa kapatmaya izin verme (isteğe bağlı, şimdilik serbest)
        // if (_donuyor) return; 

        if (carkPanel == null || carkPanelCanvasGroup == null)
        {
            // Referans yoksa direkt kapat
            if (carkPanelObje != null) carkPanelObje.SetActive(false);
            return;
        }

        // Scale küçültme ve Alpha düşürme animasyonu
        carkPanel.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
        carkPanelCanvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
        {
            if (carkPanelObje != null) carkPanelObje.SetActive(false);
        });
    }
}
