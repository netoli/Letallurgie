using UnityEngine;

// ============================================================
// balance_apparence.cs
// ------------------------------------------------------------
// Auteur original : coéquipier
// Modifié         : Fanny Fortier — intégration controleurBalance
// ------------------------------------------------------------
// Description :
//   Pilote visuellement les trois parties de la balance
//   (bras central, plateau gauche, plateau droit) en fonction
//   d'un float_de_rotation allant de -1.0 (penché gauche)
//   à 1.0 (penché droit), avec lerp smooth vers la cible.
//
//   Appeler DefinirRotation(float) depuis controleurBalance
//   pour mettre à jour l'état visuel.
// ============================================================

public class balance_apparence : MonoBehaviour
{
    // ===================== INSPECTEUR =====================

    [Tooltip("Vitesse de transition smooth vers la nouvelle inclinaison. " +
             "Valeur plus haute = transition plus rapide.")]
    [SerializeField] private float _vitesseLerp = 3f;

    public GameObject gauche_hold;
    public GameObject droite_hold;
    public GameObject balanceur;

    // ===================== ÉTAT INTERNE =====================

    // Va de -1.0 à 1.0 : -1.0 = complètement penché gauche,
    // 1.0 = complètement penché droite. Suivi en temps réel par Update.
    private float _float_de_rotation = 0f;

    // Valeur cible vers laquelle _float_de_rotation lerpe.
    // Mise à jour par DefinirRotation().
    private float _cibleRotation = 0f;

    // ===================== MÉTHODE PUBLIQUE =====================

    /// <summary>
    /// Définit l'inclinaison cible de la balance.
    /// Appelé par controleurBalance à chaque changement de poids.
    /// </summary>
    /// <param name="valeur">Valeur entre -1.0 (gauche) et 1.0 (droite).</param>
    public void DefinirRotation(float valeur)
    {
        _cibleRotation = Mathf.Clamp(valeur, -1f, 1f);
    }

    /// <summary>
    /// Retourne l'inclinaison visuelle actuelle du balanceur en degrés,
    /// par rapport à l'équilibre (0° = horizontal). Positif = penche droite.
    /// Utile pour les logs de debug : la valeur lerpée change en temps réel.
    /// </summary>
    public float ObtenirAngleVisuel()
    {
        return _float_de_rotation * 23.5f;
    }

    /// <summary>
    /// Retourne l'inclinaison cible en degrés (sans le lerp).
    /// Correspond directement au ratio de poids calculé par controleurBalance.
    /// </summary>
    public float ObtenirAngleCible()
    {
        return _cibleRotation * 23.5f;
    }

    // ===================== UNITY =====================

    void Update()
    {
        // Lerp smooth vers la cible
        _float_de_rotation = Mathf.Lerp(
            _float_de_rotation, _cibleRotation,
            Time.deltaTime * _vitesseLerp);

        // Appliquer aux transforms
        balanceur.transform.rotation = Quaternion.Euler(
            -90f + (_float_de_rotation * 23.5f), 0, -90f);

        gauche_hold.transform.localPosition = new UnityEngine.Vector3(
            6.04f,
            8.37f + (_float_de_rotation * 1.79f),
            -5.2f + (-_float_de_rotation * 0.6f));

        droite_hold.transform.localPosition = new UnityEngine.Vector3(
            6.04f,
            8.37f + (-_float_de_rotation * 1.79f),
            4.32f + (-_float_de_rotation * 1.04f));
    }
}
