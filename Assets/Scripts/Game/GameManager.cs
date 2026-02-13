using UnityEngine;
using System;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private int _totalScore;
    public int TotalScore => _totalScore;

    [Header("Game Progress")]
    [Header("Game Progress")]
    public int CurrentQuestionNumber; // 1-based index (count)

    private int _currentLevelScore;
    public int CurrentLevelScore => _currentLevelScore;

    // Timer variables
    private float _startTime;
    private bool _isTimerRunning;

    // Events
    public static event Action<int> OnScoreUpdated; // int: current animated score to display
    public static event Action OnScoreAnimationStart;
    public static event Action OnScoreAnimationEnd;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadScore();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadScore()
    {
        _totalScore = PlayerPrefs.GetInt("TotalScore", 0);
        _currentLevelScore = PlayerPrefs.GetInt("CurrentLevelScore", 0);
    }

    private void SaveScore()
    {
        PlayerPrefs.SetInt("TotalScore", _totalScore);
        PlayerPrefs.SetInt("CurrentLevelScore", _currentLevelScore);
        PlayerPrefs.Save();
    }

    public void ResetLevelScore()
    {
        _currentLevelScore = 0;
        PlayerPrefs.SetInt("CurrentLevelScore", 0);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        // Initial UI update
        OnScoreUpdated?.Invoke(_totalScore);
    }

    public void StartTimer()
    {
        _startTime = Time.time;
        _isTimerRunning = true;
        // Debug.Log("Timer Started: " + _startTime);
    }

    public void StopTimer()
    {
        _isTimerRunning = false;
    }

    public void CalculateAndAddScore(int wordLength)
    {
        if (!_isTimerRunning)
        {
            // If timer wasn't running (maybe mostly for testing or edge cases), just use base score
            AddScore(wordLength * 5); 
            return;
        }

        StopTimer();
        float duration = Time.time - _startTime;
        // Debug.Log("Duration: " + duration);

        int multiplier = 5; // Default (Tier 4)

        if (duration < 10f)
        {
            multiplier = 20; // Tier 1
        }
        else if (duration < 20f)
        {
            multiplier = 15; // Tier 2
        }
        else if (duration < 30f)
        {
            multiplier = 10; // Tier 3
        }

        int scoreToAdd = wordLength * multiplier;
        AddScore(scoreToAdd);
    }

    private void AddScore(int amount)
    {
        int oldScore = _totalScore;
        _totalScore += amount;
        _currentLevelScore += amount;
        SaveScore();

        // Animate the score
        // We simulate the "counting up" effect
        // We use a temporary value to tween
        int tempScore = oldScore;
        
        OnScoreAnimationStart?.Invoke();

        DOTween.To(()=> tempScore, x=> tempScore = x, _totalScore, 1.0f)
            .OnUpdate(() => 
            {
                OnScoreUpdated?.Invoke(tempScore);
            })
            .OnComplete(() => 
            {
                OnScoreUpdated?.Invoke(_totalScore); // Ensure final value is set
                OnScoreAnimationEnd?.Invoke();
            });
    }
}
