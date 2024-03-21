using UnityEngine;
using Unity.Netcode;

public class Throwing : NetworkBehaviour
{
    [Header("References")]
    public Transform cam;
    public Transform attackPoint;
    private GameObject objectToThrowPrefab;

    [Header("Settings")]
    
    public NetworkVariable<int> totalThrows = new NetworkVariable<int>(30);

    public float throwCooldown;

    [Header("Throwing")]
    public KeyCode throwKey = KeyCode.Mouse0;
    public float throwForce;
    public float throwUpwardForce;

    private bool readyToThrow;

    private void Start()
    {
        
        if (IsServer)
        {
            totalThrows.Value = 30; 
        }

        readyToThrow = true;
        objectToThrowPrefab = Resources.Load<GameObject>("prefabs/ObjectToThrow");

        if (objectToThrowPrefab == null)
        {
            Debug.LogError("Ͷ����Ԥ�Ƽ�δ�ҵ�");
        }
        else
        {
            Debug.Log("Ͷ����Ԥ�Ƽ����سɹ�");
        }
    }

    private void Update()
    {
        
        if (IsOwner && Input.GetKeyDown(throwKey) && readyToThrow && totalThrows.Value > 0)
        {
            readyToThrow = false; 
            ThrowServerRpc(attackPoint.position, cam.forward);
            Invoke(nameof(ResetThrow), throwCooldown);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ThrowServerRpc(Vector3 position, Vector3 direction)
    {
        if (objectToThrowPrefab == null)
        {
            Debug.LogError("��������Ͷ����Ԥ�Ƽ�δ�ҵ�");
            return;
        }

        GameObject projectile = Instantiate(objectToThrowPrefab, position, Quaternion.identity);
        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("Ͷ����Ԥ�Ƽ���ȱ�� NetworkObject ���");
            return;
        }

        networkObject.Spawn(); 

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Ͷ����Ԥ�Ƽ���ȱ�� Rigidbody ���");
            return;
        }

        
        Vector3 force = direction * throwForce + Vector3.up * throwUpwardForce;
        rb.AddForce(force, ForceMode.Impulse);

        
        totalThrows.Value -= 1;
    }

    private void ResetThrow()
    {
        readyToThrow = true;
    }
}
