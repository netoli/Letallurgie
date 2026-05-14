using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class slotObjetInventaire : MonoBehaviour
{
    [Header("References")]
    public Image iconeObjet;
    public TMP_Text texteNombreObjets;

    // Référence optionnelle à la zone de Drop pour l'énigme balance
    private static ZoneDepotJoueur _zoneBalance;

    private objetInventaire objetAssocie;

    public static void AssignerZoneBalance(ZoneDepotJoueur zone)
    {
        _zoneBalance = zone;
    }

    public void Configurer(objetInventaire objet, int quantite)
    {

        Button btn = GetComponent<Button>();
        Debug.Log($"[Slot] btn={btn}, _zoneBalance={_zoneBalance}, " +
                  $"scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        if (btn != null && _zoneBalance != null)
            btn.onClick.AddListener(UtiliserSurBalance);

        objetAssocie = objet;

        if (iconeObjet != null)
        {
            iconeObjet.sprite = objet.icone;

            Debug.Log($"Configurer: objet={objet.nomObjet} sprite={(objet.icone != null ? objet.icone.name : "NULL")}");

            // On NE force PLUS SetNativeSize() : sinon l'Image prend
            // la resolution pixel du sprite (ex: 512x512) au lieu de
            // la taille definie dans le prefab du slot. Pour garder
            // les proportions de l'icone, coche "Preserve Aspect"
            // sur le composant Image du prefab slot dans l'Inspector.

            MettreAJourQuantite(quantite);
        }
        else
        {
            Debug.LogWarning("Configurer: iconeObjet est null");
        }

        // Ajouter le listener de clic sur le bouton si on est dans la scène manoir
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SCENE4-Manoir")
        {
            if (btn != null && _zoneBalance != null)
                btn.onClick.AddListener(UtiliserSurBalance);
        }
    }

        // Appelé quand le joueur clique sur le slot en mode balance
    private void UtiliserSurBalance()
    {
        if (objetAssocie == null || _zoneBalance == null) return;
        if (objetAssocie.prefab3D == null)
        {
            Debug.LogWarning($"[Slot] {objetAssocie.nomObjet} n'a pas de prefab3D!");
            return;
        }

        // Instancier l'objet 3D sur la balance
        GameObject instance = Instantiate(
            objetAssocie.prefab3D,
            _zoneBalance.transform.position,
            Quaternion.identity);

        objetPesable composant = instance.GetComponent<objetPesable>();
        if (composant != null)
        {
            _zoneBalance.AjouterObjet(composant);
            gestionInventaire.Instance.RetirerObjet(objetAssocie);
            Debug.Log($"[Slot] {objetAssocie.nomObjet} envoyé sur la balance");
        }
        else
        {
            Debug.LogError($"[Slot] prefab3D de {objetAssocie.nomObjet} " +
                           $"sans composant objetPesable!");
            Destroy(instance);
        }
    }


    public void MettreAJourQuantite(int quantite)
    {
        if (texteNombreObjets != null)
            texteNombreObjets.text = quantite.ToString();
    }

    public objetInventaire ObtenirObjet()
    {
        return objetAssocie;
    }
}