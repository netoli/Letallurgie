using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class gestionSousTitre : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text texteInterlocuteur;
    [SerializeField] private TMP_Text textePropos;
    [SerializeField] private GameObject conteneurSousTitre;

    [Tooltip("RectTransform du panel qui doit se redimensionner " +
        "selon la longueur du texte (le fond orange). " +
        "Doit avoir un Content Size Fitter + Layout Group.")]
    [SerializeField] private RectTransform panelAjustable;

    [Header("Parametres")]
    [SerializeField] private float dureeAffichage;

    [Tooltip("Largeur maximum (pixels) que le texte propos peut " +
        "atteindre avant de passer a la ligne. Tant que le texte " +
        "est plus court, la bulle s'adapte naturellement a sa " +
        "longueur. Au-dela, le texte wrap et la bulle grandit en " +
        "hauteur a la place. Mettre a 0 pour desactiver la limite.")]
    [SerializeField] private float largeurMaxPropos = 1100f;

    [Header("Effet brouillard")]
    [Tooltip("Particle System du brouillard sur le sous-titre. Si " +
        "renseigne, sa Shape (Rectangle) sera redimensionnee a " +
        "chaque replique pour matcher la taille reelle de la bulle.")]
    [SerializeField] private ParticleSystem fxBrouillard;

    [Tooltip("Multiplicateurs appliques a la taille du panel quand " +
        "on met a jour la Shape du Particle System. Permet de " +
        "deborder (>1) ou rester a l'interieur (<1) de la bulle.")]
    [SerializeField] private Vector2 fxBrouillardMultiplicateur =
        new Vector2(1f, 1f);

    private float tailleOriginaleInterlocuteur;
    private float tailleOriginalePropos;
    private Color couleurOriginaleInterlocuteur;
    private Color couleurOriginalePropos;
    private Coroutine coroutineMasquage;

    // Vrai si AfficherSousTitre a ete appele avant que Start() tourne.
    // Dans ce cas Start() ne doit pas cacher le conteneur (il affiche
    // deja un sous-titre en cours).
    private bool _premierAppelEffectue = false;

    void Start()
    {
        tailleOriginaleInterlocuteur = texteInterlocuteur.fontSize;
        tailleOriginalePropos = textePropos.fontSize;

        // Memoriser la couleur configuree dans l'Inspector pour
        // qu'elle reste la couleur "par defaut" du jeu. Les options
        // d'accessibilite (jaune/cyan) ne s'appliquent que si le
        // joueur les choisit explicitement.
        couleurOriginaleInterlocuteur = texteInterlocuteur.color;
        couleurOriginalePropos = textePropos.color;

        // Ne cacher le conteneur que si AfficherSousTitre n'a pas deja
        // ete appele avant Start(). Cela arrive quand canvas_hud etait
        // inactif au chargement : Start() est differe au frame suivant,
        // mais AfficherSousTitre peut etre appele dans le meme frame que
        // canvasHud.SetActive(true). Sans ce guard, Start() cachait le
        // premier sous-titre deja affiche.
        if (!_premierAppelEffectue && conteneurSousTitre != null)
            conteneurSousTitre.SetActive(false);
    }

    public void AfficherSousTitre(string interlocuteur, string propos,
        float dureeCustom = -1f)
    {
        _premierAppelEffectue = true;

        if (coroutineMasquage != null)
            StopCoroutine(coroutineMasquage);

        texteInterlocuteur.text = interlocuteur + " :";
        textePropos.text = propos;

        // Activer le conteneur AVANT AppliquerOptionsAccessibilite.
        // canvas_hud etant inactif au chargement de la scene, le
        // Start() de ce composant n'a pas encore forcement tourne
        // quand AfficherSousTitre est appele pour la premiere fois.
        // Dans ce cas couleurOriginale vaut default(Color) = (0,0,0,0)
        // et AppliquerOptionsAccessibilite rendrait le texte invisible.
        // SetActive(true) declenche Awake/OnEnable de TMP_Text, qui
        // initialise m_fontColor depuis les donnees serialisees, ce qui
        // permet ensuite de recapturer la vraie couleur originale.
        conteneurSousTitre.SetActive(true);

        AppliquerOptionsAccessibilite();

        // Force TMP a recalculer son rendu avant que le Content Size
        // Fitter ne lise la taille preferee, sinon la bulle peut
        // garder la largeur du texte precedent pendant 1 frame.
        texteInterlocuteur.ForceMeshUpdate();
        textePropos.ForceMeshUpdate();

        // Calcul de la largeur dynamique du propos :
        //   - Si le texte tient sur une ligne en moins de largeurMaxPropos,
        //     on contraint le Layout Element a cette largeur naturelle
        //     => la bulle reste juste autour du texte (largeur adaptee).
        //   - Sinon, on contraint a largeurMaxPropos
        //     => le texte wrap en multiligne et la bulle grandit en hauteur.
        if (largeurMaxPropos > 0f)
        {
            // Largeur naturelle si le texte etait sur une seule ligne
            Vector2 tailleUneLigne =
                textePropos.GetPreferredValues(textePropos.text);
            float largeurCible = Mathf.Min(
                tailleUneLigne.x, largeurMaxPropos);

            var le = textePropos.GetComponent<UnityEngine.UI.LayoutElement>();
            if (le == null)
                le = textePropos.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = largeurCible;
            le.flexibleWidth = 0f;
        }

        if (panelAjustable != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelAjustable);

        // Adapte la zone d'emission du Particle System brouillard
        // a la taille reelle de la bulle (apres le LayoutRebuilder).
        if (fxBrouillard != null && panelAjustable != null)
        {
            Vector2 tailleBulle = panelAjustable.rect.size;
            var shape = fxBrouillard.shape;
            shape.scale = new Vector3(
                tailleBulle.x * fxBrouillardMultiplicateur.x,
                tailleBulle.y * fxBrouillardMultiplicateur.y,
                shape.scale.z);
        }

        // dureeCustom < 0 = l'appelant gere la duree lui-meme (cas
        // du DialogueTuto qui defile plusieurs repliques avec ESC).
        // On ne lance PAS le masquage auto dans ce cas.
        if (dureeCustom < 0f)
            return;

        float duree = dureeCustom > 0 ? dureeCustom : dureeAffichage;
        coroutineMasquage = StartCoroutine(MasquerApresDelai(duree));
    }

    public void RafraichirOptions()
    {
        if (conteneurSousTitre != null
            && conteneurSousTitre.activeSelf)
        {
            AppliquerOptionsAccessibilite();
        }
    }

    private void AppliquerOptionsAccessibilite()
    {
        // Taille
        int indexTaille = PlayerPrefs.GetInt("tailleSousTitre", 1);
        float[] multiplicateurs = { 0.75f, 1f, 1.35f };

        if (indexTaille >= 0 && indexTaille < multiplicateurs.Length)
        {
            // Start() peut lire fontSize=0 si TMP_Text n'a pas encore
            // effectue sa premiere passe d'initialisation (bug connu
            // quand gestionSousTitre.Start() s'execute avant que TMP
            // ne soit pret). On recapture ici lors du premier appel
            // reel (le GO vient d'etre active), et on applique un
            // fallback a 21 si le composant retourne toujours 0.
            if (tailleOriginaleInterlocuteur <= 0f)
            {
                tailleOriginaleInterlocuteur = texteInterlocuteur.fontSize;
                if (tailleOriginaleInterlocuteur <= 0f)
                    tailleOriginaleInterlocuteur = 21f;
            }
            if (tailleOriginalePropos <= 0f)
            {
                tailleOriginalePropos = textePropos.fontSize;
                if (tailleOriginalePropos <= 0f)
                    tailleOriginalePropos = 21f;
            }

            float mult = multiplicateurs[indexTaille];
            texteInterlocuteur.fontSize =
                tailleOriginaleInterlocuteur * mult;
            textePropos.fontSize =
                tailleOriginalePropos * mult;
        }

        // Couleur :
        //   index 0 = couleur normale (celle configuree dans
        //             l'Inspector du gestionSousTitre, ex: #E0C9A2)
        //   index 1 = jaune (accessibilite haute visibilite)
        //   index 2 = cyan (accessibilite haute visibilite)
        int indexCouleur = PlayerPrefs.GetInt("couleurSousTitre", 0);

        if (indexCouleur == 1)
        {
            texteInterlocuteur.color = Color.yellow;
            textePropos.color = Color.yellow;
        }
        else if (indexCouleur == 2)
        {
            texteInterlocuteur.color = Color.cyan;
            textePropos.color = Color.cyan;
        }
        else
        {
            // Meme garde que pour la taille : Start() peut avoir lu
            // couleurOriginale = (0,0,0,0) si canvas_hud etait inactif
            // quand Start() s'est execute. Maintenant que SetActive(true)
            // a ete appele avant cet appel, TMP est initialise et on peut
            // relire la vraie couleur serialisee depuis l'Inspector.
            if (couleurOriginaleInterlocuteur.a <= 0f)
                couleurOriginaleInterlocuteur = texteInterlocuteur.color;
            if (couleurOriginalePropos.a <= 0f)
                couleurOriginalePropos = textePropos.color;

            // index 0 (defaut) : on restaure la couleur de l'Inspector
            texteInterlocuteur.color = couleurOriginaleInterlocuteur;
            textePropos.color = couleurOriginalePropos;
        }
    }

    public void MasquerSousTitre()
    {
        if (coroutineMasquage != null)
            StopCoroutine(coroutineMasquage);

        conteneurSousTitre.SetActive(false);
    }

    private System.Collections.IEnumerator MasquerApresDelai(float delai)
    {
        yield return new WaitForSeconds(delai);
        conteneurSousTitre.SetActive(false);
    }
}
