// ============================================================
// declencheurAction.cs
// ------------------------------------------------------------
// Composant generique de "glue" pour le flow tutoriel.
//
// Pose ce composant sur un GameObject dans la scene, configure
// dans l'Inspector :
//   - idActionEcoutee : l'idAction qui doit etre signalee
//   - delaiAvant      : delai en secondes avant l'action
//   - objetsAActiver  / objetsADesactiver : pointeurs, snap points...
//   - actionsACAppeler : UnityEvent pour appeler n'importe quelle
//                        methode (ex: DialogueTuto.DemarrerAuto())
//   - idActionFin     : si rempli, sera signale apres execution
//                        (permet de chainer plusieurs declencheurs)
//
// Le composant s'abonne a gestionChapitres.OnActionSignalee et
// se declenche UNE SEULE FOIS par run (sauf si autoReset = true).
//
// Cas d'usage typique :
//   - "bouteille_deposee_table" => active pointeur sur verre,
//                                  fait parler le PNJ client
//   - "audio_client_fini"       => active snap point sur comptoir
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class declencheurAction : MonoBehaviour
{
    [Header("Action a ecouter")]
    [Tooltip("L'idAction qui doit etre signalee par gestionChapitres " +
        "pour declencher ce composant.")]
    [SerializeField] private string idActionEcoutee;

    [Tooltip("Delai (s) entre la reception de l'action et l'execution " +
        "des effets. Utile pour laisser le temps a une animation, un " +
        "fade, etc. 0 = immediat.")]
    [SerializeField] private float delaiAvant = 0f;

    [Header("Effets sur les GameObjects")]
    [Tooltip("GameObjects a activer (SetActive(true)).")]
    [SerializeField] private GameObject[] objetsAActiver;

    [Tooltip("GameObjects a desactiver (SetActive(false)).")]
    [SerializeField] private GameObject[] objetsADesactiver;

    [Header("Actions personnalisees")]
    [Tooltip("UnityEvent appele apres les activations/desactivations. " +
        "Permet d'appeler par exemple DialogueTuto.DemarrerAuto() ou " +
        "n'importe quelle methode publique d'un autre composant.")]
    [SerializeField] private UnityEvent actionsACAppeler;

    [Header("Chainage")]
    [Tooltip("Si rempli, cette idAction sera signalee a gestionChapitres " +
        "APRES l'execution. Permet de chainer des declencheurs.")]
    [SerializeField] private string idActionFin;

    [Header("Options")]
    [Tooltip("Si coche, le composant peut etre redeclenche plusieurs " +
        "fois. Sinon il ne se declenche qu'une seule fois par run.")]
    [SerializeField] private bool autoReset = false;

    [Tooltip("Si coche, logue chaque etape dans la console.")]
    [SerializeField] private bool debugLogs = true;

    private bool dejaDeclenche = false;

    void OnEnable()
    {
        if (gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.OnActionSignalee += GererAction;
        }
    }

    void OnDisable()
    {
        if (gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.OnActionSignalee -= GererAction;
        }
    }

    private void GererAction(string idAction)
    {
        if (string.IsNullOrEmpty(idActionEcoutee)) return;
        if (idAction != idActionEcoutee) return;
        if (dejaDeclenche && !autoReset) return;

        if (debugLogs)
        {
            Debug.Log($"[declencheurAction:{name}] Action recue " +
                $"'{idAction}', declenchement dans {delaiAvant}s.");
        }

        dejaDeclenche = true;

        if (delaiAvant <= 0f)
        {
            Executer();
        }
        else
        {
            StartCoroutine(ExecuterApresDelai());
        }
    }

    private IEnumerator ExecuterApresDelai()
    {
        yield return new WaitForSeconds(delaiAvant);
        Executer();
    }

    private void Executer()
    {
        if (debugLogs)
        {
            Debug.Log($"[declencheurAction:{name}] Execution.");
        }

        if (objetsAActiver != null)
        {
            foreach (var go in objetsAActiver)
            {
                if (go != null) go.SetActive(true);
            }
        }

        if (objetsADesactiver != null)
        {
            foreach (var go in objetsADesactiver)
            {
                if (go != null) go.SetActive(false);
            }
        }

        actionsACAppeler?.Invoke();

        if (!string.IsNullOrEmpty(idActionFin)
            && gestionChapitres.Instance != null)
        {
            if (debugLogs)
            {
                Debug.Log($"[declencheurAction:{name}] " +
                    $"Signal d'enchainement: '{idActionFin}'.");
            }
            gestionChapitres.Instance.SignalerAction(idActionFin);
        }
    }

    /// <summary>
    /// Permet de remettre le declencheur a zero manuellement
    /// (ex: a la reinitialisation d'un chapitre).
    /// </summary>
    public void Reinitialiser()
    {
        dejaDeclenche = false;
    }
}
