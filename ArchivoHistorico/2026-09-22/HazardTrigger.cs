using UnityEngine;
public sealed class HazardTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other){if(!other.CompareTag("Player"))return;var player=other.GetComponent<PlayerController3D>();if(player!=null)player.ResetToSpawn();}
}
