using System.Collections.Generic;
using UnityEngine;

public class DEBUGBalancePlateau : MonoBehaviour
{
    [SerializeField] private ZoneDepotJoueur _zoneJoueur;
    [SerializeField] private Transform _spawnPointBalance;

    public List<objetInventaire> inventaireTest;

    [ContextMenu("DEBUG — Simuler dépôt objet A")]
    private void DEBUGSimulerDepotA()
    {
        // On vérifie que la liste n'est pas vide et que l'élément 0 existe
        if (inventaireTest.Count > 0 && inventaireTest[0] != null)
        {
            // On utilise l'élément 0 de ta liste au lieu de _dataA
            objetInventaire data = inventaireTest[0];

            // 1. Instancier le prefab 3D défini dans le ScriptableObject
            GameObject instance3D = Instantiate(data.prefab3D, _spawnPointBalance.position, Quaternion.identity);

            // 2. Récupérer le composant objetPesable
            objetPesable composantPesable = instance3D.GetComponent<objetPesable>();

            // 3. L'ajouter à la zone de dépôt
            if (composantPesable != null)
            {
                _zoneJoueur.AjouterObjet(composantPesable);
            }
            else
            {
                Debug.LogError("Le prefab instancié n'a pas de script 'objetPesable' attaché !");
            }
        }
        else
        {
            Debug.LogWarning("La liste inventaireTest est vide ou l'index 0 est nul.");
        }
    }
}