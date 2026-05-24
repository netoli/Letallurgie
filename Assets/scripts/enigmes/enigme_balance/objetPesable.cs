using UnityEngine;

public class objetPesable : MonoBehaviour
{
    [Header("Poids")]
    [SerializeField] private int _valeurPoids = 1;

    public int valeurPoids => _valeurPoids;

    /// <summary>
    /// Définit le poids initial. Appelé au dépôt si le composant
    /// a été ajouté dynamiquement (prefab sans objetPesable).
    /// </summary>
    public void DefinirPoids(int poids)
    {
        _valeurPoids = Mathf.Max(1, poids);
    }

    public void ModifierPoids(float multiplicateur)
    {
        _valeurPoids = Mathf.Max(1, Mathf.RoundToInt(_valeurPoids * multiplicateur));
        Debug.Log($"[ObjetPesable] {gameObject.name} - nouveau poids : {_valeurPoids}");
    }
}
