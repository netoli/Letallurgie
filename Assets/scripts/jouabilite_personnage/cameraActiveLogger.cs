// ============================================================
// cameraActiveLogger.cs
// ------------------------------------------------------------
// Outil de diagnostic temporaire pour identifier TOUTES les
// cameras actives et celle qui rend reellement la Game view
// (= celle avec le plus haut Depth parmi les enabled).
// ============================================================

using UnityEngine;
using System.Collections.Generic;

public class cameraActiveLogger : MonoBehaviour
{
    [Tooltip("Intervalle (s) entre deux logs. 0 = chaque frame.")]
    [SerializeField] private float intervalleLog = 1f;

    private float prochainLog;
    private string dernierEtatSignature = "";

    void Update()
    {
        if (Time.unscaledTime < prochainLog) return;
        prochainLog = Time.unscaledTime + intervalleLog;

        // Recupere TOUTES les cameras actives dans la scene
        Camera[] toutes = Camera.allCameras;

        if (toutes.Length == 0)
        {
            Debug.LogWarning("[CamLogger] Aucune camera active !");
            return;
        }

        // Trouve celle avec le plus haut depth (= celle qui rend par-dessus)
        Camera renduePrincipale = null;
        float depthMax = float.MinValue;
        foreach (var c in toutes)
        {
            if (c.depth > depthMax)
            {
                depthMax = c.depth;
                renduePrincipale = c;
            }
        }

        // Genere une signature de l'etat pour ne logger qu'au changement
        List<string> sig = new List<string>();
        foreach (var c in toutes)
        {
            sig.Add($"{c.name}(d={c.depth})");
        }
        sig.Sort();
        string nouvelleSig = string.Join(",", sig);

        if (nouvelleSig != dernierEtatSignature)
        {
            dernierEtatSignature = nouvelleSig;
            Debug.Log($"[CamLogger] {toutes.Length} cam(s) active(s) : " +
                $"{string.Join(" | ", sig)}");
            if (renduePrincipale != null)
            {
                Debug.Log($"[CamLogger] => Rendu Game view = " +
                    $"'{renduePrincipale.name}' (depth={renduePrincipale.depth}, " +
                    $"pos={renduePrincipale.transform.position}, " +
                    $"parent={(renduePrincipale.transform.parent != null ? renduePrincipale.transform.parent.name : "(root)")}, " +
                    $"GO.active={renduePrincipale.gameObject.activeInHierarchy})");
            }
        }
    }
}
