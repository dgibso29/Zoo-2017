using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fence : BuildableObject
{

    public string fenceType;
    [SerializeField]
    string rotationDirection;
    [SerializeField]
    bool isSlope = false;
    /// <summary>
    /// Enclosure that this fence is part of. Used when saving/loading and when destroying the fence.
    /// </summary>
    [SerializeField]
    Enclosure fenceEnclosure;


    public Fence(float positionX, float positionZ, float objectHeight, float sizeVertical, float objectPrice, string objectID, string objectName) : base(positionX, positionZ, objectHeight, sizeVertical, objectPrice, objectID, objectName)
    {
        this.positionX = positionX;
        this.positionZ = positionZ;
        this.objectHeight = objectHeight;
        this.objectPrice = objectPrice;
        this.objectID = objectID;
    }

    private void Awake()
    {
        sizeVertical = GetComponent<Collider>().bounds.size.y;
    }

    public string RotationDirection
    {
        get { return rotationDirection; }
        set { rotationDirection = value; }
    }

    public bool IsSlope
    {
        get { return isSlope; }
        set { isSlope = value; }
    }

    public Enclosure FenceEnclosure
    {
        get { return fenceEnclosure; }
        set { fenceEnclosure = value; }
    }

}
