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
            // Doğru cevap işlemler
            
            // 1. Çizgileri sil ama YAZILARI SİLME. Animasyonun çalışması için harflerin kalması gerekir.
            _wordConnectManager.IsInteractable = false;
            _wordConnectManager.ResetSelection(false, false); 

            // 2. Harfleri Kutulara Taşı
            _wordManager.MoveLettersToBoxes(() => 
            {
               // Animasyon bittiğinde yapılacaklar buraya eklenebilir.
               // Örn: Yeni soruya geçiş.
            });
        }
        else
        {
            // Yanlış cevap işlemleri
            // Sadece resetle (True = Shake efekti ile, Default ClearText = true ile yazıları sil)
            _wordConnectManager.ResetSelection(true);
        }
    }
}
