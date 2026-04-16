using System;
using System.Security.Cryptography;
using UnityEngine;

public class animation_heureux_pnj : MonoBehaviour
{
    public Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        animator.SetInteger("randi_chance", 1);
    }

    // Update is called once per frame
    void Update()
    {

    }
    void change_animation()
    {
        if (animator.GetInteger("randi_chance") != 0)
        {
            animator.SetInteger("randi_chance", 0);
        }
        else
        {
            animator.SetInteger("randi_chance", UnityEngine.Random.Range(0, 3));
        }
    }

}
