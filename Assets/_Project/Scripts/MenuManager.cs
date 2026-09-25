using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;

// Handles the Welcome panel and the Play / Play Again / Quit buttons.
// - At start: shows the Welcome panel, pauses the game and shows the mouse cursor.
// - Play: hides the panel, starts the timer, hides the cursor for camera control.
// - When the game ends (win or lose): shows the cursor so the buttons can be clicked.
public class MenuManager : MonoBehaviour
{
    public GameObject welcomePanel;
    [Tooltip("The PlayerArmature's StarterAssetsInputs (for cursor / camera look)")]
    public StarterAssetsInputs playerInput;

    void Start()
    {
        PuzzleManager.Instance.OnGameEnded += HandleGameEnded;   // event-based

        if (welcomePanel) welcomePanel.SetActive(true);
        Time.timeScale = 0f;          // freeze everything behind the panel
        SetMenuMode(true);
    }

    void OnDestroy()
    {
        if (PuzzleManager.Instance != null) PuzzleManager.Instance.OnGameEnded -= HandleGameEnded;
    }

    // ---- Buttons (hook these up in each Button's OnClick) ----

    public void Play()
    {
        if (welcomePanel) welcomePanel.SetActive(false);
        Time.timeScale = 1f;
        SetMenuMode(false);
        PuzzleManager.Instance.StartGame();
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // stop Play mode in the Editor
#else
        Application.Quit();
#endif
    }

    // ---- Helpers ----

    void HandleGameEnded()
    {
        SetMenuMode(true);
    }

    // Menu mode = cursor visible and camera look disabled
    void SetMenuMode(bool menu)
    {
        Cursor.lockState = menu ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = menu;

        if (playerInput)
        {
            playerInput.cursorLocked = !menu;
            playerInput.cursorInputForLook = !menu;
            playerInput.look = Vector2.zero;
            playerInput.move = Vector2.zero;
        }
    }
}
