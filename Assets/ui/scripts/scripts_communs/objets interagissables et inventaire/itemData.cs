using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "inventaire/Item")]
public class itemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    // Ajoute ce que tu veux plus tard : description, valeur, etc.
}