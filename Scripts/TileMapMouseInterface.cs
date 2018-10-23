using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class TileMapMouseInterface : MonoBehaviour {

    Texture selectionBoxTexture;
    Tile selectedSelectionTile;
    Tile selectedTile;
    Tile cornerTile; // Used to determine new fence facing when building...fences. Yep. 
    Tile startFenceTile; // Used to determine.. yeah, you guessed it
    Vector3 selectedVertex;
    int currentHeight;
    Tile[] selectionBoxTilesToPaint;
    Tile[] selectedOverlayTiles;
    Tile[] selectedSelectionTiles;
    Tile[] selectedTiles;
    Tile[] previousTiles;
    Chunk[,] selectionChunks;

    int selectedTileFace; // Where N = 0, E = 90, S = 180, W = 270

    public LandscapingTools landscapingTools;
    public World world;
    public ZooInfo zooInfo;

    string selectedTileSide; // Selected side of current tile (north, east, south, west)
    string vertexType;
    float swathSize = 1; // Size of landscaping swath (size * size == dimensions of swath)

    public bool overUI;
    bool pauseVertexSelect = false;
    bool leftMouseDown = false;
    bool leftMouseHeld = false;

    private void Start()
    {
        previousTiles = new Tile[world.worldSize * world.worldSize];
    }

    // Update is called once per frame
    void Update()
    {
        GetSelectedTile();
        GetSelectedSelectionTile();
        if (GetCurrentTile != null)
        {
            GetSelectedTileCorner();
            GetSelectedTileSide();
        }
        CheckIfOverUI();
        //GetCurrentHeight();    

        if (Input.GetMouseButtonDown(0))
        {
            leftMouseDown = true;

            //string testSave = JsonUtility.ToJson(GetCurrentTile);
            //Debug.Log(testSave);
            //testSave = JsonUtility.ToJson(GetCurrentTile, true);
            //Debug.Log(testSave);

        }
        else
        {
            leftMouseDown = false;
        }
        if (Input.GetMouseButton(0))
        {
            leftMouseHeld = true;
        }
        else
        {
            leftMouseHeld = false;
        }        
    }


    public Tile GetCurrentTile
    {
        get { return selectedTile; }
    }

    public Tile GetCornerTile
    {
        get { return cornerTile; }
    }

    public Tile GetStartFenceTile
    {
        get { return startFenceTile; }
    }

    public Vector3 GetCurrentVertex
    {
        get { return GetCurrentVertex; }
    }

    public string GetCurrentVertexType
    {
        get { return vertexType; }
    }

    public Tile[] GetSelectedTiles
    {
        get { return selectedTiles; }
    }

    public bool VertexSelectOnOff
    {
        get { return pauseVertexSelect; }
        set { pauseVertexSelect = value; }
    }

    public bool IsOverUI
    {
        get { return overUI; }
    }

    public float SwathSize
    {
        get { return swathSize; }
        set { swathSize = value; }
    }

    public string SelectedTileSide
    {
        get { return selectedTileSide; }
    }

    public IEnumerator DragBox(Tile startMouseTile, string returnFunction)// string determines return function ie painting, leveling, pathing, etc
    {
        int blankTile = 0;  // Default selectionBoxMesh texture -- 100% transparent
        int overlayTile = 1; // Selection box selection texture -- make sure to set back to blank once done using!
        string returnType = returnFunction;
        Tile startTile = startMouseTile; // Cache starting tile
        int startX = (int)startMouseTile.tileCoordX;
        int startZ = (int)startMouseTile.tileCoordZ;

        //Debug.Log("current tile is (" + GetCurrentTile.tileCoordX + "," + GetCurrentTile.tileCoordZ);

        while (Input.GetMouseButton(0))
        {
            if (GetCurrentTile != null)
            {
                // Cache current & start position position and compare
                int currentX = (int)GetCurrentTile.tileCoordX;
                int currentZ = (int)GetCurrentTile.tileCoordZ;

                // Reset arrays to blank             
                selectedTiles = new Tile[world.worldSize * world.worldSize];
                selectionBoxTilesToPaint = new Tile[world.worldSize * world.worldSize];
                #region Non-Path/Fence Drag Boxes
                if (GetCurrentTile != null && returnType != "path" && returnType != "fence" && GetCurrentTile != startTile)
                {

                    // If X & Z coordinates both increase from starting position
                    if (currentX > startX && currentZ > startZ)
                    {
                        int i = 0;
                        for (int x = startX; x < currentX + 1; x++)
                        {
                            for (int z = startZ; z < currentZ + 1; z++)
                            {
                                selectedTiles[i] = world.MapData[x, z];
                                selectionBoxTilesToPaint[i] = world.MapData[x, z];
                                i++;
                            }
                        }
                    }
                    // If X coordinate increases while Z decreases
                    else if (currentX > startX && currentZ < startZ)
                    {
                        int i = 0;
                        for (int x = startX; x < currentX + 1; x++)
                        {
                            for (int z = currentZ; z < startZ + 1; z++)
                            {
                                selectedTiles[i] = world.MapData[x, z];
                                selectionBoxTilesToPaint[i] = world.MapData[x, z];
                                i++;
                            }
                        }
                    }
                    // If X coordinate decreases while Z increases
                    else if (currentX < startX && currentZ > startZ)
                    {
                        int i = 0;
                        for (int x = currentX; x < startX + 1; x++)
                        {
                            for (int z = startZ; z < currentZ + 1; z++)
                            {
                                selectedTiles[i] = world.MapData[x, z];
                                selectionBoxTilesToPaint[i] = world.MapData[x, z];
                                i++;

                            }
                        }
                    }
                    // If X & Z coordinates both decrease
                    else if (currentX < startX && currentZ < startZ)
                    {
                        int i = 0;
                        for (int x = currentX; x < startX + 1; x++)
                        {
                            for (int z = currentZ; z < startZ + 1; z++)
                            {

                                selectedTiles[i] = world.MapData[x, z];
                                selectionBoxTilesToPaint[i] = world.MapData[x, z];
                                i++;
                            }
                        }
                    }
                    // If the X coordinate does not change and Z increases
                    else if (currentX == startX && currentZ > startZ)
                    {
                        int i = 0;
                        for (int z = startZ; z < currentZ + 1; z++)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    // If the X coordinate does not change and Z decreases
                    else if (currentX == startX && currentZ < startZ)
                    {
                        int i = 0;
                        for (int z = currentZ; z < startZ + 1; z++)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    // If the Z coordinate does not change and X increases
                    else if (currentX > startX && currentZ == startZ)
                    {
                        int i = 0;
                        for (int x = startX; x < currentX + 1; x++)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    // If the Z coordinate does not change and X decreases
                    else if (currentX < startX && currentZ == startZ)
                    {
                        int i = 0;
                        for (int x = currentX; x < startX + 1; x++)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    //TODO: ADD 1 DECREASE/INCREASE WHILE ONE IS CONSTANT FOR EACH SCENARIO. ALSO FIX EVERYTHING BUT THE FIRST\
                    // ALSO PATH AKA ONLY 1 COORD INCREASES/DECREASES               
                }
                #endregion
                #region Path Drag Boxes
                if (GetCurrentTile != null && returnType == "path" && GetCurrentTile != startTile)
                {
                    startFenceTile = startMouseTile;
                    #region Straight Lines
                    // If the X coordinate does not change and Z increases
                    if (currentX == startX && currentZ > startZ)
                    {
                        int i = 0;
                        for (int z = startZ; z < currentZ + 1; z++)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    // If the X coordinate does not change and Z decreases
                    else if (currentX == startX && currentZ < startZ)
                    {
                        int i = 0;
                        for (int z = currentZ; z < startZ + 1; z++)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    // If the Z coordinate does not change and X increases
                    else if (currentX > startX && currentZ == startZ)
                    {
                        int i = 0;
                        for (int x = startX; x < currentX + 1; x++)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    // If the Z coordinate does not change and X decreases
                    else if (currentX < startX && currentZ == startZ)
                    {
                        int i = 0;
                        for (int x = currentX; x < startX + 1; x++)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    #endregion
                    #region Top Right
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the top right of the current tile
                    else if (currentZ - startZ >= currentX - startX && currentZ > startZ && currentX > startX)
                    {
                        int i = 0;
                        for (int z = startZ; z < currentZ; z++)
                        {
                            selectedTiles[i] = world.MapData[startX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[startX, z];
                            i++;
                        }
                        cornerTile = world.MapData[startX, currentZ];
                        for (int x = startX; x < currentX + 1; x++)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    // If the Z coordinate of the current tile is < the X coordinate, and we are to the top right of the current tile
                    else if (currentZ - startZ < currentX - startX && currentZ > startZ && currentX > startX)
                    {
                        int i = 0;
                        for (int x = startX; x < currentX; x++)
                        {
                            selectedTiles[i] = world.MapData[x, startZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, startZ];
                            i++;
                        }
                        cornerTile = world.MapData[currentX, startZ];
                        for (int z = startZ; z < currentZ + 1; z++)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    #endregion
                    #region Bottom Right
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the bottom right of the current tile
                    else if (startZ - currentZ >= currentX - startX && currentZ < startZ && currentX > startX)
                    {
                        int i = 0;
                        for (int z = startZ; z > currentZ; z--)
                        {
                            selectedTiles[i] = world.MapData[startX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[startX, z];
                            i++;
                        }
                        cornerTile = world.MapData[startX, currentZ];
                        for (int x = startX; x < currentX + 1; x++)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the bottom right of the current tile
                    else if (startZ - currentZ < currentX - startX && currentZ < startZ && currentX > startX)
                    {
                        int i = 0;
                        for (int x = startX; x < currentX; x++)
                        {
                            selectedTiles[i] = world.MapData[x, startZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, startZ];
                            i++;
                        }
                        cornerTile = world.MapData[currentX, startZ];
                        for (int z = startZ; z > currentZ - 1; z--)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    #endregion
                    #region Bottom Left
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the bottom left of the current tile
                    else if (startZ - currentZ >= startX - currentX && currentZ < startZ && currentX < startX)
                    {
                        int i = 0;
                        for (int z = startZ; z > currentZ; z--)
                        {
                            selectedTiles[i] = world.MapData[startX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[startX, z];
                            i++;
                        }
                        cornerTile = world.MapData[startX, currentZ];
                        for (int x = startX; x > currentX - 1; x--)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    // If the Z coordinate of the current tile is < the X coordinate, and we are to the bottom left of the current tile
                    else if (startZ - currentZ < startX - currentX && currentZ < startZ && currentX < startX)
                    {
                        int i = 0;
                        for (int x = startX; x > currentX; x--)
                        {
                            selectedTiles[i] = world.MapData[x, startZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, startZ];
                            i++;
                        }
                        cornerTile = world.MapData[currentX, startZ];
                        for (int z = startZ; z > currentZ - 1; z--)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    #endregion
                    #region Top Left
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the top left of the current tile
                    if (currentZ - startZ >= startX - currentX && currentZ > startZ && currentX < startX)
                    {
                        int i = 0;
                        for (int z = startZ; z < currentZ; z++)
                        {
                            selectedTiles[i] = world.MapData[startX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[startX, z];
                            i++;
                        }
                        cornerTile = world.MapData[startX, currentZ];
                        for (int x = startX; x > currentX - 1; x--)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the top left of the current tile
                    else if (currentZ - startZ < startX - currentX && currentZ > startZ && currentX < startX)
                    {
                        int i = 0;
                        for (int x = startX; x > currentX; x--)
                        {
                            selectedTiles[i] = world.MapData[x, startZ];
                            selectionBoxTilesToPaint[i] = world.MapData[x, startZ];
                            i++;
                        }
                        cornerTile = world.MapData[currentX, startZ];
                        for (int z = startZ; z < currentZ + 1; z++)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            selectionBoxTilesToPaint[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    #endregion
                }
                #endregion
                #region Fence Drag Boxes
                // Need drag select to make a single tile wide line in the shape of an L from the start tile to the current tile, where the
                // orientation of the L depends on the current tile coordinates, with the Z coordinate always taking precedence
                if (GetCurrentTile != null && (returnType == "fence") && GetCurrentTile != startTile)
                {
                    startFenceTile = startMouseTile;
                    #region Straight Lines
                    // If the X coordinate does not change and Z increases
                    if (currentX == startX && currentZ > startZ)
                    {
                        int i = 0;
                        for (int z = startZ; z < currentZ + 1; z++)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    // If the X coordinate does not change and Z decreases
                    else if (currentX == startX && currentZ < startZ)
                    {
                        int i = 0;
                        for (int z = currentZ; z < startZ + 1; z++)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    // If the Z coordinate does not change and X increases
                    else if (currentX > startX && currentZ == startZ)
                    {
                        int i = 0;
                        for (int x = startX; x < currentX + 1; x++)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    // If the Z coordinate does not change and X decreases
                    else if (currentX < startX && currentZ == startZ)
                    {
                        int i = 0;
                        for (int x = currentX; x < startX + 1; x++)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    #endregion
                    #region Top Right
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the top right of the current tile
                    else if (currentZ - startZ >= currentX - startX && currentZ > startZ && currentX > startX)
                    {
                        int i = 0;
                        for (int z = startZ; z < currentZ; z++)
                        {
                            selectedTiles[i] = world.MapData[startX, z];
                            i++;
                        }
                        cornerTile = world.MapData[startX, currentZ];
                        for (int x = startX; x < currentX + 1; x++)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    // If the Z coordinate of the current tile is < the X coordinate, and we are to the top right of the current tile
                    else if (currentZ - startZ < currentX - startX && currentZ > startZ && currentX > startX)
                    {
                        int i = 0;
                        for (int x = startX; x < currentX; x++)
                        {
                            selectedTiles[i] = world.MapData[x, startZ];
                            i++;
                        }
                        cornerTile = world.MapData[currentX, startZ];
                        for (int z = startZ; z < currentZ + 1; z++)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    #endregion
                    #region Bottom Right
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the bottom right of the current tile
                    else if (startZ - currentZ >= currentX - startX && currentZ < startZ && currentX > startX)
                    {
                        int i = 0;
                        for (int z = startZ; z > currentZ; z--)
                        {
                            selectedTiles[i] = world.MapData[startX, z];
                            i++;
                        }
                        cornerTile = world.MapData[startX, currentZ];
                        for (int x = startX; x < currentX + 1; x++)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the bottom right of the current tile
                    else if (startZ - currentZ < currentX - startX && currentZ < startZ && currentX > startX)
                    {
                        int i = 0;
                        for (int x = startX; x < currentX; x++)
                        {
                            selectedTiles[i] = world.MapData[x, startZ];
                            i++;
                        }
                        cornerTile = world.MapData[currentX, startZ];
                        for (int z = startZ; z > currentZ - 1; z--)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    #endregion
                    #region Bottom Left
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the bottom left of the current tile
                    else if (startZ - currentZ >= startX - currentX && currentZ < startZ && currentX < startX)
                    {
                        int i = 0;
                        for (int z = startZ; z > currentZ; z--)
                        {
                            selectedTiles[i] = world.MapData[startX, z];
                            i++;
                        }
                        cornerTile = world.MapData[startX, currentZ];
                        for (int x = startX; x > currentX - 1; x--)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    // If the Z coordinate of the current tile is < the X coordinate, and we are to the bottom left of the current tile
                    else if (startZ - currentZ < startX - currentX && currentZ < startZ && currentX < startX)
                    {
                        int i = 0;
                        for (int x = startX; x > currentX; x--)
                        {
                            selectedTiles[i] = world.MapData[x, startZ];
                            i++;
                        }
                        cornerTile = world.MapData[currentX, startZ];
                        for (int z = startZ; z > currentZ - 1; z--)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    #endregion
                    #region Top Left
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the top left of the current tile
                    if (currentZ - startZ >= startX - currentX && currentZ > startZ && currentX < startX)
                    {
                        int i = 0;
                        for (int z = startZ; z < currentZ; z++)
                        {
                            selectedTiles[i] = world.MapData[startX, z];
                            i++;
                        }
                        cornerTile = world.MapData[startX, currentZ];
                        for (int x = startX; x > currentX - 1; x--)
                        {
                            selectedTiles[i] = world.MapData[x, currentZ];
                            i++;
                        }
                    }
                    // If the Z coordinate of the current tile is >= the X coordinate, and we are to the top left of the current tile
                    else if (currentZ - startZ < startX - currentX && currentZ > startZ && currentX < startX)
                    {
                        int i = 0;
                        for (int x = startX; x > currentX; x--)
                        {
                            selectedTiles[i] = world.MapData[x, startZ];
                            i++;
                        }
                        cornerTile = world.MapData[currentX, startZ];
                        for (int z = startZ; z < currentZ + 1; z++)
                        {
                            selectedTiles[i] = world.MapData[currentX, z];
                            i++;
                        }
                    }
                    #endregion
                }
                #endregion                 
                // If the coordinates stay the same
                if (GetCurrentTile == startTile)
                {
                    //Debug.Log("Single tile selected");
                    selectedTiles[0] = GetCurrentTile;
                    if(returnType != "fence")
                    selectionBoxTilesToPaint[0] = GetCurrentTile;
                }
                if(previousTiles != selectedTiles)
                PaintSelectionTiles(selectionBoxTilesToPaint, overlayTile);
                yield return new WaitForSeconds(.05f);
            }
            //else if(selectedTiles != previousTiles)
            //{
            //    PaintSelectionTiles(selectionBoxTilesToPaint, overlayTile);
            //    yield return new WaitForSeconds(.1f);
            //}
            yield return new WaitForSeconds(.05f);
            PaintSelectionTiles(selectionBoxTilesToPaint, blankTile);
        }
        // Call proper function based calling function
        if (returnType == "paint")
        {
            landscapingTools.PaintTerrain(selectedTiles);
        }
        else if (returnType == "level")
        {
            landscapingTools.ModifyTerrain(selectedTiles, startTile.height);
        }
        else if (returnType == "path" || returnType == "fence")
        {
            cornerTile = null;
        }
        else if(returnType == "purchaseLand")
        {
            zooInfo.PurchaseLand(selectedTiles);
        }
        PaintSelectionTiles(selectionBoxTilesToPaint, blankTile);
        previousTiles = selectedTiles;
        yield break;

    }

    // Used to show single tile selection overlay on tiles & determine current vertex for single-vertex height changes
    public IEnumerator SingleVertexSelection()
    {
        int currentX = (int)GetCurrentTile.tileCoordX;
        int currentZ = (int)GetCurrentTile.tileCoordZ;
        int lowerLeftSelect = 2;
        int upperLeftSelect = 3;
        int upperRightSelect = 4;
        int lowerRightSelect = 5;
        int pausedUpdateSelect = 0;

        if (selectionBoxTilesToPaint != null)
        {
            if (selectionBoxTilesToPaint[0] != GetCurrentTile || GetCurrentTile == null)
            {
                PaintSelectionTiles(selectionBoxTilesToPaint, 0);
            }
        }        
        //if (pauseVertexSelect)
        //{
        //    PaintSelectionTiles(selectionBoxTilesToPaint, pausedUpdateSelect);
        //}
        if (!pauseVertexSelect)
        {
            // Reset arrays to blank
            selectedTiles = new Tile[world.worldSize * world.worldSize];
            selectionBoxTilesToPaint = new Tile[world.worldSize * world.worldSize];

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            // Grab the coordinates of the tile moused over, and adjust it to the current tile size (Likely always 1 for Zoo.)
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity) && !pauseVertexSelect)
            {
                float x = (hitInfo.point.x / world.tileSize);
                float y = (hitInfo.point.y / world.tileSize);
                float z = (hitInfo.point.z / world.tileSize);
                //Debug.Log("Coordinates in world space = (" + x + "," + y + "," + z + ")");
                //Debug.Log("Coordinates in tile space = (" + GetCurrentTile.tileCoordX + "," + GetCurrentTile.height + "," + GetCurrentTile.tileCoordZ + ")");
                // Determine which vertex is currently selected by checking mouse position relative to the tile
                if (x - currentX < .50 && z - currentZ < .50)
                {
                    selectedVertex = GetCurrentTile.lowerLeft;
                    selectionBoxTilesToPaint[0] = GetCurrentTile;
                    selectedTiles[0] = GetCurrentTile;
                    if (previousTiles != selectionBoxTilesToPaint)
                    {
                        PaintSelectionTiles(selectionBoxTilesToPaint, lowerLeftSelect);
                    }
                    vertexType = "lowerLeft";
                    pausedUpdateSelect = lowerLeftSelect;
                }
                else if (x - currentX < .50 && z - currentZ > .50)
                {
                    selectedVertex = GetCurrentTile.upperLeft;
                    selectionBoxTilesToPaint[0] = GetCurrentTile;
                    selectedTiles[0] = GetCurrentTile;
                    if (previousTiles != selectionBoxTilesToPaint)
                    {
                        PaintSelectionTiles(selectionBoxTilesToPaint, upperLeftSelect);
                    }
                    vertexType = "upperLeft";
                    pausedUpdateSelect = upperLeftSelect;

                }
                else if (x - currentX > .50 && z - currentZ > .50)
                {
                    selectedVertex = GetCurrentTile.upperRight;
                    selectionBoxTilesToPaint[0] = GetCurrentTile;
                    selectedTiles[0] = GetCurrentTile;
                    if (previousTiles != selectionBoxTilesToPaint)
                    {
                        PaintSelectionTiles(selectionBoxTilesToPaint, upperRightSelect);
                    }
                    vertexType = "upperRight";
                    pausedUpdateSelect = upperRightSelect;
                }
                else if (x - currentX > .50 && z - currentZ < .50)
                {
                    selectedVertex = GetCurrentTile.lowerRight;
                    selectionBoxTilesToPaint[0] = GetCurrentTile;
                    selectedTiles[0] = GetCurrentTile;
                    if (previousTiles != selectionBoxTilesToPaint)
                    {
                        PaintSelectionTiles(selectionBoxTilesToPaint, lowerRightSelect);
                    }
                    vertexType = "lowerRight";
                    pausedUpdateSelect = lowerRightSelect;
                }
                //PaintSelectionTiles(selectionBoxTilesToPaint, 0);
            }
            if (GetCurrentTile == null)
            {
                PaintSelectionTiles(selectionBoxTilesToPaint, 0);
            }
        }
        previousTiles = selectionBoxTilesToPaint;
        yield return new WaitForSeconds(.15f);
    }

    public IEnumerator SwathTileSelection()
    {
        int blankTile = 0;  // Default selectionBoxMesh texture -- 100% transparent
        int overlayTile = 1; // Selection box selection texture -- make sure to set back to blank once done using!
        int currentX = (int)GetCurrentTile.tileCoordX;
        int currentZ = (int)GetCurrentTile.tileCoordZ;

        if (selectionBoxTilesToPaint != null)
        {
            PaintSelectionTiles(selectionBoxTilesToPaint, 0);
        }
        if (!pauseVertexSelect && GetCurrentTile != null)
        {
            // Reset arrays to blank
            selectedTiles = new Tile[world.worldSize * world.worldSize];
            selectionBoxTilesToPaint = new Tile[world.worldSize * world.worldSize];

            if (swathSize == 1)
            {
                selectedTiles[0] = GetCurrentTile;
                selectionBoxTilesToPaint[0] = GetCurrentTile;
                if (previousTiles != selectionBoxTilesToPaint)
                    PaintSelectionTiles(selectionBoxTilesToPaint, overlayTile);
            }
            if(swathSize > 1)
            {
                int i = 0;
                for (int x = currentX; x < currentX + swathSize; x++)
                {
                    for(int z = currentZ; z < currentZ + swathSize; z++)
                    {
                        if (world.MapData[x, z] != null)
                        {
                            selectedTiles[i] = world.MapData[x, z];
                            selectionBoxTilesToPaint[i] = world.MapData[x, z];
                        }
                        i++;
                    }              
                }
                if (previousTiles != selectionBoxTilesToPaint)
                {
                    PaintSelectionTiles(selectionBoxTilesToPaint, overlayTile);
                }
            }
        }
        if (GetCurrentTile == null)
        {
            PaintSelectionTiles(selectionBoxTilesToPaint, 0);
        }
        previousTiles = selectedTiles;
        yield return new WaitForSeconds(.05f);
    }

    void GetSelectedTile()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        LayerMask layerMask = 1 << 11;
        layerMask |= 1 << 12;
        layerMask = ~layerMask;
        // Grab the coordinates of the tile moused over, and adjust it to the current tile size (Likely always 1 for Zoo.)
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask))
        {
            int x = Mathf.FloorToInt(hitInfo.point.x / world.tileSize);
            int y = Mathf.FloorToInt(hitInfo.point.y / world.tileSize);
            int z = Mathf.FloorToInt(hitInfo.point.z / world.tileSize);
            if (world.GetTile(x, z) != null)
            {
                selectedTile = world.GetTile(x, z);
            }
            if (Input.GetMouseButtonDown(0) && selectedTile != null)
            {
                Debug.Log("Selecting Tile (" + x + "," + selectedTile.tileHeight + "," + z + ") of type " + selectedTile.tileType);
            }
        }
        else
        {
            selectedTile = null;
            //if (Input.GetMouseButtonDown(0))
            //{
            //    //Debug.Log("No tile detected!");
            //}
        }
    }

    void GetSelectedSelectionTile()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        LayerMask layerMask = 1 << 5;
        layerMask = ~layerMask;
        // Grab the coordinates of the selection box tile moused over, and adjust it to the current tile size (Likely always 1 for Zoo.)
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask))
        {
            int x = Mathf.FloorToInt(hitInfo.point.x / world.tileSize);
            int y = Mathf.FloorToInt(hitInfo.point.y / world.tileSize);
            int z = Mathf.FloorToInt(hitInfo.point.z / world.tileSize);
            selectedSelectionTile = world.GetTile(x, z);
            //if (Input.GetMouseButtonDown(0))
                //Debug.Log("Selecting selection box Tile (" + x + "," + y + "," + z + ") of type " + selectedSelectionTile.tileType);
        }
        else
        {
            selectedSelectionTile = null;
            //if (Input.GetMouseButtonDown(0))
            //{
            //    Debug.Log("No selection box tile detected!");
            //}
        }
    }

    void GetSelectedTileCorner()
    {
        int currentX = (int)GetCurrentTile.tileCoordX;
        int currentZ = (int)GetCurrentTile.tileCoordZ;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        LayerMask layerMask = 1 << 11;
        layerMask |= 1 << 12;
        layerMask = ~layerMask;
        // Grab the coordinates of the tile moused over, and adjust it to the current tile size (Likely always 1 for Zoo.)
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask))
        {
            float x = (hitInfo.point.x / world.tileSize);
            float y = (hitInfo.point.y / world.tileSize);
            float z = (hitInfo.point.z / world.tileSize);
            //Debug.Log("Coordinates in world space = (" + x + "," + y + "," + z + ")");
            //Debug.Log("Coordinates in tile space = (" + GetCurrentTile.tileCoordX + "," + GetCurrentTile.height + "," + GetCurrentTile.tileCoordZ + ")");
            // Determine which vertex is currently selected by checking mouse position relative to the tile
            if (x - currentX < .50 && z - currentZ < .50)
            {
                vertexType = "lowerLeft";
            }
            else if (x - currentX < .50 && z - currentZ > .50)
            {
                vertexType = "upperLeft";
            }
            else if (x - currentX > .50 && z - currentZ > .50)
            {
                vertexType = "upperRight";
            }
            else if (x - currentX > .50 && z - currentZ < .50)
            {

                vertexType = "lowerRight";
            }
        }
    }

    void GetSelectedTileSide()
    {
        int currentX = (int)GetCurrentTile.tileCoordX;
        int currentZ = (int)GetCurrentTile.tileCoordZ;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        LayerMask layerMask = 1 << 11;
        layerMask |= 1 << 12;
        layerMask = ~layerMask;
        Vector3 tileCenter = new Vector3(currentX + .5f, GetCurrentTile.tileHeight, currentZ + .5f);
        // Grab the coordinates of the tile moused over, and adjust it to the current tile size (Likely always 1 for Zoo.)
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask))
        {
            float x = (hitInfo.point.x / world.tileSize);
            float y = (hitInfo.point.y / world.tileSize);
            float z = (hitInfo.point.z / world.tileSize);
            Vector3 direction = new Vector3(x, y, z);
            direction -= tileCenter;
            if(Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
            {
                direction.z = 0;
            }
            else
            {
                direction.x = 0;
            }
            direction.y = 0f;
            direction.Normalize();
            // Set tile side
            // If North side
            if (direction.z == 1)
            {
                selectedTileSide = "north";
            }
            // If East side
            else if (direction.x == 1)
            {
                selectedTileSide = "east";
            }
            // If South side
            else if (direction.z == -1)
            {
                selectedTileSide = "south";
            }
            // If West side
            else if (direction.x == -1)
            {
                selectedTileSide = "west";
            }
        }
    }

    void CheckIfOverUI()
    {
        if(EventSystem.current.IsPointerOverGameObject())
        {
            overUI = true;
        }
        else
        {
            overUI = false;
        }
    }

    public GameObject GetBuildableObjectAtMouse()
    {
        // Check if the mouse is currently over a buildable object
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        LayerMask layerMask = 1 << 12;
        // Grab that object if there
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask))
        {
            return hitInfo.collider.gameObject;
        }
        else
        {
            return null;
        }
    }

    void PaintSelectionTiles(Tile[] overlayTiles, int selectionTileType)
    {
        int newSelectionTileType = selectionTileType;
        selectionChunks = world.selectionChunks;
        // Create array to story dirty chunks to update at end of frame
        Chunk[,] dirtyChunks = new Chunk[selectionChunks.Length, selectionChunks.Length];
        for (int i = 0; i < overlayTiles.Length; i++)
        {
            if (overlayTiles[i] != null)
            {
                dirtyChunks[(int)overlayTiles[i].tileCoordX / world.chunkSize, (int)overlayTiles[i].tileCoordZ / world.chunkSize] = selectionChunks[(int)overlayTiles[i].tileCoordX / world.chunkSize, (int)overlayTiles[i].tileCoordZ / world.chunkSize];
                overlayTiles[i].selectionTileType = newSelectionTileType;
                //Debug.Log("Attempting to paint selection tile " + overlayTiles[i].tileCoordX + "," + overlayTiles[i].tileCoordZ + "," + newSelectionTileType);
            }
        }
        foreach (Chunk chunk in dirtyChunks)
        {
            if (chunk != null)
            {
                chunk.DrawTiles(world.MapData, true);
            }
        }
    }

    public Tile[] GetAdjacentTiles(Tile tile)
    {
        Tile[] neighborTiles = new Tile[8];
        int centerX = (int)tile.tileCoordX;
        int centerZ = (int)tile.tileCoordZ;
        // Set up S tile
        if (centerZ > 0)
        {
            if(world.MapData[centerX, centerZ - 1] != null)
            neighborTiles[0] = world.MapData[centerX, centerZ - 1];
        }
        else
        {
            neighborTiles[0] = null;
        }
        // If both tile coordinates are greater than 0, set up the SW tile
        if (centerZ > 0 && centerX > 0)
        {
            if(world.MapData[centerX - 1, centerZ - 1] != null)
            neighborTiles[1] = world.MapData[centerX - 1, centerZ - 1];
        }
        else
        {
            neighborTiles[1] = null;
        }
        // If the tile X coordinate is greater than 0, set up the W tile
        if (centerX > 0)
        {
            if(world.MapData[centerX - 1, centerZ] != null)
            neighborTiles[2] = world.MapData[centerX - 1, centerZ];
        }
        else
        {
            neighborTiles[2] = null;
        }
        // If the tile Z coordinate is less than world size and tile X is greater than 0, set up the NW tile
        if (centerZ < world.worldSize && centerX > 0)
        {
            if(world.MapData[centerX - 1, centerZ + 1] != null)
            neighborTiles[3] = world.MapData[centerX - 1, centerZ + 1];
        }
        else
        {
            neighborTiles[3] = null;
        }
        // If the tile Z coordinate is less than world size, set up the N tile
        if (centerZ < world.worldSize)
        {
            if(world.MapData[centerX, centerZ + 1] != null)
            neighborTiles[4] = world.MapData[centerX, centerZ + 1];
        }
        else
        {
            neighborTiles[4] = null;
        }
        // If both tiles are less than world size, set up the NE tile
        if (centerZ < world.worldSize && centerX < world.worldSize)
        {
            if(world.MapData[centerX + 1, centerZ + 1] != null)
            neighborTiles[5] = world.MapData[centerX + 1, centerZ + 1];
        }
        else
        {
            neighborTiles[5] = null;
        }
        // If the tile X coordinate is less than world size, set up the E tile
        if (centerX < world.worldSize)
        {
            if(world.MapData[centerX + 1, centerZ] != null)
            neighborTiles[6] = world.MapData[centerX + 1, centerZ];
        }
        else
        {
            neighborTiles[6] = null;
        }
        // If the tile X coordinate is less than world size and the tile Z coordinate is greater than 0, set up the SE tile
        if (centerZ > 0 && centerX < world.worldSize)
        {
            if(world.MapData[centerX + 1, centerZ - 1] != null)
            neighborTiles[7] = world.MapData[centerX + 1, centerZ - 1];
        }
        else
        {
            neighborTiles[7] = null;
        }
        return neighborTiles;
    }
}
