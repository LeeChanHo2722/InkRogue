using UnityEngine;

public class SpawnPointGizmo : MonoBehaviour
{
    public float radius = 0.5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}