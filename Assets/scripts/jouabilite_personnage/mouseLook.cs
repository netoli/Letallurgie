using UnityEngine;
using UnityEngine.InputSystem;

public class mouseLook : MonoBehaviour
{
    public float mouseSensitivity = 300f;
    public Transform playerBody;

    void Update()
    {
        if (Mouse.current == null) return;

        float mouseX = Mouse.current.delta.x.ReadValue()
                       * mouseSensitivity * Time.deltaTime;

        playerBody.Rotate(Vector3.up * mouseX);
    }
}