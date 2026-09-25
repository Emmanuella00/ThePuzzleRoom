using System.Collections;
using UnityEngine;

// Puzzle type: "Rotating objects to align symbols".
// Put this on the REAL object (e.g. the Painting or the TV).
// At Start it automatically:
//   1) creates a see-through GHOST copy in the correct position (the clue),
//   2) turns the real object out of alignment.
// Each press of E (handled by PlayerCarry) turns it by one step.
// When it lines up with the ghost again, the task is completed.
public class RotationTask : MonoBehaviour
{
    [Header("Task")]
    [Tooltip("0 = task 1, 1 = task 2, 2 = task 3 ...")]
    public int taskIndex = 2;
    public string taskName = "Painting";

    [Header("Rotation")]
    [Tooltip("World axis to turn around. Picture/TV on a side wall = (1, 0, 0)")]
    public Vector3 rotationAxis = new Vector3(1f, 0f, 0f);
    public float stepAngle = 45f;
    [Tooltip("How many steps out of place the object starts")]
    public int startStepsOff = 3;
    public float turnSpeed = 360f;   // degrees per second (animation)

    [Header("Ghost clue")]
    public Material ghostMaterial;   // use GhostMat

    [Header("Interconnection (optional)")]
    [Tooltip("Task index that must be finished first, or -1 for none")]
    public int requiredTaskIndex = -1;

    [Header("Reward / feedback (optional)")]
    [Tooltip("Something to switch ON when solved, e.g. a light for the TV screen")]
    public GameObject activateOnSolve;
    public AudioClip turnSound;
    [Tooltip("Plays when this object becomes available (e.g. TV wakes up)")]
    public AudioClip unlockSound;

    public bool IsSolved { get; private set; }
    public bool IsTurning { get; private set; }

    // Can the player use it right now? (false while the required task isn't done)
    public bool IsAvailable =>
        requiredTaskIndex < 0 ||
        (PuzzleManager.Instance != null && PuzzleManager.Instance.IsTaskCompleted(requiredTaskIndex));

    private Vector3 pivot;
    private Vector3 correctPos;
    private Quaternion correctRot;
    private int stepsOff;              // how many steps away from correct
    private int stepsPerCircle;
    private GameObject ghost;
    private HighlightPulse glow;
    [HideInInspector] public bool isGhostCopy;

    void Start()
    {
        if (isGhostCopy) return;

        glow = GetComponent<HighlightPulse>();
        correctPos = transform.position;
        correctRot = transform.rotation;
        pivot = GetBoundsCenter();
        stepsPerCircle = Mathf.Max(1, Mathf.RoundToInt(360f / stepAngle));

        CreateGhost();

        // Knock the real object out of place
        stepsOff = ((startStepsOff % stepsPerCircle) + stepsPerCircle) % stepsPerCircle;
        transform.RotateAround(pivot, rotationAxis, -stepsOff * stepAngle);

        if (activateOnSolve) activateOnSolve.SetActive(false);

        // Locked? Then hide the ghost and the glow until the required task is done
        if (!IsAvailable)
        {
            if (ghost) ghost.SetActive(false);
            StartCoroutine(TurnGlowOffNextFrame());
            PuzzleManager.Instance.OnTaskCompleted += HandleTaskCompleted;   // event-based
        }
    }

    IEnumerator TurnGlowOffNextFrame()
    {
        yield return null;                 // wait until HighlightPulse has started
        if (!IsAvailable && glow) glow.SetPulsing(false);
    }

    void HandleTaskCompleted(int index)
    {
        if (index != requiredTaskIndex || IsSolved) return;
        // Unlocked! The object "wakes up": glow + ghost appear
        if (ghost) ghost.SetActive(true);
        if (glow) glow.SetPulsing(true);
        PuzzleManager.Instance.PlaySound(unlockSound);
        PuzzleManager.Instance.OnTaskCompleted -= HandleTaskCompleted;
    }

    void OnDestroy()
    {
        if (PuzzleManager.Instance != null) PuzzleManager.Instance.OnTaskCompleted -= HandleTaskCompleted;
    }

    // Called by PlayerCarry when the player presses E nearby
    public void TryRotate()
    {
        if (IsSolved || IsTurning) return;
        PuzzleManager pm = PuzzleManager.Instance;

        if (!IsAvailable) return;   // not glowing = not usable yet

        pm.PlaySound(turnSound);
        StartCoroutine(TurnOneStep());
    }

    IEnumerator TurnOneStep()
    {
        IsTurning = true;
        float turned = 0f;
        while (turned < stepAngle)
        {
            float delta = Mathf.Min(turnSpeed * Time.deltaTime, stepAngle - turned);
            transform.RotateAround(pivot, rotationAxis, delta);
            turned += delta;
            yield return null;
        }
        IsTurning = false;

        stepsOff = (stepsOff - 1 + stepsPerCircle) % stepsPerCircle;
        if (stepsOff == 0) Solve();
    }

    void Solve()
    {
        IsSolved = true;
        transform.SetPositionAndRotation(correctPos, correctRot);   // exact alignment
        if (ghost) ghost.SetActive(false);
        if (glow) glow.MarkSolved();
        if (activateOnSolve) activateOnSolve.SetActive(true);

        PuzzleManager.Instance.CompleteTask(taskIndex);
        PuzzleManager.Instance.ShowMessage(taskName + " is straight again!");
    }

    void CreateGhost()
    {
        ghost = Instantiate(gameObject, correctPos, correctRot, transform.parent);
        ghost.name = name + "_Ghost";

        // Strip everything that makes it interactive
        RotationTask rt = ghost.GetComponent<RotationTask>();
        rt.isGhostCopy = true;
        rt.enabled = false;
        Destroy(rt);
        foreach (HighlightPulse h in ghost.GetComponentsInChildren<HighlightPulse>()) { h.enabled = false; Destroy(h); }
        foreach (Collider c in ghost.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (Light l in ghost.GetComponentsInChildren<Light>()) Destroy(l.gameObject);

        // Make it see-through
        if (ghostMaterial)
        {
            foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>())
            {
                Material[] m = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < m.Length; i++) m[i] = ghostMaterial;
                r.sharedMaterials = m;
            }
        }
    }

    Vector3 GetBoundsCenter()
    {
        Renderer[] rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return transform.position;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b.center;
    }
}
