using UnityEngine;
using UnityEngine.UI;

public class scrollBarFixe : MonoBehaviour
{
    [Range(0.05f, 0.5f)]
    public float tailleHandle;

    Scrollbar scrollbar;

    void Awake()
    {
        scrollbar = GetComponent<Scrollbar>();
    }

    void LateUpdate()
    {
        if (scrollbar != null)
            scrollbar.size = tailleHandle;
    }
}