using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LandscapingTools : MonoBehaviour {

    int newTileType = 0;
    int newCliffType = 1;
    Tile currentTile;
    Tile[] selectedTiles;

    List<Tile> updatedTiles;
    List<Tile> tilesToUpdate;
    //Chunk[] selectedChunks;
    Chunk[,] chunks;
    Chunk[,] dirtyChunks;
    bool landscapingIsActivated = false;
    bool cliffMode = false;
    string currentAction = "swath";
    float currentSwathSize;
    float heightScale;
    public Slider swathSlider;
    public Text swathText;

    public ConstructionTools constructionTools;
    public TileMapMouseInterface mouseInterface;
    public World world;

    // Use this for initialization
    void Awake()
    {
        updatedTiles = new List<Tile>(world.worldSize * world.worldSize);
        tilesToUpdate = new List<Tile>(world.worldSize * world.worldSize);
        currentSwathSize = swathSlider.value;
        heightScale = world.GetHeightScale;
    }
    // Update is called once per frame
    void Update()
    {
        if(currentSwathSize != swathSlider.value)
        UpdateSwathSliderText(swathSlider.value);

        if(world.chunks != null && chunks == null)
        {
            chunks = world.chunks;
            dirtyChunks = new Chunk[chunks.Length, chunks.Length];
        }

        // Needs to be updated to have separate if statements for painting terrain, levelling terrain, and raising/lowering

        currentTile = mouseInterface.GetCurrentTile;
        if (landscapingIsActivated && !mouseInterface.IsOverUI)
        {
            // If we are currently in terrain paint mode...
            if (Input.GetMouseButtonDown(0) && currentTile != null && currentAction == "paint")
            {
                mouseInterface.StartCoroutine(mouseInterface.DragBox(currentTile, "paint"));
            }
            else
            {
                mouseInterface.StopCoroutine(mouseInterface.DragBox(currentTile, "paint"));
            }
            // If we are in level terrain mode...
            if (Input.GetMouseButtonDown(0) && currentTile != null && currentAction == "level")
            {
                mouseInterface.StartCoroutine(mouseInterface.DragBox(currentTile, "level"));
            }
            else
            {
                mouseInterface.StopCoroutine(mouseInterface.DragBox(currentTile, "level"));
            }
            // If we are in single vertex modification mode 
            if (currentTile != null && currentAction == "swath" && swathSlider.value == 0)
            {
                mouseInterface.StartCoroutine(mouseInterface.SingleVertexSelection());
                if (Input.GetMouseButtonDown(0))
                {
                    StartCoroutine(ModifyVertexHeight());
                }
            }
            else
            {
                mouseInterface.StopCoroutine(mouseInterface.SingleVertexSelection());
            }
            // If we are in swath tile modififcation mode (1x1, 2x2, 3x3, etc)
            if (currentTile != null && currentAction == "swath" && swathSlider.value > 0)
            {
                mouseInterface.StartCoroutine(mouseInterface.SwathTileSelection());
                if (Input.GetMouseButtonDown(0))
                {
                    StartCoroutine(ModifyVertexHeight());
                }
            }
            else
            {
                mouseInterface.StopCoroutine(mouseInterface.SwathTileSelection());
            }
            if (!landscapingIsActivated)
            {

            }
        }
    }

    public void SetNewTileType(int newType)
    {
        newTileType = newType;
    }
    public void SetNewCliffType(int newCliff)
    {
        newCliffType = newCliff;
    }

    public void ActivateLandscaping()
    {
        if (!landscapingIsActivated)
            landscapingIsActivated = true;
        else
            landscapingIsActivated = false;
    }
    public Tile[] SelectedTiles
    {
        set { selectedTiles = value; }
    }

    public void ToggleCliffMode()
    {
        if (!cliffMode)
        {
            cliffMode = true;
        }
        else
        {
            cliffMode = false;
        }
    }

    public void PaintTerrain(Tile[] selectedTiles)
    {
        chunks = world.chunks;
        this.selectedTiles = selectedTiles;
        // Create array to story dirty chunks to update at end of frame
        Chunk[,] dirtyChunks = new Chunk[chunks.Length, chunks.Length];
        foreach (Tile tile in selectedTiles)
        {
            if (tile != null)
            {
                // Add each 'dirty' chunk to the array based on which tiles are being modified
                dirtyChunks[(int)tile.tileCoordX / world.chunkSize, (int)tile.tileCoordZ / world.chunkSize] = chunks[(int)tile.tileCoordX / world.chunkSize, (int)tile.tileCoordZ / world.chunkSize];
                tile.tileType = newTileType;
                tile.cliffType = newCliffType;
                //Debug.Log("Attempting to paint tile " + tile.tileCoordX + "," + tile.tileCoordZ + " " + newTileType);
            }
        }

        foreach (Chunk chunk in dirtyChunks)
        {
            if (chunk != null)
            {
                chunk.DrawTiles(world.MapData, false);
            }
        }
    }

    public void ModifyTerrain(Tile[] selectedTiles, float height, string vertex = null)
    {
        float newHeight = height * heightScale;
        chunks = world.chunks;
        // Create array to story dirty chunks to update at end of frame
        if (currentAction == "level")
        {
            foreach (Tile tile in selectedTiles)
            {
                if (tile != null)
                {
                    dirtyChunks[(int)tile.tileCoordX / world.chunkSize, (int)tile.tileCoordZ / world.chunkSize] = chunks[(int)tile.tileCoordX / world.chunkSize, (int)tile.tileCoordZ / world.chunkSize];
                    tile.height = newHeight/heightScale;
                    tile.recalculate();
                    tile.ReSetStats();
                    tile.HasBeenUpdated = true;
                    //updatedTiles.Add(tile);
                }
            }
        }
        if (currentAction == "swath" && swathSlider.value > 0)
        {
            List<float> tileHeights = new List<float>();
            float minHeight = 0f;
            float maxHeight = 0f;
            foreach (Tile t in selectedTiles)
            {
                if(t != null)
                {
                    tileHeights.Add(t.tileHeight);
                }
            }
            tileHeights.Sort();
            float[] tileHeightSorted = tileHeights.ToArray();
            minHeight = tileHeights[0];
            if(tileHeights.Count != 0)
            maxHeight = tileHeights[tileHeights.Count - 1];
            Debug.Log(tileHeightSorted[0]);
            foreach (Tile tile in selectedTiles)
            {
                if (tile != null)
                {
                    if(newHeight > 0 || newHeight == 0)
                    {
                        if(tile.tileHeight == minHeight || (tile.tileHeight == minHeight - .5f && tile.isSlope))
                        {
                            // Add each 'dirty' chunk to the array based on which tiles are being modified
                            dirtyChunks[(int)tile.tileCoordX / world.chunkSize, (int)tile.tileCoordZ / world.chunkSize] = chunks[(int)tile.tileCoordX / world.chunkSize, (int)tile.tileCoordZ / world.chunkSize];
                            tile.height = tile.height + (newHeight);
                            tile.recalculate();
                            tile.ReSetStats();
                            tile.HasBeenUpdated = true;
                            //updatedTiles.Add(tile);
                            //Debug.Log("Attempting to paint tile " + tile.tileCoordX + "," + tile.tileCoordZ + " " + newTileType);
                        }
                    }
                    if(newHeight < 0)
                    {
                        if (tile.tileHeight == maxHeight || (tile.tileHeight == maxHeight + .5f && tile.isSlope))
                        {
                            // Add each 'dirty' chunk to the array based on which tiles are being modified
                            dirtyChunks[(int)tile.tileCoordX / world.chunkSize, (int)tile.tileCoordZ / world.chunkSize] = chunks[(int)tile.tileCoordX / world.chunkSize, (int)tile.tileCoordZ / world.chunkSize];
                            tile.height = tile.height + (newHeight);
                            tile.recalculate();
                            tile.ReSetStats();
                            tile.HasBeenUpdated = true;
                            //updatedTiles.Add(tile);
                            //Debug.Log("Attempting to paint tile " + tile.tileCoordX + "," + tile.tileCoordZ + " " + newTileType);
                        }
                    }
                }
            }
        }
        foreach (Tile tile in selectedTiles)
        {
            #region Vertices
            if (tile != null && currentAction == "swath" && swathSlider.value == 0)
            {
                // TODO: HANDLE DIFFERENT SITUATIONS FOR EACH VERTEX -- I SUSPECT THIS IS WHERE THE ISSUES LIE
                // Add each 'dirty' chunk to the array based on which tiles are being modified
                dirtyChunks[(int)tile.tileCoordX / world.chunkSize, (int)tile.tileCoordZ / world.chunkSize] = chunks[(int)tile.tileCoordX / world.chunkSize, (int)tile.tileCoordZ / world.chunkSize];
                if (vertex == "lowerLeft")
                {
                    tile.lowerLeft.y = tile.lowerLeft.y + (newHeight);
                    if (tile.upperLeft.y < tile.lowerLeft.y - heightScale || tile.upperLeft.y > tile.lowerLeft.y + heightScale)
                    {
                        tile.upperLeft.y = tile.upperLeft.y + (newHeight);
                    }
                    if (tile.lowerRight.y < tile.lowerLeft.y - heightScale || tile.lowerRight.y > tile.lowerLeft.y + heightScale)
                    {
                        tile.lowerRight.y = tile.lowerRight.y + (newHeight);
                    }
                    if (tile.upperRight.y < tile.lowerLeft.y - (2 * heightScale) || tile.upperRight.y > tile.lowerLeft.y + (2 * heightScale))
                    {
                        tile.upperRight.y = tile.upperRight.y + (newHeight);
                    }
                    tile.ReSetStats();
                }
                else if (vertex == "upperLeft")
                {
                    tile.upperLeft.y = tile.upperLeft.y + (newHeight);
                    if (tile.lowerLeft.y < tile.upperLeft.y - heightScale || tile.lowerLeft.y > tile.upperLeft.y + heightScale)
                    {
                        tile.lowerLeft.y = tile.lowerLeft.y + (newHeight);
                    }
                    if (tile.upperRight.y < tile.upperLeft.y - heightScale || tile.upperRight.y > tile.upperLeft.y + heightScale)
                    {
                        tile.upperRight.y = tile.upperRight.y + (newHeight);
                    }
                    if (tile.lowerRight.y < tile.upperLeft.y - (2 * heightScale) || tile.lowerRight.y > tile.upperLeft.y + (2 * heightScale))
                    {
                        tile.lowerRight.y = tile.lowerRight.y + (newHeight);
                    }
                    tile.ReSetStats();
                }
                else if (vertex == "upperRight")
                {
                    tile.upperRight.y = tile.upperRight.y + (newHeight);
                    if (tile.lowerRight.y < tile.upperRight.y - heightScale || tile.lowerRight.y > tile.upperRight.y + heightScale)
                    {
                        tile.lowerRight.y = tile.lowerRight.y + (newHeight);
                    }
                    if (tile.upperLeft.y < tile.upperRight.y - heightScale || tile.upperLeft.y > tile.upperRight.y + heightScale)
                    {
                        tile.upperLeft.y = tile.upperLeft.y + (newHeight);
                    }
                    if (tile.lowerLeft.y < tile.upperRight.y - (2 * heightScale) || tile.lowerLeft.y > tile.upperRight.y + (2 * heightScale))
                    {
                        tile.lowerLeft.y = tile.lowerLeft.y + (newHeight);
                    }
                    tile.ReSetStats();
                }
                else if (vertex == "lowerRight")
                {
                    tile.lowerRight.y = tile.lowerRight.y + (newHeight);
                    if (tile.lowerLeft.y < tile.lowerRight.y - heightScale || tile.lowerLeft.y > tile.lowerRight.y + heightScale)
                    {
                        tile.lowerLeft.y = tile.lowerLeft.y + (newHeight);
                    }
                    if (tile.upperRight.y < tile.lowerRight.y - heightScale || tile.upperRight.y > tile.lowerRight.y + heightScale)
                    {
                        tile.upperRight.y = tile.upperRight.y + (newHeight);
                    }
                    if (tile.upperLeft.y < tile.lowerRight.y - (2 * heightScale) || tile.upperLeft.y > tile.lowerRight.y + (2 * heightScale))
                    {
                        tile.upperLeft.y = tile.upperLeft.y + (newHeight);
                    }
                    tile.ReSetStats();
                }
                //Debug.Log("Attempting to paint tile " + tile.tileCoordX + "," + tile.tileCoordZ + " " + newTileType);
            }
            #endregion
            // TO DO: UPDATE ADJACENT TILES TO MATCH HEIGHT UNLESS IN CLIFF MODE
            if (!cliffMode && tile != null && swathSlider.value != 0)
            {
                if (currentAction == "level")
                {
                    UpdateAdjacentTileHeight(tile);
                }
                else
                {
                    UpdateAdjacentTileHeight(tile);
                }
            }
            else
            {
                if (tile != null)
                    foreach (BuildableObject obj in tile.Objects)
                    {
                        if (obj != null)
                        {
                            if (obj.GetComponent<Path>() != null)
                            {
                                constructionTools.UpdatePath(tile, obj.GetComponent<Path>());
                            }
                            if(obj.GetComponent<Fence>() != null)
                            {
                                tile.ReSetStats();
                                constructionTools.UpdateFence(tile, obj.GetComponent<Fence>());
                            }
                        }                        
                    }
            }
            if (tile != null)
                tile.ReSetStats();
        }

        //if(cliffMode  || currentAction == "singleVertex")
        foreach (Chunk chunk in dirtyChunks)
        {
            if (chunk != null)
            {
                chunk.DrawTiles(world.MapData, false);
            }
        }
    }

    public string CurrentAction
    {
        get { return currentAction; }
        set { currentAction = value; }
    }

    public bool CliffMode
    {
        get { return cliffMode; }
        set { cliffMode = value; }
    }

    public IEnumerator ModifyVertexHeight()
    {
        mouseInterface.VertexSelectOnOff = true;
        Tile curTile = mouseInterface.GetCurrentTile;
        Tile[] tiles = mouseInterface.GetSelectedTiles;
        float newHeight = 0;
        float increment = .05f;
        float yStart = Camera.main.ScreenToViewportPoint(Input.mousePosition).y;
        string currentVertexType = mouseInterface.GetCurrentVertexType;
        while (Input.GetMouseButton(0))
        {
            float newY = Camera.main.ScreenToViewportPoint(Input.mousePosition).y;
            //Debug.Log("Start height = " + yStart + " and new height = " + newY);

            if (newY - yStart > increment)
            {
                newHeight = 1;
                if (curTile.isSlope)
                {
                    newHeight -= 1;
                }
                ModifyTerrain(tiles, newHeight, currentVertexType);
                yStart = yStart + increment;
            }
            if (yStart - newY > increment)
            {
                newHeight = -1;
                ModifyTerrain(tiles, newHeight, currentVertexType);
                yStart = yStart - increment;
            }

            yield return new WaitForEndOfFrame();

        }
        mouseInterface.VertexSelectOnOff = false;
        if (Input.GetMouseButtonUp(0))
        {
            yield break;
        }
    }        

    void UpdateAdjacentTileHeight(Tile curTile)
    {
        // ONLY NORTH AND WEST IS MESSED UP -- WHY??
        Tile tile = curTile;
        //Tile[] recalcTiles;
        // Update the vertices of neighboring tiles of the initial tile
        UpdateTile(curTile);
        //Debug.Log(tilesToUpdate.Count + "," + updatedTiles.Count);
        if (tilesToUpdate.Count != 0)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
                //Debug.Log(tilesToUpdate.Count + "," + updatedTiles.Count);
            }
        }

        #region Repeat hell...
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }
        if (tilesToUpdate.Count > updatedTiles.Count)
        {
            Tile[] currentList = tilesToUpdate.ToArray();
            foreach (Tile t in currentList)
            {
                if (!t.HasBeenUpdated)
                {
                    UpdateTile(t);
                    t.HasBeenUpdated = true;
                }
            }
        }

        #endregion
        foreach (Tile t in updatedTiles)
        {
            t.HasBeenUpdated = false;
            dirtyChunks[(int)t.tileCoordX / world.chunkSize, (int)t.tileCoordZ / world.chunkSize] = chunks[(int)t.tileCoordX / world.chunkSize, (int)t.tileCoordZ / world.chunkSize];

        }
        foreach(Tile t in tilesToUpdate)
        {
            if (!updatedTiles.Contains(t))
            {
                t.HasBeenUpdated = false;
            }
        }
        updatedTiles.Clear();
        tilesToUpdate.Clear();
    }

    void UpdateTile(Tile tile)
    {
        //TO DO -- It appears adding +1 to the vertices is causing issues. 
        Tile[] nearTiles = null;
        Tile[] recalcTiles = new Tile[8];
        Tile near = null;
        bool recalc = false;
        nearTiles = mouseInterface.GetAdjacentTiles(tile);
        #region South Tile
        if (nearTiles[0] != null && nearTiles[0].HasBeenUpdated != true) // S tile
        {
            recalc = false;
            near = nearTiles[0];
            near.upperLeft.y = tile.lowerLeft.y;
            near.upperRight.y = tile.lowerRight.y;
            if (near.lowerRight.y < near.upperRight.y - heightScale)
            {
                near.lowerRight.y = near.upperRight.y - heightScale;
                recalc = true;
            }
            if(near.lowerRight.y > near.upperRight.y + heightScale)
            {
                near.lowerRight.y = near.upperRight.y + heightScale;
                recalc = true;
            }
            if (near.lowerLeft.y < near.upperLeft.y - heightScale)
            {
                near.lowerLeft.y = near.upperLeft.y - heightScale;
                recalc = true;
            }
            if (near.lowerLeft.y > near.upperLeft.y + heightScale)
            {
                near.lowerLeft.y = near.upperLeft.y + heightScale;
                recalc = true;
            }
            if (recalc)
            {
                recalcTiles[0] = near;
            }
            else
            {
                recalcTiles[0] = null;
            }
        }
        #endregion
        #region Southwest Tile
        if (nearTiles[1] != null && nearTiles[1].HasBeenUpdated != true) // SW tile
        {
            recalc = false;
            near = nearTiles[1];
            near.upperRight.y = tile.lowerLeft.y;
            if (near.lowerRight.y < near.upperRight.y - heightScale)
            {
                near.lowerRight.y = near.upperRight.y - heightScale;
                recalc = true;
            }
            if(near.lowerRight.y > near.upperRight.y + heightScale)
            {
                near.lowerRight.y = near.upperRight.y + heightScale;
                recalc = true;
            }
            if (near.upperLeft.y < near.upperRight.y - heightScale)
            {
                near.upperLeft.y = near.upperRight.y - heightScale;
                recalc = true;
            }
            if(near.upperLeft.y > near.upperRight.y + heightScale)
            {
                near.upperLeft.y = near.upperRight.y + heightScale;
                recalc = true;
            }
            if (near.lowerLeft.y < near.upperRight.y - (2 * heightScale))
            {
                near.lowerLeft.y = near.upperRight.y - (2 * heightScale);
                recalc = true;
            }
            if(near.lowerLeft.y > near.upperRight.y + (2 * heightScale))
            {
                near.lowerLeft.y = near.upperRight.y + (2 * heightScale);
                recalc = true;
            }
            if (recalc)
            {
                recalcTiles[1] = near;
            }
            else
            {
                recalcTiles[1] = null;
            }
        }
        #endregion
        #region West Tile
        if (nearTiles[2] != null && nearTiles[2].HasBeenUpdated != true) // W tile
        {
            recalc = false;
            near = nearTiles[2];
            near.lowerRight.y = tile.lowerLeft.y;
            near.upperRight.y = tile.upperLeft.y;
            if (near.upperLeft.y < near.upperRight.y - heightScale)
            {
                near.upperLeft.y = near.upperRight.y - heightScale;
                recalc = true;
            }
            if(near.upperLeft.y > near.upperRight.y + heightScale)
            {
                near.upperLeft.y = near.upperRight.y + heightScale;
                recalc = true;
            }
            if (near.lowerLeft.y < near.lowerRight.y - heightScale)
            {
                near.lowerLeft.y = near.lowerRight.y - heightScale;
                recalc = true;
            }
            if(near.lowerLeft.y > near.lowerRight.y + heightScale)
            {
                near.lowerLeft.y = near.lowerRight.y + heightScale;
                recalc = true;
            }
            if (recalc)
            {
                recalcTiles[2] = near;
            }
            else
            {
                recalcTiles[2] = null;
            }
        }
        #endregion
        #region Northwest Tile
        if (nearTiles[3] != null && nearTiles[3].HasBeenUpdated != true) // NW tile
        {
            recalc = false;
            near = nearTiles[3];
            near.lowerRight.y = tile.upperLeft.y;
            if (near.upperRight.y < near.lowerRight.y - heightScale)
            {
                near.upperRight.y = near.lowerRight.y - heightScale;
                recalc = true;
            }
            if(near.upperRight.y > near.lowerRight.y + heightScale)
            {
                near.upperRight.y = near.lowerRight.y + heightScale;
                recalc = true;
            }
            if (near.lowerLeft.y < near.lowerRight.y - heightScale)
            {
                near.lowerLeft.y = near.lowerRight.y - heightScale;
                recalc = true;
            }
            if(near.lowerLeft.y > near.lowerRight.y + heightScale)
            {
                near.lowerLeft.y = near.lowerRight.y + heightScale;
                recalc = true;
            }
            if (near.upperLeft.y < near.lowerRight.y - (2 * heightScale))
            {
                near.upperLeft.y = near.lowerRight.y - (2 * heightScale);
                recalc = true;
            }
            if(near.upperLeft.y > near.lowerRight.y + (2 * heightScale))
            {
                near.upperLeft.y = near.lowerRight.y + (2 * heightScale);
                recalc = true;
            }
            if (recalc)
            {
                recalcTiles[3] = near;
            }
            else
            {
                recalcTiles[3] = null;
            }
        }
        #endregion
        #region North Tile
        if (nearTiles[4] != null && nearTiles[4].HasBeenUpdated != true) // N tile
        {
            recalc = false;
            near = nearTiles[4];
            near.lowerLeft.y = tile.upperLeft.y;
            near.lowerRight.y = tile.upperRight.y;
            if (near.upperRight.y < near.lowerRight.y - heightScale)
            {
                near.upperRight.y = near.lowerRight.y - heightScale;
                recalc = true;
            }
            if(near.upperRight.y > near.lowerRight.y + heightScale)
            {
                near.upperRight.y = near.lowerRight.y + heightScale;
                recalc = true;
            }
            if (near.upperLeft.y < near.lowerLeft.y - heightScale)
            {
                near.upperLeft.y = near.lowerLeft.y - heightScale;
                recalc = true;
            }
            if(near.upperLeft.y > near.lowerLeft.y + heightScale)
            {
                near.upperLeft.y = near.lowerLeft.y + heightScale;
                recalc = true;
            }
            if (recalc)
            {
                recalcTiles[4] = near;
            }
            else
            {
                recalcTiles[4] = null;
            }
        }
        #endregion
        #region Northeast Tile
        if (nearTiles[5] != null && nearTiles[5].HasBeenUpdated != true) // NE tile
        {
            recalc = false;
            near = nearTiles[5];
            near.lowerLeft.y = tile.upperRight.y;
            if (near.lowerRight.y < near.lowerLeft.y - heightScale)
            {
                near.lowerRight.y = near.lowerLeft.y - heightScale;
                recalc = true;
            }
            if(near.lowerRight.y > near.lowerLeft.y + heightScale)
            {
                near.lowerRight.y = near.lowerLeft.y + heightScale;
                recalc = true;
            }
            if (near.upperLeft.y < near.lowerLeft.y - heightScale)
            {
                near.upperLeft.y = near.lowerLeft.y - heightScale;
                recalc = true;
            }
            if(near.upperLeft.y > near.lowerLeft.y + heightScale)
            {
                near.upperLeft.y = near.lowerLeft.y + heightScale;
                recalc = true;
            }
            if (near.upperRight.y < near.lowerLeft.y - (2 * heightScale))
            {
                near.upperRight.y = near.lowerLeft.y - (2 * heightScale);
                recalc = true;
            }
            if(near.upperRight.y > near.lowerLeft.y + (2 * heightScale))
            {
                near.upperRight.y = near.lowerLeft.y + (2 * heightScale);
                recalc = true;
            }
            if (recalc)
            {
                recalcTiles[5] = near;
            }
            else
            {
                recalcTiles[5] = null;
            }
        }
        #endregion
        #region East Tile
        if (nearTiles[6] != null && nearTiles[6].HasBeenUpdated != true) // E tile
        {
            recalc = false;
            near = nearTiles[6];
            near.upperLeft.y = tile.upperRight.y;
            near.lowerLeft.y = tile.lowerRight.y;
            if (near.upperRight.y < near.upperLeft.y - heightScale)
            {
                near.upperRight.y = near.upperLeft.y - heightScale;
                recalc = true;
            }
            if(near.upperRight.y > near.upperLeft.y + heightScale)
            {
                near.upperRight.y = near.upperLeft.y + heightScale;
                recalc = true;
            }
            if (near.lowerRight.y < near.lowerLeft.y - heightScale)
            {
                near.lowerRight.y = near.lowerLeft.y - heightScale;
                recalc = true;
            }
            if(near.lowerRight.y > near.lowerLeft.y + heightScale)
            {
                near.lowerRight.y = near.lowerLeft.y + heightScale;
                recalc = true;
            }
            if (recalc)
            {
                recalcTiles[6] = near;
            }
            else
            {
                recalcTiles[6] = null;
            }
        }
        #endregion
        #region Southeast Tile
        if (nearTiles[7] != null && nearTiles[7].HasBeenUpdated != true) // SE tile
        {
            recalc = false;
            near = nearTiles[7];
            near.upperLeft.y = tile.lowerRight.y;
            if (near.upperRight.y < near.upperLeft.y - heightScale)
            {
                near.upperRight.y = near.upperLeft.y - heightScale;
                recalc = true;
            }
            if(near.upperRight.y > near.upperLeft.y + heightScale)
            {
                near.upperRight.y = near.upperLeft.y + heightScale;
                recalc = true;
            }
            if (near.lowerLeft.y < near.upperLeft.y - heightScale)
            {
                near.lowerLeft.y = near.upperLeft.y - heightScale;
                recalc = true;
            }
            if(near.lowerLeft.y > near.upperLeft.y + heightScale)
            {
                near.lowerLeft.y = near.upperLeft.y + heightScale;
                recalc = true;
            }
            if (near.lowerRight.y < near.upperLeft.y - (2 * heightScale))
            {
                near.lowerRight.y = near.upperLeft.y - (2 * heightScale);
                recalc = true;
            }
            if(near.lowerRight.y > near.upperLeft.y + (2 * heightScale))
            {
                near.lowerRight.y = near.upperLeft.y + (2 * heightScale);
                recalc = true;
            }
            if (recalc)
            {
                recalcTiles[7] = near;
            }
            else
            {
                recalcTiles[7] = null;
            }
        }
        #endregion
        foreach (Tile t in recalcTiles)
        {
            if (t != null)
            {
                if(!tilesToUpdate.Contains(t))
                tilesToUpdate.Add(t);
            }
        }
        foreach(Tile t in nearTiles)
        {
            if(t != null)
            {
                t.ReSetStats();
                foreach(BuildableObject obj in t.Objects)
                {
                    if(obj.GetComponent<Path>() != null)
                    {
                        constructionTools.UpdatePath(t, obj.GetComponent<Path>());
                    }
                    if (obj.GetComponent<Fence>() != null)
                    {
                        tile.ReSetStats();
                        constructionTools.UpdateFence(t, obj.GetComponent<Fence>());
                    }
                }
            }
        }
        foreach (BuildableObject obj in tile.Objects)
        {
            if (obj.GetComponent<Path>() != null)
            {
                constructionTools.UpdatePath(tile, obj.GetComponent<Path>());
            }
            if (obj.GetComponent<Fence>() != null)
            {
                tile.ReSetStats();
                constructionTools.UpdateFence(tile, obj.GetComponent<Fence>());
            }
        }
        tile.HasBeenUpdated = true;
        //tile.ReSetStats();
        if (!updatedTiles.Contains(tile))
            updatedTiles.Add(tile);
        //return recalcTiles;
    }

      void UpdateSwathSliderText(float value)
    {
        if(value == 0)
        {
            swathText.text = "Corner";
        }
        if(value > 0)
        {
            swathText.text = value + "x" + value;
        }
        currentSwathSize = value;
    }
}
