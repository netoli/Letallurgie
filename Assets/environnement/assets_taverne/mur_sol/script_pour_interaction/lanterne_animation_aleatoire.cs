using UnityEngine;

public class lanterne_animation_aleatoire : MonoBehaviour
{
    [Tooltip("GameObject de la lanterne contenant l'Animator. Si null, " +
        "essaie automatiquement de prendre l'Animator sur CE GameObject. " +
        "Evite les NullReferenceException si la ref n'est pas assignee.")]
    public GameObject lanterne;

    void Start()
    {
        // Fallback : si lanterne n'est pas assignee, on tente sur ce
        // GameObject (utile pour les setups oubliees apres un merge).
        GameObject cible = lanterne != null ? lanterne : gameObject;

        Animator anim = cible.GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogWarning($"[lanterne_animation_aleatoire] {name} : " +
                "pas d'Animator trouve sur la cible. Animation skippee.");
            return;
        }

        anim.Play("rot_lanterne_anim", 0, Random.Range(0f, 1f));
    }
}
