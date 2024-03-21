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
            Debug.LogError("投掷物预制件未找到");
        }
        else
        {
            Debug.Log("投掷物预制件加载成功");
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
            Debug.LogError("服务器端投掷物预制件未找到");
            return;
        }

        GameObject projectile = Instantiate(objectToThrowPrefab, position, Quaternion.identity);
        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("投掷物预制件上缺少 NetworkObject 组件");
            return;
        }

        networkObject.Spawn(); 

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("投掷物预制件上缺少 Rigidbody 组件");
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
