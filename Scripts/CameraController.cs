using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    Vector3 lastMousePosition;

    UnityEngine.GameObject cameraTarget;

    //public Vector3 cameraStartingPosition;
    public float cameraPanSpeed;
    public float minCameraZoom;
    public float maxCameraZoom;
    public float zoomSpeed;
    public float mouseDragSensitivity;
    public UnityEngine.GameObject cameraRotationWidget;
    //float currentCameraRotation;

	// Use this for initialization
	void Start () {
        cameraTarget = gameObject;
        //cameraTarget.transform.position = cameraStartingPosition;
	}


    // Update is called once per frame
    void Update () {

        //currentCameraRotation = cameraTarget.transform.rotation.y;

        if (Input.GetButtonDown("Rotate Clockwise"))
        {
            //cameraTarget.transform.Rotate(0, 90f, 0);
            cameraTarget.transform.Rotate(Vector3.up, 90, Space.World);

        }
        if (Input.GetButtonDown("Rotate Counter-Clockwise"))
        {
            //cameraTarget.transform.Rotate(0, -90f, 0);
            cameraTarget.transform.Rotate(Vector3.up, -90, Space.World);
        }

        if (Input.GetButton("Camera Up"))
        {
            cameraTarget.transform.position += cameraRotationWidget.transform.forward.normalized * cameraPanSpeed;
        }
        if (Input.GetButton("Camera Down"))
        {
            cameraTarget.transform.position -= cameraRotationWidget.transform.forward.normalized * cameraPanSpeed;
        }
        if (Input.GetButton("Camera Right"))
        {
            cameraTarget.transform.position += cameraRotationWidget.transform.right.normalized * cameraPanSpeed;

        }
        if (Input.GetButton("Camera Left"))
        {
            cameraTarget.transform.position -= cameraRotationWidget.transform.right.normalized * cameraPanSpeed;
        }

        // -------------------Code for Zooming Out------------
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            //if (Camera.main.fieldOfView <= 125)
            //    Camera.main.fieldOfView += 2;
            if (Camera.main.orthographicSize <= maxCameraZoom)
                Camera.main.orthographicSize += zoomSpeed;

        }
        // ---------------Code for Zooming In------------------------
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            //if (Camera.main.fieldOfView > 2)
            //    Camera.main.fieldOfView -= 2;
            if (Camera.main.orthographicSize >= minCameraZoom)
                Camera.main.orthographicSize -= zoomSpeed;
        }

        if (Input.GetButtonDown("Pan Camera"))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetButton("Pan Camera"))
        {
            Vector3 mousePosChange = Input.mousePosition - lastMousePosition;
            cameraTarget.transform.Translate(-(mousePosChange.x * mouseDragSensitivity), -(mousePosChange.y * mouseDragSensitivity), 0);
            lastMousePosition = Input.mousePosition;
        }

    }
}
