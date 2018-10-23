using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[System.Serializable]
public class BuildableObject : MonoBehaviour {

    public float positionX;
    public float positionZ;
    public float objectHeight;

    // Object position in world space
    public Vector3 objectWorldPosition;
    public Quaternion objectRotation;
    public Vector2 objectTileCoordinates;

    // Object Attributes, set in inspector
    public float sizeX;
    public float sizeZ;
    public float sizeVertical;
    public float objectPrice;
    public float offsetX = 0f;
    public float offsetZ = 0f;
    public string objectID;
    public string objectName; // Name of object in UI

    // If tile is quarter tile sized, this is the corner it is on
    public string objectVertex;

    // Constructor for buildings and scenery
    public BuildableObject(float positionX, float positionZ, float objectHeight, float sizeX, float sizeZ, float sizeVertical, float objectPrice, string objectID, string objectName, string objectVertex = null)
    {
        this.positionX = positionX;
        this.positionZ = positionZ;
        this.objectHeight = objectHeight;
        this.objectPrice = objectPrice;
        this.objectID = objectID;
    }

    // Construction for fences and paths
    public BuildableObject(float positionX, float positionZ, float objectHeight, float sizeVertical, float objectPrice, string objectID, string objectName)
    {
        this.positionX = positionX;
        this.positionZ = positionZ;
        this.objectHeight = objectHeight;
        this.objectPrice = objectPrice;
        this.objectID = objectID;

    }

    private void Awake()
    {
        // Set bounding box size to 95% to allow side by side construction (100% means touching = intersect) IF NOT PATH
        BoxCollider col = gameObject.GetComponent<BoxCollider>();
        col.size = new Vector3(.95f, .99f, .95f);

    }

    public void RecalculatePosition(float vertX = 0, float vertZ = 0)
    {
        positionX = gameObject.transform.position.x - vertX;
        objectHeight = gameObject.transform.position.y - (sizeVertical/2);
        positionZ = gameObject.transform.position.z - vertZ;
        objectWorldPosition = gameObject.transform.position;
        objectRotation = gameObject.transform.rotation;
    }

}
