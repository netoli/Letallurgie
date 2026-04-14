using System.Collections.Generic;
using UnityEngine;

public class inventaire  : MonoBehaviour
{
    public static inventaire  instance; // accessible de partout
    private List<itemData> items = new List<itemData>();

    void Awake()
    {
        instance = this;
    }

    public void AddItem(itemData item)
    {
        items.Add(item);
        Debug.Log("Ramassé : " + item.itemName);
    }
}