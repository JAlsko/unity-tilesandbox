using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    private MeshRenderer meshRend;
    private ParallaxGroup pg;

    private Transform player;

    public float moveSpeedRatio = 1f;

    private float layerBaseHeight;

    void Start()
    {
        meshRend = GetComponent<MeshRenderer>();
        pg = transform.parent.GetComponent<ParallaxGroup>();
        player = pg.player;
        layerBaseHeight = transform.localPosition.y;
    }

    void Update()
    {
        if (player) {
            if (player.transform.position.x > pg.xBounds.x && player.transform.position.x < pg.xBounds.y) {
                meshRend.material.mainTextureOffset = Vector2.right * (player.transform.position.x * moveSpeedRatio * pg.baseHorizontalSpeedRatio);
            }
            if (player.transform.position.y > pg.baseHeight) {
                transform.localPosition = new Vector3(transform.localPosition.x, layerBaseHeight + (-(player.transform.position.y-pg.baseHeight) * (pg.baseVerticalSpeedRatio * moveSpeedRatio)), transform.localPosition.z);
            }
        }
    }
}
