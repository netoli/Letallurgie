// ============================================================
// DEBUGBalancePlateau.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026
// ------------------------------------------------------------
// Description :
//   Script de debug temporaire. Permet de tester la balance
//   avec des touches clavier directement en jeu.
//   Touches 1-5 : déposer objet, Shift+1-5 : retirer objet
//   À SUPPRIMER avant la build finale.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class DEBUGBalancePlateau : MonoBehaviour
{
    [SerializeField] private ZoneDepotJoueur _zoneJoueur;
    [SerializeField] private Transform _spawnPointBalance;
    public List<objetInventaire> inventaireTest;

    // Garde une référence aux objets instanciés pour pouvoir les retirer
    private List<objetPesable> _objetsInstancies = new List<objetPesable>();
    void Start()
    {
        slotObjetInventaire.AssignerZoneBalance(_zoneJoueur);
    }

    void Update()
    {
        // Shift enfoncé = retirer, sinon = déposer
        bool retirer = Input.GetKey(KeyCode.LeftShift)
                    || Input.GetKey(KeyCode.RightShift);

        if (Input.GetKeyDown(KeyCode.Alpha1))
            GererObjet(0, retirer);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            GererObjet(1, retirer);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            GererObjet(2, retirer);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            GererObjet(3, retirer);
        if (Input.GetKeyDown(KeyCode.Alpha5))
            GererObjet(4, retirer);
    }

    private void GererObjet(int index, bool retirer)
    {
        if (index >= inventaireTest.Count
            || inventaireTest[index] == null)
        {
            Debug.LogWarning($"[DEBUG] Aucun objet à l'index {index}");
            return;
        }

        if (retirer)
        {
            RetirerObjet(index);
        }
        else
        {
            DeposerObjet(index);
        }
    }

    private void DeposerObjet(int index)
    {
        // Vérifier si cet objet est déjà déposé
        if (_objetsInstancies.Count > index
            && _objetsInstancies[index] != null)
        {
            Debug.LogWarning($"[DEBUG] Objet {index + 1} déjà sur la balance");
            return;
        }

        objetInventaire data = inventaireTest[index];

        if (data.prefab3D == null)
        {
            Debug.LogWarning($"[DEBUG] {data.nomObjet} n'a pas de prefab3D!");
            return;
        }

        // Décaler chaque objet légèrement pour qu'ils se chevauchent pas
        Vector3 position = _spawnPointBalance.position
            + Vector3.right * index * 0.3f;

        GameObject instance = Instantiate(
            data.prefab3D, position, Quaternion.identity);

        objetPesable composant = instance.GetComponent<objetPesable>();

        if (composant != null)
        {
            // Étendre la liste si nécessaire
            while (_objetsInstancies.Count <= index)
                _objetsInstancies.Add(null);

            _objetsInstancies[index] = composant;
            _zoneJoueur.AjouterObjet(composant);

            Debug.Log($"[DEBUG] Touche {index + 1} — " +
                      $"Déposé : {data.nomObjet} (poids {composant.valeurPoids})");
        }
        else
        {
            Debug.LogError($"[DEBUG] {data.nomObjet} : " +
                           $"prefab3D sans composant objetPesable!");
            Destroy(instance);
        }
    }

    private void RetirerObjet(int index)
    {
        if (index >= _objetsInstancies.Count
            || _objetsInstancies[index] == null)
        {
            Debug.LogWarning($"[DEBUG] Aucun objet {index + 1} à retirer");
            return;
        }

        objetPesable composant = _objetsInstancies[index];
        _zoneJoueur.RetirerObjet(composant);
        Destroy(composant.gameObject);
        _objetsInstancies[index] = null;

        Debug.Log($"[DEBUG] Shift+{index + 1} — Retiré objet {index + 1}");
    }
}