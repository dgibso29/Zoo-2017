using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class SavedGame : ScriptableObject {

    public string saveName;
    public string saveGameVersion;
    public string gameVersion;
    public string gameVersionName;
    public string zooName;
    public long realDate;
    public string inGameDateFormatted;
    public long inGameDateRaw;
    public float timePlayed;
    public MapDataJSON mapDataJSON;
    public int worldSize;
    public float currentFunds;
    public int totalGuestsSpawned;
    public int totalAnimalSpawned;
    public int totalEnclosuresBuilt;
    public List<string> buildingsBuiltJSON = new List<string>();
    public List<string> sceneryBuiltJSON = new List<string>();
    public List<string> pathsBuiltJSON = new List<string>();
    public List<string> fencesBuiltJSON = new List<string>();
    public List<string> enclosuresJSON = new List<string>();
    public List<string> guestsJSON = new List<string>();
    public List<string> animalsJSON = new List<string>();

    public void init(string saveName, string saveGameVersion, string gameVersion, string gameVersionName, string inGameDateFormatted, DateTime inGameDateRaw, float timePlayed, int worldSize, float currentFunds, List<BuildableObject> objectsBuilt, string zooName, List<Enclosure> enclosures, List<Guest> guests, List<Animal> animals, int totalGuestsSpawned, int totalAnimalsSpawned, int totalEnclosuresBuilt)
    {
        this.saveName = saveName;
        this.saveGameVersion = saveGameVersion;
        this.gameVersion = gameVersion;
        this.gameVersionName = gameVersionName;
        this.zooName = zooName;
        realDate = DateTime.Now.ToBinary();
        this.inGameDateFormatted = inGameDateFormatted;
        this.inGameDateRaw = inGameDateRaw.ToBinary();
        this.timePlayed = timePlayed;
        this.worldSize = worldSize;
        this.currentFunds = currentFunds;
        SaveEnclosuresToJSON(enclosures);
        SaveObjectsBuiltToJSON(objectsBuilt);
        SaveGuestsToJSON(guests);
        this.totalGuestsSpawned = totalGuestsSpawned;
    }

    public void SaveObjectsBuiltToJSON(List<BuildableObject> objectsBuilt)
    {
        foreach (BuildableObject obj in objectsBuilt)
        {
            if (obj.GetComponent<Building>() == true)
            {
                buildingsBuiltJSON.Add(JsonUtility.ToJson(obj));
            }
            else if (obj.GetComponent<Path>() == true)
            {
                pathsBuiltJSON.Add(JsonUtility.ToJson(obj));
            }
            else if (obj.GetComponent<Scenery>() == true)
            {
                sceneryBuiltJSON.Add(JsonUtility.ToJson(obj));
            }
            else if (obj.GetComponent<Fence>() == true)
            {
                fencesBuiltJSON.Add(JsonUtility.ToJson(obj));
            }
        }
    }

    public void SaveEnclosuresToJSON(List<Enclosure> enclosures)
    {
        foreach(Enclosure e in enclosures)
        {
            enclosuresJSON.Add(JsonUtility.ToJson(e));
        }
    }

    public void SaveGuestsToJSON(List<Guest> guests)
    {
        foreach(Guest guest in guests)
        {
            guestsJSON.Add(JsonUtility.ToJson(guest.data));
        }
    }

    public void SaveAnimalsToJSON(List<Animal> animals)
    {
        foreach(Animal animal in animals)
        {
            animalsJSON.Add(JsonUtility.ToJson(animal.data));
        }
    }
    
    [Serializable]
    public struct MapDataJSON
    {
        [SerializeField]
        public Tile[] mapData;
    }


}
