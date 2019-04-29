using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder : MonoBehaviour
{
    NavGrid grid;

    public Transform seeker, target;

    void Start() {
        grid = GetComponent<NavGrid>();
    }

    void Update() {
        FindPath(seeker.position, target.position);
    }

    void FindPath(Vector3 startPos, Vector3 targetPos) {
        NavNode startNode = grid.NavNodeFromWorldPoint(startPos);
        NavNode targetNode = grid.NavNodeFromWorldPoint(targetPos);

        List<NavNode> openSet = new List<NavNode>();
        HashSet<NavNode> closedSet = new HashSet<NavNode>();
        openSet.Add(startNode);

        while (openSet.Count > 0) {
            NavNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++) {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)) {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode) {
                RetracePath(startNode, targetNode);
                return;
            }

            foreach (NavNode neighbor in grid.GetNeighbors(currentNode)) {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                int newMovementCost = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newMovementCost < neighbor.gCost || !openSet.Contains(neighbor)) {
                    neighbor.gCost = newMovementCost;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor)) {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
    }

    void RetracePath(NavNode startNode, NavNode endNode) {
        List<NavNode> path = new List<NavNode>();
        NavNode currentNode = endNode;

        while (currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        grid.path = path;
    }

    int GetDistance(NavNode nodeA, NavNode nodeB) {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY) {
            return 14 * dstY + 10 * (dstX - dstY);
        } else {
            return 14 * dstX + 10 * (dstY - dstX);
        }
    }
}
