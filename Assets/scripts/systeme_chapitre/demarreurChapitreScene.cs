// ============================================================
// demarreurChapitreScene.cs
// ------------------------------------------------------------
// Petit utilitaire a poser dans une scene pour declencher
// automatiquement un chapitre via gestionChapitres au demarrage.
//
// Permet d'avoir des banniere annonce-chapitre qui se lancent
// des l'arrivee dans une nouvelle scene, sans coder de logique
// custom. Ex : scene2_usine demarre le chapitre 'le_sauvetage'.
//
// SETUP UNITY :
// 1. Creer un GameObject vide nomme 'starter_chapitre_le_sauvetage'
// 2. Add Component -> demarreurChapitreScene
// 3. Renseigner 'Id Chapitre' (ex : 'le_sauvetage')
// 4. (Optionnel) Renseigner 'Delai Avant Demarrage' pour laisser
//    respirer le debut de scene.
// ============================================================

using System.Collections;
using UnityEngine;

public class demarreurChapitreScene : MonoBehaviour
{
    [Header("Chapitre")]
    [Tooltip("ID exact du chapitre a demarrer au Start de la scene. " +
        "Doit correspondre a un DonneesChapitre.idChapitre configure " +
        "dans gestionChapitres.")]
    [SerializeField] private string idChapitre;

    [Header("Timing")]
    [Tooltip("Delai (s) avant de demarrer le chapitre. Utile pour " +
        "laisser respirer le debut de scene avant la banniere. " +
        "0 = demarrage immediat au Start.")]
    [SerializeField] private float delaiAvantDemarrage = 0.5f;

    [Tooltip("Si coche, logue chaque etape dans la console.")]
    [SerializeField] private bool debugLogs = true;

    void Start()
    {
        if (string.IsNullOrEmpty(idChapitre))
        {
            Debug.LogWarning($"[DemarreurChapitre] {name} : " +
                "idChapitre vide, rien a demarrer.");
            return;
        }

        StartCoroutine(DemarrerApresDelai());
    }

    private IEnumerator DemarrerApresDelai()
    {
        if (delaiAvantDemarrage > 0f)
            yield return new WaitForSecondsRealtime(delaiAvantDemarrage);

        if (gestionChapitres.Instance == null)
        {
            Debug.LogWarning($"[DemarreurChapitre] {name} : " +
                "gestionChapitres.Instance introuvable. Le chapitre " +
                $"'{idChapitre}' ne peut pas etre demarre. Verifie " +
                "que tu arrives dans cette scene depuis scene0_tuto " +
                "(qui instancie gestionChapitres en DontDestroyOnLoad).");
            yield break;
        }

        if (debugLogs)
            Debug.Log($"[DemarreurChapitre] {name} : demarrage du " +
                $"chapitre '{idChapitre}'.");

        gestionChapitres.Instance.DemarrerChapitre(idChapitre);
    }
}
