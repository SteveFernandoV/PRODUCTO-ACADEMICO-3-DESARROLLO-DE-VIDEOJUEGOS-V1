using UnityEngine;
public sealed class Collectible : MonoBehaviour
{
    public float spinSpeed=90f,bobHeight=.25f; Vector3 start;
    void Start(){start=transform.position;}
    void Update(){transform.Rotate(Vector3.up,spinSpeed*Time.deltaTime,Space.World);transform.position=start+Vector3.up*(Mathf.Sin(Time.time*2.4f)*bobHeight);}
    void OnTriggerEnter(Collider other){if(!other.CompareTag("Player"))return;PA3GameManager.Instance?.Collect();Destroy(gameObject);}
}
