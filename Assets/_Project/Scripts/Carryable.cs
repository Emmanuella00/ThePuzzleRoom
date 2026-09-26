using UnityEngine;

public class Carryable : MonoBehaviour
{
    [HideInInspector] public PlacementTask correctSpot;   
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
        SetColliders(false);                  
        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
    }

    
    public void Drop(Transform player)
    {
        IsCarried = false;
        transform.SetParent(originalParent);

        
        if (correctSpot != null && correctSpot.TryPlace(this))
        {
            IsPlaced = true;
            SetColliders(true);
            return;
        }

        
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
