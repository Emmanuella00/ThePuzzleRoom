using UnityEngine;

// Put this on any object the player can pick up with E (e.g. the FlowerPot).
public class Carryable : MonoBehaviour
{
    [HideInInspector] public PlacementTask correctSpot;   // filled in automatically by PlacementTask
    public bool IsPlaced { get; private set; }
    public bool IsCarried { get; private set; }

    private Collider[] colliders;
    private Transform originalParent;

    void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
        originalParent = transform.parent;
    }

    public void PickUp(Transform holdPoint)
    {
        IsCarried = true;
        SetColliders(false);                 // so it doesn't bump into the player
        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
    }

    // Called when the player presses E again
    public void Drop(Transform player)
    {
        IsCarried = false;
        transform.SetParent(originalParent);

        // Close enough to its ghost? Then it snaps into place and the task completes.
        if (correctSpot != null && correctSpot.TryPlace(this))
        {
            IsPlaced = true;
            SetColliders(true);
            return;
        }

        // Otherwise put it down in front of the player, on whatever surface is below
        Vector3 p = player.position + player.forward * 0.9f;
        if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            p.y = hit.point.y;
        else
            p.y = player.position.y;
        transform.position = p;
        SetColliders(true);
    }

    void SetColliders(bool on)
    {
        foreach (Collider c in colliders) c.enabled = on;
    }
}
