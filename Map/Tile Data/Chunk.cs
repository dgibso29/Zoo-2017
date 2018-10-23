using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    #region mesh creation variables
    //mesh creation variables
    private List<Vector3> verts = new List<Vector3>(75000);
    private List<int>[] tris;
    private List<Vector2> uvs = new List<Vector2>(75000);
    private int vertCount = 0;
    //cached objects
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private MeshCollider col;
    private Vector2 chunkPosition; // Chunk position BASED ON SW ( LOCAL 0,0) TILE -- Mirrored in WorldData chunks[] ie chunks[0,0] = chunkPosition 0,0, so on.
    private Vector3 chunkWorldPos;
    private World world;
    #endregion
    #region textures

    Material[] selectionMaterials;
    Material[] terrainMaterials;

    [HideInInspector]
    public Material[] terrainTextureMaterials;
    bool selectionTilePass;
    private float tunit = .25f;
    #endregion

    //tweakables
    public int chunkSize = 10;  //max tiles per side this chunk can handle
    public float tileSize = 1f;
    // Use this for initialization
    //public Chunk(int chunkX, int chunkY)
    //{
    //    //fetch objects
    //    //TODO: fix this for instantiation
    //    mesh = GetComponent<MeshFilter>().sharedMesh;
    //    col = GetComponent<MeshCollider>();

    //}

    void Awake()
    {
        //fetch objects
        //TODO: fix this for instantiation
        world = FindObjectOfType<World>();
        meshRenderer = GetComponent<MeshRenderer>();
        mesh = new Mesh();
        col = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();
        terrainTextureMaterials = new Material[10];
        selectionMaterials = Resources.LoadAll<Material>("Textures/SelectionBoxMaterials");
        terrainMaterials = Resources.LoadAll<Material>("Textures/MapMaterials");
    }

    #region mesh-altering methods
    void DrawQuad(Vector3 UpperLeft, Vector3 UpperRight, Vector3 LowerRight, Vector3 LowerLeft, int terrainTexture)
    {
        //add verticies
        verts.Add(UpperLeft);
        verts.Add(UpperRight);
        verts.Add(LowerRight);
        verts.Add(LowerLeft);

        //do triangles
        tris[terrainTexture].Add(vertCount);        //1
        tris[terrainTexture].Add(vertCount + 1);    //2
        tris[terrainTexture].Add(vertCount + 2);    //3

        tris[terrainTexture].Add(vertCount);        //1
        tris[terrainTexture].Add(vertCount + 2);    //3
        tris[terrainTexture].Add(vertCount + 3);    //4

        //TODO: tweak this
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(0, 0));

        vertCount += 4;
    }
    void DrawTile(Tile t, int terrainTexture, int cliffTexture = -1)
    {

        Tile[] adjacentTiles = world.GetAdjacentTiles(t);
        //fetch the corner points, transforming them to world-space in the process
        Vector3 p1 = transform.InverseTransformPoint(t.upperLeft);  // NW Corner
        Vector3 p2 = transform.InverseTransformPoint(t.upperRight); // NE Corner
        Vector3 p3 = transform.InverseTransformPoint(t.lowerRight); // SE Corner
        Vector3 p4 = transform.InverseTransformPoint(t.lowerLeft);  // SW Corner

        // Set up the vectors for cliff faces
        Vector3 b1 = transform.InverseTransformPoint(t.bottomUpperLeft);    // NW Bottom corner
        Vector3 b2 = transform.InverseTransformPoint(t.bottomUpperRight);  // NE Bottom corner
        Vector3 b3 = transform.InverseTransformPoint(t.bottomLowerRight);   // SE Bottom corner
        Vector3 b4 = transform.InverseTransformPoint(t.bottomLowerLeft);    // SW Bottom corner
        

        //if we're just drawing on the flat
        if (!t.isSlope)
        {
            verts.Add(p1);
            verts.Add(p2);
            verts.Add(p3);
            verts.Add(p4);

            if (cliffTexture > -1)
            {
                verts.Add(b1);
                verts.Add(b2);
                verts.Add(b3);
                verts.Add(b4);
            }


            //do triangles
            tris[terrainTexture].Add(vertCount + 0);    //1        0
            tris[terrainTexture].Add(vertCount + 1);    //2        1
            tris[terrainTexture].Add(vertCount + 2);    //3        2

            tris[terrainTexture].Add(vertCount + 0);    //1        0
            tris[terrainTexture].Add(vertCount + 2);    //3        2 
            tris[terrainTexture].Add(vertCount + 3);    //4        3
            #region Create edge cliff faces
            if (cliffTexture > -1)
            {
                // Do cliff triangles for S face
                if (adjacentTiles[0] == null)
                {
                    tris[cliffTexture].Add(vertCount + 3);    //1        0
                    tris[cliffTexture].Add(vertCount + 2);    //2        3
                    tris[cliffTexture].Add(vertCount + 6);    //3        7

                    tris[cliffTexture].Add(vertCount + 3);    //1        0
                    tris[cliffTexture].Add(vertCount + 6);    //3        7 
                    tris[cliffTexture].Add(vertCount + 7);    //4        5
                }
                // Do cliff triangles for W face
                if (adjacentTiles[2] == null)
                {
                    tris[cliffTexture].Add(vertCount + 0);    //1        0
                    tris[cliffTexture].Add(vertCount + 3);    //2        3
                    tris[cliffTexture].Add(vertCount + 7);    //3        7

                    tris[cliffTexture].Add(vertCount + 0);    //1        0
                    tris[cliffTexture].Add(vertCount + 7);    //3        7 
                    tris[cliffTexture].Add(vertCount + 4);    //4        5
                }               
                // Do cliff triangles for N face
                if (adjacentTiles[4] == null)
                {
                    tris[cliffTexture].Add(vertCount + 1);    //1        0
                    tris[cliffTexture].Add(vertCount + 0);    //2        3
                    tris[cliffTexture].Add(vertCount + 4);    //3        7

                    tris[cliffTexture].Add(vertCount + 1);    //1        0
                    tris[cliffTexture].Add(vertCount + 4);    //3        7 
                    tris[cliffTexture].Add(vertCount + 5);    //4        5
                }
                // Do cliff triangles for E face
                if (adjacentTiles[6] == null)
                {
                    tris[cliffTexture].Add(vertCount + 2);    //1        0
                    tris[cliffTexture].Add(vertCount + 1);    //2        3
                    tris[cliffTexture].Add(vertCount + 5);    //3        7

                    tris[cliffTexture].Add(vertCount + 2);    //1        0
                    tris[cliffTexture].Add(vertCount + 5);    //3        7 
                    tris[cliffTexture].Add(vertCount + 6);    //4        5
                }
            }
            #endregion

            #region Draw cliff faces
            if (cliffTexture > -1)
            {
                // Do cliff triangles for S face
                if (CheckSouthHeight(adjacentTiles, t) && adjacentTiles[0] != null)
                {
                    tris[cliffTexture].Add(vertCount + 3);    //1        0
                    tris[cliffTexture].Add(vertCount + 2);    //2        3
                    tris[cliffTexture].Add(vertCount + 6);    //3        7

                    tris[cliffTexture].Add(vertCount + 3);    //1        0
                    tris[cliffTexture].Add(vertCount + 6);    //3        7 
                    tris[cliffTexture].Add(vertCount + 7);    //4        5
                }
                // Do cliff triangles for W face
                if (CheckWestHeight(adjacentTiles, t) && adjacentTiles[2] != null)
                {
                    tris[cliffTexture].Add(vertCount + 0);    //1        0
                    tris[cliffTexture].Add(vertCount + 3);    //2        3
                    tris[cliffTexture].Add(vertCount + 7);    //3        7

                    tris[cliffTexture].Add(vertCount + 0);    //1        0
                    tris[cliffTexture].Add(vertCount + 7);    //3        7 
                    tris[cliffTexture].Add(vertCount + 4);    //4        5
                }
                // Do cliff triangles for N face
                if (CheckNorthHeight(adjacentTiles, t) && adjacentTiles[4] != null)
                {
                    tris[cliffTexture].Add(vertCount + 1);    //1        0
                    tris[cliffTexture].Add(vertCount + 0);    //2        3
                    tris[cliffTexture].Add(vertCount + 4);    //3        7

                    tris[cliffTexture].Add(vertCount + 1);    //1        0
                    tris[cliffTexture].Add(vertCount + 4);    //3        7 
                    tris[cliffTexture].Add(vertCount + 5);    //4        5
                }
                // Do cliff triangles for E face
                if (CheckEastHeight(adjacentTiles, t) && adjacentTiles[6] != null)
                {
                    tris[cliffTexture].Add(vertCount + 2);    //1        0
                    tris[cliffTexture].Add(vertCount + 1);    //2        3
                    tris[cliffTexture].Add(vertCount + 5);    //3        7

                    tris[cliffTexture].Add(vertCount + 2);    //1        0
                    tris[cliffTexture].Add(vertCount + 5);    //3        7 
                    tris[cliffTexture].Add(vertCount + 6);    //4        5
                }
                vertCount += 4;
            }
            #endregion

            vertCount += 4;

            //TODO: tweak this
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 0));

            if (cliffTexture > -1)
            {
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 1));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 0));
            }
            return;
        }

        //calculate how many points are at the reference height;
        int pointsAtReference = 0;
        if (t.upperRight.y.Equals(t.height)) { pointsAtReference++; }
        if (t.upperLeft.y.Equals(t.height)) { pointsAtReference++; }
        if (t.lowerRight.y.Equals(t.height)) { pointsAtReference++; }
        if (t.lowerLeft.y.Equals(t.height)) { pointsAtReference++; }

        //only one point of off-spec
        if (pointsAtReference == 3)
        {
            #region is the NW or SE point below ref?
            if (!t.upperLeft.y.Equals(t.height) || !t.lowerRight.y.Equals(t.height))
            {
                //Draw tile where the upper-left corner is dropped
                verts.Add(p1);
                verts.Add(p2);
                verts.Add(p4);

                verts.Add(p2);
                verts.Add(p3);
                verts.Add(p4);

                if (cliffTexture > -1)
                {
                    verts.Add(b1);
                    verts.Add(b2);
                    verts.Add(b3);
                    verts.Add(b4);
                }

                tris[terrainTexture].Add(vertCount);
                tris[terrainTexture].Add(vertCount + 1);
                tris[terrainTexture].Add(vertCount + 2);

                tris[terrainTexture].Add(vertCount + 3);
                tris[terrainTexture].Add(vertCount + 4);
                tris[terrainTexture].Add(vertCount + 5);

                #region Create edge cliff faces
                if (cliffTexture > -1)
                {
                    // Do cliff triangles for S face
                    if (adjacentTiles[0] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                    // Do cliff triangles for W face
                    if (adjacentTiles[2] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 6);    //4        5
                    }
                    // Do cliff triangles for N face
                    if (adjacentTiles[4] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 0);    //2        3
                        tris[cliffTexture].Add(vertCount + 6);    //3        7

                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 6);    //3        7 
                        tris[cliffTexture].Add(vertCount + 7);    //4        5
                    }
                    // Do cliff triangles for E face
                    if (adjacentTiles[6] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 1);    //2        3
                        tris[cliffTexture].Add(vertCount + 7);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 7);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                }
                #endregion

                #region Draw cliff faces
                if (cliffTexture > -1)
                {
                    // Do cliff triangles for S face
                    if (CheckSouthHeight(adjacentTiles, t) && adjacentTiles[0] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5

                    }
                    //Do cliff triangles for W face
                    if (CheckWestHeight(adjacentTiles, t) && adjacentTiles[2] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 6);    //4        5
                    }
                    // Do cliff triangles for N face
                    if (CheckNorthHeight(adjacentTiles, t) && adjacentTiles[4] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 0);    //2        3
                        tris[cliffTexture].Add(vertCount + 6);    //3        7

                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 6);    //3        7 
                        tris[cliffTexture].Add(vertCount + 7);    //4        5
                    }
                    // Do cliff triangles for E face
                    if (CheckEastHeight(adjacentTiles, t) && adjacentTiles[6] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 1);    //2        3
                        tris[cliffTexture].Add(vertCount + 7);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 7);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                    vertCount += 4;
                }
                #endregion

                vertCount += 6;

                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 0));

                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));

                if (cliffTexture > -1)
                {
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 1));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 0));
                }
                return;
            }
            #endregion
            #region is the NE or SW point below ref?
            if (!t.upperRight.y.Equals(t.height) || !t.lowerLeft.y.Equals(t.height))
            {
                verts.Add(p1);
                verts.Add(p2);
                verts.Add(p3);

                verts.Add(p1);
                verts.Add(p3);
                verts.Add(p4);

                if (cliffTexture > -1)
                {
                    verts.Add(b1);
                    verts.Add(b2);
                    verts.Add(b3);
                    verts.Add(b4);
                }

                tris[terrainTexture].Add(vertCount + 0);
                tris[terrainTexture].Add(vertCount + 1);
                tris[terrainTexture].Add(vertCount + 2);

                tris[terrainTexture].Add(vertCount + 3);
                tris[terrainTexture].Add(vertCount + 4);
                tris[terrainTexture].Add(vertCount + 5);

                #region Create edge cliff faces
                if (cliffTexture > -1)
                {
                    // Do cliff triangles for S face
                    if (adjacentTiles[0] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 2);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 2);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                    // Do cliff triangles for W face
                    if (adjacentTiles[2] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 6);    //4        5
                    }
                    // Do cliff triangles for N face
                    if (adjacentTiles[4] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 0);    //2        3
                        tris[cliffTexture].Add(vertCount + 6);    //3        7

                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 6);    //3        7 
                        tris[cliffTexture].Add(vertCount + 7);    //4        5
                    }
                    // Do cliff triangles for E face
                    if (adjacentTiles[6] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 1);    //2        3
                        tris[cliffTexture].Add(vertCount + 7);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 7);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                }
                #endregion

                #region Draw cliff faces
                if (cliffTexture > -1)
                {
                    // Do cliff triangles for S face
                    if (CheckSouthHeight(adjacentTiles, t) && adjacentTiles[0] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 2);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 2);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                    //Do cliff triangles for W face
                    if (CheckWestHeight(adjacentTiles, t) && adjacentTiles[2] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 6);    //4        5
                    }
                    // Do cliff triangles for N face
                    if (CheckNorthHeight(adjacentTiles, t) && adjacentTiles[4] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 0);    //2        3
                        tris[cliffTexture].Add(vertCount + 6);    //3        7

                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 6);    //3        7 
                        tris[cliffTexture].Add(vertCount + 7);    //4        5
                    }
                    // Do cliff triangles for E face
                    if (CheckEastHeight(adjacentTiles, t) && adjacentTiles[6] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 1);    //2        3
                        tris[cliffTexture].Add(vertCount + 7);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 7);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                    vertCount += 4;
                }
                #endregion

                vertCount += 6;

                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));

                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));

                if (cliffTexture > -1)
                {
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 1));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 0));
                }

                return;
            }
            #endregion
        }
        //two points off-spec
        if (pointsAtReference == 2)
        {
            #region draw a basic tile
            verts.Add(p1);
            verts.Add(p2);
            verts.Add(p3);
            verts.Add(p4);

            if (cliffTexture > -1)
            {
                verts.Add(b1);
                verts.Add(b2);
                verts.Add(b3);
                verts.Add(b4);
            }

            //do triangles
            tris[terrainTexture].Add(vertCount);        //1
            tris[terrainTexture].Add(vertCount + 1);    //2
            tris[terrainTexture].Add(vertCount + 2);    //3

            tris[terrainTexture].Add(vertCount);        //1
            tris[terrainTexture].Add(vertCount + 2);    //3
            tris[terrainTexture].Add(vertCount + 3);    //4

            #region Create edge cliff faces
            if (cliffTexture > -1)
            {
                // Do cliff triangles for S face
                if (adjacentTiles[0] == null)
                {
                    tris[cliffTexture].Add(vertCount + 3);    //1        0
                    tris[cliffTexture].Add(vertCount + 2);    //2        3
                    tris[cliffTexture].Add(vertCount + 6);    //3        7

                    tris[cliffTexture].Add(vertCount + 3);    //1        0
                    tris[cliffTexture].Add(vertCount + 6);    //3        7 
                    tris[cliffTexture].Add(vertCount + 7);    //4        5
                }
                // Do cliff triangles for W face
                if (adjacentTiles[2] == null)
                {
                    tris[cliffTexture].Add(vertCount + 0);    //1        0
                    tris[cliffTexture].Add(vertCount + 3);    //2        3
                    tris[cliffTexture].Add(vertCount + 7);    //3        7

                    tris[cliffTexture].Add(vertCount + 0);    //1        0
                    tris[cliffTexture].Add(vertCount + 7);    //3        7 
                    tris[cliffTexture].Add(vertCount + 4);    //4        5
                }
                // Do cliff triangles for N face
                if (adjacentTiles[4] == null)
                {
                    tris[cliffTexture].Add(vertCount + 1);    //1        0
                    tris[cliffTexture].Add(vertCount + 0);    //2        3
                    tris[cliffTexture].Add(vertCount + 4);    //3        7

                    tris[cliffTexture].Add(vertCount + 1);    //1        0
                    tris[cliffTexture].Add(vertCount + 4);    //3        7 
                    tris[cliffTexture].Add(vertCount + 5);    //4        5
                }
                // Do cliff triangles for E face
                if (adjacentTiles[6] == null)
                {
                    tris[cliffTexture].Add(vertCount + 2);    //1        0
                    tris[cliffTexture].Add(vertCount + 1);    //2        3
                    tris[cliffTexture].Add(vertCount + 5);    //3        7

                    tris[cliffTexture].Add(vertCount + 2);    //1        0
                    tris[cliffTexture].Add(vertCount + 5);    //3        7 
                    tris[cliffTexture].Add(vertCount + 6);    //4        5
                }
            }
            #endregion

            #region Draw cliff faces
            if (cliffTexture > -1)
            {
                // Do cliff triangles for S face
                if (CheckSouthHeight(adjacentTiles, t))
                {
                    tris[cliffTexture].Add(vertCount + 3);    //1        0
                    tris[cliffTexture].Add(vertCount + 2);    //2        3
                    tris[cliffTexture].Add(vertCount + 6);    //3        7

                    tris[cliffTexture].Add(vertCount + 3);    //1        0
                    tris[cliffTexture].Add(vertCount + 6);    //3        7 
                    tris[cliffTexture].Add(vertCount + 7);    //4        5
                }
                // Do cliff triangles for W face
                if (CheckWestHeight(adjacentTiles, t))
                {
                    tris[cliffTexture].Add(vertCount + 0);    //1        0
                    tris[cliffTexture].Add(vertCount + 3);    //2        3
                    tris[cliffTexture].Add(vertCount + 7);    //3        7

                    tris[cliffTexture].Add(vertCount + 0);    //1        0
                    tris[cliffTexture].Add(vertCount + 7);    //3        7 
                    tris[cliffTexture].Add(vertCount + 4);    //4        5
                }
                // Do cliff triangles for N face
                if (CheckNorthHeight(adjacentTiles, t))
                {
                    tris[cliffTexture].Add(vertCount + 1);    //1        0
                    tris[cliffTexture].Add(vertCount + 0);    //2        3
                    tris[cliffTexture].Add(vertCount + 4);    //3        7

                    tris[cliffTexture].Add(vertCount + 1);    //1        0
                    tris[cliffTexture].Add(vertCount + 4);    //3        7 
                    tris[cliffTexture].Add(vertCount + 5);    //4        5
                }
                // Do cliff triangles for E face
                if (CheckEastHeight(adjacentTiles, t))
                {
                    tris[cliffTexture].Add(vertCount + 2);    //1        0
                    tris[cliffTexture].Add(vertCount + 1);    //2        3
                    tris[cliffTexture].Add(vertCount + 5);    //3        7

                    tris[cliffTexture].Add(vertCount + 2);    //1        0
                    tris[cliffTexture].Add(vertCount + 5);    //3        7 
                    tris[cliffTexture].Add(vertCount + 6);    //4        5
                }
                vertCount += 4;
            }

            #endregion
            // CLIFF FACE DRAWING NEEDS TO COME BEFORE VERTCOUNT APPARENTLY -- stick to this order for everything
            vertCount += 4;

            //TODO: tweak this
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 0));


            if (cliffTexture > -1)
            {
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
            }

            return;
            #endregion
        }

        //only one point is ON-spec
        if (pointsAtReference == 1)
        {
            #region is the NW or SE point below ref?
            if (t.upperLeft.y.Equals(t.height) || t.lowerRight.y.Equals(t.height))
            {
                //Draw tile where the upper-left corner is dropped
                verts.Add(p1);
                verts.Add(p2);
                verts.Add(p4);

                verts.Add(p2);
                verts.Add(p3);
                verts.Add(p4);

                if (cliffTexture > -1)
                {
                    verts.Add(b1);
                    verts.Add(b2);
                    verts.Add(b3);
                    verts.Add(b4);
                }

                tris[terrainTexture].Add(vertCount);
                tris[terrainTexture].Add(vertCount + 1);
                tris[terrainTexture].Add(vertCount + 2);

                tris[terrainTexture].Add(vertCount + 3);
                tris[terrainTexture].Add(vertCount + 4);
                tris[terrainTexture].Add(vertCount + 5);

                #region Create edge cliff faces
                if (cliffTexture > -1)
                {
                    // Do cliff triangles for S face
                    if (adjacentTiles[0] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                    // Do cliff triangles for W face
                    if (adjacentTiles[2] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 6);    //4        5
                    }
                    // Do cliff triangles for N face
                    if (adjacentTiles[4] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 0);    //2        3
                        tris[cliffTexture].Add(vertCount + 6);    //3        7

                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 6);    //3        7 
                        tris[cliffTexture].Add(vertCount + 7);    //4        5
                    }
                    // Do cliff triangles for E face
                    if (adjacentTiles[6] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 1);    //2        3
                        tris[cliffTexture].Add(vertCount + 7);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 7);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                }
                #endregion

                #region Draw cliff faces
                if (cliffTexture > -1)
                {
                    // Do cliff triangles for S face
                    if (CheckSouthHeight(adjacentTiles, t) && adjacentTiles[0] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5

                    }
                    //Do cliff triangles for W face
                    if (CheckWestHeight(adjacentTiles, t) && adjacentTiles[2] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 6);    //4        5
                    }
                    // Do cliff triangles for N face
                    if (CheckNorthHeight(adjacentTiles, t) && adjacentTiles[4] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 0);    //2        3
                        tris[cliffTexture].Add(vertCount + 6);    //3        7

                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 6);    //3        7 
                        tris[cliffTexture].Add(vertCount + 7);    //4        5
                    }
                    // Do cliff triangles for E face
                    if (CheckEastHeight(adjacentTiles, t) && adjacentTiles[6] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 1);    //2        3
                        tris[cliffTexture].Add(vertCount + 7);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 7);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                    vertCount += 4;
                }
                #endregion

                vertCount += 6;
                
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 0));

                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));

                if (cliffTexture > -1)
                {
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 1));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 0));
                }

                return;
            }
            #endregion
            #region is the NE or SW point below ref?
            if (t.upperRight.y.Equals(t.height) || t.lowerLeft.y.Equals(t.height))
            {
                verts.Add(p1);
                verts.Add(p2);
                verts.Add(p3);

                verts.Add(p1);
                verts.Add(p3);
                verts.Add(p4);

                if (cliffTexture > -1)
                {
                    verts.Add(b1);
                    verts.Add(b2);
                    verts.Add(b3);
                    verts.Add(b4);
                }

                tris[terrainTexture].Add(vertCount);
                tris[terrainTexture].Add(vertCount + 1);
                tris[terrainTexture].Add(vertCount + 2);

                tris[terrainTexture].Add(vertCount + 3);
                tris[terrainTexture].Add(vertCount + 4);
                tris[terrainTexture].Add(vertCount + 5);

                #region Create edge cliff faces
                if (cliffTexture > -1)
                {
                    // Do cliff triangles for S face
                    if (adjacentTiles[0] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 2);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 2);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                    // Do cliff triangles for W face
                    if (adjacentTiles[2] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 6);    //4        5
                    }
                    // Do cliff triangles for N face
                    if (adjacentTiles[4] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 0);    //2        3
                        tris[cliffTexture].Add(vertCount + 6);    //3        7

                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 6);    //3        7 
                        tris[cliffTexture].Add(vertCount + 7);    //4        5
                    }
                    // Do cliff triangles for E face
                    if (adjacentTiles[6] == null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 1);    //2        3
                        tris[cliffTexture].Add(vertCount + 7);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 7);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                }
                #endregion

                #region Draw cliff faces
                if (cliffTexture > -1)
                {
                    // Do cliff triangles for S face
                    if (CheckSouthHeight(adjacentTiles, t) && adjacentTiles[0] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 2);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 2);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                    //Do cliff triangles for W face
                    if (CheckWestHeight(adjacentTiles, t) && adjacentTiles[2] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 5);    //2        3
                        tris[cliffTexture].Add(vertCount + 9);    //3        7

                        tris[cliffTexture].Add(vertCount + 0);    //1        0
                        tris[cliffTexture].Add(vertCount + 9);    //3        7 
                        tris[cliffTexture].Add(vertCount + 6);    //4        5
                    }
                    // Do cliff triangles for N face
                    if (CheckNorthHeight(adjacentTiles, t) && adjacentTiles[4] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 0);    //2        3
                        tris[cliffTexture].Add(vertCount + 6);    //3        7

                        tris[cliffTexture].Add(vertCount + 1);    //1        0
                        tris[cliffTexture].Add(vertCount + 6);    //3        7 
                        tris[cliffTexture].Add(vertCount + 7);    //4        5
                    }
                    // Do cliff triangles for E face
                    if (CheckEastHeight(adjacentTiles, t) && adjacentTiles[6] != null)
                    {
                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 1);    //2        3
                        tris[cliffTexture].Add(vertCount + 7);    //3        7

                        tris[cliffTexture].Add(vertCount + 4);    //1        0
                        tris[cliffTexture].Add(vertCount + 7);    //3        7 
                        tris[cliffTexture].Add(vertCount + 8);    //4        5
                    }
                    vertCount += 4;
                }
                #endregion

                vertCount += 6;

                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));

                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));

                if (cliffTexture > -1)
                {
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 1));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 0));
                }                            
                
                return;
            }
            #endregion
        }
    }
    public void DrawTiles(Tile[,] map, bool selectionTilePass)
    {
        this.selectionTilePass = selectionTilePass;
        if (selectionTilePass)
        {
            terrainTextureMaterials = selectionMaterials;
        }
        else
        {
            terrainTextureMaterials = terrainMaterials;
        }
        tris = new List<int>[terrainTextureMaterials.Length];

        for (int i = 0; i < terrainTextureMaterials.Length; i++)
        {
            tris[i] = new List<int>();
        }
        verts.Clear();
        uvs.Clear();

        //figure out where we should start!
        int startX = (int)(transform.position.x / tileSize);
        int startZ = (int)(transform.position.z / tileSize);

        //find out where we should END
        int endX = Mathf.Min(startX + chunkSize, map.GetLength(0));
        int endZ = Mathf.Min(startZ + chunkSize, map.GetLength(1));

        //iterate though the list
        for (int x = startX; x < endX; x++)
        {
            for (int z = startZ; z < endZ; z++)
            {
                Tile t = map[x, z];
                if (selectionTilePass)
                {                
                    DrawTile(t, t.selectionTileType);
                }
                else
                {
                    DrawTile(t, t.tileType, t.cliffType);
                }
            }
        }

        //then comit to the changes
        CommitMeshChanges();

    }
    #endregion

    //calculate the textures properlly
    Vector2 getUVCoordinates(Vector2 input, Vector2 texture)
    {
        Vector2 returnVect = new Vector2((input.x * tunit) + (texture.x * tunit), (input.y * tunit) + (texture.y * tunit));
        return returnVect;
    }

    //method that comits all our changes to the mesh proper
    void CommitMeshChanges()
    {
        //if (mesh == null)
        //{
        //    Mesh newMesh = new Mesh();
        //    mesh = newMesh;
        //}
        bool updatedMats = false;
        if (updatedMats == false)
        {
            updatedMats = true;
            meshRenderer.sharedMaterials = terrainTextureMaterials;
        }

        //load data into the mesh
        mesh.Clear();
        mesh.subMeshCount = terrainTextureMaterials.Length;
        mesh.vertices = verts.ToArray();
        //mesh.triangles = tris.ToArray();

        //Assign submeshes...
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            if (tris[i].Count > 0)
            {
                mesh.SetTriangles(tris[i].ToArray(), i);
            }
            else
            {
                mesh.SetTriangles(new int[3] { 0, 0, 0 }, i);
            }
        }
        // Debug.Log("Chunk (" + chunkPosition.x + "," + chunkPosition.y + ") submeshcount = " + mesh.subMeshCount);
        mesh.uv = uvs.ToArray();

        //clean up the data

        mesh.RecalculateNormals();


        //assign the data to the mesh colider
        //col.sharedMesh = null;
        col.sharedMesh = mesh;
        meshFilter.sharedMesh = mesh;

        //clear everything
        verts.Clear();
        for (int i = 0; i < terrainTextureMaterials.Length; i++)
        {
            tris[i].Clear();
        }
        uvs.Clear();

        vertCount = 0;
    }

    public Vector2 ChunkPosition
    {
        get { return chunkPosition; }
        set { chunkPosition = value; }
    }

    public Vector3 ChunkWorldPosition
    {
        get { return chunkWorldPos; }  
        set { chunkWorldPos = value; }
    }

    public Material[] TerrainTextureMaterials
    {
        set { terrainTextureMaterials = value; }
    }

    bool CheckSouthHeight(Tile[] tiles, Tile t)
    {
        if (tiles[0] != null && (tiles[0].upperRight.y < t.lowerRight.y || tiles[0].upperLeft.y < t.lowerLeft.y))
        {
            return true;
        }
        else
            return false;

    }
    bool CheckWestHeight(Tile[] tiles, Tile t)
    {
        if (tiles[2] != null && (tiles[2].upperRight.y < t.upperLeft.y || tiles[2].lowerRight.y < t.lowerLeft.y))
        {
            return true;
        }
        else
            return false;
    }
    bool CheckNorthHeight(Tile[] tiles, Tile t)
    {
        if (tiles[4] != null && (tiles[4].lowerRight.y < t.upperRight.y || tiles[4].lowerLeft.y < t.upperLeft.y))
        {
            return true;
        }
        else
            return false;
    }
    bool CheckEastHeight(Tile[] tiles, Tile t)
    {
        if (tiles[6] != null && (tiles[6].upperLeft.y < t.upperRight.y || tiles[6].lowerLeft.y < t.lowerRight.y))
        {
            return true;
        }
        else
            return false;
    }
}



// OLD CODE BELOW ---- KEEP IN CASE NEW FAILS


//public class TileGraphicsMap : MonoBehaviour
//{

//    public int sizeX;
//    public int sizeZ;
//    public float tileSize = 1.0f;

//    public Texture2D terrainTiles;
//    public int tileResolution;
//    public TileDataMap map;

//    Texture2D newTexture;
//    Color[][] tiles;

//    // Use this for initialization
//    void Start()
//    {
//        BuildMesh();
//    }


//    Color[][] ChopUpTiles()
//    {
//        int numTilesPerRow = terrainTiles.width / tileResolution;
//        int numRows = terrainTiles.height / tileResolution;

//        tiles = new Color[numTilesPerRow * numRows][];

//        for (int y = 0; y < numRows; y++)
//        {
//            for (int x = 0; x < numTilesPerRow; x++)
//            {
//                tiles[y * numTilesPerRow + x] = terrainTiles.GetPixels(x * tileResolution, y * tileResolution, tileResolution, tileResolution);
//            }
//        }

//        return tiles;
//    }


//    public void BuildTexture()
//    {
//        if(map == null)
//        map = new TileDataMap(sizeX, sizeZ);

//        int texWidth = sizeX * tileResolution;
//        int texHeight = sizeZ * tileResolution;
//        Texture2D texture = new Texture2D(texWidth, texHeight);

//        tiles = ChopUpTiles();

//        for (int y = 0; y < sizeZ; y++)
//        {
//            for (int x = 0; x < sizeX; x++)
//            {
//                //Debug.Log("Tile Type " + map.GetTile(0,0).GetTileType());
//                //Debug.Log(tiles[map.GetTile(x, y).GetTileType()]);
//                Color[] p = tiles[map.GetTile(x, y).tileType];
//                texture.SetPixels(x * tileResolution, y * tileResolution, tileResolution, tileResolution, p);
//            }
//        }

//        texture.filterMode = FilterMode.Bilinear;
//        texture.wrapMode = TextureWrapMode.Clamp;
//        texture.Apply();
//        newTexture = texture;
//        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
//        meshRenderer.sharedMaterials[0].mainTexture = texture;

//        Debug.Log("Done Texture!");
//    }

//    public void BuildMesh()
//    {
//        int numberOfTiles = sizeX * sizeZ;
//        int numberOfTriangles = numberOfTiles * 2;

//        int verticeSizeX = sizeX + 1;
//        int verticeSizeZ = sizeZ + 1;
//        //int numberOfVertices = verticeSizeX * verticeSizeZ; // Connected terrain only
//        int numberOfVertices = numberOfTiles * 4; // Allows RCT style terrain -- both connected and separate ie raise 1 tile vs using mountain tool

//        // Generate mesh data
//        Vector3[] vertices = new Vector3[numberOfVertices];
//        Vector3[] normals = new Vector3[numberOfVertices];
//        Vector2[] uv = new Vector2[numberOfVertices];

//        int[] triangles = new int[numberOfTriangles * 3];

//        //int x, z;
//        for (int z = 0; z < verticeSizeZ; z++)
//        {
//            for (int x = 0; x < verticeSizeX; x++)
//            {
//                vertices[z * verticeSizeX + x] = new Vector3(x * tileSize, 0, z * tileSize);
//                normals[z * verticeSizeX + x] = Vector3.up;
//                uv[z * verticeSizeX + x] = new Vector2((float)x / sizeX, (float)z / sizeZ);
//            }
//        }

//        for (int z = 0; z < sizeZ; z++)
//        {
//            for (int x = 0; x < sizeX; x++)
//            {
//                int squareIndex = z * sizeX + x;
//                int triangleOffset = squareIndex * 6;

//                triangles[triangleOffset + 0] = z * verticeSizeX + x + 0;
//                triangles[triangleOffset + 2] = z * verticeSizeX + x + verticeSizeX + 1;
//                triangles[triangleOffset + 1] = z * verticeSizeX + x + verticeSizeX + 0;


//                triangles[triangleOffset + 4] = z * verticeSizeX + x + 0;
//                triangles[triangleOffset + 3] = z * verticeSizeX + x + 1;
//                triangles[triangleOffset + 5] = z * verticeSizeX + x + verticeSizeX + 1;

//            }
//        }

//        // Create a new Mesh and populate it with the data
//        Mesh mesh = new Mesh();
//        mesh.vertices = vertices;
//        mesh.triangles = triangles;
//        mesh.normals = normals;
//        mesh.uv = uv;

//        // Assign our mesh to our filter/renderer/collider
//        MeshFilter meshFilter = GetComponent<MeshFilter>();
//        //MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
//        MeshCollider meshCollider = GetComponent<MeshCollider>();

//        meshFilter.mesh = mesh;
//        meshCollider.sharedMesh = mesh;

//        BuildTexture();

//    }

//    public void UpdateTextures(int tileX, int tileY, int newTileTexture, int swathSize)
//    {
//        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
//        Texture2D texture = newTexture;

//        Color[] p = tiles[newTileTexture];
//        texture.SetPixels(tileX * tileResolution, tileY * tileResolution, swathSize * tileResolution, swathSize * tileResolution, p);
//        texture.Apply();
//        //for (int y = 0; y < sizeZ; y++)
//        //{
//        //    for (int x = 0; x < sizeX; x++)
//        //    {

//        //    }
//        //}
//    }
//}
