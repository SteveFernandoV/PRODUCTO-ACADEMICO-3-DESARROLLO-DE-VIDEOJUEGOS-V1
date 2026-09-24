using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class EnemyChaser : MonoBehaviour
{
    [Header("Persecución")]
    public Transform target;
    public float moveSpeed = 3.8f;
    public float catchDistance = 1.05f;
    bool caught;
    Terrain currentTerrain;

    void Awake()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // Direct pursuit keeps this enemy functional before the custom terrain has a baked NavMesh.
        SnapToGround();
    }

    void Update()
    {
        if (caught || target == null) return;
        Vector3 flatOffset = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        if (flatOffset.sqrMagnitude <= catchDistance * catchDistance &&
            Mathf.Abs(target.position.y - transform.position.y) < 2f) { CatchPlayer(); return; }

        if (flatOffset.sqrMagnitude > 0.01f)
        {
            Vector3 direction = flatOffset.normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
        }
        SnapToGround();
    }

    void SnapToGround()
    {
        Vector3 position = transform.position;
        if (currentTerrain == null || !Contains(currentTerrain, position))
        {
            currentTerrain = null;
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                if (Contains(terrain, position)) { currentTerrain = terrain; break; }
            }
        }
        if (currentTerrain == null) return;
        position.y = currentTerrain.SampleHeight(position) + currentTerrain.transform.position.y;
        transform.position = position;
    }

    static bool Contains(Terrain terrain, Vector3 position)
    {
        if (terrain == null || terrain.terrainData == null) return false;
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        return position.x >= origin.x && position.x <= origin.x + size.x &&
               position.z >= origin.z && position.z <= origin.z + size.z;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) CatchPlayer();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player")) CatchPlayer();
    }

    void CatchPlayer()
    {
        if (caught) return;
        caught = true;
        PA3MenuController.ReturnToMainMenu("El guardián te atrapó. Tu partida se reinició: inténtalo otra vez.");
    }
}
