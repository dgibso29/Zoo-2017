using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowDelay : MonoBehaviour {

    public float delayTime;

	// Use this for initialization
	void Start () {
        StartCoroutine(DestroyWindow());
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    IEnumerator DestroyWindow()
    {
        yield return new WaitForSeconds(delayTime);
        Destroy(gameObject);
    }

}
