using UnityEngine;

public class balance_apparence : MonoBehaviour
{
    float float_de_rotation = 0f;
    public GameObject gauche_hold;
    public GameObject droite_hold;
    public GameObject balanceur;
    //va de -1.0 a 1.0 ou -1.0 est completement penché à gauche et 1.0 complètement penché à droite
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        balanceur.transform.rotation = Quaternion.Euler((-90f+(float_de_rotation*23.5f)), 0, -90f);
        gauche_hold.transform.localPosition = new Vector3(6.04f, 8.37f + (float_de_rotation * 1.79f), -5.2f + (-float_de_rotation * 0.6f));
        droite_hold.transform.localPosition = new Vector3(6.04f, 8.37f + (-float_de_rotation * 1.79f), 4.32f + (-float_de_rotation * 1.04f));
        //float_de_rotation += 0.001f;
        if (float_de_rotation > 1f)
        {
            float_de_rotation = -1f;
        }
    }
}
