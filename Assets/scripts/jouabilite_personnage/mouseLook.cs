using UnityEngine;
using UnityEngine.InputSystem;

public class mouseLook : MonoBehaviour
{
    public float mouseSensitivity = 300f;
    public Transform playerBody;

    // Force la valeur au démarrage pour ignorer toute valeur
    // sérialisée différente dans les scènes individuelles.
    void Awake()
    {
        mouseSensitivity = 300f;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        float mouseX = Mouse.current.delta.x.ReadValue()
                       * mouseSensitivity * Time.deltaTime;

        playerBody.Rotate(Vector3.up * mouseX);
    }
}