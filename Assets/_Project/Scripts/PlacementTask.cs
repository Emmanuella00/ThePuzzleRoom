using System.Collections;
using UnityEngine;

// Puzzle type: "Placing objects on correct surfaces".
// Put this on the GHOST (a see-through copy of the object, standing where it belongs).
// When the player puts the real object down near the ghost, it snaps into place.
public class PlacementTask : MonoBehaviour
{
    [Header("Task")]
    [Tooltip("0 = task 1, 1 = task 2, ...")]
    public int taskIndex = 0;
    public string taskName = "Flower pot";
    [Tooltip("The real object the player carries here")]
    public Carryable item;
    [Tooltip("How close (metres) the object must be dropped to snap in")]
    public float snapRange = 1.2f;

    [Header("Optional extra clue")]
    public Light clueLight;

    [Header("Order (optional)")]
    [Tooltip("Task index that must be finished first, or -1 for none (0 = task 1)")]
    public int requiredTaskIndex = -1;
    [Tooltip("Plays when this task becomes available")]
    public AudioClip unlockSound;

    public bool IsDone { get; private set; }

    // Can the player do this task right now?
    public bool IsAvailable =>
        requiredTaskIndex < 0 ||
        (PuzzleManager.Instance != null && PuzzleManager.Instance.IsTaskCompleted(requiredTaskIndex));

    void Start()
    {
        if (item != null) item.correctSpot = this;
        if (clueLight) clueLight.color = new Color(1f, 0.8f, 0.3f);

        // Locked? Hide the ghost, the light and the item's glow until the required task is done
        if (!IsAvailable)
        {
            SetClueVisible(false);
            StartCoroutine(TurnItemGlowOffNextFrame());
            PuzzleManager.Instance.OnTaskCompleted += HandleTaskCompleted;   // event-based
        }
    }

    IEnumerator TurnItemGlowOffNextFrame()
    {
        yield return null;   // wait until HighlightPulse has started
        HighlightPulse glow = item ? item.GetComponent<HighlightPulse>() : null;
        if (!IsAvailable && glow) glow.SetPulsing(false);
    }

    void HandleTaskCompleted(int index)
    {
        if (index != requiredTaskIndex || IsDone) return;
        // Unlocked: the item starts glowing and its ghost appears
        SetClueVisible(true);
        HighlightPulse glow = item ? item.GetComponent<HighlightPulse>() : null;
        if (glow) glow.SetPulsing(true);
        PuzzleManager.Instance.PlaySound(unlockSound);
        PuzzleManager.Instance.OnTaskCompleted -= HandleTaskCompleted;
    }

    void OnDestroy()
    {
        if (PuzzleManager.Instance != null) PuzzleManager.Instance.OnTaskCompleted -= HandleTaskCompleted;
    }

    void SetClueVisible(bool on)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = on;
        if (clueLight) clueLight.enabled = on;
    }

    public bool TryPlace(Carryable c)
    {
        if (IsDone || c != item || !IsAvailable) return false;

        Vector3 a = c.transform.position;
        Vector3 b = transform.position;
        a.y = 0f; b.y = 0f;
        if (Vector3.Distance(a, b) > snapRange) return false;

        // Snap the real object exactly onto the ghost
        c.transform.SetPositionAndRotation(transform.position, transform.rotation);
        IsDone = true;

        // Hide the ghost
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        if (clueLight) clueLight.color = Color.green;

        // Green flash on the real object, then stop glowing
        HighlightPulse glow = c.GetComponent<HighlightPulse>();
        if (glow) glow.MarkSolved();

        PuzzleManager.Instance.CompleteTask(taskIndex);
        PuzzleManager.Instance.ShowMessage(taskName + " is back in place!");
        return true;
    }
}
