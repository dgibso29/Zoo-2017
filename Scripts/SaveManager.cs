using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{

    public static SaveManager instance = null;

    public string currentGameVersion;
    public string currentGameVersionName;
    private string saveGameVersion; // Used to identify what version a particular game was started on

    private string pathToSaves;

    float totalTimePlayed;
    float timePlayedThisSession;

    SavedGame loadedSave;

    public ZooInfo zooInfo;
    public TimeManager timeManager;
    public EconomyTools economy;
    public ConstructionTools constructionTools;
    public World world;

    // Use this for initialization
    void Awake()
    {
        //Check if instance already exists
        if (instance == null)

            //if not, set instance to this
            instance = this;

        //If instance already exists and it's not this:
        else if (instance != this)

            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);

        DontDestroyOnLoad(this);
        pathToSaves = Application.persistentDataPath + "/Data/Saved Games/";
        Directory.CreateDirectory(pathToSaves);

    }

    // Update is called once per frame
    void Update()
    {

    }



    public void SaveGame(string saveName)
    {
        timePlayedThisSession = Time.time;
        totalTimePlayed = totalTimePlayed += timePlayedThisSession;
        if (saveGameVersion == null)
            saveGameVersion = currentGameVersion;
        SavedGame newSave = ScriptableObject.CreateInstance(typeof(SavedGame)) as SavedGame;
        newSave.init(saveName, saveGameVersion, currentGameVersion, currentGameVersionName, timeManager.GetCurrentDate, timeManager.GetRawDate, totalTimePlayed, world.worldSize, economy.CurrentFunds, ConstructionTools.objectsBuilt, zooInfo.ZooName, ZooInfo.enclosures, ZooInfo.guestsInZoo, ZooInfo.animalsInZoo, ZooInfo.totalGuestsSpawned, ZooInfo.totalAnimalsSpawned, ZooInfo.totalEnclosuresBuilt);
        newSave.mapDataJSON.mapData = SaveMapDataToJSON(world.MapData);
        string gameToSave = JsonUtility.ToJson(newSave);
        File.WriteAllText(pathToSaves + saveName + ".txt", gameToSave);
    }

    public void LoadGameWrapper(string saveToLoad)
    {
        StartCoroutine(LoadGame(saveToLoad));
        //LoadGame(saveToLoad);
    }

    public IEnumerator LoadGame(string saveToLoad)
    {
        string gameToLoad = File.ReadAllText(pathToSaves + saveToLoad + ".txt");
        loadedSave = ScriptableObject.CreateInstance(typeof(SavedGame)) as SavedGame;
        JsonUtility.FromJsonOverwrite(gameToLoad, loadedSave);

        SceneManager.LoadSceneAsync("loadingscreen");
        Scene loadingScreen = SceneManager.GetSceneByName("loadingscreen");
        SceneManager.SetActiveScene(loadingScreen);
        SceneManager.LoadSceneAsync("game");
        yield return new WaitForEndOfFrame();
        Scene newScene = SceneManager.GetSceneByName("game");

        yield return new WaitUntil(() => newScene.isLoaded);
        Debug.Log("Scene Loaded");
        //SceneManager.SetActiveScene(newScene);
        //Resources.UnloadUnusedAssets();
        Invoke("FinishLoadingGame", .5f);

        //yield break;

    }
    /// <summary>
    /// Finishes loading of game AFTER scene transition has taken place, and then sets active scene to the loaded game.
    /// </summary>
    void FinishLoadingGame()
    {
        // Make sure everything is referenced properly
        UpdateReferences();
        if (world == null)
            Debug.Log("FUCKINGFUCK");
        // Now set all the data
        LoadGameData(loadedSave);
        world.GenerateWorld();
        constructionTools.ClearObjectsBuiltOnLoad();
        constructionTools.RebuildObjectsFromSave(loadedSave.fencesBuiltJSON);
        constructionTools.RebuildObjectsFromSave(loadedSave.pathsBuiltJSON);
        constructionTools.RebuildObjectsFromSave(loadedSave.buildingsBuiltJSON);
        constructionTools.RebuildObjectsFromSave(loadedSave.sceneryBuiltJSON);
        LoadEnclosures();
        zooInfo.LoadGuests(loadedSave.guestsJSON);
        ///zooInfo.LoadAnimals(loadedSave.animalsJSON);
        Debug.Log("Generate returned complete");

        Scene newScene = SceneManager.GetSceneByName("game");
        SceneManager.SetActiveScene(newScene);

    }

    public void StartNewGameWrapper(int newWorldSize)
    {
        StartCoroutine(StartNewGame(newWorldSize));
    }

    public IEnumerator StartNewGame(int newWorldSize = 150)
    {
        //SceneManager.LoadSceneAsync("loadingscreen");
        //Scene loadingScreen = SceneManager.GetSceneByName("loadingscreen");
        //SceneManager.SetActiveScene(loadingScreen);
        SceneManager.LoadSceneAsync("game");
        Scene newScene = SceneManager.GetSceneByName("game");
        yield return new WaitUntil(() => newScene.isLoaded);
        yield return new WaitForEndOfFrame();
        // Make sure everything is referenced properly
        UpdateReferences();

        world.worldSize = newWorldSize;
        world.GenerateWorld();

        SceneManager.SetActiveScene(newScene);
        yield break;


    }

    /// <summary>
    /// Save mapData array to JSON by converting the 2D Tile array to a 1D tile array for serialization
    /// </summary>
    /// <param name="mapData"></param>
    /// <returns></returns>
    Tile[] SaveMapDataToJSON(Tile[,] mapData)
    {
        Tile[] mapDataJSON = new Tile[mapData.Length + 1];

        int i = 0;
        for (int x = 0; x < mapData.GetLength(0); x++)
        {
            for (int z = 0; z < mapData.GetLength(1); z++)
            {
                mapDataJSON[i] = mapData[x, z];
                i++;
            }
        }
        Debug.Log(mapDataJSON[0].tileCoordX + "," + mapDataJSON[0].tileCoordZ);
        return mapDataJSON;

    }

    /// <summary>
    /// Load mapData from JSON by converting the saved 1D tile array to a 2d tile array
    /// </summary>
    /// <param name="mapDataJSON"></param>
    /// <returns></returns>
    Tile[,] LoadMapDataFromJSON(Tile[] mapDataJSON)
    {
        if (mapDataJSON == null)
            Debug.Log("FUCK");
        Tile[,] loadedMapData = new Tile[world.worldSize + 1, world.worldSize + 1];
        for (int i = 0; i < mapDataJSON.Length; i++)
        {
            Tile curTile = mapDataJSON[i];
            curTile.Objects = new List<BuildableObject>();
            loadedMapData[(int)curTile.tileCoordX, (int)curTile.tileCoordZ] = curTile;
        }
        loadedMapData[0, 0] = mapDataJSON[0];
        return loadedMapData;
    }



    void LoadGameData(SavedGame loadedSave)
    {
        saveGameVersion = loadedSave.saveGameVersion;
        totalTimePlayed = loadedSave.timePlayed;
        world.worldSize = loadedSave.worldSize;
        zooInfo.ZooName = loadedSave.zooName;
        ZooInfo.totalGuestsSpawned = loadedSave.totalGuestsSpawned;
        ZooInfo.totalAnimalsSpawned = loadedSave.totalAnimalSpawned;
        ZooInfo.totalEnclosuresBuilt = loadedSave.totalEnclosuresBuilt;
        timeManager.SetTimeFromSave(DateTime.FromBinary(loadedSave.inGameDateRaw));
        economy.CurrentFunds = loadedSave.currentFunds;
        //ConstructionTools.objectsBuilt = loadedSave.LoadObjectsBuiltFromJSON();
        world.MapData = LoadMapDataFromJSON(loadedSave.mapDataJSON.mapData);
        Debug.Log("Done loading data");
    }



    void UpdateReferences()
    {
        GameObject gameManager = null;
        GameObject constructionManager = null;
        gameManager = GameObject.FindGameObjectWithTag("GameManager");
        constructionManager = GameObject.FindGameObjectWithTag("ConstructionManager");

        world = GameObject.FindGameObjectWithTag("World").GetComponent<World>();
        zooInfo = gameManager.GetComponent<ZooInfo>();
        timeManager = gameManager.GetComponent<TimeManager>();
        economy = gameManager.GetComponent<EconomyTools>();
        constructionTools = constructionManager.GetComponent<ConstructionTools>();

        Debug.Log("Updated References");

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("World"))
        {
            Debug.Log(obj.name);
        }

    }

    /// <summary>
    /// On loading game, re-reference Tiles & Fences that make up each enclosure in ZooInfo.enclosures.
    /// </summary>
    public void LoadEnclosures()
    {
        ZooInfo.enclosures = new List<Enclosure>();
        foreach (string json in loadedSave.enclosuresJSON)
        {
            Enclosure newEnclosure = JsonUtility.FromJson<Enclosure>(json);
            ZooInfo.enclosures.Add(newEnclosure);
        }
        foreach (Enclosure e in ZooInfo.enclosures)
        {
            List<Tile> newTiles = new List<Tile>();
            List<Fence> newFences = new List<Fence>();          
            int i = 0;
            foreach (Vector2  tileCoords in e.enclosureTileCoords)
            {
                newTiles.Add(world.MapData[(int)tileCoords.x, (int)tileCoords.y]);
                i++;
            }
            e.EnclosureTiles = newTiles;
            foreach (Tile t in e.EnclosureTiles)
            {
                t.enclosureID = e.enclosureID;
                foreach (BuildableObject obj in t.Objects)
                {
                    if (obj.GetComponent<Fence>() != null)
                    {
                        if (obj.GetComponent<Fence>().FenceEnclosure.EnclosureName == e.EnclosureName)
                        {
                            newFences.Add(obj.GetComponent<Fence>());
                            obj.GetComponent<Fence>().FenceEnclosure = e;
                        }
                    }
                }
            }
            e.EnclosureFences = newFences;
            Debug.Log(e.EnclosureName + " fence count = " + e.EnclosureFences.Count);
        }
    }
}
