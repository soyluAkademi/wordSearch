using UnityEngine;
using System;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }

    public static event Action<int> OnGoldChanged;

    private const string PREF_GOLD = "PlayerGold";
    private int _currentGold;

    public int CurrentGold => _currentGold;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGold();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadGold()
    {
        // Default 150 gold if key doesn't exist
        _currentGold = PlayerPrefs.GetInt(PREF_GOLD, 150);
    }

    private void Start()
    {
        // Notify UI at start
        OnGoldChanged?.Invoke(_currentGold);
    }

    public void AddGold(int amount)
    {
        _currentGold += amount;
        SaveGold();
        OnGoldChanged?.Invoke(_currentGold);
    }

    public bool SpendGold(int amount)
    {
        if (_currentGold >= amount)
        {
            _currentGold -= amount;
            SaveGold();
            OnGoldChanged?.Invoke(_currentGold);
            return true;
        }
        return false;
    }

    public bool HasEnoughGold(int amount)
    {
        return _currentGold >= amount;
    }

    public void ResetGold()
    {
        _currentGold = 150; // Default start amount
        SaveGold();
        OnGoldChanged?.Invoke(_currentGold);
    }

    private void SaveGold()
    {
        PlayerPrefs.SetInt(PREF_GOLD, _currentGold);
        PlayerPrefs.Save();
    }
}
