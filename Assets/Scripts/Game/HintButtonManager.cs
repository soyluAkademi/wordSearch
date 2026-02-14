using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using System.Collections; // Added for IEnumerator

public class HintButtonManager : MonoBehaviour
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

    private bool isProcessing = false;

    private void OnQuestionProgressUpdated(int current, int total)
    {
        _revealedIndices.Clear();
        isProcessing = false;
    }

    private void OnHintClicked()
    {
        if (wordManager != null && wordManager.IsInteractionLocked) return;
        if (isProcessing) return;
        RevealRandomLetters(1, 25);
    }

    private void OnMultiHintClicked()
    {
        if (wordManager != null && wordManager.IsInteractionLocked) return;
        if (isProcessing) return;
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
        
        RevealRandomLetters(countToReveal, 50);
    }

    private void OnWordHintClicked()
    {
        if (wordManager != null && wordManager.IsInteractionLocked) return;
        if (isProcessing) return;
        // Reveal all remaining
        if (wordManager == null) return;
        string answer = wordManager.CurrentAnswer;
        if (string.IsNullOrEmpty(answer)) return;
        
        RevealRandomLetters(answer.Length, 100); 
    }

    private void RevealRandomLetters(int count, int cost)
    {
        if (wordManager == null || letterBoxesManager == null) return;
        
        string answer = wordManager.CurrentAnswer;
        if (string.IsNullOrEmpty(answer)) return;

        // Lock inputs
        isProcessing = true;

        // Check if word is already fully resolved (by user or hints)
        if (IsWordFullyResolved(answer))
        {
            isProcessing = false;
            return;
        }

        // Find unrevealed indices (checking board state too)
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < answer.Length; i++)
        {
            // Not in our list AND not visibly solved on board
            if (!_revealedIndices.Contains(i) && !IsLetterAlreadyRevealed(i, answer[i]))
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0) 
        {
            // No slots left to reveal
            // This might happen if user solved everything but didn't trigger win yet?
            // Or simple sync issue. Just unlock.
            isProcessing = false;
            return;
        }

        // --- COST CHECK ---
        if (cost > 0 && GoldManager.Instance != null)
        {
            if (!GoldManager.Instance.SpendGold(cost))
            {
                // Not enough gold
                // TODO: Maybe show "Not Enough Gold" popup?

                isProcessing = false;
                return;
            }
        }

        int revealCount = Mathf.Min(count, availableIndices.Count);
        
        // Pick and reveal
        List<int> chosenIndices = new List<int>();
        for (int i = 0; i < revealCount; i++)
        {
             int randIndexInList = Random.Range(0, availableIndices.Count);
             int targetIndex = availableIndices[randIndexInList];
             chosenIndices.Add(targetIndex);
             availableIndices.RemoveAt(randIndexInList);
        }

        foreach(int idx in chosenIndices)
        {
             RevealLetter(idx, answer[idx]);
        }

        // Re-check completion accurately
        if (!IsWordFullyResolved(answer))
        {
            Invoke(nameof(ResetProcessing), 0.5f);
        }
        else
        {
            // Completed. 
            // RevealLetter will trigger win if _revealedIndices count allows, 
            // but we should arguably trigger if board is full regardless of who revealed it.
            // For now, relying on RevealLetter's check which we might need to update or sync.
        }
    }

    private bool IsLetterAlreadyRevealed(int index, char expectedChar)
    {
        if (letterBoxesManager == null || letterBoxesManager.ActiveBoxes == null) return false;
        if (index >= letterBoxesManager.ActiveBoxes.Count) return false;

        GameObject box = letterBoxesManager.ActiveBoxes[index];
        if (box == null) return false;

        TextMeshProUGUI txt = box.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null && !string.IsNullOrWhiteSpace(txt.text))
        {
            // If text matches expected char, it is revealed
            return txt.text.Trim().ToUpper() == expectedChar.ToString().ToUpper();
        }
        return false;
    }

    private bool IsWordFullyResolved(string answer)
    {
        for (int i = 0; i < answer.Length; i++)
        {
            if (_revealedIndices.Contains(i)) continue;
            if (IsLetterAlreadyRevealed(i, answer[i])) continue;
            return false; // Found an unresolved letter
        }
        return true;
    }

    private void ResetProcessing()
    {
        isProcessing = false;
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
            CancelInvoke(nameof(ResetProcessing));
            // isProcessing remains true, effectively locking input until level change

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
