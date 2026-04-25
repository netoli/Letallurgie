using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class gestionSelectionInventaire : MonoBehaviour
{
    public static gestionSelectionInventaire Instance;

    public event Action<objetInventaire> onSelectionChangee;

    private objetInventaire objetSelectionne;

    void Awake()
    {
        Instance = this;
    }

    public void Selectionner(objetInventaire objet)
    {
        if (objetSelectionne == objet)
        {
            Deselectionner();
            return;
        }
        objetSelectionne = objet;
        onSelectionChangee?.Invoke(objetSelectionne);
    }

    public void Deselectionner()
    {
        objetSelectionne = null;
        onSelectionChangee?.Invoke(null);
    }

    public objetInventaire ObtenirSelection()
    {
        return objetSelectionne;
    }

    public bool AQuelqueChoseDeSelectionne()
    {
        return objetSelectionne != null;
    }

    void Update()
    {
        if (Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame
            && AQuelqueChoseDeSelectionne())
        {
            Deselectionner();
        }
    }
}