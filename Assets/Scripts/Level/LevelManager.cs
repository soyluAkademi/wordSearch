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

    private static bool isGameJustStarted = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;

        // Oyun ilk açıldığında kontrol et
        if (isGameJustStarted)
        {
            isGameJustStarted = false;
            
            if (PlayerPrefs.HasKey("LastPlayedLevel"))
            {
                string lastLevel = PlayerPrefs.GetString("LastPlayedLevel");
                // Eğer kayıtlı level şu anki leveldan farklıysa oraya git
                if (!string.IsNullOrEmpty(lastLevel) && lastLevel != activeSceneName)
                {
                    SceneManager.LoadScene(lastLevel);
                    return; // Bu sahnede daha fazla işlem yapma
                }
            }
        }

        // Mevcut leveli kaydet (Her sahne açılışında güncellenir)
        currentLevel = activeSceneName;
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
