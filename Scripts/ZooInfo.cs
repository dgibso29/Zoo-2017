using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZooInfo : MonoBehaviour {

    public TileMapMouseInterface mouseInterface;
    public World world;
    public EconomyTools economy;
    public GameObject uiCanvas;
    /// <summary>
    /// List of all enclosures in the Zoo.
    /// </summary>
    public static List<Enclosure> enclosures;
    /// <summary>
    /// List of all guests currently in the Zoo.
    /// </summary>
    public static List<Guest> guestsInZoo;
    /// <summary>
    /// List of all guests currently in the Zoo
    /// </summary>
    public static List<Animal> animalsInZoo;
    /// <summary>
    /// Total number of guests that have visited the zoo.
    /// </summary>
    public static int totalGuestsSpawned = 1;
    /// <summary>
    /// Determines ID # of next new guest.
    /// </summary>
    static int guestIDTracker = 1;
    /// <summary>
    /// Total number of animals that have been in the zoo.
    /// </summary>
    public static int totalAnimalsSpawned = 1;
    /// <summary>
    /// Determines the Unique ID # of the next new animal.
    /// </summary>
    static int animalUniqueIDTracker = 1;
    /// <summary>
    /// Total enclosures built in the zoo. Used to determine unique enclosure IDs
    /// </summary>
    public static int totalEnclosuresBuilt = 1;

    public float guestSpawnRate = .5f;

    private bool purchasingLand;
    private string zooName = "Tahendo Zoo";
    private float costOfLand = 50f;
    bool spawningGuests = false;


    // Use this for initialization
    void Start ()
    {
        enclosures = new List<Enclosure>();
        guestsInZoo = new List<Guest>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (purchasingLand)
        {
            if (Input.GetMouseButtonDown(0) && mouseInterface.GetCurrentTile != null)
            {
                mouseInterface.StartCoroutine(mouseInterface.DragBox(mouseInterface.GetCurrentTile, "purchaseLand"));
            }
            if (Input.GetMouseButtonDown(1))
            {
                purchasingLand = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            spawningGuests = true;
            StartCoroutine(GuestSpawner());
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            spawningGuests = false;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Number of Guests: " + guestsInZoo.Count);
        }

    }

    IEnumerator GuestSpawner()
    {
        while (spawningGuests)
        {
            Tile test = world.MapData[0, 0];
            Path testPath = test.GetPathAtHeight(0f);
            Vector3 newPos = new Vector3(testPath.transform.position.x, .38f, testPath.transform.position.z);

            GameObject guestObj = Instantiate<GameObject>(Resources.Load<GameObject>("Guests/guestTest"), newPos, new Quaternion());
            Color newColor = Random.ColorHSV();
            List<Color> newColors = new List<Color>();
            newColors.Add(newColor);
            guestObj.GetComponentInChildren<Renderer>().material.SetColor("_Color", newColor);
            guestsInZoo.Add(guestObj.GetComponent<Guest>());
            yield return new WaitForSeconds(guestSpawnRate);
        }
        yield break;
    }

    public void SpawnGuest(GuestData data)
    {

        Vector3 newPos = new Vector3(data.position.x, data.position.y, data.position.z);

        GameObject guestObj = Instantiate<GameObject>(Resources.Load<GameObject>("Guests/guestTest"), newPos, new Quaternion());
        Color newColor = Random.ColorHSV();
        List<Color> newColors = new List<Color>();
        newColors.Add(newColor);
        guestObj.GetComponentInChildren<Renderer>().material.SetColor("_Color", newColor);
        Guest guest = guestObj.GetComponent<Guest>();
        //guest.data.SetDataOnLoad(data.GuestID, data.GuestName);
        guest.data = data;
        guestsInZoo.Add(guest);
        //Debug.Log("Spawned " + guest.name + "!");
    }

    public void LoadGuests(List<string> guestsJSON)
    {
        foreach(string guest in guestsJSON)
        {
            SpawnGuest(JsonUtility.FromJson<GuestData>(guest));
        }
    }

    public bool PurchasingLand
    {
        get { return purchasingLand; }
        set { purchasingLand = value; }
    }

    public void StartPurchasingLand()
    {
        if (purchasingLand == false)
        {
            // TODO: Show owned/unowned land overlay
            purchasingLand = true;
        }
        else if (purchasingLand == true)
        {
            // TODO: Hide overlay
            purchasingLand = false;
        }
    }
    public void StopPurchasingLand()
    {
        if (purchasingLand == true)
        {
            // TODO: Hide overlay
            purchasingLand = false;
        }
    }

    public string ZooName
    {
        get { return zooName; }
        set { zooName = value; }
    }

    public void PurchaseLand(Tile[] landToPurchase)
    {
        int tilesToPurchase = 0;
        foreach(Tile t in landToPurchase)
        {
            if (t != null && !t.IsOwnedByZoo)
            {
                tilesToPurchase++;
            }
        }
        if(economy.AttemptPurchase(economy.CalculateCost(costOfLand, tilesToPurchase)))
        {
            foreach (Tile t in landToPurchase)
            {
                if (t != null && !t.IsOwnedByZoo)
                {
                    t.IsOwnedByZoo = true;
                }
            }
        }
    }

    public void ChangeZooName(string newName)
    {
        zooName = newName;
    }

    public bool CheckIfLandOwnedByZoo(Tile tileToBuildOn)
    {
        if (tileToBuildOn.IsOwnedByZoo)
        {
            return true;
        }
        else
        {
            GameObject error = Instantiate(Resources.Load<GameObject>("UI/ErrorMessagePanel"), Input.mousePosition, new Quaternion(), uiCanvas.transform);
            error.GetComponentInChildren<Text>().text = "Cannot build here! Land is not owned by zoo!";
            return false;
        }
    }
    public List<Tile> CheckIfLandOwnedByZoo(List<Tile> tilesToBuildOn)
    {
        bool throwError = false;
        List<Tile> clearToBuild = new List<Tile>();
        foreach(Tile t in tilesToBuildOn)
        {
            if(t != null && t.IsOwnedByZoo)
            {
                clearToBuild.Add(t);
            }
            else if(t != null && !t.IsOwnedByZoo)
            {
                throwError = true;
            }
        }
        if(throwError)
        {
            GameObject error = Instantiate(Resources.Load<GameObject>("UI/ErrorMessagePanel"), Input.mousePosition, new Quaternion(), uiCanvas.transform);
            error.GetComponentInChildren<Text>().text = "Cannot build here! Land is not owned by zoo!";
        }
        return clearToBuild;
    }

}
