using UnityEngine;
using Unity.Cinemachine;

public class mouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody; // glisse persoTests ici

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") 
                       * mouseSensitivity * Time.deltaTime;

        // Tourne uniquement le corps horizontalement
        // Cinemachine s'occupe du reste (vertical)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}