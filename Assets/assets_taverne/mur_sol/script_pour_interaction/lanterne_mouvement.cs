using Unity.VisualScripting;
using UnityEngine;

public class lanterne_mouvement : MonoBehaviour
{
    public GameObject lumiere;
    private Vector3 position_demander;
    private Vector3 og_pos;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        og_pos = lumiere.transform.position;
        InvokeRepeating("changer_lumiere_place",0f,0.3f);
    }

    // Update is called once per frame
    void Update()
    {
        lumiere.transform.position = Vector3.Lerp(lumiere.transform.position, og_pos+position_demander, 0.004f);

    }
    void changer_lumiere_place()
    {
        position_demander= new Vector3(Random.Range(-0.1f,0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
    }
}
