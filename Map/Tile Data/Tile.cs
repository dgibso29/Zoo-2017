using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tile {

    public Vector3 upperLeft;           // NW Corner
    public Vector3 upperRight;          // NE Corner
    public Vector3 lowerRight;          // SE Corner
    public Vector3 lowerLeft;           // SW Corner    
    public Vector3 bottomUpperLeft;     // NW Bottom corner
    public Vector3 bottomUpperRight;    // NE Bottom corner
    public Vector3 bottomLowerRight;    // SE Bottom corner
    public Vector3 bottomLowerLeft;     // SW Bottom corner

    public float bottomHeight;
    public float height;
    public bool isSlope = false;
    /// <summary>
    /// ID of enclosure tile is in, if any.
    /// </summary>
    public int enclosureID = 0;

    [SerializeField]
    private float tileSize = 1f; //tile size in meters
    [SerializeField]
    private bool isOwnedByZoo = false;
    [SerializeField]
    private bool hasBeenUpdated = false;
    [SerializeField]
    private float centerX;
    [SerializeField]
    private float centerZ;
    [SerializeField]
    private int type;
    [SerializeField]
    private int cliff;
    [SerializeField]
    private int selectionType; // array index for selectionbox tiles

    private List<BuildableObject> tileObjects = new List<BuildableObject>(); // Array of objects on the tile -- used to track object location, save/load, everything

    // Tile Terrain and Cliff textures are always ordered such that Grass = 0, Grass Cliff = 1, Sand = 2, Sand Cliff = 3, so on.

    public Tile(float centerX, float centerZ, float height, float size, int type, int selectionType, float bottomHeight, int cliff)
    {
        this.height = height;
        this.bottomHeight = bottomHeight;
        this.centerX = centerX;
        this.centerZ = centerZ;
        this.type = type;
        this.cliff = cliff;
        this.selectionType = selectionType;
        //make sure we're set to the right size
        tileSize = size;
        float halfSize = size / 2f;

        //setup the vectors! 
        upperLeft = new Vector3((centerX * tileSize), height, (centerZ * tileSize) + tileSize);              // NW corner
        upperRight = new Vector3((centerX * tileSize) + tileSize, height, (centerZ * tileSize) + tileSize);  // NE corner
        lowerRight = new Vector3((centerX * tileSize) + tileSize, height, (centerZ * tileSize));             // SE corner
        lowerLeft = new Vector3((centerX * tileSize), height, (centerZ * tileSize));                         // SW corner
                                                                                                             // Set up the bottom vectors
        bottomUpperLeft = new Vector3((centerX * tileSize), bottomHeight, (centerZ * tileSize) + tileSize);              // NW bottom corner
        bottomUpperRight = new Vector3((centerX * tileSize) + tileSize, bottomHeight, (centerZ * tileSize) + tileSize);  // NE bottom corner
        bottomLowerRight = new Vector3((centerX * tileSize) + tileSize, bottomHeight, (centerZ * tileSize));             // SE bottom corner
        bottomLowerLeft = new Vector3((centerX * tileSize), bottomHeight, (centerZ * tileSize));                         // SW bottom corner

        
    }

    public float tileCoordX
    {
        get { return centerX; }
    }

    public float tileCoordZ
    {
        get { return centerZ; }
    }

    public float tileHeight
    {
        get { return height; }
        set { height = value; }
    }

    public int tileType
    {
        get { return type; }
        set { type = value; }
    }

    public int cliffType
    {
        get { return cliff; }
        set { cliff = value; }
    }
    public int selectionTileType
    {
        get { return selectionType; }
        set { selectionType = value; }
    }
    public bool HasBeenUpdated
    {
        get { return hasBeenUpdated; }
        set { hasBeenUpdated = value; }
    }

    public List<BuildableObject> Objects
    {
        get { return tileObjects; }
        set { tileObjects = value; }
    }

    public void AddObjectToTile(BuildableObject newObject)
    {
        tileObjects.Add(newObject);
    }

    public bool IsOwnedByZoo
    {
        get { return isOwnedByZoo; }
        set { isOwnedByZoo = value; }
    }

    /// <summary>
    /// Check if this tile has a path at the height of the provided path, including sloped paths.
    /// </summary>
    /// <param name="pathToCheck"></param>
    /// <returns></returns>
    public bool CheckForPath(Path pathToCheck, float heightScale)
    {
        foreach (BuildableObject p in Objects)
        {
            if (p.GetComponent<Path>() != null)
                if (p.objectHeight == pathToCheck.objectHeight && !p.GetComponent<Path>().IsSlope)
                {
                    return true;
                }
                else if (p.GetComponent<Path>().IsSlope && (p.objectHeight - (heightScale / 2) == pathToCheck.objectHeight || p.objectHeight + (heightScale / 2) == pathToCheck.objectHeight))
                {
                    return true;
                }
                else
                {
                    return false;
                }
        }
        return false;
    }
    /// <summary>
    /// Get the path object at the same height of the provided path, including if sloped.
    /// </summary>
    /// <param name="pathToCheck"></param>
    /// <param name="heightScale"></param>
    /// <returns></returns>
    public Path GetPath(Path pathToCheck, float heightScale)
    {
        foreach (BuildableObject p in Objects)
        {
            if (p.GetComponent<Path>() != null)
                if (p.objectHeight == pathToCheck.objectHeight && !p.GetComponent<Path>().IsSlope)
            {
                return p.GetComponent<Path>();
            }
            else if (p.GetComponent<Path>().IsSlope && (p.objectHeight - (heightScale / 2) == pathToCheck.objectHeight || p.objectHeight + (heightScale / 2) == pathToCheck.objectHeight))
            {
                return p.GetComponent<Path>();
            }
            else
            {
                return null;
            }
        }
        return null;
    }
    public Path GetPathAtHeight(float height)
    {
        foreach (BuildableObject p in Objects)
        {
            if (p.GetComponent<Path>() != null)
                if (p.objectHeight == height)
                {
                    return p.GetComponent<Path>();
                }
        }
        return null;
    }

    public void recalculate()
    {
        //reset our vectors
        upperLeft = new Vector3((centerX * tileSize), height, (centerZ * tileSize) + tileSize);
        upperRight = new Vector3((centerX * tileSize) + tileSize, height, (centerZ * tileSize) + tileSize);
        lowerRight = new Vector3((centerX * tileSize) + tileSize, height, (centerZ * tileSize));
        lowerLeft = new Vector3((centerX * tileSize), height, (centerZ * tileSize));
    }

    public void ReSetStats()
    {
        height = Mathf.Max(upperRight.y, upperLeft.y, lowerLeft.y, lowerRight.y);

        if ((upperLeft.y + upperRight.y + lowerLeft.y + lowerRight.y) / 4f != height)
        {
            isSlope = true;
        }
        else
        {
            isSlope = false;
        }
    }
}
