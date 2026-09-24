using UnityEngine;
using UnityEngine.InputSystem;

// Put this on a trigger zone in front of the door.
// When all tasks are done, the player types the 5-digit code and presses Enter.
// 3 wrong attempts = Game Over (lose condition).
public class DoorKeypad : MonoBehaviour
{
    [Tooltip("Empty object at the door's hinge edge. The Door is a child of it.")]
    public Transform doorHinge;
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public int maxAttempts = 3;

    [Header("Feedback (optional)")]
    public Light statusLight;       // red = locked, green = unlocked
    public AudioClip wrongSound;
    public AudioClip unlockSound;

    private bool playerNear;
    private bool unlocked;
    private int attemptsLeft;
    private string typed = "";
    private Quaternion closedRot, openRot;
    private float openProgress;

    private static readonly Key[] digitKeys = {
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
    private static readonly Key[] numpadKeys = {
        Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
        Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

    void Start()
    {
        attemptsLeft = maxAttempts;
        if (doorHinge)
        {
            closedRot = doorHinge.localRotation;
            openRot = closedRot * Quaternion.Euler(0f, openAngle, 0f);
        }
        if (statusLight) statusLight.color = Color.red;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || unlocked) return;
        playerNear = true;
        typed = "";
        ShowKeypad();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || unlocked) return;
        playerNear = false;
        PuzzleManager.Instance.ShowMessage("");
    }

    void Update()
    {
        // Swing the door open smoothly after unlocking
        if (unlocked && doorHinge && openProgress < 1f)
        {
            openProgress += Time.deltaTime * openSpeed;
            doorHinge.localRotation = Quaternion.Slerp(closedRot, openRot, openProgress);
        }

        PuzzleManager pm = PuzzleManager.Instance;
        if (!playerNear || unlocked || pm.IsGameOver) return;
        if (!pm.AllTasksDone) return;   // keypad only works once the room is clean

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        for (int d = 0; d < 10; d++)
        {
            if (kb[digitKeys[d]].wasPressedThisFrame || kb[numpadKeys[d]].wasPressedThisFrame)
            {
                if (typed.Length < pm.doorCode.Length) { typed += d; ShowKeypad(); }
            }
        }

        if (kb.backspaceKey.wasPressedThisFrame && typed.Length > 0)
        {
            typed = typed.Substring(0, typed.Length - 1);
            ShowKeypad();
        }

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            CheckCode();
    }

    void ShowKeypad()
    {
        PuzzleManager pm = PuzzleManager.Instance;
        if (!pm.AllTasksDone)
        {
            pm.ShowMessage("The lock won't respond yet.", 0f);   // stays until you walk away
            return;
        }
        pm.ShowMessage("[ " + typed.PadRight(pm.doorCode.Length, '_') + " ]   Enter = confirm   (" +
                       attemptsLeft + " tries left)", 0f);
    }

    void CheckCode()
    {
        PuzzleManager pm = PuzzleManager.Instance;
        if (typed == pm.doorCode)
        {
            unlocked = true;
            if (statusLight) statusLight.color = Color.green;
            pm.PlaySound(unlockSound);
            pm.ShowMessage("*click*  The door opens!");
            return;
        }

        attemptsLeft--;
        typed = "";
        pm.PlaySound(wrongSound);
        if (attemptsLeft <= 0)
            pm.Lose("Too many wrong codes. The door is locked for good!");
        else
            ShowKeypad();
    }
}
