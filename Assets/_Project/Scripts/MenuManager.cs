using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;


public class MenuManager : MonoBehaviour
{
    public GameObject welcomePanel;
    [Tooltip("The PlayerArmature's StarterAssetsInputs (for cursor / camera look)")]
    public StarterAssetsInputs playerInput;

    void Start()
    {
        PuzzleManager.Instance.OnGameEnded += HandleGameEnded;   

        if (welcomePanel) welcomePanel.SetActive(true);
        Time.timeScale = 0f;          
        SetMenuMode(true);
    }

    void OnDestroy()
    {
        if (PuzzleManager.Instance != null) PuzzleManager.Instance.OnGameEnded -= HandleGameEnded;
    }

    

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
        UnityEditor.EditorApplication.isPlaying = false;   
#else
        Application.Quit();
#endif
    }

    

    void HandleGameEnded()
    {
        SetMenuMode(true);
    }

    
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
