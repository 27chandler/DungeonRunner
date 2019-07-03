using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;

public class Pathing_AI : MonoBehaviour {

    private Rigidbody2D rb;
    public Tilemap tile_map;
    [SerializeField] private GameObject pathing_master;
    [SerializeField] private float chase_force = 20.0f;
    private Path_Master pm = null;
    Path_Master.DIRECTION dir = Path_Master.DIRECTION.None;
    // Use this for initialization
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        pm = pathing_master.GetComponent<Path_Master>();
    }

    // Update is called once per frame
    void Update()
    {
        dir = pm.Get_Dir_From_Pos(this.transform.position);
        Vector2 target_pos = transform.position;
        if (dir == Path_Master.DIRECTION.East)
        {
            target_pos.x += 10.0f;
        }
        if (dir == Path_Master.DIRECTION.West)
        {
            target_pos.x -= 10.0f;
        }
        if (dir == Path_Master.DIRECTION.North)
        {
            target_pos.y += 10.0f;
        }
        if (dir == Path_Master.DIRECTION.South)
        {
            target_pos.y -= 10.0f;
        }

        AI_Movement_Utils.Seek_Target(rb, target_pos, chase_force);

    }


    private void OnCollisionStay2D(Collision2D collision)
    {


        //Vector2 dig_target = rb.position + (steering_dir * 0.5f);
        //Vector3Int tile_pos = tile_map.WorldToCell(dig_target);
        //Debug.Log("Current Location: " + dig_target);
        //Debug.Log("Hit at: " + tile_pos);
        //Debug.DrawLine(rb.position, dig_target, Color.white, 2.5f);
        //tile_map.SetTile(tile_pos, null);


    }
}
