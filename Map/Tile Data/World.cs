using UnityEngine;

public class World : MonoBehaviour {

    // Reference TileDataTile for list of terrain types

    int _sizeX;
    int _sizeY;

    //tweakables
    public int worldSize = 250; //tiles per side of the world -- MUST BE 1X1, 2X2, 3X3, 4X4, 5X5, ETC.
    public int chunkSize = 50;  //Number of tiles per side of each chunk
    public float tileSize = 1f; //meters per side of each tiles
    public float noiseScale = .125f;
    public float heightScale;   //meters of elevation each new level gives us
    public float flatness = 5f;     //we have to roll over this to bump the height up
    public int defaultTerrainType = 0;
    public int defaultCliffType = 1;
    public int defaultSelectionTileType = 0;
    public int bottomOfMapHeight = -10;
    Material[] terrainTextureMaterials;
    public UnityEngine.GameObject selectionBoxMap;
    //public GameObject chunk;  // used to create all chunks -- chunk at world pos (5,0,5)

    private int numberOfChunks; // Calculate the number of chunks based on World size
    private static Tile[,] mapData;    //set of all the tiles that make up the world
    public Chunk[,] chunks; //set of all the chunks we're going to use to draw the world. Chunks will be arranged in coordinates much like tiles -- (0,0), (0,1), (1,0), (1,1), etc
    public Chunk[,] selectionChunks; //set of all the selection box chunks
    private GameObject[,] chunkObjects; // Set all chunk gameobjects to this array
    private GameObject[,] chunkObjectsSelectionBox; // Set all chunk gameobjects for selection box to this array

    // Use this for initialization
    void Start()
    {       
        // Generate the world!
        //GenerateWorld();
    }

    public void GenerateWorld()
    {
        Pathfinding.world = GetComponent<World>();
        numberOfChunks = worldSize / 10;
        //initiate things
        if (mapData == null)
        {
            mapData = new Tile[worldSize + 1, worldSize + 1];
            Debug.Log("Created mapData array");

            for (int x = 0; x < worldSize; x++)
            {
                for (int z = 0; z < worldSize; z++)
                {
                    mapData[x, z] = new Tile(x, z, 0f, tileSize, defaultTerrainType, defaultSelectionTileType, bottomOfMapHeight, defaultCliffType);

                }
            }
        }

        //generate the terrain!
        //GenerateTerrain();

        //Instantiate the chunk & chunkobject arrays
        chunkObjects = new GameObject[numberOfChunks / 5, numberOfChunks / 5];
        chunks = new Chunk[numberOfChunks / 5, numberOfChunks / 5];

        //Create each chunk
        for (int x = 0; x < numberOfChunks / 5; x++)
        {
            for (int z = 0; z < numberOfChunks / 5; z++)
            {
                Vector3 newChunkPos = new Vector3(x * chunkSize, 0f, z * chunkSize);
                chunkObjects[x, z] = new GameObject("Chunk (" + x + "," + z + ")");
                chunkObjects[x, z].layer = 8;
                chunkObjects[x, z].transform.parent = gameObject.transform;
                chunkObjects[x, z].transform.position = newChunkPos;
                chunkObjects[x, z].AddComponent<Chunk>();
                chunks[x, z] = chunkObjects[x, z].GetComponent<Chunk>();
            }
        }

        //Debug.Log(chunks.GetLength(0));

        //Tell each chunk to draw their share of the mesh
        for (int x = 0; x < numberOfChunks / 5; x++)
        {
            for (int z = 0; z < numberOfChunks / 5; z++)
            {
                Chunk newChunk = chunks[x, z];
                newChunk.ChunkPosition = new Vector2(x, z);
                newChunk.ChunkWorldPosition = new Vector3(x * chunkSize, 0f, z * chunkSize);
                newChunk.tileSize = tileSize;
                newChunk.chunkSize = chunkSize;
                //newChunk.TerrainTextureMaterials = terrainTextureMaterials;
                newChunk.DrawTiles(mapData, false);
            }
        }

        //Instantiate the chunk & chunkobject arrays for selectionBoxMap
        chunkObjectsSelectionBox = new GameObject[numberOfChunks / 5, numberOfChunks / 5];
        selectionChunks = new Chunk[numberOfChunks / 5, numberOfChunks / 5];

        //Create each selection box chunk
        for (int x = 0; x < numberOfChunks / 5; x++)
        {
            for (int z = 0; z < numberOfChunks / 5; z++)
            {
                Vector3 newChunkPos = new Vector3(x * chunkSize, 0f, z * chunkSize);
                chunkObjectsSelectionBox[x, z] = new GameObject("SelectionBoxChunk (" + x + "," + z + ")");
                chunkObjectsSelectionBox[x, z].layer = 9;
                chunkObjectsSelectionBox[x, z].transform.parent = selectionBoxMap.transform;
                chunkObjectsSelectionBox[x, z].transform.position = newChunkPos;
                chunkObjectsSelectionBox[x, z].AddComponent<Chunk>();
                selectionChunks[x, z] = chunkObjectsSelectionBox[x, z].GetComponent<Chunk>();
            }
        }

        //Debug.Log(chunks.GetLength(0));

        //Tell each selection box chunk to draw their share of the mesh
        for (int x = 0; x < numberOfChunks / 5; x++)
        {
            for (int z = 0; z < numberOfChunks / 5; z++)
            {
                Chunk newChunk = selectionChunks[x, z];
                newChunk.ChunkPosition = new Vector2(x, z);
                newChunk.ChunkWorldPosition = new Vector3(x * chunkSize, 0f, z * chunkSize);
                newChunk.tileSize = tileSize;
                newChunk.chunkSize = chunkSize;
                newChunk.DrawTiles(mapData, true);
            }
        }

        Debug.Log("Map generation complete");

    }
    void GenerateTerrain()
    {
        //make some height
        for (int x = 0; x < worldSize; x++)
        {
            for (int z = 0; z < worldSize; z++)
            {
                Tile active = mapData[x, z];

                if (active.isSlope)
                {
                    break;
                }
                active.upperLeft = adjustVector(active.upperLeft, flatness);
                active.upperRight = adjustVector(active.upperRight, flatness);
                active.lowerRight = adjustVector(active.lowerRight, flatness);
                active.lowerLeft = adjustVector(active.lowerLeft, flatness);
                active.ReSetStats();
            }
        }
    }

    public Tile[] GetAdjacentTiles(Tile tile)
    {
        Tile[] neighborTiles = new Tile[8];
        int centerX = (int)tile.tileCoordX;
        int centerZ = (int)tile.tileCoordZ;
        // 0 = S tile
        if (centerZ > 0)
        {
            if (mapData[centerX, centerZ - 1] != null)
                neighborTiles[0] = mapData[centerX, centerZ - 1];
        }
        else
        {
            neighborTiles[0] = null;
        }
        // If both tile coordinates are greater than 0, set up the SW tile
        if (centerZ > 0 && centerX > 0)
        {
            if (mapData[centerX - 1, centerZ - 1] != null)
                neighborTiles[1] = mapData[centerX - 1, centerZ - 1];
        }
        else
        {
            neighborTiles[1] = null;
        }
        // If the tile X coordinate is greater than 0, set up the W tile
        if (centerX > 0)
        {
            if (mapData[centerX - 1, centerZ] != null)
                neighborTiles[2] = mapData[centerX - 1, centerZ];
        }
        else
        {
            neighborTiles[2] = null;
        }
        // If the tile Z coordinate is less than world size and tile X is greater than 0, set up the NW tile
        if (centerZ < worldSize && centerX > 0)
        {
            if (mapData[centerX - 1, centerZ + 1] != null)
                neighborTiles[3] = mapData[centerX - 1, centerZ + 1];
        }
        else
        {
            neighborTiles[3] = null;
        }
        // If the tile Z coordinate is less than world size, set up the N tile
        if (centerZ < worldSize)
        {
            if (mapData[centerX, centerZ + 1] != null)
                neighborTiles[4] = mapData[centerX, centerZ + 1];
        }
        else
        {
            neighborTiles[4] = null;
        }
        // If both tiles are less than world size, set up the NE tile
        if (centerZ < worldSize && centerX < worldSize)
        {
            if (mapData[centerX + 1, centerZ + 1] != null)
                neighborTiles[5] = mapData[centerX + 1, centerZ + 1];
        }
        else
        {
            neighborTiles[5] = null;
        }
        // If the tile X coordinate is less than world size, set up the E tile
        if (centerX < worldSize)
        {
            if (mapData[centerX + 1, centerZ] != null)
                neighborTiles[6] = mapData[centerX + 1, centerZ];
        }
        else
        {
            neighborTiles[6] = null;
        }
        // If the tile X coordinate is less than world size and the tile Z coordinate is greater than 0, set up the SE tile
        if (centerZ > 0 && centerX < worldSize)
        {
            if (mapData[centerX + 1, centerZ - 1] != null)
                neighborTiles[7] = mapData[centerX + 1, centerZ - 1];
        }
        else
        {
            neighborTiles[7] = null;
        }
        return neighborTiles;
    }

    Vector3 adjustVector(Vector3 input, float threshold)
    {
        float newHeight = input.y;

        newHeight = (int)perlin(input.x, input.z);

        return new Vector3(input.x, newHeight, input.z);
    }

    float perlin(float x, float z)
    {
        float perlinX = x * noiseScale;
        float perlinZ = z * noiseScale;

        return (Mathf.PerlinNoise(perlinX, perlinZ)) * heightScale;
    }

    public Tile GetTile(int x, int z)
    {
        if (mapData[x, z] != null)
        {
            return mapData[x, z];
        }
        else
        {
            return null;
        }
    }
    public Tile GetTile(Vector2 tileCoords)
    {
        return mapData[(int)tileCoords.x, (int)tileCoords.y];
    }    

    public Chunk GetChunk(int x, int z)
    {
        return chunks[x, z];
    }

    public Tile[,] MapData
    {
        get { return mapData; }
        set { mapData = value; }
    }

    public static Tile[,] GetMapDataStatic
    {
        get { return mapData; }
        set { mapData = value; }
    }

    public float GetHeightScale
    {
        get { return heightScale; }
    }
}
