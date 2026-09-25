using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Put this on PlayerArmature.
// Press E near a Carryable object to pick it up, press E again to put it down.
// Press E near a RotationTask object (painting, TV) to turn it one step.
// Press E near a SequenceDevice (Task 5) to switch it off.
public class PlayerCarry : MonoBehaviour
{
    public float pickupRange = 1.5f;
    [Tooltip("Empty child of the player, in front of the chest")]
    public Transform holdPoint;
    [Tooltip("Small on-screen control hint, e.g. [E] Pick up")]
    public TextMeshProUGUI promptText;

    private Carryable carried;

    void Update()
    {
        PuzzleManager pm = PuzzleManager.Instance;
        if (pm != null && (pm.IsGameOver || !pm.IsRunning)) { SetPrompt(""); return; }

        Keyboard kb = Keyboard.current;
        bool ePressed = kb != null && kb.eKey.wasPressedThisFrame;

        if (carried == null)
        {
            // Something to pick up?
            Carryable nearest = FindNearest();
            if (nearest != null)
            {
                SetPrompt("[E] Pick up");
                if (ePressed) { carried = nearest; carried.PickUp(holdPoint); }
                return;
            }

            // Something to rotate?
            RotationTask rot = FindNearestRotatable();
            if (rot != null)
            {
                SetPrompt("[E] Rotate");
                if (ePressed) rot.TryRotate();
                return;
            }

            // A device to switch off (Task 5)?
            SequenceDevice dev = FindNearestDevice();
            if (dev != null)
            {
                SetPrompt(dev.prompt);
                if (ePressed) dev.Press();
                return;
            }

            SetPrompt("");
        }
        else
        {
            SetPrompt("[E] Put down");
            if (ePressed)
            {
                carried.Drop(transform);
                carried = null;
            }
        }
    }

    SequenceDevice FindNearestDevice()
    {
        Vector3 center = transform.position + Vector3.up * 1.0f;
        Collider[] hits = Physics.OverlapSphere(center, pickupRange + 0.5f, ~0, QueryTriggerInteraction.Ignore);
        SequenceDevice best = null;
        float bestDist = float.MaxValue;
        foreach (Collider h in hits)
        {
            SequenceDevice d = h.GetComponentInParent<SequenceDevice>();
            if (d == null || d.Done || d.sequence == null || !d.sequence.IsCurrentStep(d)) continue;
            float dist = Vector3.Distance(center, h.bounds.ClosestPoint(center));
            if (dist < bestDist) { bestDist = dist; best = d; }
        }
        return best;
    }

    RotationTask FindNearestRotatable()
    {
        Vector3 center = transform.position + Vector3.up * 1.2f;
        Collider[] hits = Physics.OverlapSphere(center, pickupRange + 0.5f, ~0, QueryTriggerInteraction.Ignore);
        RotationTask best = null;
        float bestDist = float.MaxValue;
        foreach (Collider h in hits)
        {
            RotationTask r = h.GetComponentInParent<RotationTask>();
            if (r == null || r.IsSolved || r.isGhostCopy || !r.IsAvailable) continue;
            float d = Vector3.Distance(center, h.bounds.ClosestPoint(center));
            if (d < bestDist) { bestDist = d; best = r; }
        }
        return best;
    }

    Carryable FindNearest()
    {
        Vector3 center = transform.position + Vector3.up * 0.8f;
        Collider[] hits = Physics.OverlapSphere(center, pickupRange, ~0, QueryTriggerInteraction.Ignore);
        Carryable best = null;
        float bestDist = float.MaxValue;
        foreach (Collider h in hits)
        {
            Carryable c = h.GetComponentInParent<Carryable>();
            if (c == null || c.IsPlaced || c.IsCarried) continue;
            if (c.correctSpot != null && !c.correctSpot.IsAvailable) continue;   // not glowing yet
            float d = Vector3.Distance(center, h.bounds.ClosestPoint(center));
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return best;
    }

    void SetPrompt(string s)
    {
        if (promptText && promptText.text != s) promptText.text = s;
    }
}
