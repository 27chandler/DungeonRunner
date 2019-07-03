using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinding : MonoBehaviour
{
    // This script is the newer version for pathfinding thats similar to floodfill but also adds distance into account
    // when picking the order of calculation for frontiers
    // It generally is less expensive than regular floodfill, except in a few mazelike situations

    public enum DIRECTION
    {
        None = 0x0,
        North = 0x1,
        East = 0x2,
        South = 0x4,
        West = 0x8,
        NorthEast = North | East,
        SouthEast = South | East,
        NorthWest = North | West,
        SouthWest = South | West,
    }

    [SerializeField] private Animator an;

    [SerializeField] private string tile_map_name;
    [SerializeField] private string target_name;

    private GameObject path_target;
    [SerializeField] private GameObject path_origin;
    [SerializeField] private int max_path_length;
    private Tilemap tile_map;
    [SerializeField] private Dictionary<Vector3Int, DIRECTION> path_grid = new Dictionary<Vector3Int, DIRECTION>();
    private List<Vector3Int> to_calc_vec = new List<Vector3Int>();
    private Vector3Int origin;
    private Vector3Int end_point;
    private bool is_path_done = false;


    private Rigidbody2D rb;
    [SerializeField] private float chase_force = 20.0f;
    DIRECTION dir = DIRECTION.None;


    // When the frontier queue is sorted, this is used to sort the queue based on the lowest distance to the destination
    private int compare_dist(Vector3Int a, Vector3Int b)
    {
        int output_a = Mathf.Abs(a.x - end_point.x) + Mathf.Abs(a.y - end_point.y);
        int output_b = Mathf.Abs(b.x - end_point.x) + Mathf.Abs(b.y - end_point.y);

        if (output_a < output_b)
        {
            return -1;
        }
        else
        {
            return 1;
        }

    }


    // Use this for initialization
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Grabs the correct tilemap for the walls
        Tilemap[] tilemap_array = FindObjectsOfType<Tilemap>();

        foreach (Tilemap tm in tilemap_array)
        {
            if (tm.name == tile_map_name)
            {
                tile_map = tm;
            }
        }

        path_target = GameObject.Find(target_name);

        Calc_Path_Grid();
    }

    void Calc_Path_Grid()
    {
        path_grid.Clear();
        origin = tile_map.WorldToCell(path_target.transform.position);
        to_calc_vec.Add(origin);

        is_path_done = false;
        for (int i = 0; i < max_path_length; i++)
        {
            if (is_path_done)
            {
                break;
            }
            Recalc_Path();
        }

        if (is_path_done)
        {
            an.Play("run");
        }
        else
        {
            an.Play("idle");
        }
    }


    void Recalc_Path()
    {
        origin = tile_map.WorldToCell(path_target.transform.position);
        end_point = tile_map.WorldToCell(path_origin.transform.position);

        to_calc_vec.Sort(compare_dist);

        int max_calc_this_frame = 3;

        if (to_calc_vec.Count < max_calc_this_frame)
        {
            max_calc_this_frame = to_calc_vec.Count;
        }


        for (int i = 0; i < max_calc_this_frame; i++)
        {
            Vector3Int current_front_pos = to_calc_vec[i];

            if (current_front_pos == origin)
            {
                path_grid[current_front_pos] = 0x0;
            }

            if (path_grid.ContainsKey(current_front_pos + Vector3Int.right))
            {
                path_grid[current_front_pos] = DIRECTION.East;
            }
            else if (path_grid.ContainsKey(current_front_pos - Vector3Int.right))
            {
                path_grid[current_front_pos] = DIRECTION.West;
            }
            else if (path_grid.ContainsKey(current_front_pos + Vector3Int.up))
            {
                path_grid[current_front_pos] = DIRECTION.North;
            }
            else if (path_grid.ContainsKey(current_front_pos - Vector3Int.up))
            {
                path_grid[current_front_pos] = DIRECTION.South;
            }

            if (end_point == current_front_pos)
            {
                is_path_done = true;
                break;
            }
        }

        to_calc_vec.Clear();


        if (!is_path_done)
        {
            foreach (KeyValuePair<Vector3Int, DIRECTION> entry in path_grid)
            {
                if ((!path_grid.ContainsKey(entry.Key + Vector3Int.right)) && (!tile_map.GetTile(entry.Key + Vector3Int.right)))
                {
                    to_calc_vec.Add(entry.Key + Vector3Int.right);
                }
                if ((!path_grid.ContainsKey(entry.Key - Vector3Int.right)) && (!tile_map.GetTile(entry.Key - Vector3Int.right)))
                {
                    to_calc_vec.Add(entry.Key - Vector3Int.right);
                }
                if ((!path_grid.ContainsKey(entry.Key + Vector3Int.up)) && (!tile_map.GetTile(entry.Key + Vector3Int.up)))
                {
                    to_calc_vec.Add(entry.Key + Vector3Int.up);
                }
                if ((!path_grid.ContainsKey(entry.Key - Vector3Int.up)) && (!tile_map.GetTile(entry.Key - Vector3Int.up)))
                {
                    to_calc_vec.Add(entry.Key - Vector3Int.up);
                }
            }
        }
 
    }

    // Update is called once per frame
    void Update()
    {

        Calc_Path_Grid();



        dir = Get_Dir_From_Pos(this.transform.position);
        Vector2 move_pos = transform.position;
        if (dir == DIRECTION.East)
        {
            move_pos.x += 10.0f;
        }
        if (dir == DIRECTION.West)
        {
            move_pos.x -= 10.0f;
        }
        if (dir == DIRECTION.North)
        {
            move_pos.y += 10.0f;
        }
        if (dir == DIRECTION.South)
        {
            move_pos.y -= 10.0f;
        }

        AI_Movement_Utils.Seek_Target(rb, move_pos, chase_force);

    }

    public void Draw_Debug() // Just debug draws the pathfinding zones
    {
        foreach(var item in path_grid.Keys)
        {
            Debug.DrawLine(item, origin,Color.red,0.1f);
        }
    }

    public DIRECTION Get_Dir_From_Pos(Vector3 i_pos)
    {
        if (path_grid.ContainsKey(tile_map.WorldToCell(i_pos)))
        {
            return path_grid[tile_map.WorldToCell(i_pos)];
        }
        else
        {
            return DIRECTION.None;
        }

    }
}
