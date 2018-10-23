using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Enclosure {

    // Set public references
    World world;
    ConstructionTools constructionTools;

    [SerializeField]
    private string enclosureName;
    /// <summary>
    /// List of Tiles in this enclosure.
    /// </summary>
    private List<Tile> enclosureTiles;
    /// <summary>
    /// List of Fences surrounding this enclosure.
    /// </summary>
    private List<Fence> enclosureFences;
    
    public List<Vector2> enclosureTileCoords;
    /// <summary>
    /// Unique numerical identifier of the enclosure
    /// </summary>
    public int enclosureID;

    public Enclosure(List<Tile> enclosureTiles, List<Fence> enclosureFences)
    {
        this.enclosureTiles = enclosureTiles;
        this.enclosureFences = enclosureFences;
        GenerateEnclosureID();
        GenerateEnclosureName();
        enclosureTileCoords = new List<Vector2>();
        AssignEnclosureTileCoordinates();
    }

    /// <summary>
    /// Grab the coordinates of each tile in this enclosure and store them for save and load, and also set the tile's enclosure ID to this one.
    /// </summary>
    void AssignEnclosureTileCoordinates()
    {
        foreach (Tile t in enclosureTiles)
        {
            t.enclosureID = enclosureID;
            enclosureTileCoords.Add(new Vector2(t.tileCoordX, t.tileCoordZ));
        }
    }        

    void GenerateEnclosureName()
    {
        int exhibitNumber = ZooInfo.enclosures.Count + 1;
        string testName = "Exhibit " + exhibitNumber;
        bool nameFound = false;
        while (!nameFound)
        {
            if(ZooInfo.enclosures.Find(e => e.EnclosureName == testName) != null)
            {
                exhibitNumber++;
                testName = "Exhibit " + exhibitNumber;
            }
            else
            {
                enclosureName = testName;
                nameFound = true;
                break;
            }
        }
    }

    void GenerateEnclosureID()
    {
        enclosureID = ZooInfo.totalEnclosuresBuilt;
        ZooInfo.totalEnclosuresBuilt++;
    }

    public void RenameEnclosure(string newEnclosureName)
    {
        enclosureName = newEnclosureName;
    }

    public void DestroyEnclosure()
    {
        foreach(Tile t in enclosureTiles)
        {
            t.enclosureID = 0;
        }
        enclosureTiles = null;
        foreach(Fence f in EnclosureFences)
        {
            f.FenceEnclosure = null;
        }
        EnclosureFences = null;
        ZooInfo.enclosures.Remove(this);

    }

    public List<Tile> EnclosureTiles
    {
        get { return enclosureTiles; }
        set { enclosureTiles = value; }
    }

    public List<Fence> EnclosureFences
    {
        get { return enclosureFences; }
        set { enclosureFences = value; }
    }

    public string EnclosureName
    {
        get { return enclosureName; }
    }
}
