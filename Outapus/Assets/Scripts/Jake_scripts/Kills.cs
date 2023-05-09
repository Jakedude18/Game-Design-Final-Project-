using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class Kills : MonoBehaviour
{
    public AudioSource deathSound;

    void OnCollisionEnter2D(Collision2D collision)
    {
        //Debug.Log(collision.gameObject);
        if (collision.gameObject.CompareTag("Player"))
        {
            deathSound.Play();
            JupiterGameHandler gameHandler = GameObject.FindWithTag("GameHandler").GetComponent<JupiterGameHandler>();
            gameHandler.DestroyClones();
            gameHandler.InitializeLevel();
        }
    }
}
