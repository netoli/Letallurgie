using UnityEngine;
using Unity.Cinemachine;

public class PlayerBodyRotation : MonoBehaviour
{
    [SerializeField] private CinemachineCamera vcam;

    void Update()
    {
        // Copie seulement la rotation Y de la vcam sur le corps
        float camY = vcam.transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, camY, 0f);
    }
}