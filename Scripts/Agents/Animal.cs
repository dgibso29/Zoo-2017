using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animal : MonoBehaviour
{
    
    /// <summary>
    /// Used to identify animal type, life stage, and gender ("a_tigris_sumatrae_adult_M")
    /// </summary>
    public string animalID;
    /// <summary>
    /// Common name of species ("Sumatran Tiger")
    /// </summary>
    public string speciesName;
    /// <summary>
    /// Scientific name of species ("Panthera tigris sumatrae")
    /// </summary>
    public string scientificName;
    /// <summary>
    /// Animal's shown name ("Sumatran Tiger 1")
    /// </summary>
    string animalName;

    /// <summary>
    /// Enclosure this animal is in
    /// </summary>
    public Enclosure enclosure;

    public int enclosureID;
    
    public AnimalData data;

    public Vector3 position;

    bool isWalking = false;
    bool walkingToPath = false;

    // Use this for initialization
    void Awake ()
    {

        data = new AnimalData(animalID, speciesName, scientificName, animalName);
        GetEnclosure();
	}
	
	// Update is called once per frame
	void Update ()
    {
        position = transform.position;

        if (!isWalking)
        {
            StartCoroutine(Walk(FindRandomTargetTile()));
        }
    }
    /// <summary>
    /// Walk to target tile
    /// </summary>
    /// <param name="targetTile"></param>
    /// <returns></returns>
    IEnumerator Walk(Tile targetTile)
    {
        isWalking = true;
        List<Pathfinding.Node> pathToTarget = Pathfinding.FindAnimalPath(FindCurrentTile(), FindRandomTargetTile(), enclosure);

        yield break;
    }

    Tile FindRandomTargetTile()
    {
        int numberOfTiles = enclosure.EnclosureTiles.Count + 1;
        int randomNumber = Random.Range(0, numberOfTiles);
        return enclosure.EnclosureTiles[randomNumber];
    }

    void GetEnclosure()
    {
        enclosureID = World.GetMapDataStatic[(int)position.x, (int)position.z].enclosureID;
        enclosure = ZooInfo.enclosures.Find(exhibit => exhibit.enclosureID == enclosureID);
    }

    Tile FindCurrentTile()
    {
        return World.GetMapDataStatic[(int)transform.position.x, (int)transform.position.z];
    }

}
