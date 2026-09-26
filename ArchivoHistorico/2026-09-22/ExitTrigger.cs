using UnityEngine;
public sealed class ExitTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other){if(other.CompareTag("Player"))PA3GameManager.Instance?.ReachExit();}
}
