using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding {

    public static World world;

    public class Node
    {
        /// <summary>
        /// Tile corresponding to this node in the mapData array.
        /// </summary>
        public Tile nodeTile;
        /// <summary>
        /// Path corresponding to this node.
        /// </summary>
        public Path nodePath;              
        /// <summary>
        /// Node from which this node was pathed to.
        /// </summary>        
        public Node parentNode;
        /// <summary>
        /// Distance from the starting node to the current node, following the current path up to this point.
        /// </summary>
        public float distanceFromStart;
        /// <summary>
        /// Distance from this node to the target node.
        /// </summary>
        public float distanceToTarget;
        /// <summary>
        /// Sum of distanceFromStart & distanceToTarget.
        /// </summary>
        public float totalPathDistance;
        /// <summary>
        /// Height of this node (in this case, this is equal to the height of the current path).
        /// </summary>
        public float nodeHeight;

        public Node(Tile nodeTile)
        {
            this.nodeTile = nodeTile;
        }
        public Node(Tile nodeTile, Node parentNode)
        {
            this.nodeTile = nodeTile;
            this.parentNode = parentNode;
        }
        public Node(Tile nodeTile, Path nodePath)
        {
            this.nodeTile = nodeTile;
            this.nodePath = nodePath;
        }
        public Node(Tile nodeTile, Path nodePath, Node parentNode)
        {
            this.nodeTile = nodeTile;
            this.nodePath = nodePath;
            this.parentNode = parentNode;
        }
        public Node(Tile nodeTile, Path nodePath, Node parentNode, float distanceFromStart, float distanceToTarget, float totalPathDistance)
        {
            this.nodeTile = nodeTile;
            this.nodePath = nodePath;
            this.parentNode = parentNode;
            this.distanceFromStart = distanceFromStart;
            this.distanceToTarget = distanceToTarget;
            this.totalPathDistance = totalPathDistance;            
        }

        void GetDistanceFromStart(Node startingNode, Node currentNode)
        {
            distanceFromStart = 0;
            Node node = currentNode;
            while(node.nodeTile != startingNode.nodeTile)
            {
                distanceFromStart += 1;
                node = node.parentNode;
            }
            //if(node.nodeTile == startingNode.nodeTile)
            //{
            //    distanceFromStart += 1;
            //}
        }

        void GetDistanceToTarget(Tile target)
        {
            distanceToTarget = Mathf.Abs(nodeTile.tileCoordX - target.tileCoordX);
            distanceToTarget += Mathf.Abs(nodeTile.tileCoordZ - target.tileCoordZ);
        }
        
         void RecalculateTotalPathDistance()
        {
            totalPathDistance = distanceFromStart + distanceToTarget;
        }

        public void SetNodeStats(Node startNode, Node currentNode, Tile targetTile)
        {
            currentNode.GetDistanceFromStart(startNode, currentNode);
            currentNode.GetDistanceToTarget(targetTile);
            currentNode.RecalculateTotalPathDistance();
        }

    }

    public class CompareNodeByPathDistance : IComparer<Node>
    {
        public int Compare(Node x, Node y)
        {
            if (x.totalPathDistance > y.totalPathDistance)
            {
                return 1;
            }
            else if (x.totalPathDistance < y.totalPathDistance)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
    #region Guest Pathfinding
    /// <summary>
    /// Find a path from the guest's current position to its desired position.
    /// </summary>
    /// <param name="startingPath"></param>
    /// <param name="targetPath"></param>
    /// <returns></returns>
    public static List<Node> FindGuestPath(Path startingPath, Path targetPath)
    {
        /// <summary>
        /// Nodes that may lead to the target path, but have not been checked.
        /// </summary>
        List<Node> openNodes = new List<Node>();
        List<Node> closedNodes = new List<Node>();
        /// <summary>
        /// List of path objects that are viable for pathfinding to the target path.
        /// </summary>
        List<Path> paths = new List<Path>();
        Tile[] adjacentTiles;
        Tile startTile = world.GetTile((int)startingPath.positionX, (int)startingPath.positionZ);
        Tile targetTile = world.GetTile((int)targetPath.positionX, (int)targetPath.positionZ);
        // Set up starting node.
        Node startNode = new Node(startTile, startingPath);
        startNode.SetNodeStats(startNode, startNode, targetTile);
        startNode.parentNode = startNode;
        openNodes.Add(startNode);
        paths.Add(startingPath);
        // Set up the target node.
        Node targetNode = new Node(targetTile, targetPath);
                
        bool lookingForPath = true;
        // Start the pathfinding loop
        while (lookingForPath)
        {
            // Find the node in openNodes that has the lowest total distance to the target path
            CompareNodeByPathDistance compareByPathLength = new CompareNodeByPathDistance();
            openNodes.Sort(compareByPathLength.Compare);
            Node currentNode = openNodes[0];
            // Get the tiles adjacent to the current
            adjacentTiles = world.GetAdjacentTiles(currentNode.nodeTile);

            // Check if each tile is valid for pathfinding, and, if so, add it to the openNodes list.
            foreach (Tile t in adjacentTiles)
            {
                // Make sure the tile is not null!
                if (t != null)
                {
                    // Since guests cannot walk diagonally, we only want the N/S/E/W tiles
                    if (t == adjacentTiles[0] || t == adjacentTiles[2] || t == adjacentTiles[4] || t == adjacentTiles[6])
                    {
                        // Check if these tiles have paths that connect to the current path.
                        if (t.GetPath(currentNode.nodePath, world.heightScale) != null)
                        {
                            // Account for top of slope direction with if statement -- if top of slope is N, then we only care about it if it's the northern node, etc.
                            Path newPath = t.GetPath(currentNode.nodePath, world.heightScale);
                            if (newPath == targetPath)
                            {
                                //Debug.Log("Found path!");
                                //Debug.Log(closedNodes.Count);
                                //Debug.Log(openNodes.Count);
                                targetNode.parentNode = currentNode;
                                lookingForPath = false;
                                break;
                            }
                            else if (!paths.Contains(newPath))
                            {
                                Node newNode = new Node(t, newPath, currentNode);
                                newNode.SetNodeStats(startNode, newNode, targetTile);
                                openNodes.Add(newNode);
                                paths.Add(newPath);
                            }
                        }
                    }
                }
            }
            // Add the current node to the list of closed nodes
            closedNodes.Add(currentNode);
            if (!lookingForPath)
            {
                closedNodes.Add(targetNode);
            }
            openNodes.Remove(currentNode);
            // If there are no more open nodes, we did not find a path -- break.
            if (openNodes.Count == 0 && lookingForPath)
            {
                //Debug.Log(closedNodes.Count);
                //Debug.Log(openNodes.Count);
                //Debug.Log("No path found!");
                lookingForPath = false;
                break;
            }
        }
        // Make sure the path is in the proper order (from starting node to target node)
        List<Node> finalPath = new List<Node>();
        finalPath.Add(targetNode);
        for(int i = 0; i < closedNodes.Count; i++)
        {
            // Make sure the next node is not the starting node
            if (finalPath[i].parentNode != startNode)
            {
                Node nextNode = closedNodes.Find(node => node.nodeTile == finalPath[i].parentNode.nodeTile);
                finalPath.Add(nextNode);
            }
            // Otherwise, if it is the starting node
            else if(finalPath[i].parentNode == startNode)
            {
                // Add it and break
                finalPath.Add(startNode);
                break;
            }
        }
        // Reverse the list so it is in the proper order
        finalPath.Reverse();
        if(finalPath[0] == startNode)
        {
            int finalIndex = finalPath.FindIndex(node => node == targetNode);
            //Debug.Log("Path start tile coords:" + startTile.tileCoordX + "," + startTile.tileCoordZ + "; target tile coords:" + targetTile.tileCoordX + "," + targetTile.tileCoordZ);
            //Debug.Log("Path is working; Final index is " + finalIndex + "; Path size is " + finalPath.Count);
        }
        // And finally return the path
        return finalPath;
    }

    /// <summary>
    /// Used when a guest is no longer on a path object.
    /// </summary>
    /// <param name="startingTile"></param>
    /// <param name="targetPath"></param>
    /// <returns></returns>
    public static List<Node> FindGuestPathToClosestPath(Tile startingTile, Path targetPath)
    {
        /// <summary>
        /// Nodes that may lead to the target path, but have not been checked.
        /// </summary>
        List<Node> openNodes = new List<Node>();
        List<Node> closedNodes = new List<Node>();
        /// <summary>
        /// List of path objects that are viable for pathfinding to the target path.
        /// </summary>
        List<Path> paths = new List<Path>();
        Tile[] adjacentTiles;
        Tile startTile = startingTile;
        Tile targetTile = world.GetTile((int)targetPath.positionX, (int)targetPath.positionZ);
        // Set up starting node.
        Node startNode = new Node(startTile);
        startNode.SetNodeStats(startNode, startNode, targetTile);
        startNode.parentNode = startNode;
        openNodes.Add(startNode);
        // Set up the target node.
        Node targetNode = new Node(targetTile, targetPath);

        bool lookingForPath = true;
        // Start the pathfinding loop
        while (lookingForPath)
        {
            // Find the node in openNodes that has the lowest total distance to the target path
            CompareNodeByPathDistance compareByPathLength = new CompareNodeByPathDistance();
            openNodes.Sort(compareByPathLength.Compare);
            Node currentNode = openNodes[0];
            // Get the tiles adjacent to the current
            adjacentTiles = world.GetAdjacentTiles(currentNode.nodeTile);

            // Check if each tile is valid for pathfinding, and, if so, add it to the openNodes list.
            foreach (Tile t in adjacentTiles)
            {
                // Make sure the tile is not null!
                if (t != null)
                {
                    //Debug.Log("Checking Tile" + t.tileCoordX + "," + t.tileCoordZ);
                    // Since guests cannot walk diagonally, we only want the N/S/E/W tiles
                    if (t == adjacentTiles[0] || t == adjacentTiles[2] || t == adjacentTiles[4] || t == adjacentTiles[6])
                    {
                        // Check if these tiles have paths that connect to the current path.
                        if (t.GetPathAtHeight(t.height) != null)
                        {
                            // Account for top of slope direction with if statement -- if top of slope is N, then we only care about it if it's the northern node, etc.
                            Path newPath = t.GetPathAtHeight(t.height);
                            if (newPath == targetPath)
                            {
                                //Debug.Log("Found path!");
                                //Debug.Log(closedNodes.Count);
                                //Debug.Log(openNodes.Count);
                                targetNode.parentNode = currentNode;
                                lookingForPath = false;
                                break;
                            }
                            else if (!paths.Contains(newPath))
                            {
                                Node newNode = new Node(t, newPath, currentNode);
                                newNode.SetNodeStats(startNode, newNode, targetTile);
                                openNodes.Add(newNode);
                                paths.Add(newPath);
                            }
                        }
                        // Otherwise, check if these tiles are valid for walking (No cliffs, fences, buildings, etc)
                        #region South Tile
                        // South tile
                        else if (t == adjacentTiles[0])
                        {
                            // Make sure we can even walk to this tile -- If the two nodes are the same height and not slopes, or are a valid combination of height and slopes, continue.
                            if ((t.height == currentNode.nodeTile.height && !t.isSlope && !currentNode.nodeTile.isSlope) || ((t.height == currentNode.nodeTile.height + world.heightScale || t.height == currentNode.nodeTile.height - world.heightScale) && t.lowerLeft.y == currentNode.nodeTile.lowerLeft.y && t.lowerRight.y == currentNode.nodeTile.lowerRight.y))
                            {
                                // Check for obstructions (Fences, buildings, etc)
                                foreach (BuildableObject obj in t.Objects)
                                {
                                    // First, fences
                                    if (obj.GetComponent<Fence>() != null)
                                    {
                                        // Check if the fence is between this tile and the next
                                        if(obj.GetComponent<Fence>().RotationDirection == "N")
                                        {
                                            // Skip this node
                                            break;
                                        }
                                    }
                                    // Next, buildings
                                    else if(obj.GetComponent<Building>() != null)
                                    {
                                        // Check if the building is too low to walk under/is in the way
                                        if(obj.GetComponent<Building>().objectHeight < t.height + 1)
                                        {
                                            // If so, skip this node
                                            break;
                                        }
                                    }
                                }
                                // Having made it this far, add the node and continue
                                Node newNode = new Node(t, currentNode);
                                newNode.SetNodeStats(startNode, newNode, targetTile);
                                openNodes.Add(newNode);
                            }
                        }
                        #endregion
                        #region West Tile
                        // West Tile
                        else if (t == adjacentTiles[2])
                        {
                            // Make sure we can even walk to this tile -- If the two nodes are the same height and not slopes, or are a valid combination of height and slopes, continue.
                            if ((t.height == currentNode.nodeTile.height && !t.isSlope && !currentNode.nodeTile.isSlope) || ((t.height == currentNode.nodeTile.height + world.heightScale || t.height == currentNode.nodeTile.height - world.heightScale) && t.lowerLeft.y == currentNode.nodeTile.lowerLeft.y && t.upperLeft.y == currentNode.nodeTile.upperLeft.y))
                            {
                                // Check for obstructions (Fences, buildings, etc)
                                foreach (BuildableObject obj in t.Objects)
                                {
                                    // First, fences
                                    if (obj.GetComponent<Fence>() != null)
                                    {
                                        // Check if the fence is between this tile and the next
                                        if (obj.GetComponent<Fence>().RotationDirection == "E")
                                        {
                                            // Skip this node
                                            break;
                                        }
                                    }
                                    // Next, buildings
                                    else if (obj.GetComponent<Building>() != null)
                                    {
                                        // Check if the building is too low to walk under/is in the way
                                        if (obj.GetComponent<Building>().objectHeight < t.height + 1)
                                        {
                                            // If so, skip this node
                                            break;
                                        }
                                    }
                                }
                                // Having made it this far, add the node and continue
                                Node newNode = new Node(t, currentNode);
                                newNode.SetNodeStats(startNode, newNode, targetTile);
                                openNodes.Add(newNode);
                            }
                        }
                        #endregion
                        #region North Tile
                        // North Tile
                        else if (t == adjacentTiles[4])
                        {
                            // Make sure we can even walk to this tile -- If the two nodes are the same height and not slopes, or are a valid combination of height and slopes, continue.
                            if ((t.height == currentNode.nodeTile.height && !t.isSlope && !currentNode.nodeTile.isSlope) || ((t.height == currentNode.nodeTile.height + world.heightScale || t.height == currentNode.nodeTile.height - world.heightScale) && t.upperLeft.y == currentNode.nodeTile.upperLeft.y && t.upperRight.y == currentNode.nodeTile.upperRight.y))
                            {
                                // Check for obstructions (Fences, buildings, etc)
                                foreach (BuildableObject obj in t.Objects)
                                {
                                    // First, fences
                                    if (obj.GetComponent<Fence>() != null)
                                    {
                                        // Check if the fence is between this tile and the next
                                        if (obj.GetComponent<Fence>().RotationDirection == "S")
                                        {
                                            // Skip this node
                                            break;
                                        }
                                    }
                                    // Next, buildings
                                    else if (obj.GetComponent<Building>() != null)
                                    {
                                        // Check if the building is too low to walk under/is in the way
                                        if (obj.GetComponent<Building>().objectHeight < t.height + 1)
                                        {
                                            // If so, skip this node
                                            break;
                                        }
                                    }
                                }
                                // Having made it this far, add the node and continue
                                Node newNode = new Node(t, currentNode);
                                newNode.SetNodeStats(startNode, newNode, targetTile);
                                openNodes.Add(newNode);
                            }
                        }
                        #endregion
                        #region East Tile
                        // East Tile
                        else if (t == adjacentTiles[6])
                        {
                            // Make sure we can even walk to this tile -- If the two nodes are the same height and not slopes, or are a valid combination of height and slopes, continue.
                            if ((t.height == currentNode.nodeTile.height && !t.isSlope && !currentNode.nodeTile.isSlope) || ((t.height == currentNode.nodeTile.height + world.heightScale || t.height == currentNode.nodeTile.height - world.heightScale) && t.upperLeft.y == currentNode.nodeTile.upperLeft.y && t.lowerRight.y == currentNode.nodeTile.lowerRight.y))
                            {
                                // Check for obstructions (Fences, buildings, etc)
                                foreach (BuildableObject obj in t.Objects)
                                {
                                    // First, fences
                                    if (obj.GetComponent<Fence>() != null)
                                    {
                                        // Check if the fence is between this tile and the next
                                        if (obj.GetComponent<Fence>().RotationDirection == "W")
                                        {
                                            // Skip this node
                                            break;
                                        }
                                    }
                                    // Next, buildings
                                    else if (obj.GetComponent<Building>() != null)
                                    {
                                        // Check if the building is too low to walk under/is in the way
                                        if (obj.GetComponent<Building>().objectHeight < t.height + 1)
                                        {
                                            // If so, skip this node
                                            break;
                                        }
                                    }
                                }
                                // Having made it this far, add the node and continue
                                Node newNode = new Node(t, currentNode);
                                newNode.SetNodeStats(startNode, newNode, targetTile);
                                openNodes.Add(newNode);
                            }
                        }
                        #endregion
                    }
                }
            }
            // Add the current node to the list of closed nodes
            closedNodes.Add(currentNode);
            if (!lookingForPath)
            {
                //Debug.Log("This is happening 1");
                closedNodes.Add(targetNode);
            }
            //Debug.Log("This is happening 2");
            //Debug.Log("Open nodes:" + openNodes.Count + "; Closed nodes:" + closedNodes.Count);

            // Not making it past here.
            openNodes.Remove(currentNode);
            // If there are no more open nodes, we did not find a path -- break.
            if (openNodes.Count == 0 && lookingForPath)
            {
                //Debug.Log("This is happening 3");

                //Debug.Log(closedNodes.Count);
                //Debug.Log(openNodes.Count);
                //Debug.Log("No path found!");
                lookingForPath = false;
                break;
            }
        }
        // Make sure the path is in the proper order (from starting node to target node)
        //Debug.Log("This is happening 4");

        List<Node> finalPath = new List<Node>(closedNodes.Count);
        finalPath.Add(targetNode);
        for (int i = 0; i < closedNodes.Count; i++)
        {
            // Make sure the next node is not the starting node
            if (finalPath[i].parentNode != startNode)
            {
                Node nextNode = closedNodes.Find(node => node.nodeTile == finalPath[i].parentNode.nodeTile);
                finalPath.Add(nextNode);
            }
            // Otherwise, if it is the starting node
            else if (finalPath[i].parentNode == startNode)
            {
                // Add it and break
                finalPath.Add(startNode);
                break;
            }
        }
        // Reverse the list so it is in the proper order
        finalPath.Reverse();
        // And finally return the path
        return finalPath;
    }

    /// <summary>
    /// Find the closest path to the guest, ignoring validity of route.
    /// </summary>
    /// <param name="startTile"></param>
    /// <returns></returns>
    public static Path FindClosestPath(Tile startTile)
    {
        Path targetPath = null;
        // The Tile we are currently checking
        Tile currentTile = startTile;
        Tile[] adjacentTiles;

        // Tiles we have not checked
        List<Tile> openTiles = new List<Tile>();
        // Tiles we have checked
        List<Tile> closedTiles = new List<Tile>();
        openTiles.Add(startTile);

        while (openTiles.Count > 0)
        {
            currentTile = openTiles[0];
            adjacentTiles = world.GetAdjacentTiles(currentTile);
            openTiles.Remove(currentTile);
            closedTiles.Add(currentTile);
            foreach(Tile t in adjacentTiles)
            {
                if(t != null)
                {
                    if (t.GetPathAtHeight(t.height) != null)
                    {
                        if(!t.GetPathAtHeight(t.height).IsElevated && !t.GetPathAtHeight(t.height).IsTunnel)
                        {
                            targetPath = t.GetPathAtHeight(t.height);
                            return targetPath;
                        }
                    }
                    foreach(Tile tile in world.GetAdjacentTiles(t))
                    {
                        if(tile != null && !openTiles.Contains(tile) && !closedTiles.Contains(tile))
                        openTiles.Add(tile);
                    }
                    closedTiles.Add(t);
                }
            }
        }
        // Shit, no path found.
        Debug.Log("Shit, no path found");
        return null;
    }
    #endregion
    #region Animal Pathfinding
    public static List<Node> FindAnimalPath(Tile startTile, Tile targetTile, Enclosure animalEnclosure)
    {
        /// <summary>
        /// Nodes that may lead to the target tile, but have not been checked.
        /// </summary>
        List<Node> openNodes = new List<Node>();
        List<Node> closedNodes = new List<Node>();
        Tile[] adjacentTiles;
        // Set up starting node.
        Node startNode = new Node(startTile);
        startNode.SetNodeStats(startNode, startNode, targetTile);
        startNode.parentNode = startNode;
        openNodes.Add(startNode);
        // Set up the target node.
        Node targetNode = new Node(targetTile);

        bool lookingForPath = true;
        // Start the pathfinding loop
        while (lookingForPath)
        {
            // Find the node in openNodes that has the lowest total distance to the target path
            CompareNodeByPathDistance compareByPathLength = new CompareNodeByPathDistance();
            openNodes.Sort(compareByPathLength.Compare);
            Node currentNode = openNodes[0];
            // Get the tiles adjacent to the current
            adjacentTiles = world.GetAdjacentTiles(currentNode.nodeTile);

            // Check if each tile is valid for pathfinding, and, if so, add it to the openNodes list.
            foreach (Tile t in adjacentTiles)
            {
                // Make sure the tile is not null, and that it is part of the animal's enclosure!
                if (t != null)
                {
                    if (animalEnclosure.EnclosureTiles.Contains(t))
                    {
                        // Check if the current tile is connected to the new tile in a valid way to be travelled.
                        if ((t.height == currentNode.nodeTile.height && !t.isSlope && !currentNode.nodeTile.isSlope) || ((t.height == currentNode.nodeTile.height + world.heightScale || t.height == currentNode.nodeTile.height - world.heightScale) && t.lowerLeft.y == currentNode.nodeTile.lowerLeft.y && t.lowerRight.y == currentNode.nodeTile.lowerRight.y))
                        {

                            if (t == targetTile)
                            {
                                //Debug.Log("Found path!");
                                //Debug.Log(closedNodes.Count);
                                //Debug.Log(openNodes.Count);
                                targetNode.parentNode = currentNode;
                                lookingForPath = false;
                                break;
                            }
                            else
                            {
                                Node newNode = new Node(t, currentNode);
                                newNode.SetNodeStats(startNode, newNode, targetTile);
                                openNodes.Add(newNode);
                            }
                        }
                    }

                }
            }
            // Add the current node to the list of closed nodes
            closedNodes.Add(currentNode);
            if (!lookingForPath)
            {
                closedNodes.Add(targetNode);
            }
            openNodes.Remove(currentNode);
            // If there are no more open nodes, we did not find a path -- break.
            if (openNodes.Count == 0 && lookingForPath)
            {
                //Debug.Log(closedNodes.Count);
                //Debug.Log(openNodes.Count);
                //Debug.Log("No path found!");
                lookingForPath = false;
                break;
            }
        }
        // Make sure the path is in the proper order (from starting node to target node)
        List<Node> finalPath = new List<Node>();
        finalPath.Add(targetNode);
        for (int i = 0; i < closedNodes.Count; i++)
        {
            // Make sure the next node is not the starting node
            if (finalPath[i].parentNode != startNode)
            {
                Node nextNode = closedNodes.Find(node => node.nodeTile == finalPath[i].parentNode.nodeTile);
                finalPath.Add(nextNode);
            }
            // Otherwise, if it is the starting node
            else if (finalPath[i].parentNode == startNode)
            {
                // Add it and break
                finalPath.Add(startNode);
                break;
            }
        }
        // Reverse the list so it is in the proper order
        finalPath.Reverse();
        if (finalPath[0] == startNode)
        {
            int finalIndex = finalPath.FindIndex(node => node == targetNode);
            //Debug.Log("Path start tile coords:" + startTile.tileCoordX + "," + startTile.tileCoordZ + "; target tile coords:" + targetTile.tileCoordX + "," + targetTile.tileCoordZ);
            //Debug.Log("Path is working; Final index is " + finalIndex + "; Path size is " + finalPath.Count);
        }
        // And finally return the path
        return finalPath;
    }
    #endregion
}
