using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path : BuildableObject
{

    public string pathType; // Type of path as int for us in choosing proper visual
    /// <summary>
    /// Direction of the top of the sloped path.
    /// </summary>
    [SerializeField]    
    public string slopeTopDirection = "no slope";
    [SerializeField]
    bool isSlope = false;
    [SerializeField]
    bool isTunnel = false;
    [SerializeField]
    bool isElevated = false;


    public Path(float positionX, float positionZ, float objectHeight, float sizeVertical, float objectPrice, string objectID, string objectName, string pathType) : base(positionX, positionZ, objectHeight, sizeVertical, objectPrice, objectID, objectName)
    {
        this.positionX = positionX;
        this.positionZ = positionZ;
        this.objectHeight = objectHeight;
        this.objectPrice = objectPrice;
        this.pathType = pathType;
        this.objectID = objectID;
    }

    public bool IsSlope
    {
        get { return isSlope; }
        set { isSlope = value; }
    }
    public bool IsTunnel
    {
        get { return isTunnel; }
        set { isTunnel = value; }
    }
    public bool IsElevated
    {
        get { return isElevated; }
        set { isElevated = value; }
    }
    /// <summary>
    /// Checks if the path is a slope, and if it is elevated or a tunnel, based on its height relative to the provided tile.
    /// </summary>
    /// <param name="tile"></param>
    public void UpdatePathHeight(Tile tile)
    {
        float tileHeight = tile.tileHeight;
        if (!isSlope)
        {
            if (!tile.isSlope)
            {
                if (tileHeight < objectHeight)
                {
                    isElevated = true;
                    isTunnel = false;
                }
                if (tileHeight == objectHeight)
                {
                    isElevated = false;
                    isTunnel = false;

                }
                if (tileHeight > objectHeight)
                {
                    isElevated = false;
                    isTunnel = true;
                }
            }
            else if (tile.isSlope)
            {
                if ((tileHeight -.25f) < objectHeight)
                {
                    isElevated = true;
                    isTunnel = false;
                }
                if ((tileHeight - .25f) == objectHeight)
                {
                    isElevated = false;
                    isTunnel = false;

                }
                if ((tileHeight - .25f) > objectHeight)
                {
                    isElevated = false;
                    isTunnel = true;
                }
            }
        }
        else if (isSlope)
        {
            if (tileHeight < objectHeight - .25f)
            {
                isElevated = true;
                isTunnel = false;
            }
            if (tileHeight == objectHeight - .25f)
            {
                isElevated = false;
                isTunnel = false;

            }
            if (tileHeight > objectHeight - .25f)
            {
                isElevated = false;
                isTunnel = true;
            }
        }
    }



}
