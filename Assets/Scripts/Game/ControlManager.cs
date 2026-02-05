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

    [SerializeField] private GameObject successParticle;
    [SerializeField] private float successDelay = 2.0f;

    public void CheckWord(string createdWord)
    {
        if (_wordManager == null || _wordConnectManager == null) return;

        string targetWord = _wordManager.CurrentAnswer;
        
        if (string.Equals(createdWord, targetWord, System.StringComparison.OrdinalIgnoreCase))
        {
            // Doğru cevap işlemler
            
            // 1. Çizgileri sil ama YAZILARI SİLME. Animasyonun çalışması için harflerin kalması gerekir.
            _wordConnectManager.IsInteractable = false;
            _wordConnectManager.ResetSelection(false, false); 


            // 2. Harfleri Kutulara Taşı
            _wordManager.MoveLettersToBoxes(() => 
            {
               // Animasyon bitti, partikül aç
               if (successParticle != null) successParticle.SetActive(true);

               // Artık yeni soruya geçişi transition ile yapıyoruz
               _wordManager.TriggerLevelTransition();
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
