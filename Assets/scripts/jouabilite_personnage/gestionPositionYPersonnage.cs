using UnityEngine;
using System.Collections;


public class gestionPositionYPersonnage : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Animator animator;

    [Header("Hauteurs")]
    public float hauteurAssise;
    public float hauteurDebout;

    [Header("Transition Stand_up")]
    public float dureeTransition;
    public AnimationCurve courbeTransition;

    private bool transitionEnCours;
    private bool standUpDeclenche;

    void Start()
    {
        controller.enabled = false;
        Vector3 pos = transform.position;
        pos.y = hauteurAssise;
        transform.position = pos;
        controller.enabled = true;
    }

    void Update()
    {
        if (!standUpDeclenche && !transitionEnCours)
        {
            AnimatorStateInfo etat = animator.GetCurrentAnimatorStateInfo(0);
            if (etat.IsName("Stand_up"))
            {
                standUpDeclenche = true;
                StartCoroutine(TransitionVersDebout());
            }
        }
    }

    IEnumerator TransitionVersDebout()
    {
        transitionEnCours = true;
        controller.enabled = false;

        float tempsEcoule = 0f;
        float yDepart = transform.position.y;

        while (tempsEcoule < dureeTransition)
        {
            tempsEcoule += Time.deltaTime;
            float t = tempsEcoule / dureeTransition;
            float tCourbe = courbeTransition.Evaluate(t);

            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(yDepart, hauteurDebout, tCourbe);
            transform.position = pos;

            yield return null;
        }

        Vector3 posFinale = transform.position;
        posFinale.y = hauteurDebout;
        transform.position = posFinale;

        controller.enabled = true;
        transitionEnCours = false;
    }
}