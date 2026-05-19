using UnityEngine;

public class lumiere_augmentation : MonoBehaviour
{
    public GameObject lumiere;
    public float taille_changement=0f;
    public float vitesse_changement=0.1f;
    public bool switcher=false;
    public float max_taille=10f;
    public float base_taille=200f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        vitesse_changement= Random.Range(vitesse_changement/10, vitesse_changement);

        if (switcher)
        {
            taille_changement+=vitesse_changement;
        }
        else
        {
            taille_changement-=vitesse_changement;
        }
        if (max_taille < taille_changement)
        {
            switcher=false;
        }
        else if(-max_taille>taille_changement)
        {
            switcher=true;
        }


        lumiere.transform.localScale = new Vector3(base_taille + taille_changement, base_taille + taille_changement, base_taille + taille_changement);
    }
}
