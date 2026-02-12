using UnityEngine;

public class TestManager : MonoBehaviour
{
    [Tooltip("Which question/level to jump to? (0-based index)")]
    public int startQuestionIndex = 0;

    private void Start()
    {
        // Wait a frame or two? Usually Start runs after Awake.
        // WordManager Awake loads questions. WordManager Start loads progress.
        // We want to override progress.
        
        // Let's invoke with a tiny delay to ensure WordManager is fully initialized
        // and its own Start() logic has run (which might load old progress).
        // By running this slightly later, we override whatever it loaded.
        Invoke(nameof(Jump), 0.1f);
    }

    private void Jump()
    {
        WordManager wm = FindObjectOfType<WordManager>();
        if (wm != null)
        {
            Debug.Log($"[TestManager] Jumping to Question Index: {startQuestionIndex}");
            wm.JumpToQuestion(startQuestionIndex);
        }
        else
        {
            Debug.LogError("[TestManager] WordManager not found!");
        }
    }
}
