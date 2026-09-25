using System.Collections;
using UnityEngine;

// Puzzle type: "Follow a sequence of steps to achieve a communicated task".
// Task 5 - the checkout routine. Put this on an empty object ("CheckoutRoutine").
// After tasks 1-4 are done, ONLY the next step glows (that's the clue).
// Press E on the glowing object -> it does its action -> the next one starts glowing.
// Objects that are not the current step don't glow and can't be used.
public class SequenceTask : MonoBehaviour
{
    [Header("Task")]
    public int taskIndex = 4;
    public string taskName = "Checkout routine";

    [Tooltip("The steps, in order: Curtains, Stereo, TV")]
    public SequenceDevice[] stepsInOrder;

    [Tooltip("Tasks that must be finished first (0 = task 1)")]
    public int[] requiredTasks = { 0, 1, 2, 3 };

    public bool IsActive { get; private set; }
    public bool IsSolved { get; private set; }

    // Only the glowing step can be used
    public bool IsCurrentStep(SequenceDevice d) =>
        IsActive && !IsSolved && current < stepsInOrder.Length && stepsInOrder[current] == d;

    private int current;          // index of the step that is glowing now
    private PuzzleManager pm;

    void Start()
    {
        pm = PuzzleManager.Instance;
        foreach (SequenceDevice d in stepsInOrder) d.sequence = this;

        // Event-based: react whenever another task is completed
        pm.OnTaskCompleted += HandleTaskCompleted;
        CheckActivation();
    }

    void OnDestroy()
    {
        if (pm != null) pm.OnTaskCompleted -= HandleTaskCompleted;
    }

    void HandleTaskCompleted(int index)
    {
        CheckActivation();
    }

    void CheckActivation()
    {
        if (IsActive || IsSolved) return;
        foreach (int t in requiredTasks)
            if (!pm.IsTaskCompleted(t)) return;

        IsActive = true;
        current = 0;
        stepsInOrder[0].SetGlowing(true);    // the first step starts glowing
    }

    public void OnDevicePressed(SequenceDevice device)
    {
        if (IsSolved || device.Done) return;

        if (!IsCurrentStep(device)) return;   // not glowing = not usable yet

        // Correct step
        device.Activate();
        pm.PlaySound(device.pressSound);
        current++;

        if (current >= stepsInOrder.Length)
            StartCoroutine(Solve());
        else
            stepsInOrder[current].SetGlowing(true);   // next step glows
    }

    IEnumerator Solve()
    {
        IsSolved = true;
        yield return new WaitForSeconds(1f);
        pm.CompleteTask(taskIndex);
        pm.ShowMessage("Room is ready for checkout!");
    }
}
