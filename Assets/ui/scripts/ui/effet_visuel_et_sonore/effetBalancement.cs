using UnityEngine;

public class effetBalancement : MonoBehaviour
{
    [Header("Balancement avant-arriere")]
    [SerializeField] private float angleMax;
    [SerializeField] private float vitesse;

    private Quaternion rotationOriginale;

    void Start()
    {
        rotationOriginale = transform.localRotation;
    }

    void Update()
    {
        float angle = Mathf.Sin(Time.time * vitesse) * angleMax;
        transform.localRotation = rotationOriginale
            * Quaternion.Euler(angle, 0f, 0f);
    }
}