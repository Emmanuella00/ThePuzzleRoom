using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

// The "brain" of the puzzle room.
// - Tracks which of the 5 tasks are completed (progress 0/5 ... 5/5)
// - Reveals one digit of the door code per completed task
// - Runs the checkout timer (lose condition)
// - Shows the Win / Lose screens and lets the player restart with R
public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    // Events other scripts can listen to (event-based interaction)
    public event Action<int> OnTaskCompleted;   // sends the task index
    public event Action OnAllTasksCompleted;
    public event Action OnGameEnded;            // win or lose (MenuManager shows the cursor)

    [Header("Puzzle")]
    [Tooltip("Number of puzzle tasks. Must match the length of the door code.")]
    public int totalTasks = 5;
    [Tooltip("The 5-digit door code. Task 1 reveals digit 1, task 2 reveals digit 2, etc.")]
    public string doorCode = "58213";

    [Header("Lose condition: checkout timer")]
    public float timeLimitSeconds = 300f;   // 5 minutes

    [Header("UI (TextMeshPro)")]
    public TextMeshProUGUI progressText;    // "Puzzle Progress: 2 / 5"
    public TextMeshProUGUI timerText;       // "Checkout: 03:41"
    public TextMeshProUGUI codeText;        // "Code: 5 _ 2 _ _"
    public TextMeshProUGUI messageText;     // short feedback messages
    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI loseReasonText;

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip taskCompleteSound;
    public AudioClip winSound;
    public AudioClip loseSound;

    public bool IsGameOver { get; private set; }
    public bool IsRunning { get; private set; }  // false while the welcome panel is open
    public int CompletedCount { get; private set; }
    public bool AllTasksDone => CompletedCount >= totalTasks;

    private bool[] completed;
    private float timeLeft;
    private Coroutine messageRoutine;

    void Awake()
    {
        Instance = this;
        completed = new bool[totalTasks];
        timeLeft = timeLimitSeconds;
        Time.timeScale = 1f;
    }

    void Start()
    {
        // If there is a MenuManager, wait for the Play button; otherwise start right away
        IsRunning = FindFirstObjectByType<MenuManager>() == null;
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        UpdateProgressUI();
        UpdateTimerUI();
        ShowMessage("");
    }

    void Update()
    {
        if (IsGameOver)
        {
            // Restart with R after winning or losing
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (!IsRunning) return;   // welcome panel still open: timer paused

#if UNITY_EDITOR
        // TESTING ONLY (works in the Editor, not in the final build):
        // F1-F5 instantly complete tasks 1-5 so you can test the door.
        Keyboard kbd = Keyboard.current;
        if (kbd != null)
        {
            if (kbd.f1Key.wasPressedThisFrame) CompleteTask(0);
            if (kbd.f2Key.wasPressedThisFrame) CompleteTask(1);
            if (kbd.f3Key.wasPressedThisFrame) CompleteTask(2);
            if (kbd.f4Key.wasPressedThisFrame) CompleteTask(3);
            if (kbd.f5Key.wasPressedThisFrame) CompleteTask(4);
        }
#endif

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            UpdateTimerUI();
            Lose("Time's up! You missed checkout.");
            return;
        }
        UpdateTimerUI();
    }

    // Called by MenuManager's Play button
    public void StartGame()
    {
        IsRunning = true;
    }

    // Called by every puzzle task when it is solved
    public void CompleteTask(int index)
    {
        if (IsGameOver) return;
        if (index < 0 || index >= totalTasks) return;
        if (completed[index]) return;   // already solved, don't count twice

        completed[index] = true;
        CompletedCount++;

        PlaySound(taskCompleteSound);
        UpdateProgressUI();
        OnTaskCompleted?.Invoke(index);

        if (AllTasksDone)
        {
            ShowMessage("Room is clean! Head to the door.");
            OnAllTasksCompleted?.Invoke();
        }
    }

    // Used by SequenceTask: a wrong move costs time (lose-condition pressure)
    public void AddTimePenalty(float seconds)
    {
        if (IsGameOver) return;
        timeLeft = Mathf.Max(0f, timeLeft - seconds);
        UpdateTimerUI();
    }

    public bool IsTaskCompleted(int index)
    {
        return index >= 0 && index < totalTasks && completed[index];
    }

    public void Win()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        PlaySound(winSound);
        if (winPanel) winPanel.SetActive(true);
        Time.timeScale = 0f;   // freeze the game
        OnGameEnded?.Invoke();
    }

    public void Lose(string reason)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        PlaySound(loseSound);
        if (loseReasonText) loseReasonText.text = reason;
        if (losePanel) losePanel.SetActive(true);
        Time.timeScale = 0f;
        OnGameEnded?.Invoke();
    }

    public void ShowMessage(string msg, float seconds = 3f)
    {
        if (messageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageText.text = msg;
        if (!string.IsNullOrEmpty(msg) && seconds > 0f)
            messageRoutine = StartCoroutine(ClearMessageAfter(seconds));
    }

    IEnumerator ClearMessageAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        messageText.text = "";
    }

    public void PlaySound(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }

    void UpdateProgressUI()
    {
        if (progressText)
            progressText.text = "Puzzle Progress: " + CompletedCount + " / " + totalTasks;

        if (codeText)
        {
            string s = "Code: ";
            for (int i = 0; i < totalTasks; i++)
                s += (completed[i] && i < doorCode.Length ? doorCode[i].ToString() : "_") + " ";
            codeText.text = s.TrimEnd();
        }
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);
        timerText.text = string.Format("Checkout: {0:00}:{1:00}", minutes, seconds);
        timerText.color = timeLeft <= 60f ? Color.red : Color.white;   
    }
}
