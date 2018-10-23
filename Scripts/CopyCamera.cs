using UnityEngine;
using System.Collections;

public class CopyCamera : MonoBehaviour
{
    public UnityEngine.GameObject GameCamera;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.position = GameCamera.transform.position;
        gameObject.transform.rotation = GameCamera.transform.rotation;
        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
    }
}