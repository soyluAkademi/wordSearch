using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    // UI veya diğer sistemlerin dinleyebileceği event
    public static event Action<string> OnLevelLoaded;

  
   private string currentLevel;

    public string CurrentLevel => currentLevel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Sahne ismini al
        currentLevel = SceneManager.GetActiveScene().name;

        // Sahne açıldığında mevcut level bilgisini yayınla
        if (!string.IsNullOrEmpty(currentLevel))
        {
            OnLevelLoaded?.Invoke(currentLevel);
        }
    }

    // Dışarıdan level değiştirmek isterseniz bunu kullanabilirsiniz
    public void SetCurrentLevel(string newLevelName)
    {
        currentLevel = newLevelName;
        OnLevelLoaded?.Invoke(currentLevel);
    }
}
