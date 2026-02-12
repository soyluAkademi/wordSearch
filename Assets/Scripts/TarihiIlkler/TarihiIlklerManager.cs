using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class TarihiIlklerManager : MonoBehaviour
{
    public static TarihiIlklerManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject contentContainer; // The "0. obje" to animate
    [SerializeField] private TextMeshProUGUI notTxt;
    [SerializeField] private Button devamEtBtn;

    [Header("Data")]
    [SerializeField] private string jsonFileName = "facts";
    
    private List<HistoricalFact> _facts = new List<HistoricalFact>();
    
    // We track which fact to show using PlayerPrefs to progress sequentially through the list
    private int _currentFactIndex = 0;

    private System.Action _onCompleteCallback;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject) is optional depending on architecture.
            // If WordManager is destroyed/reloaded on scene change, this needs to persist or re-init.
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadFacts();

        if (panel != null) panel.SetActive(false);
        if (devamEtBtn != null) devamEtBtn.onClick.AddListener(OnContinueClicked);
    }

    private void LoadFacts()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFileName);
        if (jsonFile != null)
        {
            FactData data = JsonUtility.FromJson<FactData>(jsonFile.text);
            if (data != null && data.historical_facts != null)
            {
                _facts = data.historical_facts;
            }
        }
        else
        {
            Debug.LogError($"'{jsonFileName}' JSON file not found in Resources!");
        }

        // Load saved index if needed to persist across sessions
        _currentFactIndex = PlayerPrefs.GetInt("TarihiIlkler_Index", 0);
    }

    public void CheckAndShowFact(int currentQuestionIndex, System.Action onComplete)
    {
        // currentQuestionIndex is 0-based.
        // It represents the question that was JUST COMPLETED.
        
        // Calculate Absolute Level (1-based)
        // 15 questions per level.
        // Index 0-14 -> Level 1
        // Index 15-29 -> Level 2
        // ...
        int absoluteLevel = (currentQuestionIndex / 15) + 1;
        
        // Calculate Question Number within Current Level (1-15)
        // Index 14 -> Question 15
        int questionInLevel = (currentQuestionIndex % 15) + 1;

        // Logic Requirements:
        // 1. Skip first few levels (e.g. start at level 3, since user said "after first 15 questions" and "ask at odd levels").
        //    Level 1 (Questions 1-15) -> Skip.
        //    Level 2 (Questions 16-30) -> Even, Skip.
        //    Level 3 -> First ODD level to show.
        // 2. Only ODD levels (3, 5, 7...).
        // 3. Random question between 5 and 12 in that level.

        bool isOddLevel = (absoluteLevel % 2 != 0);
        bool isLevelHighEnough = (absoluteLevel >= 3); 

        if (isOddLevel && isLevelHighEnough)
        {
            // Determine the target question index for this specific level.
            // We use a deterministic random based on level number so it doesn't change if the user restarts the app mid-level.
            int targetQuestion = GetTargetQuestionForLevel(absoluteLevel);

            if (questionInLevel == targetQuestion)
            {
                // Condition met!
                if (_facts.Count > 0)
                {
                    _onCompleteCallback = onComplete;
                    ShowFact();
                    return;
                }
            }
        }

        // If condition not met, proceed immediately
        onComplete?.Invoke();
    }

    private int GetTargetQuestionForLevel(int level)
    {
        // Use a seed based on level to get a consistent random number for that level
        // Random.InitState modifies global state, better to use a simple hash or local Random instance if available.
        // Unity's Random.InitState is global. To avoid side effects on game logic, verify where this is called.
        // It's called at end of level, likely safe. Or use System.Random.
        
        System.Random rng = new System.Random(level * 12345); // deterministic seed
        // Returns integer between 5 and 12 (inclusive 5, exclusive 13)
        return rng.Next(5, 13); 
    }

    private void ShowFact()
    {
        if (panel == null || contentContainer == null || notTxt == null)
        {
            Debug.LogError("TarihiIlklerManager UI references missing!");
            _onCompleteCallback?.Invoke();
            return;
        }

        // Get current fact
        if (_currentFactIndex >= _facts.Count) _currentFactIndex = 0; // Loop if exhausted
        HistoricalFact factParams = _facts[_currentFactIndex];
        
        notTxt.text = factParams.fact;

        // Increment and save index
        _currentFactIndex++;
        PlayerPrefs.SetInt("TarihiIlkler_Index", _currentFactIndex);
        PlayerPrefs.Save();

        // Animations
        panel.SetActive(true);

        // Prep container for animation
        contentContainer.transform.localScale = Vector3.zero;
        CanvasGroup cg = contentContainer.GetComponent<CanvasGroup>();
        if (cg == null) cg = contentContainer.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Animate In
        contentContainer.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        cg.DOFade(1f, 0.5f);
    }

    private void OnContinueClicked()
    {
        if (contentContainer != null)
        {
            // Animate Out
            CanvasGroup cg = contentContainer.GetComponent<CanvasGroup>();
            contentContainer.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
            if(cg != null) cg.DOFade(0f, 0.3f);
        }

        // Wait for animation then close
        DOVirtual.DelayedCall(0.35f, () => 
        {
            if (panel != null) panel.SetActive(false);
            _onCompleteCallback?.Invoke();
            _onCompleteCallback = null;
        });
    }

    [System.Serializable]
    public class FactData
    {
        public List<HistoricalFact> historical_facts;
    }

    [System.Serializable]
    public class HistoricalFact
    {
        public int id;
        public string fact;
    }
}
