using UnityEngine;

public class TestJournal : MonoBehaviour
{
    public Sprite iconeTest1;
    public Sprite iconeTest2;

    private void Start()
    {
        JournalManager.Instance.AjouterEntreeJournal(iconeTest1, 
                                                    "Clé", 
                                                    "Une vieille clé en laiton... En forme de menottes? Elle ne correspond à aucune serrure de la taverne. ",
                                                    "Les motifs de cette clé impliquent que c'est pour un gros cadenas... Comme dans les prisons? Mais il n'y a aucune prison au village...");
        JournalManager.Instance.AjouterEntreeJournal(iconeTest2, 
                                                    "Note déchirée",
                                                    "Un morceau de papier froissé, déchiré sur un côté. L'écriture est tremblante. On peut y lire \"ne sais pas où ils m'emmènent. Ça fait des semaines qu'on entend parler de disparitions. Je pensais que c'était vrai qu...\"",
                                                    "Quelqu'un a écrit ça en panique avant de disparaître... Ce n'est pas l'écriture du tavernier... Il n'est pas le premier et, si je n'agis pas, pas le dernier. ");
    }
}