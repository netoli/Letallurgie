using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody; // persoTests

    private float xRotation = 0f;

    void Update()
{
    float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
    float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

    xRotation -= mouseY;
    xRotation = Mathf.Clamp(xRotation, -90f, 90f);

    // Rotation verticale sur cameraHolder
    transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

    // Rotation horizontale sur le corps
    playerBody.Rotate(Vector3.up * mouseX);
}
}