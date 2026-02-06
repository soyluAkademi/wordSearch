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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static bool isGameJustStarted = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;

        // Oyun ilk açıldığında kontrol et
        if (isGameJustStarted)
        {
            if (PlayerPrefs.HasKey("LastPlayedLevel"))
            {
                string lastLevel = PlayerPrefs.GetString("LastPlayedLevel");
                // Eğer kayıtlı level şu anki leveldan farklıysa oraya git
                if (!string.IsNullOrEmpty(lastLevel) && lastLevel != activeSceneName)
                {
                    isGameJustStarted = false; // Redirecting, so flag as handled
                    SceneManager.LoadScene(lastLevel);
                    return; 
                }
            }
            
            isGameJustStarted = false;
            // İlk sahne için manuel kaydetme (OnSceneLoaded çalışmayabilir)
            SaveAndNotify(activeSceneName);
        }
        else
        {
            // Eğer sahneye sonradan bu script eklenirse veya instance korunmazsa
            SaveAndNotify(activeSceneName);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SaveAndNotify(scene.name);
    }

    private void SaveAndNotify(string levelName)
    {
        currentLevel = levelName;
        PlayerPrefs.SetString("LastPlayedLevel", currentLevel);
        PlayerPrefs.Save();

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
