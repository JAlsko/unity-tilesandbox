using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum VerticalPathDirection {
    Forward = 0,
    Up = 1,
    Down = -1
}

public enum HorizontalPathDirection {
    Left = -1,
    None = 0,
    Right = 1
}

public class EnemyPathfinding : MonoBehaviour
{
    public Transform rightGround;
    public Transform leftGround;
    public Transform ground;
    public float preJumpDistance = .5f;
    public float jumpableHeight = 2f;
    public float jumpableLength = 1f;

    public Transform rightGap;
    public Transform rightJumpable;
    public Transform rightUnJumpable;

    private Vector3 rightGapCheck;
    private Vector3 rightJumpableCheck;
    private Vector3 rightUnJumpableCheck;

    private Vector3 leftGapCheck;
    private Vector3 leftJumpableCheck;
    private Vector3 leftUnJumpableCheck;

    public bool gap;
    public bool jumpable;
    public bool unjumpable;

    public LayerMask groundMask;

    public CharacterMover cMov;

    public Transform currentTarget = null;
    private List<NavNode> currentPath = new List<NavNode>();

    void Start()
    {
        rightGapCheck =rightGap.position;// rightGround.position - (Vector3.up * 0.5f); //Checks for gap immediately in front of player
        rightJumpableCheck =rightJumpable.position;// rightGround.position + (Vector3.right * preJumpDistance); //Checks for walls in front of player by small distance (to time jump over wall)
        rightUnJumpableCheck =rightUnJumpable.position;// rightGround.position + (Vector3.up * jumpableHeight) + (Vector3.up * 0.5f); //Checks for wall at height that is unjumpable

        leftGapCheck = leftGround.position - (Vector3.up * 0.5f); //Checks for gap immediately in front of player
        leftJumpableCheck = leftGround.position + (Vector3.left * preJumpDistance); //Checks for walls in front of player by small distance (to time jump over wall)
        leftUnJumpableCheck = leftGround.position + (Vector3.up * jumpableHeight) + (Vector3.up * 0.5f); //Checks for wall at height that is unjumpable
    }

    void Update()
    {
        UpdatePath();
        FollowPath();
    }

    private void UpdatePath() {
        currentPath = AStarPathfinder.Instance.FindPath(ground.position, currentTarget.position);
    }

    public void UpdateTarget(Transform newTarget) {
        currentTarget = newTarget;
        UpdatePath();
    }

    VerticalPathDirection GetNextVerticalDirection(int pathSteps = 0) {
        if (currentPath.Count <= pathSteps) {
            return VerticalPathDirection.Forward;
        }
        NavNode nextNode = currentPath[pathSteps];
        int vertPos = (int)rightGround.position.y + 1;
        if (nextNode.gridY > vertPos) {
            return VerticalPathDirection.Up;
        } else if (nextNode.gridY < vertPos) {
            return VerticalPathDirection.Down;
        }

        return VerticalPathDirection.Forward;
    }

    HorizontalPathDirection GetNextHorizontalDirection(int pathSteps = 0) {
        if (currentPath.Count <= pathSteps) {
            return HorizontalPathDirection.None;
        }
        NavNode nextNode = currentPath[pathSteps];
        int horizPos = (int)rightGround.position.x;
        if (nextNode.gridX > horizPos) {
            return HorizontalPathDirection.Right;
        } else if (nextNode.gridX < horizPos) {
            return HorizontalPathDirection.Left;
        }

        return HorizontalPathDirection.None;
    }

    HorizontalPathDirection GetEventualHorizontalDirection(int limit = 100000) {
        for (int i = 0; i < currentPath.Count && i < limit; i++) {
            if (currentPath[i].gridX != currentPath[0].gridX) {
                return currentPath[i].gridX > currentPath[0].gridX ? HorizontalPathDirection.Right : HorizontalPathDirection.Left;
            }
        }

        return HorizontalPathDirection.None;
    }

    void FollowPath() {
        VerticalPathDirection nextVertDir = GetNextVerticalDirection();
        HorizontalPathDirection nextHorizDir = GetEventualHorizontalDirection(6);

        Vector2 gapCheck = nextHorizDir == HorizontalPathDirection.Left ? leftGapCheck : rightGapCheck;
        Vector2 jumpableCheck = nextHorizDir == HorizontalPathDirection.Left ? leftJumpableCheck : rightJumpableCheck;
        Vector2 unjumpableCheck = nextHorizDir == HorizontalPathDirection.Left ? leftUnJumpableCheck : rightUnJumpableCheck;

        RaycastHit2D jumpableGap = Physics2D.Raycast(gapCheck, Vector2.right, jumpableLength, groundMask);
        RaycastHit2D jumpableWall = Physics2D.Raycast(jumpableCheck, Vector2.up, jumpableHeight, groundMask);
        RaycastHit2D largeWall = Physics2D.Raycast(unjumpableCheck, Vector2.right, 0.1f, groundMask);

        gap = jumpableGap;
        jumpable = jumpableWall;
        unjumpable = largeWall;

        //Debug.Log("Next vertical direction: " + nextVertDir + " -- Next horizontal direction: " + nextHorizDir);

        if (jumpableGap && nextVertDir != VerticalPathDirection.Down) {
            //If there is a gap that needs to be jumped, do this
            cMov.Jump();
        }

        else if (largeWall && nextVertDir != VerticalPathDirection.Forward) {
            //If there is an insurmountable wall that we need to climb, do this
            return;
        }

        else if (jumpableWall && nextVertDir == VerticalPathDirection.Up) {
            //If there is a wall that can and must be jumped over, do this
            cMov.Jump();
        }

        //else {
            //If 
            // 1)The path goes down and there's a gap
            // 2)The path goes forward/down and there's a large wall
            // 3)The path goes forward/down and there's a jumpable wall
            // 4)The path goes forward
            cMov.Move((int)nextHorizDir);
        //}
    }
}
