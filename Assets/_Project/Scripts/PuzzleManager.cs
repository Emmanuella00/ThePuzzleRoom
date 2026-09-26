using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;


public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    public event Action<int> OnTaskCompleted;   
    public event Action OnAllTasksCompleted;
    public event Action OnGameEnded;            

    [Header("Puzzle")]
    [Tooltip("Number of puzzle tasks. Must match the length of the door code.")]
    public int totalTasks = 5;
    [Tooltip("The 5-digit door code. Task 1 reveals digit 1, task 2 reveals digit 2, etc.")]
    public string doorCode = "26255";

    [Header("Lose condition: checkout timer")]
    public float timeLimitSeconds = 300f;   // 5 minutes

    [Header("UI (TextMeshPro)")]
    public TextMeshProUGUI progressText;    
    public TextMeshProUGUI timerText;       
    public TextMeshProUGUI codeText;        
    public TextMeshProUGUI messageText;     
    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI loseReasonText;

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip taskCompleteSound;
    public AudioClip winSound;
    public AudioClip loseSound;

    public bool IsGameOver { get; private set; }
    public bool IsRunning { get; private set; }  
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
            
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (!IsRunning) return;   

#if UNITY_EDITOR
        
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

    
    public void StartGame()
    {
        IsRunning = true;
    }

    
    public void CompleteTask(int index)
    {
        if (IsGameOver) return;
        if (index < 0 || index >= totalTasks) return;
        if (completed[index]) return;   

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
        Time.timeScale = 0f;   
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
