using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavGrid : MonoBehaviour
{
    public Vector2Int gridWorldSize = new Vector2Int();
    NavNode[,] grid;

    WorldController wCon;

    Vector3 gizmosCubeSize;
    Vector3 worldCenter = Vector3.zero;

    void Start() {
        wCon = GetComponent<WorldController>();
    }

    public void InitializeNavGrid() {
        gizmosCubeSize = new Vector3(.9f, .9f, .1f);

        gridWorldSize.x = WorldController.GetWorldWidth();
        gridWorldSize.y = WorldController.GetWorldHeight();

        worldCenter = new Vector3(gridWorldSize.x/2, gridWorldSize.y/2, 0);

        CreateGrid();
    }

    void CreateGrid() {
        grid = new NavNode[gridWorldSize.x, gridWorldSize.y];

        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.up * gridWorldSize.y/2 - Vector3.right * .5f - Vector3.up * .5f;

        for (int x = 0; x < gridWorldSize.x; x++) {
            for (int y = 0; y < gridWorldSize.y; y++) {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x + 1) + Vector3.up * (y + 1);
                bool walkable = !wCon.isTile(x, y);
                grid[x, y] = new NavNode(walkable, worldPoint, x, y);
            }
        }
    }

    public void UpdateNavNode(int x, int y, bool walkable) {
        grid[x, y].walkable = walkable;
    }

    public NavNode NavNodeFromWorldPoint(Vector3 worldPos) {
        float percentX = worldPos.x / gridWorldSize.x;
        float percentY = worldPos.y / gridWorldSize.y;
        Mathf.Clamp01(percentX);
        Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridWorldSize.x-1) * percentX);
        int y = Mathf.RoundToInt((gridWorldSize.y-1) * percentY);

        return grid[x, y];
    }

    public List<NavNode> GetNeighbors(NavNode node) {
        List<NavNode> neighbors = new List<NavNode>();

        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0)
                    continue;

                int realX = node.gridX + x;
                int realY = node.gridY + y;

                if (realX >= 0 && realX < gridWorldSize.x && realY >= 0 && realY < gridWorldSize.y) {
                    neighbors.Add(grid[realX, realY]);
                }
            }
        }

        return neighbors;
    }

    public List<NavNode> path;
    void OnDrawGizmos() {
        Gizmos.DrawWireCube(worldCenter, new Vector3(gridWorldSize.x, gridWorldSize.y, 1));
        
        if (grid != null) {
            foreach (NavNode node in grid) {
                Gizmos.color = node.walkable ? Color.clear : Color.red;
                if (path != null) {
                    if (path.Contains(node)) {
                        Gizmos.color = Color.black;
                    }
                }
                Gizmos.DrawCube(node.worldPos + worldCenter, gizmosCubeSize);
            }
        }
    }
}
