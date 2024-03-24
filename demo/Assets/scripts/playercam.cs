using UnityEngine;
using Unity.Netcode; 

public class playercam : NetworkBehaviour
{
    public float sensX;
    public float sensY;
            
    public Transform orientation;

    float xRotation;
    float yRotation;

    private void Start()
    {
        if (IsOwner)
        {
            LockCursor();
        }
        else
        {
            DisableCameraAndControls();
        }
    }

    private void DisableCameraAndControls()
    {
        var camera = GetComponentInChildren<Camera>();
        if (camera != null)
        {
            camera.enabled = false;
        }
        enabled = false; 
    }

    void Update()
    {
        if (!IsOwner) return;

        if (GlobalCursorManager.CursorLocked)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                UnlockCursor();
            }

            if (Input.GetMouseButtonDown(0))
            {
                LockCursor();
            }
        }

        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GlobalCursorManager.CursorLocked = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GlobalCursorManager.CursorLocked = false;
    }
}
