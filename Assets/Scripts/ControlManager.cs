using UnityEngine;

public class ControlManager : MonoBehaviour
{
    private WordManager _wordManager;
    private WordConnectManager _wordConnectManager;

    private void Awake()
    {
        _wordManager = FindAnyObjectByType<WordManager>();
        _wordConnectManager = FindAnyObjectByType<WordConnectManager>();
    }

    public void CheckWord(string createdWord)
    {
        if (_wordManager == null || _wordConnectManager == null) return;

        string targetWord = _wordManager.CurrentAnswer;
        
        // Büyük/küçük harf duyarlılığını kaldırmak isterseniz ToUpper() kullanabilirsiniz.
        // Genelde veriler büyük harf tutulur.
        if (string.Equals(createdWord, targetWord, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("DOĞRU KELİME!");
            
            // Doğru cevap işlemler
            // Doğru cevap işlemler
            // 1. Seçimleri sıfırla (çizgileri sil) - ŞİMDİLİK İPTAL
            // _wordConnectManager.ResetSelection();
            
            // 2. Yeni Soruya Geç - ŞİMDİLİK İPTAL
            // _wordManager.NextQuestion();

            // TODO: Buraya animasyon vb. eklenecek.
        }
        else
        {
            Debug.Log("YANLIŞ KELİME!");
            
            // Yanlış cevap işlemleri
            // Sadece resetle
            _wordConnectManager.ResetSelection(true);
        }
    }
}
