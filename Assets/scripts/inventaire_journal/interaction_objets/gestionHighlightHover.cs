using UnityEngine;

public class gestionHighlightHover : MonoBehaviour
{

    [Header("SpriteRenderer de highlight")]
    [SerializeField] private GameObject highlight;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Highlighter(bool actif)
    {
        if (highlight != null)
            highlight.SetActive(actif);
    }
}
