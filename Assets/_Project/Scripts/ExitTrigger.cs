using UnityEngine;

// Put this on a trigger zone in the doorway.
// When the player walks through the open door, they win.
public class ExitTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && PuzzleManager.Instance.AllTasksDone)
            PuzzleManager.Instance.Win();
    }
}
