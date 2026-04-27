using UnityEngine;

public class lanterne_animation_aleatoire : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject lanterne;
    void Start()
    {
        lanterne.GetComponent<Animator>().Play("rot_lanterne_anim", 0, Random.Range(0f, 1f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
