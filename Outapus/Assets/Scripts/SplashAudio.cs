using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplashAudio : MonoBehaviour
{
    public AudioSource splash;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)) {
            splash.Play();
        }
    }
}
