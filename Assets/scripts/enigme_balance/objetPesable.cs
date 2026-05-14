using UnityEngine;

public class objetPesable : MonoBehaviour
{

    [Header("Poids")]
    [SerializeField] private int _valeurPoids = 1;

    public int valeurPoids => _valeurPoids;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ModifierPoids(float multiplicateur)
    {
        _valeurPoids = Mathf.Max(1, Mathf.RoundToInt(_valeurPoids * multiplicateur));
        Debug.Log($"[ObjetPesable] {gameObject.name} - nouveau poids : {_valeurPoids}");
    }
}
