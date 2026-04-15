using UnityEngine;
using TMPro;

public class gestionTutoriel : MonoBehaviour
{
    public GameObject gestionnaire;

    public TMP_Text titreTuto;
    public TMP_Text texteTuto;
    [SerializeField] private string[] titresTutos;
    [SerializeField] private string[] textesTutos;

    public bool nextTuto;
    public float tutoActuel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nextTuto = false;
        tutoActuel = 0;

    }

    // Update is called once per frame
    void Update()
    {
        if (nextTuto == true)
        {
            tutoActuel += 1;
            nextTuto = false;
            Debug.Log(tutoActuel);
        }

        if (tutoActuel >= 4)
        {//Pourrais poser probleme, a verifier
            gestionnaire.SetActive(false);
        }

        if (tutoActuel == 0)
        {
            titreTuto.text = titresTutos[0];
            texteTuto.text = textesTutos[0];
        }
        else if (tutoActuel == 1)
        {
            titreTuto.text = titresTutos[1];
            texteTuto.text = textesTutos[1];
        }
        else if (tutoActuel == 2)
        {
            titreTuto.text = titresTutos[2];
            texteTuto.text = textesTutos[2];
        }
        else
        {
            titreTuto.text = titresTutos[3];
            texteTuto.text = textesTutos[3];
        }
    }

    public void activationNextTuto()
    {
        nextTuto = true;
    }
}
