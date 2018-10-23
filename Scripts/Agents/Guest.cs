using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guest : MonoBehaviour {

    bool isWalking = false;
    bool walkingToPath = false;

    public GuestData data;

	// Use this for initialization
	void Awake ()
    {
        data = new GuestData(transform.position);
	}
	
	// Update is called once per frame
	void Update () {
        data.position = transform.position;

        if (!isWalking)
        {
            StartCoroutine(Walk());
        }
	}

    private void FixedUpdate()
    {

    }

    IEnumerator Walk()
    {
        List<Pathfinding.Node> pathToTarget = null;
        isWalking = true;
        List<Path> paths = GeneratePathList();
        // Try to path. Break if no path.
        try
        {
            int randomTarget = FindRandomTargetPath(paths);
            //Debug.Log("Target path is " + paths[randomTarget].objectTileCoordinates.x + "," + paths[randomTarget].objectTileCoordinates.y);
            pathToTarget = Pathfinding.FindGuestPath(FindCurrentPath(paths), paths[randomTarget]);
        }
        catch
        {
            //Debug.Log("Breaking -- No path found");
            paths = GeneratePathList();
            // Check if the guest is currently on a path
            if (FindCurrentPath(paths) == null)
            {
                // If not, tell them to walk to the closest path following a VALID path.
                //Debug.Log("Attempting to walk to closest path");
                StartCoroutine(WalkToClosestPath());
                yield break;
            }
            isWalking = false;
            yield break;
        }
        if (pathToTarget != null)
        {
            //Debug.Log("Attempting to walk on path");
            bool completedPath = false;
            float offsetX = Random.Range(-.45f, .46f);
            float offsetZ = Random.Range(-.45f, .46f);
            while (!completedPath)
            {
                for(int i = 1; i < pathToTarget.Count; i++)
                {
                    // If the target path no longer exists, break.
                    if(pathToTarget[i].nodePath == null)
                    {
                        //Debug.Log("Breaking -- path interrupted");
                        isWalking = false;
                        completedPath = true;
                        yield break;
                    }
                    walkingToPath = true;
                    StartCoroutine(WalkToPath(pathToTarget[i].nodePath, offsetX, offsetZ));
                    yield return new WaitUntil(() => !walkingToPath);
                }
                isWalking = false;
                completedPath = true;
                //Debug.Log("Reached target");
                yield break;
            }

        }
        else
        {
            //Debug.Log("No Path");
            isWalking = false;
            yield break;
        }
    }

    /// <summary>
    /// Guest will find a path to the nearest path object following a valid path, and then walk to it.
    /// </summary>
    /// <returns></returns>
    IEnumerator WalkToClosestPath()
    {
        //Debug.Log("Happening");
        isWalking = true;
        List<Pathfinding.Node> pathToTarget = null;
        List<Path> paths = GeneratePathList();

        // Find the closest path to the guest        
        //try
        //{
            pathToTarget = Pathfinding.FindGuestPathToClosestPath(FindCurrentTile(), Pathfinding.FindClosestPath(FindCurrentTile()));
        //}
        //catch
        //{
        //    Debug.Log("Failing to walk to closest path");
        //    // fuck
        //    isWalking = false;
        //    yield break;
        //}
        walkingToPath = true;
        StartCoroutine(WalkToTarget(pathToTarget));

        yield return new WaitUntil(() => !walkingToPath);

        isWalking = false;
        yield break;
    }

    List<Path> GeneratePathList()
    {
        List<Path> paths = new List<Path>();
        foreach(BuildableObject obj in ConstructionTools.objectsBuilt)
        {
            if(obj.GetComponent<Path>() != null)
            {
                paths.Add(obj.GetComponent<Path>());
            }
        }
        return paths;
    }

    int FindRandomTargetPath(List<Path> paths)
    {
        int numberOfPaths = paths.Count;
        return Random.Range(0, numberOfPaths);
    }

    Tile FindCurrentTile()
    {
        return World.GetMapDataStatic[(int)transform.position.x, (int)transform.position.z];        
    }

    Path FindCurrentPath(List<Path> paths)
    {
        int posX = (int)transform.position.x;
        int posZ = (int)transform.position.z;
        Tile currentTile = FindCurrentTile();
        //Debug.Log("Current tile: " + currentTile.tileCoordX + "," + currentTile.tileCoordZ);
        return currentTile.GetPathAtHeight(0f);
    }

    IEnumerator WalkToTarget(List<Pathfinding.Node> pathToTarget)
    {
        // Generate position offset...
        float offsetX = Random.Range(0.15f, .96f);
        float offsetZ = Random.Range(0.15f, .96f);
        //Debug.Log(pathToTarget.Count);
        for (int i = 0; i < pathToTarget.Count; i++)
        {
            Tile targetTile = pathToTarget[i].nodeTile;
            Vector3 targetPos = new Vector3((targetTile.tileCoordX + offsetX), targetTile.height + .37f, (targetTile.tileCoordZ + offsetZ));
            while (transform.position != targetPos)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, .01f);
                yield return new WaitForEndOfFrame();
                //Debug.Log("should be walking");
            }
        }

        walkingToPath = false;
        yield break;
    }

    IEnumerator WalkToPath(Path targetPath, float offsetX, float offsetZ)
    {
        float posX;
        float posZ;

        posX = targetPath.transform.position.x;
        posZ = targetPath.transform.position.z;
        float newPosX = posX + offsetX;
        float newPosZ = posZ + offsetZ;

        Vector3 targetPos = new Vector3(newPosX, targetPath.objectHeight + .37f, newPosZ);
            while (transform.position != targetPos)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, .01f);
                yield return new WaitForEndOfFrame();
                if (targetPath == null)
                {
                    walkingToPath = false;
                    yield break;
                }
            }

            // WHAT IF this function took the entire path and just walked to each one in turn, so we can generate the position offset and then keep it constant??


            //yield return new WaitUntil(() => transform.position == targetPos);
            //yield return new WaitForSeconds(1f);
            //Debug.Log("finished waiting");
            walkingToPath = false;
            yield break;

    }
}
