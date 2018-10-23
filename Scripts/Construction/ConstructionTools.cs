using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ConstructionTools : MonoBehaviour {

    public TileMapMouseInterface mouseInterface;
    public World world;
    public EconomyTools economy;
    public ZooInfo zooInfo;

    Tile currentTile;
    Tile startTile; // Used when raising/lowering building height
    Tile startingFenceTile;

    bool insideCorner = false; // Use to determine how to handle fence corners
    bool modifiedHeight = false;
    public bool building = false;
    float rotation = 0;
    float newHeight = 0;
    float lockedFenceRotation; // Used when clicking and dragging fences to keep first line of fences steady

    float vertX = 0;
    float vertZ = 0;

    List<GameObject> toDestroy;
    GameObject[] buildableObjectsLoaded { get; set; }

    public static Dictionary<string, GameObject> buildableObjects = new Dictionary<string, GameObject>();

    public static List<BuildableObject> objectsBuilt = new List<BuildableObject>();

    GameObject currentBuildObject;
    BuildableObject currentNewObject;
    BuildableObject collidedObject;

    List<Fence> blueprintFences = new List<Fence>();
    List<Tile> clearToBuildThese = new List<Tile>();

    public bool clearancesOn { get; set; }

    public Material blueprintMaterial;
    public Material blockedBlueprintMaterial;

    public Toggle constructionToolsToggle;
    public Image constructionWindow;    

	// Use this for initialization
	void Start ()
    {
        LoadBuildableObjectsDictionary();
        toDestroy = new List<GameObject>(world.worldSize * 2);
        clearancesOn = true;
    }

    public void LoadBuildableObjectsDictionary()
    {
        buildableObjectsLoaded = Resources.LoadAll<GameObject>("BuildableObjects");
        if (buildableObjects.Count < 1)
            foreach (GameObject obj in buildableObjectsLoaded)
            {
                buildableObjects.Add(obj.GetComponent<BuildableObject>().objectID, obj);
            }
    }

    // Update is called once per frame
    void Update ()
    {
        // Make sure current tile is always accurate
        currentTile = mouseInterface.GetCurrentTile;
        if(startingFenceTile == null)
        {
            startingFenceTile = currentTile;
        }

            #region Building Objects
            // If the player has chosen an object to build
            #region Building is fence or a wall
            if (building && (currentBuildObject.GetComponent<Fence>() || (currentBuildObject.GetComponent<Scenery>() != null && currentBuildObject.GetComponent<Scenery>().isWall)))
        {
            if (Input.GetMouseButtonDown(0) && !mouseInterface.IsOverUI)
            {
                lockedFenceRotation = rotation;
                startingFenceTile = currentTile;
                mouseInterface.StartCoroutine(mouseInterface.DragBox(currentTile, "fence"));
            }
            if (Input.GetMouseButtonUp(0) && !mouseInterface.IsOverUI)
            {
                float tempRotation = lockedFenceRotation;
                clearToBuildThese = zooInfo.CheckIfLandOwnedByZoo(clearToBuildThese);
                if (economy.AttemptPurchase(economy.CalculateCost(currentBuildObject, clearToBuildThese)))
                    // Check finances to see if we can build or not -- check cost of total construction, then throw error!
                    foreach (Tile tile in clearToBuildThese)
                    {
                        if (tile != null)
                        {
                            BuildObject(tile, currentBuildObject, tempRotation);
                            if (tile == mouseInterface.GetCornerTile)
                            {
                                tempRotation = GetNewFenceRotation(mouseInterface.GetStartFenceTile, mouseInterface.GetCornerTile, currentTile, lockedFenceRotation);
                                if (insideCorner)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                startingFenceTile = null;
            }
            if (Input.GetMouseButtonDown(1))
            {
                StopBuilding();
            }
        }
        #endregion

        #region Building is path
        if (building && currentBuildObject.GetComponent<Path>())
        {           
            if (Input.GetMouseButtonDown(0) && !mouseInterface.IsOverUI)
            {
                startingFenceTile = currentTile;
                mouseInterface.StartCoroutine(mouseInterface.DragBox(currentTile, "path"));
            }
            if (Input.GetMouseButtonUp(0) && !mouseInterface.IsOverUI)
            {
                clearToBuildThese = zooInfo.CheckIfLandOwnedByZoo(clearToBuildThese);
                if (economy.AttemptPurchase(economy.CalculateCost(currentBuildObject, clearToBuildThese)))
                    // Check finances to see if we can build or not -- check cost of total construction, then throw error!
                    foreach (Tile tile in clearToBuildThese)
                    {
                        if (tile != null)
                        {                            
                            BuildObject(tile, currentBuildObject);
                        }
                    }
                startingFenceTile = null;
            }
            if (Input.GetMouseButtonDown(1))
            {
                StopBuilding();
            }
        }
        #endregion

        #region Building if NOT fence or path
        else if (building && currentBuildObject.GetComponent<Fence>() == null && currentBuildObject.GetComponent<Path>() == null)
        {
            if(Input.GetButtonDown("Rotate Building"))
            {
                if (rotation < 270)
                {
                    rotation += 90f;
                }
                else if (rotation >= 270)
                {
                    rotation = 0f;
                }
            }
            if (Input.GetKeyDown(KeyCode.LeftShift)/* || Input.GetKeyDown(KeyCode.LeftControl)*/)
            {
                startTile = currentTile;
                StartCoroutine(ModifyBuildingHeight(currentTile));
                modifiedHeight = true;
            }
            if (Input.GetMouseButtonDown(0) && !mouseInterface.IsOverUI)
            {
                if (!modifiedHeight)
                {
                    startTile = currentTile;
                }
                if (clearToBuildThese.Contains(startTile))
                if (zooInfo.CheckIfLandOwnedByZoo(startTile))
                {
                    if (economy.AttemptPurchase(economy.CalculateCost(currentBuildObject)))
                    {
                        if (modifiedHeight)
                        {
                            BuildObject(startTile, currentBuildObject, rotation);
                        }
                        else
                        {
                            BuildObject(startTile, currentBuildObject, rotation);
                        }
                    }
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                StopBuilding();
            }
        }
        #endregion
        #endregion

        #region Deleting Objects
        if(Input.GetMouseButtonDown(1) && !mouseInterface.IsOverUI)
        {
            GameObject toBeDeleted = mouseInterface.GetBuildableObjectAtMouse();
            if(toBeDeleted != null)
            {
                DeleteObject(toBeDeleted);
                toBeDeleted = null;
            }

        }
        #endregion
    }

    public void StopBuilding()
    {
        building = false;
        if (currentBuildObject != null)
        {
            currentBuildObject = null;
        }

    }
    // Used to start construction by selecting object in interface
    public void StartConstruction(string selectedObject)
    {
        if (buildableObjects[selectedObject] != currentBuildObject)
        {
            // Setcurrent build object to reference when attempting to build & showing 'blueprint'
            currentBuildObject = buildableObjects[selectedObject];
            // Set boolean building to true to initiate construction in Update
            building = true;
            if (currentBuildObject.GetComponent<Building>() != null)
            {
                constructionWindow.gameObject.SetActive(false);
                constructionToolsToggle.isOn = false;
            }
            StartCoroutine(ShowBuildingBlueprint(currentBuildObject));
            rotation = 0f;
        }

    }

    // Attempt to build the object when called -- Must pass clearance & money checks
    public void BuildObject(Tile tile, GameObject objToBuild, float newRotation = 0f)
    {
        // Check clearances in this function and throw error message if building is blocked
        #region Build non-fence objects
        if (objToBuild.GetComponent<Fence>() == null && objToBuild.GetComponent<Path>() == null)
        {
            // Create the object and set its position & rotation
            GameObject newObj = Instantiate(objToBuild);
            BuildableObject newObject = newObj.GetComponent<BuildableObject>();
            //currentNewObject = newObject;
            BuildableObject objectToBuild = objToBuild.GetComponent<BuildableObject>();
            Quaternion newTransformRotation;
            float offsetX = GetVerticeOffset().x;
            float offsetZ = GetVerticeOffset().y;
            if (modifiedHeight)
            {
                offsetX = vertX;
                offsetZ = vertZ;
            }
            tile.AddObjectToTile(newObject);
            objectsBuilt.Add(newObject);

            if (objectToBuild.sizeZ >= 1 && objectToBuild.sizeX >= 1)
            {
                newObject.transform.position = new Vector3(tile.tileCoordX + .5f + newObject.sizeX - 1, tile.height + newHeight + (objectToBuild.sizeVertical / 2f), tile.tileCoordZ + .5f + newObject.sizeZ - 1);
            }
            if (objectToBuild.sizeZ < 1 && objectToBuild.sizeX < 1)
            {
                newObject.transform.position = new Vector3(tile.tileCoordX + offsetX + .5f, tile.height + (objectToBuild.sizeVertical / 2f) + (newHeight / 2f), tile.tileCoordZ + offsetZ + .5f);
            }
            newTransformRotation = Quaternion.Euler(0f, newRotation, 0f);
            newObject.transform.rotation = newTransformRotation;
            newObject.offsetX = offsetX;
            newObject.offsetZ = offsetZ;
            newObject.objectTileCoordinates = new Vector2(tile.tileCoordX, tile.tileCoordZ);

            newObject.RecalculatePosition(offsetX, offsetZ);

            // End building phase -- Only use for Buildings. Otherwise building is ended by player closing window.
            if (objectToBuild.GetComponent<Building>() != null)
            {
                building = false;
            }
        }
        #endregion
        #region Build fences
        else if (objToBuild.GetComponent<Fence>() != null || (currentBuildObject.GetComponent<Scenery>() != null && currentBuildObject.GetComponent<Scenery>().isWall))
        {
            float offsetX = 0;
            float offsetZ = 0;
            Quaternion newTransformRotation;
            float adjustedHeight = tile.height;
            float fenceSlope = 0f;
            GameObject newObj = null;
            string setRotationTo = "N";

            #region Check Rotation
            // Update new coordinates based on rotation, and check if sloped tile, then check if fence will be on flat or sloped portion
            // 0 = North
            if (newRotation == 0)
            {
                offsetX = .5f;
                offsetZ = .95f;
                setRotationTo = "N";
            }
            // 90 = East
            else if (newRotation == 90)
            {
                offsetX = .95f;
                offsetZ = .5f;
                setRotationTo = "E";
            }
            // 180 = South
            else if (newRotation == 180)
            {
                offsetX = .5f;
                offsetZ = .05f;
                setRotationTo = "S";
            }
            // 270 = West
            else if (newRotation == 270)
            {
                offsetX = .05f;
                offsetZ = .5f;
                setRotationTo = "W";
            }
            #endregion
            // Create the fence and set its position & rotation
            // Choose between flat & sloped fence based on slope of fence
            newObj = Instantiate(objToBuild);
            newObj.GetComponent<Fence>().RotationDirection = setRotationTo;
            BuildableObject newObject = newObj.GetComponent<BuildableObject>();
            BuildableObject objectToBuild = objToBuild.GetComponent<BuildableObject>();
            tile.AddObjectToTile(newObject);
            objectsBuilt.Add(newObject);

            newObject.transform.position = new Vector3(tile.tileCoordX + offsetX, adjustedHeight + (newObject.sizeVertical / 2), tile.tileCoordZ + offsetZ);
            newTransformRotation = Quaternion.Euler(0f, newRotation, fenceSlope);
            newObject.transform.rotation = newTransformRotation;

            newObject.offsetX = offsetX;
            newObject.offsetZ = offsetZ;
            newObject.objectTileCoordinates = new Vector2(tile.tileCoordX, tile.tileCoordZ);

            newObject.RecalculatePosition();
            if (newObj.GetComponent<Fence>() != null)
            {
                UpdateFence(tile, newObj.GetComponent<Fence>());
                CheckForEnclosure(newObj.GetComponent<Fence>());
            }

        }
        #endregion
        #region Build Paths
        else if (objToBuild.GetComponent<Path>() != null)
        {
            float offsetX = 0.5f;
            float offsetZ = 0.5f;
            Quaternion newTransformRotation;
            float adjustedHeight = tile.height;
            GameObject newObj = null;
            newObj = Instantiate(objToBuild);
            Path newObject = newObj.GetComponent<Path>();
            //Path objectToBuild = objToBuild.GetComponent<Path>();
            tile.AddObjectToTile(newObject);
            objectsBuilt.Add(newObject);

            newObject.transform.position = new Vector3(tile.tileCoordX + offsetX, adjustedHeight + newObject.sizeVertical/2f, tile.tileCoordZ + offsetZ);
            newTransformRotation = Quaternion.Euler(0f, 0f, 0f);
            newObject.transform.rotation = newTransformRotation;

            newObject.offsetX = offsetX;
            newObject.offsetZ = offsetZ;
            newObject.objectTileCoordinates = new Vector2(tile.tileCoordX, tile.tileCoordZ);

            newObject.RecalculatePosition();
            // If the path is NOT a slope, update it
            if (!newObject.IsSlope)
            {
                UpdatePath(tile, newObject);
            }
            // Always update the adjacent paths -- They'll adjust if the center path is a slope or not. WILL NEED TO TAKE ORIENTATION OF SLOPED PATH INTO CONSIDERATION. Overload UpdatePath..?
            foreach(Tile t in world.GetAdjacentTiles(tile))
            {
                if(t != null)
                if (t.GetPath(newObject, world.GetHeightScale) != null)
                {
                    UpdatePath(t, t.GetPath(newObject, world.GetHeightScale));
                }
            }
        }
        #endregion
    }

    // Attempt to delete the given object when called.
    public void DeleteObject(GameObject gameObjectToDelete)
    {
        // Reference the BuildableObject and its Tile
        BuildableObject objToDelete = gameObjectToDelete.GetComponent<BuildableObject>();
        Tile objTile = world.GetTile(objToDelete.objectTileCoordinates);

        // Declare any potential variables                
        List<Path> pathsToUpdate = new List<Path>(); // List of paths to be updated on the current path is deleted

        // Do we need to confirm deletion (fence in exhibit, building)?
        // This is not a thing yet.
        


        #region Pre-Delete Maintenance Tasks
        // Do any needed maintenance tasks (Update paths, refund money, remove object from lists, etc) before deleting the object

        // Remove the object from the list of objects so it is no longer saved and loaded.
        objectsBuilt.Remove(objToDelete);
        // Remove the object from its Tile's list of objects
        objTile.Objects.Remove(objToDelete);

        // If object is a path, reference all paths connected to it
        if (objToDelete.GetComponent<Path>() != null)
        {
            Tile[] neighbours = world.GetAdjacentTiles(objTile);
            Path pathToDelete = objToDelete.GetComponent<Path>();
            foreach(Tile t in neighbours)
            {
                if(t != null)
                if(t.CheckForPath(pathToDelete, world.heightScale))
                {
                   pathsToUpdate.Add(t.GetPath(pathToDelete, world.heightScale));
                }
            }
        }
        
        // If object is a fence belonging to an enclosure, destroy that enclosure!
        if(objToDelete.GetComponent<Fence>() != null)
        {
            if(objToDelete.GetComponent<Fence>().FenceEnclosure != null)
            {
                objToDelete.GetComponent<Fence>().FenceEnclosure.DestroyEnclosure();
            }
        }

        // Refund a portion of the build cost
        economy.AddFunds(objToDelete.objectPrice * economy.refundPercentage);


        #endregion

        // Having passed all checks and done all pre-deletion maintenance, now delete the object!

        Destroy(gameObjectToDelete);        
        // This seems too easy.

        #region Post-Delete Maintenance Tasks

        // If there are paths to update, update them!
        foreach(Path p in pathsToUpdate)
        {
            UpdatePath(world.GetTile(p.objectTileCoordinates), p);
        }

        #endregion
        // If nothing fucked up
        Debug.Log("Object Deleted!");
    }

    IEnumerator ModifyBuildingHeight(Tile curTile)
    {
        newHeight = curTile.height;
        if (curTile.isSlope)
        {
            newHeight = (curTile.height / 2);
        }
        vertX = GetVerticeOffset().x;
        vertZ = GetVerticeOffset().y;
        float increment = .01f;
        float yStart = Camera.main.ScreenToViewportPoint(Input.mousePosition).y;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        // While holding shift, raise and lower height
        while (Input.GetKey(KeyCode.LeftShift))
        {
            float newY = Camera.main.ScreenToViewportPoint(Input.mousePosition).y;
            //Debug.Log("Start height = " + yStart + " and new height = " + newY);

            if (newY - yStart > increment)
            {
                newHeight += .5f;
                yStart = yStart + increment;
            }
            if (yStart - newY > increment)
            {
                newHeight -= .5f;
                yStart = yStart - increment;
            }
            else if (clearancesOn && !CheckClearances(curTile, currentNewObject))
            {
                // If the collider object is a path, always increase height by 1 (may change if needed)
                if (collidedObject.GetComponent<Path>() == true)
                {
                    newHeight += 1;
                }
                else
                {
                    newHeight += collidedObject.sizeVertical*2;
                }
            }
            yield return new WaitForEndOfFrame();
        }
        //if (Input.GetKeyDown(KeyCode.LeftControl))
        //{
        //    if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity))
        //    {
        //        GameObject hitObj = hitInfo.collider.gameObject;
        //        // If the object hit is a buildable object (For better or worse, this currently includes paths!)
        //        if (hitObj.GetComponent<BuildableObject>() != null)
        //        {
        //            BuildableObject buildObj = hitObj.GetComponent<BuildableObject>();
        //            buildObj.objectHeight = newHeight;
        //        }
        //    }
        //}
        //if (Input.GetKeyUp(KeyCode.LeftControl))
        //{
        //    newHeight = (int)curTile.height;
        //}
        newHeight = curTile.height;
        if (curTile.isSlope)
        {
            newHeight = (curTile.height / 2);
        }
        modifiedHeight = false;
        yield break;
    }
    IEnumerator ShowBuildingBlueprint(GameObject objToBuild)
    {
        #region Non-Fence Objects
        // For non-fence objects
        if (objToBuild.GetComponent<Fence>() == null && objToBuild.GetComponent<Path>() == null)
        {                  
            // Create the object blueprint and set its position & rotation
            GameObject newObj = Instantiate(objToBuild);
            newObj.layer = 11;
            BuildableObject newObject = newObj.GetComponent<BuildableObject>();
            currentNewObject = newObject;
            BuildableObject objectToBuild = objToBuild.GetComponent<BuildableObject>();
            Tile tile = null;
            MeshRenderer objRenderer = newObject.GetComponent<MeshRenderer>();
            Quaternion newTransformRotation;
            while (building)
            {

                #region Full Size Objects
                if (objectToBuild.sizeZ >= 1 && objectToBuild.sizeX >= 1)
                {
                    if (modifiedHeight)
                    {
                        tile = startTile;
                        if (tile == null)
                            tile = currentTile;
                        newObject.transform.position = new Vector3(tile.tileCoordX + .5f + newObject.sizeX - 1, tile.height + .5f + newHeight, tile.tileCoordZ + .5f + newObject.sizeZ - 1);
                        newTransformRotation = Quaternion.Euler(0f, rotation, 0f);
                        newObject.transform.rotation = newTransformRotation;
                        newObject.RecalculatePosition();
                        bool clear = CheckClearances(tile, newObject);
                        #region UpdateBlueprint
                        if (clear)
                        {
                            clearToBuildThese.Add(tile);
                            if (!tile.IsOwnedByZoo)
                            {
                                objRenderer.sharedMaterial = blockedBlueprintMaterial;
                            }
                            else
                            {
                                objRenderer.sharedMaterial = blueprintMaterial;
                            }
                        }
                        else if (!clear)
                        {
                            objRenderer.sharedMaterial = blockedBlueprintMaterial;
                        }
                        #endregion
                    }
                    else if (currentTile != null)
                    {
                        tile = currentTile;
                        newObject.transform.position = new Vector3(tile.tileCoordX + .5f + newObject.sizeX - 1, tile.height + .5f + newHeight, tile.tileCoordZ + .5f + newObject.sizeZ - 1);
                        newTransformRotation = Quaternion.Euler(0f, rotation, 0f);
                        newObject.transform.rotation = newTransformRotation;
                        newObject.RecalculatePosition();
                        bool clear = CheckClearances(tile, newObject);
                        #region UpdateBlueprint
                        if (clear)
                        {
                            clearToBuildThese.Add(tile);
                            if (!tile.IsOwnedByZoo)
                            {
                                objRenderer.sharedMaterial = blockedBlueprintMaterial;
                            }
                            else
                            {
                                objRenderer.sharedMaterial = blueprintMaterial;
                            }
                        }
                        else if (!clear)
                        {
                            objRenderer.sharedMaterial = blockedBlueprintMaterial;
                        }
                        #endregion
                    }
                }
                #endregion
                #region 1/4 or 1/2 Tile Objects
                else if (objectToBuild.sizeZ < 1 && objectToBuild.sizeX < 1)
                {
                    if (modifiedHeight && newObj != null)
                    {
                        tile = startTile;
                        if (tile == null)
                            tile = currentTile;
                        newObject.transform.position = new Vector3(tile.tileCoordX + vertX + .5f, tile.height + (objectToBuild.sizeVertical / 2f) + (newHeight / 2f), tile.tileCoordZ + vertZ + .5f);
                        newTransformRotation = Quaternion.Euler(0f, rotation, 0f);
                        newObject.transform.rotation = newTransformRotation;
                        newObject.RecalculatePosition();
                        bool clear = CheckClearances(tile, newObject);
                        #region UpdateBlueprint
                        if (clear)
                        {
                            clearToBuildThese.Add(tile);
                            if (!tile.IsOwnedByZoo)
                            {
                                objRenderer.sharedMaterial = blockedBlueprintMaterial;
                            }
                            else
                            {
                                objRenderer.sharedMaterial = blueprintMaterial;
                            }
                        }
                        else if (!clear)
                        {
                            objRenderer.sharedMaterial = blockedBlueprintMaterial;
                        }
                        #endregion
                    }
                    else if (currentTile != null)
                    {
                        tile = currentTile;
                        newObject.transform.position = new Vector3(tile.tileCoordX + GetVerticeOffset().x + .5f, tile.height + (objectToBuild.sizeVertical / 2f) + (newHeight / 2f), tile.tileCoordZ + GetVerticeOffset().y + .5f);
                        newTransformRotation = Quaternion.Euler(0f, rotation, 0f);
                        newObject.transform.rotation = newTransformRotation;
                        newObject.RecalculatePosition();
                        bool clear = CheckClearances(tile, newObject);
                        #region UpdateBlueprint
                        if (clear)
                        {
                            clearToBuildThese.Add(tile);
                            if (!tile.IsOwnedByZoo)
                            {
                                objRenderer.sharedMaterial = blockedBlueprintMaterial;
                            }
                            else
                            {
                                objRenderer.sharedMaterial = blueprintMaterial;
                            }
                        }
                        else if (!clear)
                        {
                            objRenderer.sharedMaterial = blockedBlueprintMaterial;
                        }
                        #endregion
                    }
                }
                #endregion
                yield return new WaitForEndOfFrame();
                clearToBuildThese.Clear();
                if(currentBuildObject != null)
                if (newObject.objectID != currentBuildObject.GetComponent<BuildableObject>().objectID)
                {
                    Destroy(newObj);
                    yield break;
                }
            }
            clearToBuildThese.Clear();
            Destroy(newObj);

        }
        #endregion
        // Check for TODO items in this region -- They will need to be applied to the BuildObject function as well
        #region Fences
        // For fences
        else if (objToBuild.GetComponent<Fence>() != null || (currentBuildObject.GetComponent<Scenery>() != null && currentBuildObject.GetComponent<Scenery>().isWall))
        {
            BuildableObject newObject = null;
            while (building)
            {
                Tile curTile = null;
                if (currentTile != null)
                    curTile = currentTile;
                #region Fences
                rotation = GetRotationFromTileSide();

                #region Create single tile                
                // Create each fence blueprint for each tile in the array
                if (currentTile != null && !Input.GetMouseButton(0) && !mouseInterface.IsOverUI)
                {
                    string setRotationTo = "N";
                    Tile tile = currentTile;
                    float offsetX = 0;
                    float offsetZ = 0;
                    Quaternion newTransformRotation;
                    float adjustedHeight = tile.height;
                    float tempRotation = rotation;
                    float fenceSlope = 0f;
                    GameObject newObj = null;
                    //if (mouseInterface.GetCornerTile != null)
                    //{
                    //    tempRotation = GetNewFenceRotation(mouseInterface.GetStartFenceTile, mouseInterface.GetCornerTile, currentTile, startingRotation);
                    //}
                    #region Check Rotation
                    // Update new coordinates based on rotation
                    // TO DO: Change offset based on fence rotation and slope
                    // TO DO: Set up fence selection function & associated array to then use to display blueprints & build
                    // TO DO: Set up clearance checks for fences
                    // 0 = South
                    if (tempRotation == 0)
                    {
                        offsetX = .5f;
                        offsetZ = .95f;
                        setRotationTo = "N";
                    }
                    // 90 = East
                    else if (tempRotation == 90)
                    {
                        offsetX = .95f;
                        offsetZ = .5f;
                        setRotationTo = "E";
                    }
                    // 180 = North
                    else if (tempRotation == 180)
                    {
                        offsetX = .5f;
                        offsetZ = .05f;
                        setRotationTo = "S";
                    }
                    // 270 = West
                    else if (tempRotation == 270)
                    {
                        offsetX = .05f;
                        offsetZ = .5f;
                        setRotationTo = "W";
                    }
                    #endregion
                    // Create the object blueprint and set its position & rotation WILL NEED TO DO FOR EACH TILE IN NEW FENCES TILE ARRAY 
                    // Choose between flat & sloped fence based on slope of fence
                    // TO DO: Adapt this code to use object id to choose the proper fence (reference objectID of the flat fence to find the sloped counterpart)
                    newObj = Instantiate(objToBuild);
                    newObj.layer = 11;
                    newObject = newObj.GetComponent<BuildableObject>();
                    newObj.GetComponent<Fence>().RotationDirection = setRotationTo;
                    BuildableObject objectToBuild = objToBuild.GetComponent<BuildableObject>();
                    MeshRenderer objRenderer = newObject.GetComponent<MeshRenderer>();
                    toDestroy.Add(newObj);
                    newObject.transform.position = new Vector3(tile.tileCoordX + offsetX, adjustedHeight + (newObject.sizeVertical / 2), tile.tileCoordZ + offsetZ);
                    newTransformRotation = Quaternion.Euler(0f, tempRotation, fenceSlope);
                    newObject.transform.rotation = newTransformRotation;
                    newObject.RecalculatePosition();
                    if (newObj.GetComponent<Fence>() != null)
                        UpdateFence(tile, newObj.GetComponent<Fence>());
                    bool clear = CheckClearances(tile, newObject);
                    #region UpdateBlueprint
                    if (clear)
                    {
                        //clearToBuildThese.Add(tile);
                        if (!tile.IsOwnedByZoo)
                        {
                            objRenderer.sharedMaterial = blockedBlueprintMaterial;
                        }
                        else
                        {
                            objRenderer.sharedMaterial = blueprintMaterial;
                        }
                    }
                    else if (!clear)
                    {
                        objRenderer.sharedMaterial = blockedBlueprintMaterial;
                    }
                    #endregion
                }
                #endregion
                // Create each tile based on what the player has drag selected
                if (mouseInterface.GetSelectedTiles != null && Input.GetMouseButton(0) && !mouseInterface.IsOverUI)
                {
                    float startingRotation = lockedFenceRotation;
                    float newRotation = startingRotation;
                    #region Create Full Blueprint
                    foreach (Tile t in mouseInterface.GetSelectedTiles)
                    {
                        // Create each fence blueprint for each tile in the array
                        if (t != null)
                        {
                            string setRotationTo = "N";
                            Tile tile = t;
                            float offsetX = 0;
                            float offsetZ = 0;
                            Quaternion newTransformRotation;
                            float adjustedHeight = tile.height;
                            float fenceSlope = 0f;
                            GameObject newObj = null;
                            if (t == mouseInterface.GetCornerTile)
                            {
                                newRotation = GetNewFenceRotation(mouseInterface.GetStartFenceTile, mouseInterface.GetCornerTile, curTile, startingRotation);
                                if (!insideCorner)
                                {
                                    #region CornerBlueprint
                                    #region Check Rotation
                                    // Update new coordinates based on rotation
                                    // TO DO: Change offset based on fence rotation and slope
                                    // TO DO: Set up fence selection function & associated array to then use to display blueprints & build
                                    // TO DO: Set up clearance checks for fences
                                    // 0 = North
                                    if (startingRotation == 0)
                                    {
                                        offsetX = .5f;
                                        offsetZ = .95f;
                                        setRotationTo = "N";
                                    }
                                    // 90 = East
                                    else if (startingRotation == 90)
                                    {
                                        offsetX = .95f;
                                        offsetZ = .5f;
                                        setRotationTo = "E";
                                    }
                                    // 180 = South
                                    else if (startingRotation == 180)
                                    {
                                        offsetX = .5f;
                                        offsetZ = .05f;
                                        setRotationTo = "S";
                                    }
                                    // 270 = West
                                    else if (startingRotation == 270)
                                    {
                                        offsetX = .05f;
                                        offsetZ = .5f;
                                        setRotationTo = "W";
                                    }
                                    #endregion

                                    // Create the object blueprint and set its position & rotation WILL NEED TO DO FOR EACH TILE IN NEW FENCES TILE ARRAY 
                                    // Choose between flat & sloped fence based on slope of fence
                                    // TO DO: Adapt this code to use object id to choose the proper fence (reference objectID of the flat fence to find the sloped counterpart)
                                    newObj = Instantiate(objToBuild);
                                    toDestroy.Add(newObj);
                                    newObj.layer = 11;
                                    newObject = newObj.GetComponent<BuildableObject>();
                                    newObj.GetComponent<Fence>().RotationDirection = setRotationTo;
                                    BuildableObject cornerObj = objToBuild.GetComponent<BuildableObject>();
                                    MeshRenderer cornerObjRenderer = newObject.GetComponent<MeshRenderer>();

                                    newObject.transform.position = new Vector3(tile.tileCoordX + offsetX, adjustedHeight + (newObject.sizeVertical / 2), tile.tileCoordZ + offsetZ);
                                    newTransformRotation = Quaternion.Euler(0f, startingRotation, fenceSlope);
                                    newObject.transform.rotation = newTransformRotation;
                                    newObject.RecalculatePosition();
                                    if (newObj.GetComponent<Fence>() != null)
                                        UpdateFence(tile, newObj.GetComponent<Fence>());
                                    bool cornerClear = CheckClearances(tile, newObject);
                                    #region UpdateBlueprint
                                    if (cornerClear)
                                    {
                                        clearToBuildThese.Add(tile);
                                        if (!tile.IsOwnedByZoo)
                                        {
                                            cornerObjRenderer.sharedMaterial = blockedBlueprintMaterial;
                                        }
                                        else
                                        {
                                            cornerObjRenderer.sharedMaterial = blueprintMaterial;
                                        }
                                    }
                                    else if (!cornerClear)
                                    {
                                        cornerObjRenderer.sharedMaterial = blockedBlueprintMaterial;
                                    }
                                    #endregion
                                    #endregion
                                }
                                else if (insideCorner)
                                {
                                    continue;
                                }
                            }

                            #region Check Rotation
                            // Update new coordinates based on rotation
                            // TO DO: Change offset based on fence rotation and slope
                            // TO DO: Set up fence selection function & associated array to then use to display blueprints & build
                            // TO DO: Set up clearance checks for fences
                            // 0 = North
                            if (newRotation == 0)
                            {
                                offsetX = .5f;
                                offsetZ = .95f;
                                setRotationTo = "N";
                            }
                            // 90 = East
                            else if (newRotation == 90)
                            {
                                offsetX = .95f;
                                offsetZ = .5f;
                                setRotationTo = "E";
                            }
                            // 180 = North
                            else if (newRotation == 180)
                            {
                                offsetX = .5f;
                                offsetZ = .05f;
                                setRotationTo = "S";
                            }
                            // 270 = West
                            else if (newRotation == 270)
                            {
                                offsetX = .05f;
                                offsetZ = .5f;
                                setRotationTo = "W";
                            }
                            #endregion
                            //Debug.Log(newRotation);

                            // Create the object blueprint and set its position & rotation WILL NEED TO DO FOR EACH TILE IN NEW FENCES TILE ARRAY 
                            // Choose between flat & sloped fence based on slope of fence
                            // TO DO: Adapt this code to use object id to choose the proper fence (reference objectID of the flat fence to find the sloped counterpart)
                            newObj = Instantiate(objToBuild);
                            toDestroy.Add(newObj);
                            newObj.layer = 11;
                            newObject = newObj.GetComponent<BuildableObject>();
                            newObj.GetComponent<Fence>().RotationDirection = setRotationTo;
                            BuildableObject objectToBuild = objToBuild.GetComponent<BuildableObject>();
                            MeshRenderer objRenderer = newObject.GetComponent<MeshRenderer>();

                            newObject.transform.position = new Vector3(tile.tileCoordX + offsetX, adjustedHeight + (newObject.sizeVertical / 2), tile.tileCoordZ + offsetZ);
                            newTransformRotation = Quaternion.Euler(0f, newRotation, fenceSlope);
                            newObject.transform.rotation = newTransformRotation;
                            newObject.RecalculatePosition();
                            if (newObj.GetComponent<Fence>() != null)
                                UpdateFence(tile, newObj.GetComponent<Fence>());

                            bool clear = CheckClearances(tile, newObject);
                            #region UpdateBlueprint
                            if (clear)
                            {
                                clearToBuildThese.Add(tile);
                                if (!tile.IsOwnedByZoo)
                                {
                                    objRenderer.sharedMaterial = blockedBlueprintMaterial;
                                }
                                else
                                {
                                    objRenderer.sharedMaterial = blueprintMaterial;
                                }
                            }
                            else if (!clear)
                            {
                                objRenderer.sharedMaterial = blockedBlueprintMaterial;
                            }
                            #endregion
                        }
                    }
                    #endregion
                }
                #endregion
                yield return new WaitForEndOfFrame();
                //Destroy all tiles in the array
                    foreach (GameObject obj in toDestroy)
                    {
                        Destroy(obj);
                    }
                clearToBuildThese.Clear();
            }
        }
        #endregion
        #region Paths
        else if(objToBuild.GetComponent<Path>() != null)
        {
            BuildableObject newObject = null;

            while (building)
            {
                Tile curTile = null;
                if (currentTile != null)
                    curTile = currentTile;
                #region Paths
                #region Create single tile path                
                // Create each fence blueprint for each tile in the array
                if (currentTile != null && !Input.GetMouseButton(0) && !mouseInterface.IsOverUI)
                {
                    Tile tile = currentTile;
                    float offsetX = 0.5f;
                    float offsetZ = 0.5f;
                    Quaternion newTransformRotation;
                    float adjustedHeight = tile.height;
                    GameObject newObj = null;
                    // Create the object blueprint and set its position & rotation WILL NEED TO DO FOR EACH TILE IN NEW FENCES TILE ARRAY 
                    // Choose between flat & sloped fence based on slope of fence
                    // TO DO: Adapt this code to use object id to choose the proper fence (reference objectID of the flat fence to find the sloped counterpart)
                    newObj = Instantiate(objToBuild);
                    newObj.layer = 11;
                    newObject = newObj.GetComponent<BuildableObject>();
                    BuildableObject objectToBuild = objToBuild.GetComponent<BuildableObject>();
                    MeshRenderer objRenderer = newObject.GetComponent<MeshRenderer>();
                    toDestroy.Add(newObj);
                    newObject.transform.position = new Vector3(tile.tileCoordX + offsetX, adjustedHeight + newObject.sizeVertical / 2f, tile.tileCoordZ + offsetZ);
                    newTransformRotation = Quaternion.Euler(0f, 0f, 0f);
                    newObject.transform.rotation = newTransformRotation;
                    newObject.RecalculatePosition();
                    bool clear = CheckClearances(tile, newObject);
                    #region UpdateBlueprint
                    if (clear)
                    {
                        clearToBuildThese.Add(tile);
                        if (!tile.IsOwnedByZoo)
                        {
                            objRenderer.sharedMaterial = blockedBlueprintMaterial;
                        }
                        else
                        {
                            objRenderer.sharedMaterial = blueprintMaterial;
                        }
                    }
                    else if (!clear)
                    {
                        objRenderer.sharedMaterial = blockedBlueprintMaterial;
                    }
                    #endregion
                }
                #endregion
                // Create each tile based on what the player has drag selected
                if (mouseInterface.GetSelectedTiles != null && Input.GetMouseButton(0) && !mouseInterface.IsOverUI)
                {
                    float newRotation = rotation;
                    #region Create Full Blueprint
                    foreach (Tile t in mouseInterface.GetSelectedTiles)
                    {
                        // Create each path blueprint for each tile in the array
                        if (t != null)
                        {
                            Tile tile = t;
                            float offsetX = 0.5f;
                            float offsetZ = 0.5f;
                            Quaternion newTransformRotation;
                            float adjustedHeight = tile.height;
                            GameObject newObj = null;
                            // Create the object blueprint and set its position & rotation WILL NEED TO DO FOR EACH TILE IN NEW FENCES TILE ARRAY 
                            // Choose between flat & sloped fence based on slope of fence
                            // TO DO: Adapt this code to use object id to choose the proper fence (reference objectID of the flat fence to find the sloped counterpart)
                            newObj = Instantiate(objToBuild); 
                            toDestroy.Add(newObj);
                            newObj.layer = 11;
                            newObject = newObj.GetComponent<BuildableObject>();
                            BuildableObject objectToBuild = objToBuild.GetComponent<BuildableObject>();
                            MeshRenderer objRenderer = newObject.GetComponent<MeshRenderer>();
                            newObject.transform.position = new Vector3(tile.tileCoordX + offsetX, adjustedHeight + newObject.sizeVertical / 2f, tile.tileCoordZ + offsetZ);
                            newTransformRotation = Quaternion.Euler(0f, 0f, 0f);
                            newObject.transform.rotation = newTransformRotation;
                            newObject.RecalculatePosition();

                            bool clear = CheckClearances(tile, newObject);
                            #region UpdateBlueprint
                            if (clear)
                            {
                                clearToBuildThese.Add(tile);
                                if (!tile.IsOwnedByZoo)
                                {
                                    objRenderer.sharedMaterial = blockedBlueprintMaterial;
                                }
                                else
                                {
                                    objRenderer.sharedMaterial = blueprintMaterial;
                                }
                            }
                            else if (!clear)
                            {
                                objRenderer.sharedMaterial = blockedBlueprintMaterial;
                            }
                            #endregion
                        }
                    }
                    #endregion
                }
                #endregion
                yield return new WaitForEndOfFrame();
                //Destroy all tiles in the array
                foreach (GameObject obj in toDestroy)
                {
                    Destroy(obj);
                }
                clearToBuildThese.Clear();
            }
        }
        #endregion
        yield break;
    }
    // TO DO: Work out how to handle objects that are larger than 1 tile in size.
    /// <summary>
    /// Check if the new object would collide with any existing objects.
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="objectToBuild"></param>
    /// <returns></returns>
    public bool CheckClearances(Tile tile, BuildableObject objectToBuild/*, float height, Vector2 newObjPosition*/)
    {
        if (clearancesOn)
        {
            bool notClear = false;
            Collider col = objectToBuild.GetComponent<Collider>();
            Collider[] overlap = Physics.OverlapBox(col.bounds.center, col.bounds.extents);
            foreach (Collider other in overlap)
            {
                if (col.bounds.Intersects(other.bounds) && other.gameObject.layer == 12 && other.gameObject != col.gameObject)
                {
                    if (col.GetComponent<Fence>() != null && other.transform.position != col.transform.position)
                    {
                        notClear = false;
                    }
                    else if (col.GetComponent<Fence>() != null && other.transform.position == col.transform.position)
                    {
                        return false;
                    }
                    else if((col.GetComponent<Fence>() != null && other.GetComponent<Path>() != null) || (col.GetComponent<Path>() != null && other.GetComponent<Fence>() != null))
                    {
                        return true;
                    }
                    else if (col.GetComponent<Path>() != null && other.transform.position == col.transform.position)
                    {
                        return false;
                    }
                    else
                    {
                        collidedObject = other.gameObject.GetComponent<BuildableObject>();
                        notClear = true;
                    }
                }
            }
            if (notClear)
                return false;
            else
            {
                return true;
            }
        }
        else
        {
            return true;
        }       
    }

    public Vector2 GetVerticeOffset()
    {
        string vertexType = mouseInterface.GetCurrentVertexType;
        // Positive numbers are actually .75
        if (vertexType == "upperLeft")
        {
            return new Vector2(-.25f, .25f);
        }
        if (vertexType == "upperRight")
        {
            return new Vector2(.25f, .25f);
        }
        if (vertexType == "lowerRight")
        {
            return new Vector2(.25f, -.25f);
        }
        if (vertexType == "lowerLeft")
        {
            return new Vector2(-.25f, -.25f);
        }
        else
        {
            return new Vector2(0f, 0f);
        }
    }

    // Used to determine proper facing of fences when building multi-dimensional fences
    float GetNewFenceRotation(Tile start, Tile corner, Tile end, float currentRotation)
    {
        // 0 = N, 90 = E, 180 = S, 270 = W
        float startX = start.tileCoordX;
        float startZ = start.tileCoordZ;
        float cornerX = corner.tileCoordX;
        float cornerZ = corner.tileCoordZ;
        float endX = end.tileCoordX;
        float endZ = end.tileCoordZ;
        insideCorner = false;
        #region Top Right
        if (endZ - startZ >= endX - startX && endZ > startZ && endX > startX)
        {
            if(currentRotation == 90)
            {
                insideCorner = true;
                return 180f;
            }
            if(currentRotation == 270)
            {
                return 0f;
            }
            if(currentRotation == 0)
            {
                return 270f;
            }
            if(currentRotation == 180)
            {
                return 90f;
            }
        }
        else if (endZ - startZ < endX - startX && endZ > startZ && endX > startX)
        {
            if (currentRotation == 0)
            {
                insideCorner = true;
                return 270;
            }
            if (currentRotation == 180)
            {
                return 90f;
            }
            if(currentRotation == 90)
            {
                return 180f;
            }
            if(currentRotation == 270)
            {
                return 0f;
            }
        }
        #endregion
        #region Bottom Right
        else if (startZ - endZ >= endX - startX && endZ < startZ && endX > startX)
        {
            if (currentRotation == 90)
            {
                insideCorner = true;
                return 0;
            }
            if (currentRotation == 270)
            {
                return 180;
            }
        }
        else if (startZ - endZ < endX - startX && endZ < startZ && endX > startX)
        {
            if (currentRotation == 180)
            {
                insideCorner = true;
                return 270;
            }
            if (currentRotation == 0)
            {
                return 90;
            }
        }
        #endregion        
        #region Bottom Left
        else if (startZ - endZ >= startX - endX && endZ < startZ && endX < startX)
        {
            if (currentRotation == 270)
            {
                insideCorner = true;
                return 0;
            }
            if (currentRotation == 90)
            {
                return 180;
            }
        }
        else if (startZ - endZ < startX - endX && endZ < startZ && endX < startX)
        {
            if (currentRotation == 180)
            {
                insideCorner = true;
                return 90;
            }
            if (currentRotation == 0)
            {
                return 270;
            }
        }
        #endregion
        #region Top Left
        else if (endZ - startZ >= startX - endX && endZ > startZ && endX < startX)
        {
            if (currentRotation == 270)
            {
                insideCorner = true;
                return 180;
            }
            if (currentRotation == 90)
            {
                return 0;
            }
        }
        else if (endZ - startZ < startX - endX && endZ > startZ && endX < startX)
        {
            if (currentRotation == 0)
            {
                insideCorner = true;
                return 900;
            }
            if (currentRotation == 180)
            {
                return 270;
            }
        }
        #endregion
        return currentRotation;
    }

    float GetRotationFromTileSide()
    {
        string side = mouseInterface.SelectedTileSide;
        if(side == "north")
        {
            return 0f;
        }
        else if(side == "east")
        {
            return 90f;
        }
        else if(side == "south")
        {
            return 180f;
        }
        else if(side == "west")
        {
            return 270f;
        }
        else
        {
            return rotation;
        }
    }

    /// <summary>
    /// Update the visual component of this path by checking adjecent tiles for paths.
    /// </summary>
    /// <param name="tileToUpdate"></param>
    public void UpdatePath(Tile tileToUpdate, Path currentPathObject)
    {
        currentPathObject.UpdatePathHeight(tileToUpdate);
        Tile t = tileToUpdate;
        Tile[] neighbours = world.GetAdjacentTiles(t);
        Path[] adjacentPaths = new Path[8];
        MeshRenderer meshRenderer = currentPathObject.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = currentPathObject.GetComponent<MeshFilter>();
        Fence fence;
        string pathType = currentPathObject.pathType;
        if (currentPathObject.IsTunnel)
            pathType = pathType + "tunnel";
        if (currentPathObject.IsElevated)
            pathType = pathType + "elevated";
        int numberOfAdjacentPaths = 0;
        int numberOfDiagonal = 0;
        int numberOfCardinal = 0;

        // Determine how many paths there are around this tile, at the same height (or if a connecting slope)
        for(int i = 0; i < 8; i++)
        {
            if (neighbours[i] != null)
            {
                if (neighbours[i].CheckForPath(currentPathObject, world.GetHeightScale))
                {
                    adjacentPaths[i] = neighbours[i].GetPath(currentPathObject, world.GetHeightScale);
                }
            }
        }
        #region Remove adjacent paths if Fence/Wall
        // Remove adjacent paths if there is a fence/wall between them and the center tile.
        foreach (BuildableObject obj in t.Objects)
        {
            // Check for fences and walls
            if (obj != null)
                if ((obj.GetComponent<Fence>() == true || (obj.GetComponent<Scenery>() != null && obj.GetComponent<Scenery>().isWall)) && (obj.objectHeight >= currentPathObject.objectHeight && obj.objectHeight < (currentPathObject.objectHeight + 1)))
                {
                    fence = obj.GetComponent<Fence>();
                    {
                        if (fence.RotationDirection == "N")
                        {
                            adjacentPaths[4] = null;
                        }
                        else if (fence.RotationDirection == "E")
                        {
                            adjacentPaths[6] = null;
                        }
                        else if (fence.RotationDirection == "S")
                        {
                            adjacentPaths[0] = null;
                        }
                        else if (fence.RotationDirection == "W")
                        {
                            adjacentPaths[2] = null;
                        }
                    }
                }
        }
        // Remove adjacent paths if there is a fence/wall on their path between them and the center tile.
        fence = null;
        foreach (Tile tile in neighbours)
        {
            if (tile != null)
                foreach (BuildableObject obj in tile.Objects)
                {
                    // Check for fences and walls
                    if (obj != null)
                        if ((obj.GetComponent<Fence>() == true || (obj.GetComponent<Scenery>() != null && obj.GetComponent<Scenery>().isWall)) && (obj.objectHeight >= currentPathObject.objectHeight && obj.objectHeight < (currentPathObject.objectHeight + 1)))
                        {
                            fence = obj.GetComponent<Fence>();
                            {
                                #region South Tile
                                if (fence.RotationDirection == "N" && tile == neighbours[0])
                                {
                                    adjacentPaths[0] = null;
                                }
                                if (fence.RotationDirection == "W" && tile == neighbours[0])
                                {
                                    adjacentPaths[1] = null;
                                }
                                if (fence.RotationDirection == "E" && tile == neighbours[0])
                                {
                                    adjacentPaths[7] = null;
                                }
                                #endregion
                                #region Southwest Tile
                                if ((fence.RotationDirection == "N" || fence.RotationDirection == "E") && tile == neighbours[1])
                                {
                                    adjacentPaths[1] = null;
                                }
                                #endregion
                                #region West Tile
                                if (fence.RotationDirection == "E" && tile == neighbours[2])
                                {
                                    adjacentPaths[2] = null;
                                }
                                if (fence.RotationDirection == "N" && tile == neighbours[2])
                                {
                                    adjacentPaths[3] = null;
                                }
                                if (fence.RotationDirection == "S" && tile == neighbours[2])
                                {
                                    adjacentPaths[1] = null;
                                }
                                #endregion
                                #region Northwest Tile
                                if ((fence.RotationDirection == "S" || fence.RotationDirection == "E") && tile == neighbours[3])
                                {
                                    adjacentPaths[3] = null;
                                }
                                #endregion
                                #region North Tile
                                if (fence.RotationDirection == "S" && tile == neighbours[4])
                                {
                                    adjacentPaths[4] = null;
                                }
                                if (fence.RotationDirection == "E" && tile == neighbours[4])
                                {
                                    adjacentPaths[5] = null;
                                }
                                if (fence.RotationDirection == "W" && tile == neighbours[4])
                                {
                                    adjacentPaths[3] = null;
                                }
                                #endregion
                                #region Northeast Tile
                                if ((fence.RotationDirection == "S" || fence.RotationDirection == "W") && tile == neighbours[5])
                                {
                                    adjacentPaths[5] = null;
                                }
                                #endregion
                                #region East Tile
                                if (fence.RotationDirection == "N" && tile == neighbours[6])
                                {
                                    adjacentPaths[5] = null;
                                }
                                if (fence.RotationDirection == "W" && tile == neighbours[6])
                                {
                                    adjacentPaths[6] = null;
                                }
                                if (fence.RotationDirection == "S" && tile == neighbours[6])
                                {
                                    adjacentPaths[7] = null;
                                }
                                #endregion
                                #region Southeast Tile
                                if ((fence.RotationDirection == "N" || fence.RotationDirection == "W") && tile == neighbours[7])
                                {
                                    adjacentPaths[7] = null;
                                }
                                #endregion
                            }
                        }
                }
        }
        #endregion
        // Determine how many are in cardinal or diagonal directions
        for (int i = 0; i < 8; i++)
        {
            if (adjacentPaths[i] != null)
            {
                if (i == 0 || i == 2 || i == 4 || i == 6)
                {
                    numberOfCardinal++;
                    numberOfAdjacentPaths += 1;
                }
                else if (i == 1 || i == 3 || i == 5 || i == 7)
                {
                    numberOfDiagonal++;
                    numberOfAdjacentPaths += 1;
                }
            }
        }
        switch (numberOfAdjacentPaths)
        {
            // If the path has no paths around it
            case 0:
                {
                    meshFilter.sharedMesh = buildableObjects[pathType + "single"].GetComponent<MeshFilter>().sharedMesh;
                    meshRenderer.sharedMaterial = buildableObjects[pathType + "single"].GetComponent<MeshRenderer>().sharedMaterial;
                    break;
                }
            #region 1 Surrounding Path
            // If that path only has 1 path around it
            // Check if the path is diagonal to the center path
            case 1:
                {
                    // Set up the proper end piece
                    // If the S tile is not null
                    if (adjacentPaths[0] != null)
                    {
                        meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                        meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                        // adjust rotation here in the future
                    }
                    // If the W tile is not null
                    else if (adjacentPaths[2] != null)
                    {
                        meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                        meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                        // adjust rotation here in the future
                    }
                    // If the N tile is not null
                    else if (adjacentPaths[4] != null)
                    {
                        meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                        meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                        // adjust rotation here in the future
                    }
                    // If the E tile is not null
                    else if (adjacentPaths[6] != null)
                    {
                        meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                        meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                        // adjust rotation here in the future
                    }
                    else if (numberOfCardinal == 0)
                    {
                        meshFilter.sharedMesh = buildableObjects[pathType + "single"].GetComponent<MeshFilter>().sharedMesh;
                        meshRenderer.sharedMaterial = buildableObjects[pathType + "single"].GetComponent<MeshRenderer>().sharedMaterial;
                    }
                    break;
                }
            #endregion
            #region 2 Surrounding Paths
            // If the path only has 2 paths around it
            case 2:
                {
                    #region Set up end
                    // Check if there are any paths diagonal to the center path
                    if (adjacentPaths[1] != null || adjacentPaths[3] != null || adjacentPaths[5] != null || adjacentPaths[7] != null)
                    {
                        // Make sure they aren't ALL diagonal!
                        if (adjacentPaths[0] != null || adjacentPaths[2] != null || adjacentPaths[4] != null || adjacentPaths[6] != null)
                        {
                            // Set up the proper end piece
                            // If the S tile is not null
                            if (adjacentPaths[0] != null)
                            {
                                meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                                meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                                // adjust rotation here in the future
                            }
                            // If the W tile is not null
                            else if (adjacentPaths[2] != null)
                            {
                                meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                                meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                                // adjust rotation here in the future
                            }
                            // If the N tile is not null
                            else if (adjacentPaths[4] != null)
                            {
                                meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                                meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                                // adjust rotation here in the future
                            }
                            // If the E tile is not null
                            else if (adjacentPaths[6] != null)
                            {
                                meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                                meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                                // adjust rotation here in the future
                            }
                        }
                        else if (numberOfCardinal == 0)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "single"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "single"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                    }
                    #endregion
                    // Otherwise, check which N/S/E/W tiles are here and adjust accordingly
                    else
                    {
                        #region Set up corner
                        // Set up if corner
                        // If the S & W tiles are not null
                        if (adjacentPaths[0] != null && adjacentPaths[2] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W & N tiles are not null
                        else if (adjacentPaths[2] != null && adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N & E tiles are not null
                        else if (adjacentPaths[4] != null && adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & S tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[0] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                        #region Set up straight tile
                        // Set up if straight
                        // If the S & N tiles are not null
                        else if (adjacentPaths[0] != null && adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & W tiles are not null
                        else if (adjacentPaths[2] != null && adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                    }
                    break;
                }
            #endregion
            #region 3 Surrounding Paths
            case 3:
                {
                    #region Set up single
                    if (numberOfCardinal == 0)
                    {
                        meshFilter.sharedMesh = buildableObjects[pathType + "single"].GetComponent<MeshFilter>().sharedMesh;
                        meshRenderer.sharedMaterial = buildableObjects[pathType + "single"].GetComponent<MeshRenderer>().sharedMaterial;
                    }
                    #endregion
                    #region Set up end
                    else if (numberOfCardinal == 1)
                    {
                        // Set up the proper end piece
                        // If the S tile is not null
                        if (adjacentPaths[0] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W tile is not null
                        else if (adjacentPaths[2] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N tile is not null
                        else if (adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E tile is not null
                        else if (adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                    }
                    #endregion
                    // Otherwise, check which N/S/E/W tiles are here and adjust accordingly
                    else if (numberOfCardinal == 2)
                    {
                        #region Set up corner
                        // Set up if corner
                        // If the S & W tiles are not null, and the SW tile is null
                        if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the S & W tiles are not null, and the SW tile is not null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W & N tiles are not null, and the SW tile is null
                        else if (adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W & N tiles are not null, and the SW tile is not null
                        else if (adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N & E tiles are not null, and the NE tile is null
                        else if (adjacentPaths[4] != null && adjacentPaths[6] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N & E tiles are not null, and the NE tile is not null
                        else if (adjacentPaths[4] != null && adjacentPaths[6] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & S tiles are not null, and the SE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[0] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & S tiles are not null, and the SE tile is not null
                        else if (adjacentPaths[6] != null && adjacentPaths[0] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                        #region Set up straight tile
                        // Set up if straight
                        // If the S & N tiles are not null, and the E/W tiles are null
                        else if (adjacentPaths[0] != null && adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & W tiles are not null, and the N/S tiles are null
                        else if (adjacentPaths[2] != null && adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                    }
                    #region Set up 3 Way junction
                    else if (numberOfCardinal == 3)
                    {
                        // If the S, W, & N tiles are not null
                        if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the E, W, & N tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the E, W, & S tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the S, E, & N tiles are not null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                    }
                    #endregion
                    break;
                }
            #endregion
            #region 4 Surrounding Paths
            case 4:
                {
                    #region Set up single
                    if (numberOfCardinal == 0)
                    {
                        meshFilter.sharedMesh = buildableObjects[pathType + "single"].GetComponent<MeshFilter>().sharedMesh;
                        meshRenderer.sharedMaterial = buildableObjects[pathType + "single"].GetComponent<MeshRenderer>().sharedMaterial;
                    }
                    #endregion
                    #region Set up end
                    else if (numberOfCardinal == 1)
                    {
                        // Set up the proper end piece
                        // If the S tile is not null
                        if (adjacentPaths[0] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W tile is not null
                        else if (adjacentPaths[2] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N tile is not null
                        else if (adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E tile is not null
                        else if (adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }

                    }
                    #endregion
                    // Otherwise, check which N/S/E/W tiles are here and adjust accordingly
                    else if (numberOfCardinal == 2)
                    {
                        #region Set up corner
                        // Set up if corner
                        // If the S & W tiles are not null, and the SW tile is null
                        if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the S & W tiles are not null, and the SW tile is not null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W & N tiles are not null, and the SW tile is null
                        else if (adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W & N tiles are not null, and the SW tile is not null
                        else if (adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N & E tiles are not null, and the NE tile is null
                        else if (adjacentPaths[4] != null && adjacentPaths[6] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N & E tiles are not null, and the NE tile is not null
                        else if (adjacentPaths[4] != null && adjacentPaths[6] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & S tiles are not null, and the SE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[0] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & S tiles are not null, and the SE tile is not null
                        else if (adjacentPaths[6] != null && adjacentPaths[0] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                        #region Set up straight tile
                        // Set up if straight
                        // If the S & N tiles are not null, and the E/W tiles are null
                        else if (adjacentPaths[0] != null && adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & W tiles are not null, and the N/S tiles are null
                        else if (adjacentPaths[2] != null && adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                    }
                    #region Set up 3 Way junction
                    else if (numberOfCardinal == 3)
                    {
                        // If the S, W, & N tiles are not null, and the NE & SE tiles are not null
                        if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the SW, S, W, & N tiles are not null, and the NW tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NW, S, W, & N tiles are not null, and the SW tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[1] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }

                        // If the E, W, & N tiles are not null, and the SW & SE tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[1] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the NW, E, W, & N tiles are not null, and the NE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NE, E, W, & N tiles are not null, and the NW tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }

                        // If the E, W, & S tiles are not null, and the NE & NW tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[5] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the SW, E, W, & S tiles are not null, and the SE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[1] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, E, W, & S tiles are not null, and the SW tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[7] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }

                        // If the S, E, & N tiles are not null, and the NW & SW tiles are not null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the NE, S, E, & N tiles are not null, and the SE tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, S, E, & N tiles are not null, and the NE tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[7] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                    }
                    #endregion
                    #region Set up 4 Way junction
                    else if (numberOfCardinal == 4)
                    {
                        meshFilter.sharedMesh = buildableObjects[pathType + "4way"].GetComponent<MeshFilter>().sharedMesh;
                        meshRenderer.sharedMaterial = buildableObjects[pathType + "4way"].GetComponent<MeshRenderer>().sharedMaterial;
                    }
                    #endregion
                    break;
                }
            #endregion
            #region 5 Surrounding Paths
            case 5:
                {
                    #region Set up end
                    if (numberOfCardinal == 1)
                    {
                        // Set up the proper end piece
                        // If the S tile is not null
                        if (adjacentPaths[0] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W tile is not null
                        else if (adjacentPaths[2] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N tile is not null
                        else if (adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E tile is not null
                        else if (adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }

                    }
                    #endregion
                    // Otherwise, check which N/S/E/W tiles are here and adjust accordingly
                    else if (numberOfCardinal == 2)
                    {
                        #region Set up corner
                        // Set up if corner
                        // If the S & W tiles are not null, and the SW tile is null
                        if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the S & W tiles are not null, and the SW tile is not null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W & N tiles are not null, and the SW tile is null
                        else if (adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W & N tiles are not null, and the SW tile is not null
                        else if (adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N & E tiles are not null, and the NE tile is null
                        else if (adjacentPaths[4] != null && adjacentPaths[6] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N & E tiles are not null, and the NE tile is not null
                        else if (adjacentPaths[4] != null && adjacentPaths[6] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & S tiles are not null, and the SE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[0] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & S tiles are not null, and the SE tile is not null
                        else if (adjacentPaths[6] != null && adjacentPaths[0] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                        #region Set up straight tile
                        // Set up if straight
                        // If the S & N tiles are not null, and the E/W tiles are null
                        else if (adjacentPaths[0] != null && adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & W tiles are not null, and the N/S tiles are null
                        else if (adjacentPaths[2] != null && adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                    }
                    #region Set up 3 Way junction
                    else if (numberOfCardinal == 3)
                    {
                        #region 3 Ways
                        // If the S, W, & N tiles are not null, and the NE & SE tiles are not null
                        if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the E, W, & N tiles are not null, and the SW & SE tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[1] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the E, W, & S tiles are not null, and the NE & NW tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[5] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the S, E, & N tiles are not null, and the NW & SW tiles are not null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        #endregion
                        #region Half 3 Ways
                        // If the SW, S, W, & N tiles are not null, and the NW tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NW, S, W, & N tiles are not null, and the SW tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[1] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NW, E, W, & N tiles are not null, and the NE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NE, E, W, & N tiles are not null, and the NW tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SW, E, W, & S tiles are not null, and the SE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[1] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, E, W, & S tiles are not null, and the SW tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[7] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NE, S, E, & N tiles are not null, and the SE tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, S, E, & N tiles are not null, and the NE tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[7] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        #endregion
                        #region Full 3 Ways
                        // If the NW, SW, S, W, & N
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[1] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, SW, E, W, & S tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[7] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NE, NW, E, W, & N tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, NE, S, E, & N tiles are not null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[7] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        #endregion
                    }
                    #endregion
                    #region Set up 4 Way junction
                    else if (numberOfCardinal == 4)
                    {
                        #region Quarter 4 ways
                        if (adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        #endregion
                    }
                    #endregion
                    break;
                }
            #endregion
            #region 6 Surrounding Paths
            case 6:
                {
                    #region Set up end
                    if (numberOfCardinal == 1)
                    {
                        // Set up the proper end piece
                        // If the S tile is not null
                        if (adjacentPaths[0] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W tile is not null
                        else if (adjacentPaths[2] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N tile is not null
                        else if (adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E tile is not null
                        else if (adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }

                    }
                    #endregion
                    // Otherwise, check which N/S/E/W tiles are here and adjust accordingly
                    else if (numberOfCardinal == 2)
                    {
                        #region Set up corner
                        // Set up if corner
                        // If the S & W tiles are not null, and the SW tile is null
                        if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the S & W tiles are not null, and the SW tile is not null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W & N tiles are not null, and the SW tile is null
                        else if (adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W & N tiles are not null, and the SW tile is not null
                        else if (adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N & E tiles are not null, and the NE tile is null
                        else if (adjacentPaths[4] != null && adjacentPaths[6] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N & E tiles are not null, and the NE tile is not null
                        else if (adjacentPaths[4] != null && adjacentPaths[6] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & S tiles are not null, and the SE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[0] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & S tiles are not null, and the SE tile is not null
                        else if (adjacentPaths[6] != null && adjacentPaths[0] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                        #region Set up straight tile
                        // Set up if straight
                        // If the S & N tiles are not null, and the E/W tiles are null
                        else if (adjacentPaths[0] != null && adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & W tiles are not null, and the N/S tiles are null
                        else if (adjacentPaths[2] != null && adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                    }
                    #region Set up 3 Way junction
                    else if (numberOfCardinal == 3)
                    {
                        #region 3 Ways
                        // If the S, W, & N tiles are not null, and the NE & SE tiles are not null
                        if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the E, W, & N tiles are not null, and the SW & SE tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[1] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the E, W, & S tiles are not null, and the NE & NW tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[5] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the S, E, & N tiles are not null, and the NW & SW tiles are not null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        #endregion
                        #region Half 3 Ways
                        // If the SW, S, W, & N tiles are not null, and the NW tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NW, S, W, & N tiles are not null, and the SW tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[1] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NW, E, W, & N tiles are not null, and the NE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NE, E, W, & N tiles are not null, and the NW tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SW, E, W, & S tiles are not null, and the SE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[1] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, E, W, & S tiles are not null, and the SW tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[7] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NE, S, E, & N tiles are not null, and the SE tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, S, E, & N tiles are not null, and the NE tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[7] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        #endregion
                        #region Full 3 Ways
                        // If the NW, SW, S, W, & N
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[1] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, SW, E, W, & S tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[7] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NE, NW, E, W, & N tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, NE, S, E, & N tiles are not null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[7] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        #endregion
                    }
                    #endregion
                    #region Set up 4 Way junction
                    else if (numberOfCardinal == 4)
                    {
                        #region Quarter 4 ways
                        if (adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        #endregion
                        #region Half 4 ways
                        if (adjacentPaths[1] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[3] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[5] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[7] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        #endregion
                        #region Split 4 ways
                        if (adjacentPaths[1] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "split4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "split4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[3] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "split4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "split4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[5] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "split4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "split4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[7] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "split4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "split4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        #endregion
                    }
                    #endregion
                    break;
                }
            #endregion
            #region 7 Surrounding Paths
            case 7:
                {
                    #region Set up end
                    if (numberOfCardinal == 1)
                    {
                        // Set up the proper end piece
                        // If the S tile is not null
                        if (adjacentPaths[0] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W tile is not null
                        else if (adjacentPaths[2] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N tile is not null
                        else if (adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E tile is not null
                        else if (adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "end"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "end"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }

                    }
                    #endregion
                    // Otherwise, check which N/S/E/W tiles are here and adjust accordingly
                    else if (numberOfCardinal == 2)
                    {
                        #region Set up corner
                        // Set up if corner
                        // If the S & W tiles are not null, and the SW tile is null
                        if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the S & W tiles are not null, and the SW tile is not null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W & N tiles are not null, and the SW tile is null
                        else if (adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the W & N tiles are not null, and the SW tile is not null
                        else if (adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N & E tiles are not null, and the NE tile is null
                        else if (adjacentPaths[4] != null && adjacentPaths[6] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the N & E tiles are not null, and the NE tile is not null
                        else if (adjacentPaths[4] != null && adjacentPaths[6] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & S tiles are not null, and the SE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[0] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "corner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "corner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & S tiles are not null, and the SE tile is not null
                        else if (adjacentPaths[6] != null && adjacentPaths[0] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "fullcorner"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "fullcorner"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                        #region Set up straight tile
                        // Set up if straight
                        // If the S & N tiles are not null, and the E/W tiles are null
                        else if (adjacentPaths[0] != null && adjacentPaths[4] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        // If the E & W tiles are not null, and the N/S tiles are null
                        else if (adjacentPaths[2] != null && adjacentPaths[6] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "straight"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "straight"].GetComponent<MeshRenderer>().sharedMaterial;
                            // adjust rotation here in the future
                        }
                        #endregion
                    }
                    #region Set up 3 Way junction
                    else if (numberOfCardinal == 3)
                    {
                        #region 3 Ways
                        // If the S, W, & N tiles are not null, and the NE & SE tiles are not null
                        if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the E, W, & N tiles are not null, and the SW & SE tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[1] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the E, W, & S tiles are not null, and the NE & NW tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[5] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        // If the S, E, & N tiles are not null, and the NW & SW tiles are not null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3way"].GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        #endregion
                        #region Half 3 Ways
                        // If the SW, S, W, & N tiles are not null, and the NW tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NW, S, W, & N tiles are not null, and the SW tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[1] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NW, E, W, & N tiles are not null, and the NE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[3] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NE, E, W, & N tiles are not null, and the NW tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[3] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SW, E, W, & S tiles are not null, and the SE tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[1] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, E, W, & S tiles are not null, and the SW tile is null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[7] != null && adjacentPaths[1] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NE, S, E, & N tiles are not null, and the SE tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[7] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, S, E, & N tiles are not null, and the NE tile is null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[7] != null && adjacentPaths[5] == null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        #endregion
                        #region Full 3 Ways
                        // If the NW, SW, S, W, & N
                        else if (adjacentPaths[0] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[1] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, SW, E, W, & S tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[0] != null && adjacentPaths[7] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the NE, NW, E, W, & N tiles are not null
                        else if (adjacentPaths[6] != null && adjacentPaths[2] != null && adjacentPaths[4] != null && adjacentPaths[5] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        // If the SE, NE, S, E, & N tiles are not null
                        else if (adjacentPaths[0] != null && adjacentPaths[6] != null && adjacentPaths[4] != null && adjacentPaths[7] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "full3way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "full3way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // set rotation
                        }
                        #endregion
                    }
                    #endregion
                    #region Set up 4 Way junction
                    else if (numberOfCardinal == 4)
                    {
                        #region Quarter 4 ways
                        if (adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        #endregion
                        #region Half 4 ways
                        if (adjacentPaths[1] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[3] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[5] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[7] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "half4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "half4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        #endregion
                        #region Split 4 ways
                        if (adjacentPaths[1] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "split4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "split4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[3] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "split4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "split4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[5] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "split4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "split4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[7] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "split4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "split4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        #endregion
                        #region 3 Quarter 4 ways
                        if (adjacentPaths[1] != null && adjacentPaths[3] != null && adjacentPaths[5] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[3] != null && adjacentPaths[5] != null && adjacentPaths[7] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[5] != null && adjacentPaths[7] != null && adjacentPaths[1] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        else if (adjacentPaths[7] != null && adjacentPaths[1] != null && adjacentPaths[3] != null)
                        {
                            meshFilter.sharedMesh = buildableObjects[pathType + "3quarter4way"].GetComponent<MeshFilter>().sharedMesh;
                            meshRenderer.sharedMaterial = buildableObjects[pathType + "3quarter4way"].GetComponent<MeshRenderer>().sharedMaterial;
                            // Set rotation
                        }
                        #endregion
                    }
                    #endregion
                    break;
                }
            #endregion
            #region 8 Surrounding Paths
            // If the Path is completely surrounded 
            case 8:
                {
                    meshFilter.sharedMesh = buildableObjects[pathType + "whole"].GetComponent<MeshFilter>().sharedMesh;
                    meshRenderer.sharedMaterial = buildableObjects[pathType + "whole"].GetComponent<MeshRenderer>().sharedMaterial;
                    break;
                }
        }
        #endregion
    }

    /// <summary>
    /// Update the height & slope of this provided fence based on the provided tile.
    /// </summary>
    /// <param name="tileToUpdate"></param>
    /// <param name="currentFenceObject"></param>
    public void UpdateFence(Tile tileToUpdate, Fence currentFenceObject)
    {
        Tile t = tileToUpdate;
        Fence fence = currentFenceObject;
        fence.transform.position = new Vector3(fence.transform.position.x, (t.tileHeight + fence.sizeVertical / 2), fence.transform.position.z);
        MeshFilter meshFilter = fence.GetComponent<MeshFilter>();
        //Mesh oldMesh = meshFilter.sharedMesh;
        Mesh oldMesh = meshFilter.mesh;
        Mesh newMesh = new Mesh();
        newMesh = Instantiate(buildableObjects[fence.objectID].GetComponent<MeshFilter>().sharedMesh);
        // Grab the vertices of the current object
        Vector3[] vertices = newMesh.vertices;
        // Set up the corners
        Vector3 corner1 = new Vector3();
        Vector3 corner2 = new Vector3();
        Vector3 corner3 = new Vector3();
        Vector3 tilePos = new Vector3(t.tileCoordX, t.tileHeight, t.tileCoordZ);
        // Update their position based on the current tile's topography.
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vert = fence.transform.TransformPoint(vertices[i]);
            //Debug.Log(vert);
            Vector3 curVert = vert;
            //curVert -= new Vector3(fence.transform.position.x, fence.transform.position.y, fence.transform.position.z);
            curVert -= fence.transform.position;
            //Debug.Log(curVert);
            //Debug.Log(testVertPos);
            if (Mathf.Abs(curVert.x) > Mathf.Abs(curVert.z))
            {
                curVert.x = 0;
            }
            else
            {
                curVert.z = 0;
            }
            curVert.y = 0f;
            curVert.Normalize();
            //Debug.Log(curVert);
            // Find which triangle the current vertice is in on the current tile, and grab those corners
            // If in the 0,1,2 triangle
            if (curVert.z == 1 || curVert.x == 1)
            {
                corner1 = t.upperLeft;
                corner2 = t.upperRight;
                corner3 = t.lowerRight;
                //Debug.Log("UPPER");
            }
            // If in the 0,2,3 triangle
            else if (curVert.z == -1 || curVert.x == -1)
            {
                //Debug.Log("LOWER");
                corner1 = t.upperLeft;
                corner2 = t.lowerRight;
                corner3 = t.lowerLeft;
            }
            // Calculate the barycentric coordinates for the vertice inside the current triangle
            Barycentric baryCoords = new Barycentric(corner1, corner2, corner3, vert);
            Vector3 interpolated = baryCoords.Interpolate(corner1, corner2, corner3);
            float newHeight = (vertices[i].y + (interpolated.y)) - t.tileHeight;
            //Debug.Log(corner1 + "," + corner2 + "," + corner3 + "," + vert);
            //Debug.Log(corner1 + "," + corner2 + "," + corner3 + "," + vert + ", interpolated: " + interpolated.y + ", newHeight: " + newHeight + ", tile height: " + t.tileHeight);
            vertices[i] = new Vector3(vertices[i].x, newHeight, vertices[i].z);

        }
        newMesh.vertices = vertices;
        newMesh.RecalculateNormals();
        meshFilter.mesh = newMesh;
        Destroy(oldMesh);
    }

    #region Enclosure Tools
    /// <summary>
    /// Check if the current fence is connected fences that fully enclose an area, and, if so, create an enclosure.
    /// </summary>
    /// <param name="fence"></param>
    public void CheckForEnclosure(Fence fence)
    {
        List<Fence> fencesFound = new List<Fence>();
        fencesFound.Add(fence);
        CheckForAdjacentFences(fence, fencesFound, fence);

    }

    /// <summary>
    /// Check if the given fence has an adjacent fence, and if so, check that fence, and so on until there are no more, or an enclosure is created.
    /// </summary>
    /// <param name="fence"></param>
    /// <param name="fencesFound"></param>
    /// <param name="previousFence"></param>
    void CheckForAdjacentFences(Fence fence, List<Fence> fencesFound, Fence previousFence)
    {
        Tile currentTile = world.MapData[(int)fence.objectTileCoordinates.x, (int)fence.objectTileCoordinates.y];
        Tile[] adjacentTiles = world.GetAdjacentTiles(currentTile);
        // Check which way the fence is rotated, and from there check if there are adjacent fences
        #region Fence is Northern
        if (fence.RotationDirection == "N")
        {
            // Declare each of the fences that we will look for
            Fence western = null;
            Fence insideWest = null;
            Fence insideEast = null;
            Fence eastern = null;
            Fence insideCornerNW = null;
            Fence insideCornerNE = null;

            // Declare the fence to add -- used to check if fence is already in list, and thus if the circuit is complete.
            Fence fenceToAdd = null;

            // Check for inner fences
            foreach(BuildableObject obj in currentTile.Objects)
            {
                if(obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if(f.RotationDirection == "W")
                    {
                        insideWest = f;
                    }
                    else if(f.RotationDirection == "E")
                    {
                        insideEast = f;
                    }
                }
            }
            // Check for E/W fences
            // Check for western fence
            if (adjacentTiles[2] != null)
                foreach (BuildableObject obj in adjacentTiles[2].Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "N")
                    {
                        western = f;
                    }
                }
            }
            // Check for eastern fence
            if (adjacentTiles[6] != null)
                foreach (BuildableObject obj in adjacentTiles[6].Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "N")
                    {
                        eastern = f;
                    }
                }
            }
            // Check for inside corners
            // Check for NW fence
            if (adjacentTiles[3] != null)
                foreach (BuildableObject obj in adjacentTiles[3].Objects)
                {
                    if (obj.GetComponent<Fence>() != null)
                    {
                        Fence f = obj.GetComponent<Fence>();
                        if (f.RotationDirection == "E")
                        {
                            insideCornerNW = f;
                        }
                    }
                }
            // Check for NE fence
            if (adjacentTiles[5] != null)
                foreach (BuildableObject obj in adjacentTiles[5].Objects)
                {
                    if (obj.GetComponent<Fence>() != null)
                    {
                        Fence f = obj.GetComponent<Fence>();
                        if (f.RotationDirection == "W")
                        {
                            insideCornerNE = f;
                        }
                    }
                }
            // Check if the inner west fence exists, if it is the previous fence, and if the western fence is the previous fence. If it passes, add it. Otherwise, move on.
            if (insideWest != null && insideWest != previousFence && western != previousFence)
            {
                fenceToAdd = insideWest;
            }
            else if (insideEast != null && insideEast != previousFence && eastern != previousFence)
            {
                fenceToAdd = insideEast;
            }
            else if (western != null && western != previousFence && insideWest != previousFence)
            {
                fenceToAdd = western;
            }
            else if (eastern != null && eastern != previousFence && insideEast != previousFence)
            {
                fenceToAdd = eastern;
            }
            else if (insideCornerNW != null && insideCornerNW != previousFence && western != previousFence && insideWest != previousFence)
            {
                fenceToAdd = insideCornerNW;
            }
            else if (insideCornerNE != null && insideCornerNE != previousFence && eastern != previousFence && insideEast != previousFence)
            {
                fenceToAdd = insideCornerNE;
            }
            // Check if the fence to add exists, and if it is already in the list. If not already in the list, add it and run CheckForAdjacentFences. 
            if (fenceToAdd != null && !fencesFound.Contains(fenceToAdd))
            {
                fencesFound.Add(fenceToAdd);
                CheckForAdjacentFences(fenceToAdd, fencesFound, fence);
            }
            // If it IS in the list, we're done! Create the enclosure!
            else if (fenceToAdd != null && fencesFound.Contains(fenceToAdd))
            {
                Debug.Log("New fence is in the list!");
                Debug.Log(fencesFound.IndexOf(fenceToAdd));
                Debug.Log(fencesFound.Count);
                // Create enclosure!
                CreateEnclosure(GetEnclosureTiles(currentTile), fencesFound);

            }
            // If there is NO new fence, we're done trying to create an enclosure -- break.
            else if (fenceToAdd == null)
            {
                // Break.
            }
        }
        #endregion
        #region Fence is Eastern
        else if (fence.RotationDirection == "E")
        {
            // Declare each of the fences that we will look for
            Fence northern = null;
            Fence insideNorth = null;
            Fence insideSouth = null;
            Fence southern = null;
            Fence insideCornerNE = null;
            Fence insideCornerSE = null;

            // Declare the fence to add -- used to check if fence is already in list, and thus if the circuit is complete.
            Fence fenceToAdd = null;

            // Check for inner fences
            foreach (BuildableObject obj in currentTile.Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "N")
                    {
                        insideNorth = f;
                    }
                    else if (f.RotationDirection == "S")
                    {
                        insideSouth = f;
                    }
                }
            }
            // Check for N/S fences
            // Check for northern fence
            if (adjacentTiles[4] != null)
                foreach (BuildableObject obj in adjacentTiles[4].Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "E")
                    {
                        northern = f;
                    }
                }
            }
            // Check for southern fence
            if (adjacentTiles[0] != null)
                foreach (BuildableObject obj in adjacentTiles[0].Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "E")
                    {
                        southern = f;
                    }
                }
            }
            // Check for inside corners
            // Check for NE fence
            if (adjacentTiles[5] != null)
                foreach (BuildableObject obj in adjacentTiles[5].Objects)
                {
                    if (obj.GetComponent<Fence>() != null)
                    {
                        Fence f = obj.GetComponent<Fence>();
                        if (f.RotationDirection == "S")
                        {
                            insideCornerNE = f;
                        }
                    }
                }
            // Check for SE fence
            if (adjacentTiles[7] != null)
                foreach (BuildableObject obj in adjacentTiles[7].Objects)
                {
                    if (obj.GetComponent<Fence>() != null)
                    {
                        Fence f = obj.GetComponent<Fence>();
                        if (f.RotationDirection == "N")
                        {
                            insideCornerSE = f;
                        }
                    }
                }
            // Check if the inner west fence exists, if it is the previous fence, and if the western fence is the previous fence. If it passes, add it. Otherwise, move on.
            if (insideNorth != null && insideNorth != previousFence && northern != previousFence)
            {
                fenceToAdd = insideNorth;
            }
            else if (insideSouth != null && insideSouth != previousFence && southern != previousFence)
            {
                fenceToAdd = insideSouth;
            }
            else if (northern != null && northern != previousFence && insideNorth != previousFence)
            {
                fenceToAdd = northern;
            }
            else if (southern != null && southern != previousFence && insideSouth != previousFence)
            {
                fenceToAdd = southern;
            }
            else if (insideCornerNE != null && insideCornerNE != previousFence && northern != previousFence && insideNorth != previousFence)
            {
                fenceToAdd = insideCornerNE;
            }
            else if (insideCornerSE != null && insideCornerSE != previousFence && southern != previousFence && insideSouth != previousFence)
            {
                fenceToAdd = insideCornerSE;
            }
            // Check if the fence to add exists, and if it is already in the list. If not already in the list, add it and run CheckForAdjacentFences. 
            if (fenceToAdd != null && !fencesFound.Contains(fenceToAdd))
            {
                fencesFound.Add(fenceToAdd);
                CheckForAdjacentFences(fenceToAdd, fencesFound, fence);
            }
            // If it IS in the list, we're done! Create the enclosure!
            else if (fenceToAdd != null && fencesFound.Contains(fenceToAdd))
            {
                Debug.Log("New fence is in the list!");
                Debug.Log(fencesFound.IndexOf(fenceToAdd));
                Debug.Log(fencesFound.Count);
                // Create enclosure!
                CreateEnclosure(GetEnclosureTiles(currentTile), fencesFound);

            }
            // If there is NO new fence, we're done trying to create an enclosure -- break.
            else if (fenceToAdd == null)
            {
                // Break.
            }
        }
        #endregion
        #region Fence is Southern
        else if (fence.RotationDirection == "S")
        {
            // Declare each of the fences that we will look for
            Fence western = null;
            Fence insideWest = null;
            Fence insideEast = null;
            Fence eastern = null;
            Fence insideCornerSW = null;
            Fence insideCornerSE = null;

            // Declare the fence to add -- used to check if fence is already in list, and thus if the circuit is complete.
            Fence fenceToAdd = null;

            // Check for inner fences
            foreach (BuildableObject obj in currentTile.Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "W")
                    {
                        insideWest = f;
                    }
                    else if (f.RotationDirection == "E")
                    {
                        insideEast = f;
                    }
                }
            }
            // Check for E/W fences
            // Check for western fence
            if(adjacentTiles[2] != null)
            foreach (BuildableObject obj in adjacentTiles[2].Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "S")
                    {
                        western = f;
                    }
                }
            }
            // Check for eastern fence
            if(adjacentTiles[6] != null)
            foreach (BuildableObject obj in adjacentTiles[6].Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "S")
                    {
                        eastern = f;
                    }
                }
            }
            // Check for inside corners
            // Check for SW fence
            if (adjacentTiles[1] != null)
                foreach (BuildableObject obj in adjacentTiles[1].Objects)
                {
                    if (obj.GetComponent<Fence>() != null)
                    {
                        Fence f = obj.GetComponent<Fence>();
                        if (f.RotationDirection == "E")
                        {
                            insideCornerSW = f;
                        }
                    }
                }
            // Check for SE fence
            if (adjacentTiles[7] != null)
                foreach (BuildableObject obj in adjacentTiles[7].Objects)
                {
                    if (obj.GetComponent<Fence>() != null)
                    {
                        Fence f = obj.GetComponent<Fence>();
                        if (f.RotationDirection == "W")
                        {
                            insideCornerSE = f;
                        }
                    }
                }
            // Check if the inner west fence exists, if it is the previous fence, and if the western fence is the previous fence. If it passes, add it. Otherwise, move on.
            if (insideWest != null && insideWest != previousFence && western != previousFence)
            {
                fenceToAdd = insideWest;
            }
            else if (insideEast != null && insideEast != previousFence && eastern != previousFence)
            {
                fenceToAdd = insideEast;
            }
            else if (western != null && western != previousFence && insideWest != previousFence)
            {
                fenceToAdd = western;
            }
            else if (eastern != null && eastern != previousFence && insideEast != previousFence)
            {
                fenceToAdd = eastern;
            }
            else if (insideCornerSW != null && insideCornerSW != previousFence && western != previousFence && insideWest != previousFence)
            {
                fenceToAdd = insideCornerSW;
            }
            else if (insideCornerSE != null && insideCornerSE != previousFence && eastern != previousFence && insideEast != previousFence)
            {
                fenceToAdd = insideCornerSE;
            }
            // Check if the fence to add exists, and if it is already in the list. If not already in the list, add it and run CheckForAdjacentFences. 
            if (fenceToAdd != null && !fencesFound.Contains(fenceToAdd))
            {
                fencesFound.Add(fenceToAdd);
                CheckForAdjacentFences(fenceToAdd, fencesFound, fence);
            }
            // If it IS in the list, we're done! Create the enclosure!
            else if (fenceToAdd != null && fencesFound.Contains(fenceToAdd))
            {
                Debug.Log("New fence is in the list!");
                Debug.Log(fencesFound.IndexOf(fenceToAdd));
                Debug.Log(fencesFound.Count);
                // Create enclosure!
                CreateEnclosure(GetEnclosureTiles(currentTile), fencesFound);

            }
            // If there is NO new fence, we're done trying to create an enclosure -- break.
            else if (fenceToAdd == null)
            {
                // Break.
            }
        }
        #endregion
        #region Fence is Western
        else if (fence.RotationDirection == "W")
        {
            // Declare each of the fences that we will look for
            Fence northern = null;
            Fence insideNorth = null;
            Fence insideSouth = null;
            Fence southern = null;
            Fence insideCornerNW = null;
            Fence insideCornerSW = null;

            // Declare the fence to add -- used to check if fence is already in list, and thus if the circuit is complete.
            Fence fenceToAdd = null;

            // Check for inner fences
            foreach (BuildableObject obj in currentTile.Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "N")
                    {
                        insideNorth = f;
                    }
                    else if (f.RotationDirection == "S")
                    {
                        insideSouth = f;
                    }
                }
            }
            // Check for N/S fences
            // Check for northern fence
            if (adjacentTiles[4] != null)
                foreach (BuildableObject obj in adjacentTiles[4].Objects)
                {
                    if (obj.GetComponent<Fence>() != null)
                    {
                        Fence f = obj.GetComponent<Fence>();
                        if (f.RotationDirection == "W")
                        {
                            northern = f;
                        }
                    }
                }
            // Check for southern fence
            if (adjacentTiles[0] != null)
                foreach (BuildableObject obj in adjacentTiles[0].Objects)
                {
                    if (obj.GetComponent<Fence>() != null)
                    {
                        Fence f = obj.GetComponent<Fence>();
                        if (f.RotationDirection == "W")
                        {
                            southern = f;
                        }
                    }
                }
            // Check for inside corners
            // Check for NW fence
            if (adjacentTiles[3] != null)
                foreach (BuildableObject obj in adjacentTiles[3].Objects)
                {
                    if (obj.GetComponent<Fence>() != null)
                    {
                        Fence f = obj.GetComponent<Fence>();
                        if (f.RotationDirection == "S")
                        {
                            insideCornerNW = f;
                        }
                    }
                }
            // Check for SW fence
            if (adjacentTiles[1] != null)
                foreach (BuildableObject obj in adjacentTiles[1].Objects)
                {
                    if (obj.GetComponent<Fence>() != null)
                    {
                        Fence f = obj.GetComponent<Fence>();
                        if (f.RotationDirection == "N")
                        {
                            insideCornerSW = f;
                        }
                    }
                }
            // Check if the inner west fence exists, if it is the previous fence, and if the western fence is the previous fence. If it passes, add it. Otherwise, move on.
            if (insideNorth != null && insideNorth != previousFence && northern != previousFence)
            {
                fenceToAdd = insideNorth;
            }
            else if (insideSouth != null && insideSouth != previousFence && southern != previousFence)
            {
                fenceToAdd = insideSouth;
            }
            else if (northern != null && northern != previousFence && insideNorth != previousFence)
            {
                fenceToAdd = northern;
            }
            else if (southern != null && southern != previousFence && insideSouth != previousFence)
            {
                fenceToAdd = southern;
            }
            else if (insideCornerNW != null && insideCornerNW != previousFence && northern != previousFence && insideNorth != previousFence)
            {
                fenceToAdd = insideCornerNW;
            }
            else if (insideCornerSW != null && insideCornerSW != previousFence && southern != previousFence && insideSouth != previousFence)
            {
                fenceToAdd = insideCornerSW;
            }
            // Check if the fence to add exists, and if it is already in the list. If not already in the list, add it and run CheckForAdjacentFences. 
            if (fenceToAdd != null && !fencesFound.Contains(fenceToAdd))
            {
                fencesFound.Add(fenceToAdd);
                CheckForAdjacentFences(fenceToAdd, fencesFound, fence);
            }
            // If it IS in the list, we're done! Create the enclosure!
            else if (fenceToAdd != null && fencesFound.Contains(fenceToAdd))
            {
                Debug.Log("New fence is in the list!");
                Debug.Log(fencesFound.IndexOf(fenceToAdd));
                Debug.Log(fencesFound.Count);
                // Create enclosure!
                CreateEnclosure(GetEnclosureTiles(currentTile), fencesFound);
            }
            // If there is NO new fence, we're done trying to create an enclosure -- break.
            else if (fenceToAdd == null)
            {
                // Break.
            }
        }
        #endregion
    }

    void CreateEnclosure(List<Tile> enclosureTiles, List<Fence> enclosureFences)
    {
        Enclosure newEnclosure = new Enclosure(enclosureTiles, enclosureFences);
        ZooInfo.enclosures.Add(newEnclosure);
        foreach(Fence f in enclosureFences)
        {
            f.FenceEnclosure = newEnclosure;
        }
        Debug.Log(newEnclosure.EnclosureName + " created with " + newEnclosure.EnclosureFences.Count + " fences & " + newEnclosure.EnclosureTiles.Count + " tiles.");
    }


    /// <summary>
    /// Find all tiles in a new enclosure using flood fill, starting with the provided tile, and return a list of them.
    /// </summary>
    /// <param name="startingTile"></param>
    /// <returns></returns>
    List<Tile> GetEnclosureTiles(Tile startingTile)
    {
        List<Tile> tilesInEnclosure = new List<Tile>();
        tilesInEnclosure.Add(startingTile);
        EnclosureFloodFill(startingTile, tilesInEnclosure, startingTile);

        Debug.Log(tilesInEnclosure.Count);
        return tilesInEnclosure;
    }

    /// <summary>
    /// Using flood fill, find all tiles in the enclosure.
    /// </summary>
    /// <param name="startingTile"></param>
    /// <param name="tilesInEnclosure"></param>
    /// <param name="previousTile"></param>
    void EnclosureFloodFill(Tile startingTile, List<Tile> tilesInEnclosure, Tile previousTile)
    {
        // Grab the adjacent tiles
        Tile[] adjacentTiles = world.GetAdjacentTiles(startingTile);

        // Set up the possible blocking inner fences
        // For the current tile
        Fence innerNorth = null;
        Fence innerEast = null;
        Fence innerSouth = null;
        Fence innerWest = null;

        // For the outer tiles
        Fence outerNorth = null;
        Fence outerEast = null;
        Fence outerSouth = null;
        Fence outerWest = null;

        // Grab any fences in the starting tile that would block flood fill.
        foreach (BuildableObject obj in startingTile.Objects)
        {
            if(obj.GetComponent<Fence>() != null)
            {
                Fence f = obj.GetComponent<Fence>();
                if (f.RotationDirection == "N")
                {
                    innerNorth = f;
                }
                else if (f.RotationDirection == "E")
                {
                    innerEast = f;
                }
                else if (f.RotationDirection == "S")
                {
                    innerSouth = f;
                }
                else if (f.RotationDirection == "W")
                {
                    innerWest = f;
                }
            }
        }
        // Check if each tile exists, and that it is not the previous tile, AND if there is an inner fence blocking the way -- if so, break!
        // Then check if there is a fence between it and the current tile. If not, add it to the list, and run flood fill on it!
        // If there is a blocking fence, break!
        #region North Tile
        // Check the North tile
        if (adjacentTiles[4] != null && adjacentTiles[4] != previousTile)
        {
            // Grab any fences in the North tile that would be in the way
            foreach (BuildableObject obj in adjacentTiles[4].Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "S")
                    {
                        outerNorth = f;  
                    }
                }
            }
        }
        #endregion
        #region East Tile
        // Check the East tile
        if (adjacentTiles[6] != null && adjacentTiles[6] != previousTile)
        {
            // Grab any fences in the North tile that would be in the way
            foreach (BuildableObject obj in adjacentTiles[6].Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "W")
                    {
                        outerEast = f;
                    }

                }
            }
        }
        #endregion
        #region South Tile
        // Check the South tile
        if (adjacentTiles[0] != null && adjacentTiles[0] != previousTile)
        {
            // Grab any fences in the North tile that would be in the way
            foreach (BuildableObject obj in adjacentTiles[0].Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "N")
                    {
                        outerSouth = f;
                    }
                }
            }
        }
        #endregion
        #region West Tile
        // Check the West tile
        if (adjacentTiles[2] != null && adjacentTiles[2] != previousTile)
        {
            // Grab any fences in the North tile that would be in the way
            foreach (BuildableObject obj in adjacentTiles[2].Objects)
            {
                if (obj.GetComponent<Fence>() != null)
                {
                    Fence f = obj.GetComponent<Fence>();
                    if (f.RotationDirection == "E")
                    {
                        outerWest = f;
                    }
                }
            }
        }
        #endregion

        // Check if each outer tile is null, and, if so, add the tile to the list and run flood fill on it
        // North                   
        if (innerNorth == null && outerNorth == null &&!tilesInEnclosure.Contains(adjacentTiles[4]))
        {
            tilesInEnclosure.Add(adjacentTiles[4]);
            EnclosureFloodFill(adjacentTiles[4], tilesInEnclosure, startingTile);
        }
        // East
        if (innerEast == null && outerEast == null && !tilesInEnclosure.Contains(adjacentTiles[6]))
        {
            tilesInEnclosure.Add(adjacentTiles[6]);
            EnclosureFloodFill(adjacentTiles[6], tilesInEnclosure, startingTile);
        }
        // South
        if (innerSouth == null && outerSouth == null && !tilesInEnclosure.Contains(adjacentTiles[0]))
        {
            tilesInEnclosure.Add(adjacentTiles[0]);
            EnclosureFloodFill(adjacentTiles[0], tilesInEnclosure, startingTile);
        }
        // West
        if (innerWest == null && outerWest == null && !tilesInEnclosure.Contains(adjacentTiles[2]))
        {
            tilesInEnclosure.Add(adjacentTiles[2]);
            EnclosureFloodFill(adjacentTiles[2], tilesInEnclosure, startingTile);
        }
    }
    #endregion

    /// <summary>
    /// Rebuild each saved object based on its saved information.
    /// </summary>
    public void RebuildObjectsFromSave(List<string> objectsToRebuild)
    {
        LoadBuildableObjectsDictionary();

        foreach (string json in objectsToRebuild)
        {
            GameObject dummy = new GameObject();
            BuildableObject dummyObj = dummy.AddComponent<Building>();
            JsonUtility.FromJsonOverwrite(json, dummyObj);

            // Create the object
            GameObject newObj = Instantiate(buildableObjects[dummyObj.objectID]);
            Destroy(dummy);
            #region Fences
            if (newObj.GetComponent<Fence>() == true)
            {
                // Set the object's info
                Fence newObjScript = newObj.GetComponent<Fence>();
                JsonUtility.FromJsonOverwrite(json, newObjScript);
                Tile thisObjTile = world.MapData[(int)newObjScript.objectTileCoordinates.x, (int)newObjScript.objectTileCoordinates.y];

                thisObjTile.AddObjectToTile(newObjScript);
                objectsBuilt.Add(newObjScript);

                // Set its positon
                newObj.transform.position = newObjScript.objectWorldPosition;
                newObj.transform.rotation = newObjScript.objectRotation;
                newObjScript.RecalculatePosition(newObjScript.offsetX, newObjScript.offsetZ);
            }
            #endregion
            #region Paths
            else if (newObj.GetComponent<Path>() == true)
            {
                // Set the object's info
                Path newObjScript = newObj.GetComponent<Path>();
                JsonUtility.FromJsonOverwrite(json, newObjScript);
                Tile thisObjTile = world.MapData[(int)newObjScript.objectTileCoordinates.x, (int)newObjScript.objectTileCoordinates.y];

                thisObjTile.AddObjectToTile(newObjScript);
                objectsBuilt.Add(newObjScript);

                // Set its positon
                newObj.transform.position = newObjScript.objectWorldPosition;
                newObj.transform.rotation = newObjScript.objectRotation;
                newObjScript.RecalculatePosition(newObjScript.offsetX, newObjScript.offsetZ);

                UpdatePath(thisObjTile, newObjScript);
            }
            #endregion
            #region Scenery
            else if (newObj.GetComponent<Scenery>() == true)
            {
                // Set the object's info
                Scenery newObjScript = newObj.GetComponent<Scenery>();
                JsonUtility.FromJsonOverwrite(json, newObjScript);
                Tile thisObjTile = world.MapData[(int)newObjScript.objectTileCoordinates.x, (int)newObjScript.objectTileCoordinates.y];

                thisObjTile.AddObjectToTile(newObjScript);
                objectsBuilt.Add(newObjScript);

                // Set its positon
                newObj.transform.position = newObjScript.objectWorldPosition;
                newObj.transform.rotation = newObjScript.objectRotation;
                newObjScript.RecalculatePosition(newObjScript.offsetX, newObjScript.offsetZ);
            }
            #endregion
            #region Buildings
            else if (newObj.GetComponent<Building>() == true)
            {
                // Set the object's info
                Building newObjScript = newObj.GetComponent<Building>();
                JsonUtility.FromJsonOverwrite(json, newObjScript);
                Tile thisObjTile = world.MapData[(int)newObjScript.objectTileCoordinates.x, (int)newObjScript.objectTileCoordinates.y];

                thisObjTile.AddObjectToTile(newObjScript);
                objectsBuilt.Add(newObjScript);

                // Set its positon
                newObj.transform.position = newObjScript.objectWorldPosition;
                newObj.transform.rotation = newObjScript.objectRotation;
                newObjScript.RecalculatePosition(newObjScript.offsetX, newObjScript.offsetZ);
            }
            #endregion
        }

        foreach (BuildableObject obj in objectsBuilt)
        {
            if (obj.GetComponent<Path>() != null)
            {
                UpdatePath(world.MapData[(int)obj.objectTileCoordinates.x, (int)obj.objectTileCoordinates.y], obj.GetComponent<Path>());
            }
            else if(obj.GetComponent<Fence>() != null)
            {
                UpdateFence(world.MapData[(int)obj.objectTileCoordinates.x, (int)obj.objectTileCoordinates.y], obj.GetComponent<Fence>());
            }
        }

    }

    /// <summary>
    /// Clear the objects built list when loading the level to prevent reference errors.
    /// </summary>
    public void ClearObjectsBuiltOnLoad()
    {
        objectsBuilt.Clear();
    }
}
