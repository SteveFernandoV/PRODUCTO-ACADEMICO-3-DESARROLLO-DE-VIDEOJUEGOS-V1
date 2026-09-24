using UnityEngine;
public sealed class ExitTrigger : MonoBehaviour
{
    bool completed;

    void OnTriggerEnter(Collider other) { TryFinish(other); }
    void OnTriggerStay(Collider other) { TryFinish(other); }

    void TryFinish(Collider other)
    {
        if (completed || !other.CompareTag("Player")) return;
        PA3GameManager manager = PA3GameManager.Instance;
        if (manager == null) return;
        if (manager.collected >= manager.totalCollectibles) completed = true;
        manager.ReachExit();
    }
}
