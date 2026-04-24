using UnityEngine;
using UnityEngine.InputSystem;

public class testPointeur : MonoBehaviour
{
    public gestionPointeur pointeur;

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            pointeur.ChangerEtat(gestionPointeur.EtatPointeur.Defaut);
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            pointeur.ChangerEtat(gestionPointeur.EtatPointeur.Interactif);
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            pointeur.ChangerEtat(gestionPointeur.EtatPointeur.PNJ);
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            pointeur.ChangerEtat(gestionPointeur.EtatPointeur.Mecanique);
    }
}