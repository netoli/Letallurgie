using UnityEngine;

// ============================================================
// copieLensCameraRendu.cs
// ------------------------------------------------------------
// Copie la lens (FOV, plans de clip) d'une camera source vers la
// camera de ce GameObject, apres le CinemachineBrain.
//
// POURQUOI : dans les scenes, le CinemachineBrain est pose sur
// camera_capture (le PARENT), mais c'est camera_principale
// (l'ENFANT, tag MainCamera) qui rend l'ecran. La POSE suit par
// parentage; la LENS, elle, n'etait appliquee par le Brain qu'a
// camera_capture -> cadrage du menu world-space decale et slider
// "champ de vision" sans effet visible. Ce composant fait suivre
// la lens a la camera qui rend reellement.
//
// Pose automatiquement par appliqueOptionsAuChargement.
// ============================================================

[DefaultExecutionOrder(1000)] // apres le CinemachineBrain
public class copieLensCameraRendu : MonoBehaviour
{
    [Tooltip("Camera dont on copie la lens (celle qui porte le " +
        "CinemachineBrain, ex. camera_capture).")]
    public Camera source;

    private Camera cible;
    private Unity.Cinemachine.CinemachineBrain brainLocal;

    void Awake()
    {
        cible = GetComponent<Camera>();
        brainLocal = GetComponent<Unity.Cinemachine.CinemachineBrain>();
    }

    void LateUpdate()
    {
        Copier();
    }

    public void Copier()
    {
        // Si le CinemachineBrain est (ou a ete deplace) sur CETTE
        // camera, il pilote deja sa lens : on ne copie rien (sinon on
        // ecraserait son FOV avec celui d'une camera figee). Le script
        // devient alors un no-op inoffensif.
        if (brainLocal == null)
            brainLocal = GetComponent<Unity.Cinemachine.CinemachineBrain>();
        if (brainLocal != null) return;

        if (source == null || cible == null) return;

        if (!Mathf.Approximately(cible.fieldOfView,
                source.fieldOfView))
            cible.fieldOfView = source.fieldOfView;

        if (!Mathf.Approximately(cible.nearClipPlane,
                source.nearClipPlane))
            cible.nearClipPlane = source.nearClipPlane;

        if (!Mathf.Approximately(cible.farClipPlane,
                source.farClipPlane))
            cible.farClipPlane = source.farClipPlane;
    }
}
