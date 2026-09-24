using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Put this on PlayerArmature.
// Press E near a Carryable object to pick it up, press E again to put it down.
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
        if (pm != null && pm.IsGameOver) { SetPrompt(""); return; }

        Keyboard kb = Keyboard.current;
        bool ePressed = kb != null && kb.eKey.wasPressedThisFrame;

        if (carried == null)
        {
            Carryable nearest = FindNearest();
            SetPrompt(nearest != null ? "[E] Pick up" : "");
            if (nearest != null && ePressed)
            {
                carried = nearest;
                carried.PickUp(holdPoint);
            }
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
            float d = Vector3.Distance(center, h.ClosestPoint(center));
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return best;
    }

    void SetPrompt(string s)
    {
        if (promptText && promptText.text != s) promptText.text = s;
    }
}
