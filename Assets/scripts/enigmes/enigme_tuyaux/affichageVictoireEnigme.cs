// ============================================================
// affichageVictoireEnigme.cs
// ------------------------------------------------------------
// Sur victoire de l'enigme (event onVictoire), active un canvas
// 'Felicitations' pendant N secondes puis le desactive. Apres
// disparition, signale une action narrative configurable pour
// chainer la suite (ex : 'enigme_tuyaux_reussite_terminee' pour
// declencher le bandeau 'trouve la porte de sortie').
//
// SETUP UNITY :
// 1. Creer un canvas 'canvas_felicitations' dans canvas_hud
//    (desactive par defaut).
// 2. Sur le GameObject qui porte gestionEnigmeTuyauterie,
//    Add Component → affichageVictoireEnigme.
// 3. Glisser canvas_felicitations dans le champ.
// 4. Configurer dureeAffichage (defaut 6s) et idActionApres
//    (defaut 'enigme_tuyaux_reussite_terminee').
// ============================================================

using System.Collections;
using UnityEngine;

public class affichageVictoireEnigme : MonoBehaviour
{
    [Header("Canvas Felicitations")]
    [SerializeField] private GameObject canvasFelicitations;

    [Header("Timing")]
    [Tooltip("Duree (s) d'affichage du canvas Felicitations.")]
    [SerializeField] private float dureeAffichage = 6f;

    [Tooltip("Delai (s) avant que le canvas s'affiche apres victoire.")]
    [SerializeField] private float delaiAvantAffichage = 0f;

    [Header("Action narrative apres disparition")]
    [Tooltip("idAction signalee apres disparition du canvas. " +
        "Permet de chainer 'bandeau trouve la porte'. " +
        "Ex : 'enigme_tuyaux_reussite_terminee'.")]
    [SerializeField] private string idActionApres =
        "enigme_tuyaux_reussite_terminee";

    [Tooltip("Delai (s) entre disparition canvas et signal idActionApres. " +
        "Defaut 3s (le user veut bandeau 'trouve la porte' 3s apres).")]
    [SerializeField] private float delaiApresDisparition = 3f;

    private gestionEnigmeTuyauterie enigme;

    void Start()
    {
        enigme = GetComponent<gestionEnigmeTuyauterie>();
        if (enigme == null)
            enigme = FindFirstObjectByType<gestionEnigmeTuyauterie>();
        if (enigme != null)
            enigme.onVictoire.AddListener(AuVictoire);

        if (canvasFelicitations != null)
            canvasFelicitations.SetActive(false);
    }

    void OnDestroy()
    {
        if (enigme != null)
            enigme.onVictoire.RemoveListener(AuVictoire);
    }

    private void AuVictoire()
    {
        StartCoroutine(SequenceVictoire());
    }

    private IEnumerator SequenceVictoire()
    {
        if (delaiAvantAffichage > 0f)
            yield return new WaitForSeconds(delaiAvantAffichage);

        if (canvasFelicitations != null)
            canvasFelicitations.SetActive(true);

        yield return new WaitForSeconds(dureeAffichage);

        if (canvasFelicitations != null)
            canvasFelicitations.SetActive(false);

        if (delaiApresDisparition > 0f)
            yield return new WaitForSeconds(delaiApresDisparition);

        if (!string.IsNullOrEmpty(idActionApres)
            && gestionChapitres.Instance != null)
        {
            Debug.Log($"[affichageVictoireEnigme] Signal action " +
                $"'{idActionApres}' apres victoire+disparition.");
            gestionChapitres.Instance.SignalerAction(idActionApres);
        }
    }
}
