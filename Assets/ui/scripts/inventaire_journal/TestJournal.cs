using UnityEngine;

public class TestJournal : MonoBehaviour
{
    // TEST !! A désactiver quand on a le personnage et les collisions
    public void TestJDB()
    {
        // Trouve tous les objets avec le tag indice
        GameObject[] objetsIndices = GameObject.FindGameObjectsWithTag("indice");

        foreach (GameObject objet in objetsIndices)
        {
            RamasserObjet ramasserObjet = objet.GetComponent<RamasserObjet>();
            if (ramasserObjet != null)
                ramasserObjet.SimulerRamassage();
        }
    }
}