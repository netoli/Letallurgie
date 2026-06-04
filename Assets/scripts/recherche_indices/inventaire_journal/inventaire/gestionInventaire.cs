using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class gestionInventaire : MonoBehaviour
{
    public static gestionInventaire Instance;

    private Dictionary<objetInventaire, int> objets =
        new Dictionary<objetInventaire, int>();

    public event Action onInventaireModifie;
    public event Action<int> onInventaireModifieHud;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // FIX (analogue JournalManager, gestionSelectionInventaire) :
            // transferer les enfants du doublon sous le singleton persistant
            // AVANT de detruire ce GameObject. Sinon tout enfant UI / slot /
            // sous-systeme parente a gestion_inventaire de la scene courante
            // est perdu au Destroy. Risque sans ce fix : l'inventaire UI de
            // scene2 ne se rafraichit plus apres RetirerObjet parce que les
            // refs visuelles parentes sous le doublon sont detruites.
            int nbEnfantsTransferes = 0;
            while (transform.childCount > 0)
            {
                Transform enfant = transform.GetChild(0);
                enfant.SetParent(Instance.transform, true);
                nbEnfantsTransferes++;
            }
            if (nbEnfantsTransferes > 0)
            {
                Debug.Log($"[gestionInventaire] Doublon '{name}' detecte " +
                    $"avec {nbEnfantsTransferes} enfant(s) — transferes " +
                    "au singleton persistant avant destruction.");
            }
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // CRITIQUE (analogue JournalManager + gestionSelectionInventaire) :
        // DontDestroyOnLoad ne fonctionne QUE sur les GameObjects ROOT.
        // Si gestion_inventaire etait parente sous un canvas ou autre,
        // DDOL echoue silencieusement → reset complet de l'inventaire au
        // LoadScene. On le detache du parent AVANT de marquer DDOL.
        if (transform.parent != null)
        {
            Debug.LogWarning($"[gestionInventaire] '{name}' etait parente " +
                $"a '{transform.parent.name}'. Detachement pour que " +
                "DontDestroyOnLoad fonctionne.");
            transform.SetParent(null, true);
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {

    }

    public void AjouterObjet(objetInventaire objet, int quantite = 1)
    {
        if (objets.ContainsKey(objet))
        {
            objets[objet] += quantite;

            if (objet.quantiteMax > 0
                && objets[objet] > objet.quantiteMax)
                objets[objet] = objet.quantiteMax;
        }
        else
        {
            objets[objet] = quantite;
        }

        Debug.Log("Ajoute: " + objet.nomObjet
            + " x" + objets[objet]);

        NotifierModif();
    }

    public void RetirerObjet(objetInventaire objet, int quantite = 1)
    {
        if (objet == null)
        {
            Debug.LogWarning("[gestionInventaire] RetirerObjet appele " +
                "avec objet=null, ignore.");
            return;
        }
        if (!objets.ContainsKey(objet))
        {
            Debug.LogWarning($"[gestionInventaire] RetirerObjet : " +
                $"'{objet.nomObjet}' absent du dico, ignore.");
            return;
        }

        int avant = objets[objet];
        objets[objet] -= quantite;

        if (objets[objet] <= 0)
            objets.Remove(objet);

        int apres = objets.ContainsKey(objet) ? objets[objet] : 0;
        Debug.Log($"[gestionInventaire] Retire: {objet.nomObjet} " +
            $"x{quantite} ({avant} → {apres}).");

        NotifierModif();
    }

    public int ObtenirQuantite(objetInventaire objet)
    {
        if (objets.ContainsKey(objet))
            return objets[objet];
        return 0;
    }

    public List<KeyValuePair<objetInventaire, int>> ObtenirParCategorie(
        CategorieObjet categorie)
    {
        List<KeyValuePair<objetInventaire, int>> resultats =
            new List<KeyValuePair<objetInventaire, int>>();

        foreach (var paire in objets)
        {
            if (paire.Key.categorie == categorie)
                resultats.Add(paire);
        }

        return resultats;
    }

    public int ObtenirTotalObjets()
    {
        int total = 0;
        foreach (var paire in objets)
            total += paire.Value;
        return total;
    }

    public void ViderInventaire()
    {
        objets.Clear();
        NotifierModif();
    }

   

    private void NotifierModif()
    {
        int total = ObtenirTotalObjets();
        int nbAbonnes = onInventaireModifie != null
            ? onInventaireModifie.GetInvocationList().Length : 0;
        int nbAbonnesHud = onInventaireModifieHud != null
            ? onInventaireModifieHud.GetInvocationList().Length : 0;
        Debug.Log($"[gestionInventaire] NotifierModif : total={total}, " +
            $"abonnes UI={nbAbonnes}, abonnes HUD={nbAbonnesHud}.");
        onInventaireModifie?.Invoke();
        onInventaireModifieHud?.Invoke(total);
    }
}