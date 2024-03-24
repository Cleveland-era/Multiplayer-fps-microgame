using UnityEngine;
using Unity.Netcode;

public class SomethingDestroy : NetworkBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (IsOwner && !collision.gameObject.CompareTag("Player"))
        {
            Destroy(collision.gameObject);
        }
    }
}
