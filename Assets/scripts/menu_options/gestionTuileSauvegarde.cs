using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// ============================================================
// gestionTuileSauvegarde.cs
// ------------------------------------------------------------
// Gere les tuiles de sauvegarde du menu options.
//
// AUTO-RESOLUTION (refonte Oli, basee sur la vraie hierarchie) :
//   Cause du bug "les boutons n'apparaissent pas au clic" : la liste
//   'tuiles' du composant avait toutes ses references vides (fileID 0).
//   Le clic appelait bien SelectionnerTuile, mais sans references aucun
//   bouton ne pouvait s'afficher.
//
//   Correctif : au OnEnable, le script DECOUVRE les regroupements
//   (regroupement_contenus_et_bouton_sauvegarde*) sous lui-meme, et pour
//   chaque tuile resout AUTOMATIQUEMENT, par nom, ce qui n'est pas deja
//   assigne dans l'Inspector :
//     - ensembleImageEtDonnees : 'ensemble_image_et_donnees_sauvegarde'
//     - boutonCharger          : 'bouton_telecharger_sauvegarde' ou
//                                'bouton_charger_sauvegarde'
//     - boutonSauvegarder      : 'bouton_sauvegarder' ou 'bouton_sauvegarde'
//     - boutonSupprimer        : 'prefab_bouton_supprimer' ou 'bouton_supprimer'
//   (Les noms varient selon les scenes/prefabs, d'ou les candidats.)
//
//   Non destructif : une reference deja assignee dans l'Inspector est
//   conservee; on ne remplit que ce qui est vide.
//
// HYPOTHESE : l'ordre des regroupements dans la hierarchie correspond a
// l'index passe par chaque bouton (SelectionnerTuile(0), (1), ...). C'est
// le cas si les tuiles ont ete dupliquees dans l'ordre. Si une tuile
// affiche les boutons d'une autre, c'est que l'ordre ne correspond pas :
// dis-le-moi et on ajustera.
// ============================================================

public class gestionTuileSauvegarde : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject prefabContenusSauvegarde;

    [Header("Tuiles (références optionnelles — auto-résolues si vides)")]
    [SerializeField] private List<TuileSauvegarde> tuiles;

    [System.Serializable]
    public class TuileSauvegarde
    {
        public Transform ensembleImageEtDonnees;
        public GameObject boutonCharger;
        public GameObject boutonSauvegarder;
        public GameObject boutonSupprimer;
        [HideInInspector] public int indexSlot;
        [HideInInspector] public bool contientSauvegarde;
        [HideInInspector] public GameObject contenuInstancie;
        [HideInInspector] public GameObject groupementRacine;
    }

    // Noms candidats (couvre les variations observees entre scenes/prefabs).
    private static readonly string[] NOMS_ENSEMBLE =
        { "ensemble_image_et_donnees_sauvegarde" };
    private static readonly string[] NOMS_CHARGER =
        { "bouton_telecharger_sauvegarde", "bouton_charger_sauvegarde" };
    private static readonly string[] NOMS_SAUVEGARDER =
        { "bouton_sauvegarder", "bouton_sauvegarde" };
    private static readonly string[] NOMS_SUPPRIMER =
        { "prefab_bouton_supprimer", "bouton_supprimer" };

    private const string PREFIXE_REGROUPEMENT =
        "regroupement_contenus_et_bouton_sauvegarde";

    private int tuileSelectionnee = -1;
    private bool dejaResolu = false;

    void OnEnable()
    {
        AutoResoudreTuiles();
        StartCoroutine(RafraichirAvecDelai());
    }

    private System.Collections.IEnumerator RafraichirAvecDelai()
    {
        yield return null;
        if (gestionPartie.Instance != null)
            RafraichirTuiles();
    }

    // ── Auto-résolution des références ────────────────────────

    private void AutoResoudreTuiles()
    {
        // Découvrir les regroupements de tuiles sous ce GameObject, dans
        // l'ordre de hiérarchie.
        List<Transform> groupements = new List<Transform>();
        CollecterGroupements(transform, groupements);

        if (groupements.Count == 0)
        {
            // Pas de regroupement trouvé sous le manager : on garde la
            // liste de l'Inspector telle quelle (cas d'un setup manuel
            // différent).
            return;
        }

        if (tuiles == null) tuiles = new List<TuileSauvegarde>();
        while (tuiles.Count < groupements.Count)
            tuiles.Add(new TuileSauvegarde());

        for (int i = 0; i < groupements.Count; i++)
        {
            Transform g = groupements[i];
            if (tuiles[i] == null) tuiles[i] = new TuileSauvegarde();
            TuileSauvegarde t = tuiles[i];

            // Racine de la tuile : cachee entierement quand le slot
            // est vide (RafraichirTuiles).
            t.groupementRacine = g.gameObject;

            if (t.ensembleImageEtDonnees == null)
            {
                Transform e = TrouverEnfantParNoms(g, NOMS_ENSEMBLE);
                t.ensembleImageEtDonnees = e != null ? e : g;
            }
            if (t.boutonCharger == null)
                t.boutonCharger = TrouverEnfantGO(g, NOMS_CHARGER);
            if (t.boutonSauvegarder == null)
                t.boutonSauvegarder = TrouverEnfantGO(g, NOMS_SAUVEGARDER);
            if (t.boutonSupprimer == null)
                t.boutonSupprimer = TrouverEnfantGO(g, NOMS_SUPPRIMER);

            // CORRECTIF "seule la 1re tuile reagit" : dans le prefab
            // prefab_ensemble_image_et_donnees_sauvegarde, le onClick
            // SelectionnerTuile a une CIBLE VIDE (m_Target: 0) et un index
            // fige a 0. Seule la tuile 1 a ete recablee a la main dans une
            // instance. On cable donc TOUTES les tuiles au runtime avec le
            // bon index, et on coupe l'appel persistant defaillant.
            CablerSelectionTuile(t, i);

            // CORRECTIF "slot fantome" : certains de ces boutons portent
            // (via prefab ou rebind) des appels qui sauvegardent SANS slot
            // (SauvegarderPartie -> prochain slot libre, max 15 -> ecrit
            // dans le slot 4 que l'UI n'affiche jamais). On coupe ces
            // appels persistants; seuls nos listeners avec slot explicite
            // (CablerBouton) restent actifs.
            CouperPersistentsDangereux(t.boutonCharger);
            CouperPersistentsDangereux(t.boutonSauvegarder);
            CouperPersistentsDangereux(t.boutonSupprimer);
        }

        dejaResolu = true;
    }

    // Methodes persistantes (cablees dans l'Inspector/prefab) a desactiver
    // sur les boutons des tuiles : elles court-circuitent la logique de
    // slot. On garde tout le reste (ex. LancerEffetClic pour le son).
    private static readonly string[] METHODES_PERSISTANTES_A_COUPER =
    {
        "SelectionnerTuile", "SauvegarderPartie", "Sauvegarder",
        "SupprimerSauvegarde", "SupprimerToutesSauvegardes",
        "ChargerEtAppliquer", "ChargerPartie"
    };

    private void CouperPersistentsDangereux(GameObject boutonGO)
    {
        if (boutonGO == null) return;
        Button btn = boutonGO.GetComponent<Button>();
        if (btn == null) return;
        CouperPersistentsDangereux(btn);
    }

    private void CouperPersistentsDangereux(Button btn)
    {
        if (btn == null) return;
        int n = btn.onClick.GetPersistentEventCount();
        for (int i = 0; i < n; i++)
        {
            string methode = btn.onClick.GetPersistentMethodName(i);
            for (int j = 0; j < METHODES_PERSISTANTES_A_COUPER.Length; j++)
            {
                if (methode == METHODES_PERSISTANTES_A_COUPER[j])
                {
                    btn.onClick.SetPersistentListenerState(i,
                        UnityEngine.Events.UnityEventCallState.Off);
                    break;
                }
            }
        }
    }

    private void CablerSelectionTuile(TuileSauvegarde t, int index)
    {
        if (t == null || t.ensembleImageEtDonnees == null) return;

        Button btn = t.ensembleImageEtDonnees.GetComponent<Button>();
        if (btn == null)
            btn = t.ensembleImageEtDonnees
                .GetComponentInChildren<Button>(true);
        if (btn == null) return;

        // Coupe le SelectionnerTuile persistant (cible vide / index 0).
        CouperPersistentsDangereux(btn);

        int indexCapture = index;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => SelectionnerTuile(indexCapture));
    }

    // Collecte les regroupements (par prefixe de nom) sous 'parent', sans
    // descendre DANS un regroupement trouve (les boutons sont a l'interieur).
    private void CollecterGroupements(Transform parent, List<Transform> liste)
    {
        foreach (Transform enfant in parent)
        {
            if (enfant.name.StartsWith(PREFIXE_REGROUPEMENT))
                liste.Add(enfant);
            else
                CollecterGroupements(enfant, liste);
        }
    }

    private Transform TrouverEnfantParNoms(Transform racine, string[] noms)
    {
        foreach (Transform enfant in racine)
        {
            for (int i = 0; i < noms.Length; i++)
                if (enfant.name == noms[i]) return enfant;

            Transform r = TrouverEnfantParNoms(enfant, noms);
            if (r != null) return r;
        }
        return null;
    }

    private GameObject TrouverEnfantGO(Transform racine, string[] noms)
    {
        Transform t = TrouverEnfantParNoms(racine, noms);
        return t != null ? t.gameObject : null;
    }

    // ── Rafraichissement ──────────────────────────────────────

    public void RafraichirTuiles()
    {
        if (!dejaResolu) AutoResoudreTuiles();

        List<gestionPartie.DonneesSauvegarde> sauvegardes =
            gestionPartie.Instance.ObtenirToutesSauvegardes();

        tuileSelectionnee = -1;

        for (int i = 0; i < tuiles.Count; i++)
        {
            TuileSauvegarde tuile = tuiles[i];
            if (tuile == null || tuile.ensembleImageEtDonnees == null)
                continue;

            tuile.indexSlot = i;

            gestionEffetsBoutonsCliques effet =
                tuile.ensembleImageEtDonnees
                    .GetComponent<gestionEffetsBoutonsCliques>();
            if (effet != null)
                effet.AppliquerCouleurNormale();

            if (tuile.contenuInstancie != null)
            {
                Destroy(tuile.contenuInstancie);
                tuile.contenuInstancie = null;
            }

            gestionPartie.DonneesSauvegarde donnees = null;
            foreach (var s in sauvegardes)
            {
                if (s.indexSlot == i) { donnees = s; break; }
            }

            // Slot vide : la tuile ENTIERE disparait de la liste
            // (demande d'Oli). Consequence assumee : on ne peut plus
            // creer une sauvegarde en cliquant un slot vide — la
            // creation passe par K, le bouton du menu pause, ou la
            // sauvegarde automatique en quittant le jeu.
            if (tuile.groupementRacine != null)
                tuile.groupementRacine.SetActive(donnees != null);

            if (donnees != null)
            {
                tuile.contientSauvegarde = true;

                GameObject contenu = Instantiate(
                    prefabContenusSauvegarde,
                    tuile.ensembleImageEtDonnees);
                tuile.contenuInstancie = contenu;

                RemplirContenu(contenu, donnees);
            }
            else
            {
                tuile.contientSauvegarde = false;
            }

            // Tous les boutons caches par defaut.
            if (tuile.boutonCharger != null)
                tuile.boutonCharger.SetActive(false);
            if (tuile.boutonSauvegarder != null)
                tuile.boutonSauvegarder.SetActive(false);
            if (tuile.boutonSupprimer != null)
                tuile.boutonSupprimer.SetActive(false);
        }
    }

    // ── Sélection (clic sur une tuile) ────────────────────────

    public void SelectionnerTuile(int index)
    {
        if (index < 0 || index >= tuiles.Count) return;

        // Désélectionne l'ancienne tuile : cache ses boutons + couleur.
        if (tuileSelectionnee >= 0 && tuileSelectionnee < tuiles.Count)
        {
            TuileSauvegarde ancienne = tuiles[tuileSelectionnee];
            if (ancienne != null)
            {
                if (ancienne.boutonCharger != null)
                    ancienne.boutonCharger.SetActive(false);
                if (ancienne.boutonSauvegarder != null)
                    ancienne.boutonSauvegarder.SetActive(false);
                if (ancienne.boutonSupprimer != null)
                    ancienne.boutonSupprimer.SetActive(false);

                if (ancienne.ensembleImageEtDonnees != null)
                {
                    gestionEffetsBoutonsCliques effetAncien =
                        ancienne.ensembleImageEtDonnees
                            .GetComponent<gestionEffetsBoutonsCliques>();
                    if (effetAncien != null)
                        effetAncien.AppliquerCouleurNormale();
                }
            }
        }

        tuileSelectionnee = index;
        TuileSauvegarde tuile = tuiles[index];
        if (tuile == null) return;

        if (tuile.ensembleImageEtDonnees != null)
        {
            gestionEffetsBoutonsCliques effetNouvel =
                tuile.ensembleImageEtDonnees
                    .GetComponent<gestionEffetsBoutonsCliques>();
            if (effetNouvel != null)
                effetNouvel.AppliquerCouleurSelectionne();
        }

        int slot = index;

        // SAUVEGARDER : TOUJOURS visible (demande d'Oli) — il sert aussi
        // a ECRASER une sauvegarde existante. Attention : depuis le menu
        // principal, aucune partie n'est en cours -> Sauvegarder() ne
        // peut rien ecrire (partieEnCours null, warning en console). En
        // jeu (pause -> options), il ecrase le fichier du slot.
        if (tuile.boutonSauvegarder != null)
        {
            tuile.boutonSauvegarder.SetActive(true);
            CablerBouton(tuile.boutonSauvegarder, () =>
            {
                gestionPartie.Instance.Sauvegarder(slot);
                RafraichirTuiles();
            });
        }

        if (tuile.contientSauvegarde)
        {
            // CHARGER : seulement si la tuile contient une sauvegarde.
            if (tuile.boutonCharger != null)
            {
                tuile.boutonCharger.SetActive(true);
                CablerBouton(tuile.boutonCharger, () =>
                {
                    gestionPartie.Instance.ChargerEtAppliquer(slot);

                    gestionInputsJeu inputs =
                        FindFirstObjectByType<gestionInputsJeu>();
                    if (inputs != null)
                    {
                        gestionsTransitions transitions =
                            inputs.GetComponent<gestionsTransitions>();
                        if (transitions != null)
                            transitions.RetourEnJeuDepuisChargement();

                        inputs.ActiverInputs();
                    }
                });
            }

            // SUPPRIMER : seulement si la tuile contient une sauvegarde.
            if (tuile.boutonSupprimer != null)
            {
                tuile.boutonSupprimer.SetActive(true);
                CablerBouton(tuile.boutonSupprimer, () =>
                {
                    gestionPartie.Instance.SupprimerSauvegarde(slot);
                    RafraichirTuiles();

                    // Le bouton Continuer du menu principal suit en
                    // direct (cache si plus aucune sauvegarde).
                    var transitions =
                        FindFirstObjectByType<gestionsTransitions>(
                            FindObjectsInactive.Include);
                    if (transitions != null)
                        transitions.MettreAJourBoutonContinuer();
                });
            }
        }
        else
        {
            if (tuile.boutonCharger != null)
                tuile.boutonCharger.SetActive(false);
            if (tuile.boutonSupprimer != null)
                tuile.boutonSupprimer.SetActive(false);
        }
    }

    private void CablerBouton(GameObject boutonGO,
        UnityEngine.Events.UnityAction action)
    {
        if (boutonGO == null) return;
        Button btn = boutonGO.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    // ── Remplissage du contenu (inchangé) ─────────────────────

    private void RemplirContenu(
        GameObject contenu,
        gestionPartie.DonneesSauvegarde donnees)
    {
        Transform contenus = contenu.transform
            .Find("contenus_sauvegarde");

        if (contenus == null)
            contenus = contenu.transform;

        RawImage imageUI = contenus
            .Find("image_capturee_dans_jeu")
            ?.GetComponent<RawImage>();

        if (imageUI != null)
        {
            Texture2D capture = gestionPartie.Instance
                .ChargerCapture(donnees.cheminCapture);

            if (capture != null)
                imageUI.texture = capture;
        }

        Transform donnesSur = contenus
            .Find("donnees_sur_sauvegarde");

        if (donnesSur != null)
        {
            TMP_Text titre = donnesSur.Find("titre_sauvegarde")
                ?.GetComponent<TMP_Text>();
            if (titre != null)
                titre.text = donnees.nomSauvegarde;

            TMP_Text nomScene = donnesSur.Find("nom_scene")
                ?.GetComponent<TMP_Text>();
            if (nomScene != null)
                nomScene.text = donnees.nomScene;
        }

        Transform ensembleDonnees = contenus
            .Find("donnees_sur_sauvegarde/ensemble_donnees_sauvegarde");

        if (ensembleDonnees == null)
            ensembleDonnees = contenus
                .Find("ensemble_donnees_sauvegarde");

        if (ensembleDonnees != null)
        {
            TMP_Text dateTexte = RechercheProfonde(
                ensembleDonnees, "date");
            if (dateTexte != null)
                dateTexte.text = donnees.date;

            TMP_Text heureTexte = RechercheProfonde(
                ensembleDonnees, "nombre_heure");
            if (heureTexte != null)
                heureTexte.text = donnees.heure;

            TMP_Text tempsTexte = RechercheProfonde(
                ensembleDonnees, "temps_ecoule");
            if (tempsTexte != null)
                tempsTexte.text = FormaterTemps(donnees.tempsDeJeu);

            TMP_Text poidsTexte = RechercheProfonde(
                ensembleDonnees, "poid_du_sauvegarde");
            if (poidsTexte != null)
                poidsTexte.text = FormaterTaille(donnees.poids);
        }
    }

    private TMP_Text RechercheProfonde(Transform parent, string nom)
    {
        foreach (Transform enfant in parent)
        {
            if (enfant.name == nom)
                return enfant.GetComponent<TMP_Text>();

            TMP_Text resultat = RechercheProfonde(enfant, nom);
            if (resultat != null)
                return resultat;
        }
        return null;
    }

    private string FormaterTaille(long octets)
    {
        if (octets < 1024)
            return octets + " o";
        if (octets < 1024 * 1024)
            return (octets / 1024f).ToString("F1") + " Ko";

        return (octets / (1024f * 1024f)).ToString("F1") + " Mo";
    }

    private string FormaterTemps(float secondes)
    {
        int heures = (int)(secondes / 3600);
        int minutes = (int)((secondes % 3600) / 60);
        int secs = (int)(secondes % 60);

        if (heures > 0)
            return heures + "h " + minutes.ToString("D2")
                + "m " + secs.ToString("D2") + "s";

        return minutes + "m " + secs.ToString("D2") + "s";
    }
}
