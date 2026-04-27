using UnityEngine;

public class billboard : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //https://gamedevbeginner.com/billboards-in-unity-and-how-to-make-your-own/
    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        transform.LookAt(mainCamera.transform);
        transform.Rotate(0, 180, 0);
    }
}
