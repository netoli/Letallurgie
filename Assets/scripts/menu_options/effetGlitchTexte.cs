using System.Collections;
using UnityEngine;
using TMPro;

// ============================================================
// effetGlitchTexte.cs
// ------------------------------------------------------------
// Effet "glitch" pour textes TMP, pilote par l'option
// d'accessibilite (toggle "effet glitch du texte").
//
// CONTEXTE : l'option existait dans le menu mais l'effet, lui,
// n'existait NULLE PART (aucun composant *Glitch* dans le projet,
// aucun texte tague texte_glitch) — le toggle ne faisait donc rien.
// gestionOptionsAccessibilite pose maintenant ce composant sur les
// textes du jeu et bascule son 'enabled'.
//
// FONCTIONNEMENT : a intervalle aleatoire, une courte rafale fait
// trembler une partie des lettres. Manipulation de VERTEX uniquement
// (jamais le CONTENU du texte) : aucun risque pour les textes qui
// changent au runtime (minuteur, compteurs) ni pour le rich text.
// Temps non-scale : l'effet vit aussi dans les menus en pause.
// ============================================================

public class effetGlitchTexte : MonoBehaviour
{
    // Constantes en code (convention projet : pas de valeurs par
    // defaut dans les champs serialises).
    private const float INTERVALLE_MIN = 4f;
    private const float INTERVALLE_MAX = 12f;
    private const float DUREE_MIN = 0.15f;
    private const float DUREE_MAX = 0.35f;
    private const float AMPLITUDE = 2.5f;
    private const float PROPORTION_LETTRES = 0.35f;
    private const float PAS_RAFALE = 0.04f;

    private TMP_Text texte;
    private float prochainGlitch;
    private Coroutine rafale;

    void Awake()
    {
        texte = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        Replanifier();
    }

    void OnDisable()
    {
        if (rafale != null)
        {
            StopCoroutine(rafale);
            rafale = null;
        }
        // Remet le mesh propre (lettres a leur place).
        if (texte != null && texte.isActiveAndEnabled)
            texte.ForceMeshUpdate();
    }

    void Update()
    {
        if (texte == null) return;
        if (rafale != null) return;
        if (Time.unscaledTime < prochainGlitch) return;
        if (!texte.isActiveAndEnabled)
        {
            Replanifier();
            return;
        }
        rafale = StartCoroutine(Rafale());
    }

    private void Replanifier()
    {
        prochainGlitch = Time.unscaledTime
            + Random.Range(INTERVALLE_MIN, INTERVALLE_MAX);
    }

    private IEnumerator Rafale()
    {
        float fin = Time.unscaledTime
            + Random.Range(DUREE_MIN, DUREE_MAX);

        while (Time.unscaledTime < fin && texte.isActiveAndEnabled)
        {
            Trembler();
            yield return new WaitForSecondsRealtime(PAS_RAFALE);
        }

        if (texte.isActiveAndEnabled)
            texte.ForceMeshUpdate();

        Replanifier();
        rafale = null;
    }

    private void Trembler()
    {
        texte.ForceMeshUpdate();
        TMP_TextInfo info = texte.textInfo;
        if (info == null || info.characterCount == 0) return;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ci = info.characterInfo[i];
            if (!ci.isVisible) continue;
            if (Random.value > PROPORTION_LETTRES) continue;

            int mi = ci.materialReferenceIndex;
            int vi = ci.vertexIndex;
            if (mi < 0 || mi >= info.meshInfo.Length) continue;

            Vector3 decalage = new Vector3(
                Random.Range(-AMPLITUDE, AMPLITUDE),
                Random.Range(-AMPLITUDE, AMPLITUDE),
                0f);

            Vector3[] verts = info.meshInfo[mi].vertices;
            if (vi + 3 >= verts.Length) continue;

            verts[vi + 0] += decalage;
            verts[vi + 1] += decalage;
            verts[vi + 2] += decalage;
            verts[vi + 3] += decalage;
        }

        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            if (info.meshInfo[m].mesh == null) continue;
            info.meshInfo[m].mesh.vertices = info.meshInfo[m].vertices;
            texte.UpdateGeometry(info.meshInfo[m].mesh, m);
        }
    }
}
