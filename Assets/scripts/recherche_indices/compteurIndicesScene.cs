// ============================================================
// compteurIndicesScene.cs
// ------------------------------------------------------------
// Surveille le JournalManager (singleton persistant) et signale
// une idAction quand le nombre d'indices ramasses atteint le
// seuil configure dans l'Inspector. Permet de declencher des
// bandeaux ou pointeurs au moment "tous indices ramasses"
// dans une scene specifique.
//
// SETUP UNITY :
// 1. Creer un GameObject vide dans la scene (ex: 'compteur_indices')
// 2. Add Component → compteurIndicesScene
// 3. Renseigner :
//    - totalIndicesAttendus : nombre d'indices total dans cette scene
//      (ex : 5 pour scene1_taverne1)
//    - idActionASignaler : 'tous_indices_ramasses' par defaut
//    - seuilFixe : si true, le seuil est compare strictement;
//      si false (recommande), le seuil est atteint des que le count
//      depasse, ce qui couvre le cas ou le joueur avait deja ramasse
//      des indices en arrivant dans la scene.
//
// NB : Le JournalManager etant un singleton persistant
// (DontDestroyOnLoad), son compteur d'entrees persiste entre scenes.
// Si ta scene 1 a 5 indices et qu'on relance le jeu, le count est a 0
// au demarrage. Si tu reloads scene1 depuis scene2 sans reset, le count
// repart de 5 et tous_indices_ramasses sera signale immediatement au
// premier indice. Pour eviter ca, soit reset entrees au demarrage de
// scene1, soit utiliser seuilFixe=true et accepter de ne signaler que
// pour la valeur exacte.
// ============================================================

using UnityEngine;

public class compteurIndicesScene : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Nombre total d'indices a ramasser dans cette scene avant " +
        "de signaler. Ex : 5 pour scene1_taverne1 (cle, note, journal, " +
        "masque, rivet-boulon).")]
    [SerializeField] private int totalIndicesAttendus = 5;

    [Tooltip("ID d'action signalee a gestionChapitres quand le seuil est " +
        "atteint. Defaut : 'tous_indices_ramasses'.")]
    [SerializeField] private string idActionASignaler = "tous_indices_ramasses";

    [Tooltip("Si coche, le signal n'est emis que quand count == total " +
        "exactement. Si decoche (recommande), le signal est emis des " +
        "que count >= total. Voir commentaire en tete de fichier.")]
    [SerializeField] private bool seuilFixe = false;

    [Tooltip("Si coche, logue chaque ajout d'indice + le declenchement.")]
    [SerializeField] private bool debugLogs = true;

    private bool dejaSignale = false;

    void OnEnable()
    {
        JournalManager.OnIndiceAjoute += AuIndiceAjoute;
    }

    void OnDisable()
    {
        JournalManager.OnIndiceAjoute -= AuIndiceAjoute;
    }

    private void AuIndiceAjoute(int nouveauCount)
    {
        if (dejaSignale) return;

        if (debugLogs)
        {
            Debug.Log($"[CompteurIndices] {name} : count={nouveauCount}, " +
                $"seuil={totalIndicesAttendus}.");
        }

        bool seuilAtteint = seuilFixe
            ? nouveauCount == totalIndicesAttendus
            : nouveauCount >= totalIndicesAttendus;

        if (!seuilAtteint) return;

        if (gestionChapitres.Instance == null)
        {
            Debug.LogWarning($"[CompteurIndices] {name} : " +
                "gestionChapitres.Instance introuvable, signal annule.");
            return;
        }

        dejaSignale = true;

        if (debugLogs)
        {
            Debug.Log($"[CompteurIndices] {name} : seuil atteint, " +
                $"signal '{idActionASignaler}'.");
        }

        gestionChapitres.Instance.SignalerAction(idActionASignaler);
    }
}
