using UnityEngine;
using UnityEngine.AI;
public sealed class EnemyChaser : MonoBehaviour
{
    public Transform target; public float updateRate=.3f; NavMeshAgent agent; float nextUpdate;
    void Awake(){agent=GetComponent<NavMeshAgent>();}
    void Update(){if(agent==null||!agent.isOnNavMesh||target==null||Time.time<nextUpdate)return;agent.SetDestination(target.position);nextUpdate=Time.time+updateRate;}
}
