using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using System.Collections; // Added for IEnumerator

public class HintManager : MonoBehaviour
{
    [SerializeField] private Button harfHintBtn;
    [SerializeField] private Button harflerHintBtn;
    [SerializeField] private Button kelimeHintBtn; 
    [SerializeField] private Sprite redSprite;
    [SerializeField] private float completionDelay = 1.0f; // New
    
    private WordManager wordManager;
    private LetterBoxesManager letterBoxesManager;

    private List<int> _revealedIndices = new List<int>();

    void Start()
    {
        if (harfHintBtn != null)
        {
            harfHintBtn.onClick.AddListener(OnHintClicked);
        }

        if (harflerHintBtn != null)
        {
            harflerHintBtn.onClick.AddListener(OnMultiHintClicked);
        }

        if (kelimeHintBtn != null)
        {
            kelimeHintBtn.onClick.AddListener(OnWordHintClicked);
        }

        if (wordManager == null) wordManager = FindAnyObjectByType<WordManager>();
        if (letterBoxesManager == null) letterBoxesManager = FindAnyObjectByType<LetterBoxesManager>();

        WordManager.OnQuestionProgressUpdated += OnQuestionProgressUpdated;
    }

    private void OnDestroy()
    {
        WordManager.OnQuestionProgressUpdated -= OnQuestionProgressUpdated;
    }

    private void OnQuestionProgressUpdated(int current, int total)
    {
        _revealedIndices.Clear();
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool state)
    {
        if (harfHintBtn != null) harfHintBtn.interactable = state;
        if (harflerHintBtn != null) harflerHintBtn.interactable = state;
        if (kelimeHintBtn != null) kelimeHintBtn.interactable = state;
    }

    private void OnHintClicked()
    {
        RevealRandomLetters(1);
    }

    private void OnMultiHintClicked()
    {
        if (wordManager == null) return;
        string answer = wordManager.CurrentAnswer;
        if (string.IsNullOrEmpty(answer)) return;

        int countToReveal = 2;
        if (answer.Length > 3)
        {
            countToReveal = Random.Range(2, 4); // 2 or 3
        }
        else if (answer.Length == 3)
        {
             countToReveal = 2;
        }
        
        RevealRandomLetters(countToReveal);
    }

    private void OnWordHintClicked()
    {
        // Reveal all remaining
        if (wordManager == null) return;
        string answer = wordManager.CurrentAnswer;
        if (string.IsNullOrEmpty(answer)) return;
        
        RevealRandomLetters(answer.Length); 
    }

    private void RevealRandomLetters(int count)
    {
        if (wordManager == null || letterBoxesManager == null) return;
        
        string answer = wordManager.CurrentAnswer;
        if (string.IsNullOrEmpty(answer)) return;

        // Lock buttons during process
        SetButtonsInteractable(false);

        // Find unrevealed indices
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < answer.Length; i++)
        {
            // Only add if not already revealed
            if (!_revealedIndices.Contains(i))
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0) 
        {
            // Already full? Should trigger win but just in case
            return;
        }

        int revealCount = Mathf.Min(count, availableIndices.Count);

        for (int i = 0; i < revealCount; i++)
        {
            if (availableIndices.Count == 0) break;

            int randIndexInList = Random.Range(0, availableIndices.Count);
            int targetIndex = availableIndices[randIndexInList];
            
            RevealLetter(targetIndex, answer[targetIndex]);
            
            availableIndices.RemoveAt(randIndexInList);
        }

        // If NOT complete, re-enable buttons after a short delay (optional, or immediate)
        // Check if actually completed
        if (_revealedIndices.Count < answer.Length)
        {
            // Opsiyonel: Animasyon süresi kadar bekleyip açabiliriz ama şimdilik direkt açalım
            // Kullanıcı seri basamasın diye Invoke ile açmak daha iyi olabilir
            Invoke(nameof(EnableButtons), 0.5f);
        }
        else
        {
            // Completed. Keep buttons disabled. 
            // Level transition will happen after delay.
        }
    }

    private void EnableButtons()
    {
        SetButtonsInteractable(true);
    }

    private void RevealLetter(int index, char letter)
    {
        // Add to revealed list
        if (!_revealedIndices.Contains(index))
        {
            _revealedIndices.Add(index);
        }

        if (letterBoxesManager.ActiveBoxes != null && index < letterBoxesManager.ActiveBoxes.Count)
        {
            GameObject box = letterBoxesManager.ActiveBoxes[index];
            if (box != null)
            {
                // Set Sprite
                Image img = box.GetComponent<Image>();
                if (img != null && redSprite != null)
                {
                    img.sprite = redSprite;
                }

                // Set Text
                TextMeshProUGUI txt = box.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = letter.ToString().ToUpper();
                    txt.color = Color.white; 
                }

                // Animation
                box.transform.DOKill(true);
                box.transform.DOShakeScale(0.5f, 0.3f, 10, 90, true);
            }
        }

        // Check Completion
        if (_revealedIndices.Count >= wordManager.CurrentAnswer.Length)
        {
            CancelInvoke(nameof(EnableButtons));
            SetButtonsInteractable(false);

            StartCoroutine(DelayedWinAnimation());
        }
    }

    private System.Collections.IEnumerator DelayedWinAnimation()
    {
        yield return new WaitForSeconds(completionDelay);

        // "Sanki doğru bilmiş gibi" - Trigger level complete with animation
        wordManager.PlayWinAnimation(() => 
        {
            wordManager.TriggerLevelCompletion();
        });
    }
}
