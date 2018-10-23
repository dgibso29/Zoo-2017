using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scenery : BuildableObject
{

    public bool isTree = false;
    public bool isWall = false;

    public Scenery(float positionX, float positionZ, float objectHeight, float sizeX, float sizeZ, float sizeVertical, float objectPrice, string objectID, string objectName) : base(positionX, positionZ, objectHeight, sizeX, sizeZ, sizeVertical, objectPrice, objectID, objectName)
    {
        this.positionX = positionX;
        this.positionZ = positionZ;
        this.objectHeight = objectHeight;
        this.objectPrice = objectPrice;
        this.objectID = objectID;
    }
    
}
