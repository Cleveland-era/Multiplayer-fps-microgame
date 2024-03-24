using UnityEngine;
using Unity.Netcode;

public class ProjectileAddon : NetworkBehaviour
{
    private Rigidbody rb;
    private bool targetHit;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (targetHit) return;
        targetHit = true;

        if (!collision.gameObject.CompareTag("Player") && IsOwner)
        {
            Destroy(gameObject);
        }
    }
}
