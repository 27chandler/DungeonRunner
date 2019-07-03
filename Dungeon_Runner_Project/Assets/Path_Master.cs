using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Path_Master : MonoBehaviour
{
    // This pathfinding script was the original version
    // It uses basic floodfill to find the best route to the destination


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


    [SerializeField] public GameObject path_target;
    [SerializeField] private int max_path_length;
    private Vector3 target_pos;
    [SerializeField] private Tilemap tile_map;
    [SerializeField] private Dictionary<Vector3Int, DIRECTION> path_grid = new Dictionary<Vector3Int, DIRECTION>();
    private Queue<Vector3Int> to_calc_queue = new Queue<Vector3Int>();
    [SerializeField]  private Vector3Int origin;

    // Use this for initialization
    void Start ()
    {
        Calc_Path_Grid();
    }

    void Calc_Path_Grid()
    {
        // Clears the pathfinding system, then adds the start location to the frontier queue
        path_grid.Clear();
        target_pos = path_target.transform.position;
        origin = tile_map.WorldToCell(path_target.transform.position);
        to_calc_queue.Enqueue(origin);
        for (int i = 0; i < max_path_length; i++)
        {
            Recalc_Path();
        }
    }


    void Recalc_Path()
    {
        // Sets the start point
        origin = tile_map.WorldToCell(path_target.transform.position);


        // Calculates frontier for pathfinding
        for (int i = 0; i < to_calc_queue.Count; i++)
        {
            Vector3Int current_front_pos = to_calc_queue.Dequeue();

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
        }


        // Adds adjacent cells for pathfinding frontier to calculate
        foreach (KeyValuePair<Vector3Int, DIRECTION> entry in path_grid)
        {
            if ((!path_grid.ContainsKey(entry.Key + Vector3Int.right)) && (!tile_map.GetTile(entry.Key + Vector3Int.right)))
            {
                to_calc_queue.Enqueue(entry.Key + Vector3Int.right);
            }
            if ((!path_grid.ContainsKey(entry.Key - Vector3Int.right)) && (!tile_map.GetTile(entry.Key - Vector3Int.right)))
            {
                to_calc_queue.Enqueue(entry.Key - Vector3Int.right);
            }
            if ((!path_grid.ContainsKey(entry.Key + Vector3Int.up)) && (!tile_map.GetTile(entry.Key + Vector3Int.up)))
            {
                to_calc_queue.Enqueue(entry.Key + Vector3Int.up);
            }
            if ((!path_grid.ContainsKey(entry.Key - Vector3Int.up)) && (!tile_map.GetTile(entry.Key - Vector3Int.up)))
            {
                to_calc_queue.Enqueue(entry.Key - Vector3Int.up);
            }
        }




        }
	
	// Update is called once per frame
	void Update ()
    {

        if (tile_map.WorldToCell(target_pos) != tile_map.WorldToCell(path_target.transform.position))
        {
            Calc_Path_Grid();
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
