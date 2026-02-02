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
            // 1. Seçimleri sıfırla (çizgileri sil)
            _wordConnectManager.ResetSelection();
            
            // 2. Yeni Soruya Geç
            // Biraz gecikme eklemek güzel olabilir ama şimdilik doğrudan geçiyoruz.
            _wordManager.NextQuestion();
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
