using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Generation_Master : MonoBehaviour
{
    // World types for the generator to use
    public enum WORLD_GEN_TYPE {NORMAL,ROOM_GRID };
    [SerializeField] WORLD_GEN_TYPE world_type = WORLD_GEN_TYPE.NORMAL;
    //

    // Tile maps
    [SerializeField] private Tilemap[] room_maps; // The different rooms which can spawn within the world by the world generator
    [SerializeField] private Tilemap end_room; // The room that contains the stairs to the next floor
    [SerializeField] private Tilemap spawn_room; // The room the player starts in
    [SerializeField] private Tilemap dungeon_walls; // The walls of the dungeon
    [SerializeField] private Tilemap dungeon_floor; // The floor of the dungeon
    //

    // Tile types
    [SerializeField] private Tile[] random_tile_replace; // This tile is drawn only within the room prefabs and is replaced by the generator into other types, not used currently
    [SerializeField] private Tile locked_door; // The tile that is placed ontop of the stairs to prevent the player from leaving without getting the key
    [SerializeField] private Tile key_tile; // The key that unlocks the locked door tile
    [SerializeField] private RuleTile dig_wall; // The tile which the player can dig through
    [SerializeField] private RuleTile perm_wall; // The tile that the player cannot dig through
    [SerializeField] private Tile[] floor_array_tiles; // Types of flooring for random flooring effect, not used currently
    [SerializeField] private Tile gold_tile; // The tile that is gold
    [SerializeField] private Tile advance_tile; // The stairs tile for advancing to the next floor

    [SerializeField] private Vector2Int world_size; // The size of the world map (this is multiplied by 10 for easier room placement)
    [SerializeField] private RectInt start_room; // Size of the start room, not used except in the older generators

    [SerializeField] private GameObject[] enemy_types; // The types of enemies that can naturally spawn with the generator
    [SerializeField] private GameObject[] unspawned_enemy_types; // The types of enemies that aren't directly spawned by the world gen

    // Various variables (that vary)
    [SerializeField] private int num_of_rooms; // The number of rooms to spawn per floor
    [SerializeField] private int num_of_enemies; // The number of enemies to spawn per floor
    [SerializeField] private int num_of_floors = 6;

    [SerializeField] private int cave_wall_density_percent;
    [SerializeField] private int wall_carve_size; // Was used in an experimental way to connect rooms, worked but didn't look nice
    [SerializeField] private int max_corridor_length;

    [SerializeField] private Text floor_text;
    [SerializeField] private float floor_text_display_time;


    private Data_Script ds;
    private CompositeCollider2D cc2d;

    

    private Dictionary<Vector3Int, TileBase> world_tile_map = new Dictionary<Vector3Int, TileBase>();

    public Vector3Int door_pos; // Position of the exit door

    public bool do_regeneration = false;
    public int floor_num = 1; // The number of the current floor

    private Vector3Int focus_pos; // Position used to keep track of where the generator is generating tiles

    private bool is_rand_place_done = false;
    Random.State world_rand_state; // Stores the randomizer state when finishing generating a level. This stops the random movement of the enemies from affecting the world gen in later levels

    // Use this for initialization
    void Start ()
    {
        GameObject data_object = GameObject.FindGameObjectWithTag("Data");
        ds = data_object.GetComponent<Data_Script>();
        world_size = ds.world_size;
        world_type = ds.world_gen_type;

        Random.InitState(ds.world_seed);

        world_rand_state = Random.state;

        do_regeneration = true;
        cc2d = dungeon_walls.GetComponent<CompositeCollider2D>();
    }

    private void Copy_To_World(Tilemap i_map, int i_focus_x, int i_focus_y)
    {
        i_map.CompressBounds();
        BoundsInt map_bounds = i_map.cellBounds;
        //TileBase[] t_base = room_maps[i_room_id].GetTilesBlock(map_bounds);
        Vector3Int offset = new Vector3Int(i_focus_x, i_focus_y, 0);
        Tilemap room = i_map;

        foreach (var pos in map_bounds.allPositionsWithin)
        {
            if (room.GetTile(pos) == advance_tile)
            {
                dungeon_floor.SetTile(pos + offset, room.GetTile(pos));
                Add_Tile(pos + offset, locked_door);
                door_pos = pos + offset;
            }
            else
            {
                Add_Tile(pos + offset, room.GetTile(pos));
            }
        }
    }

    private void Add_Key()
    {
        bool is_key_placed = false;
        while (!is_key_placed)
        {
            Vector3Int rand_key_pos = new Vector3Int(Random.Range(10, world_size.x * 10), Random.Range(10, world_size.y * 10), 0);

            if (Get_Tile(rand_key_pos) != perm_wall)
            {
                Add_Tile(rand_key_pos, key_tile);
                is_key_placed = true;
            }
        }

    }

    private bool Check_Bounds_Clear(BoundsInt i_bounds,Vector3Int i_pos, RuleTile i_tile)
    {
        bool is_clear = true;
        foreach(Vector3Int pos in i_bounds.allPositionsWithin)
        {
            if (dungeon_walls.GetTile(i_pos + pos) == i_tile)
            {
                is_clear = false;
            }
        }
        return is_clear;
    }

    private bool Check_Simple_Corridor(Vector3Int i_start_pos, Vector3Int i_dir, int i_length, RuleTile i_tile)
    {
        bool is_clear = true;
        for (int i = 0; i < i_length+1; i++)
        {
            if (dungeon_walls.GetTile(i_start_pos + (i_dir * i)) == i_tile)
            {
                is_clear = false;
            }
        }

        return is_clear;

    }

    private void Simple_Corridor(Vector3Int i_start_pos, Vector3Int i_dir, int i_length)
    {

        for (int i = 0; i < i_length+1; i++)
        {
            dungeon_walls.SetTile(i_start_pos + (i_dir * i), null);
        }
    }

    private void Grid_Based_Room_Generation()
    {
        // Fills the world in with dig walls
        Fill_Area(dig_wall, 0, 0, world_size.x * 10, world_size.y * 10);



        // Places room at random in a grid
        for (int i = 0; i < world_size.x; i++)
        {
            for (int j = 0; j < world_size.y; j++)
            {
                if ((i == world_size.x - 1) && (j == world_size.y - 1))
                {
                    Copy_To_World(end_room, 10 * i, 10 * j);
                }
                else if ((i == 0) && (j == 0))
                {
                    Copy_To_World(spawn_room, 10 * i, 10 * j);
                }
                else
                {
                    Copy_To_World(room_maps[Random.Range(0, room_maps.Length)], 10 * i, 10 * j);
                }
            }
            
        }

        // Fills in the edges with permanent walls
        for (int i = 0; i < world_size.x * 10; i++)
        {
            for (int j = 0; j < world_size.y * 10; j++)
            {
                if ((i == 0) || (j == 0) || (i + 1 == world_size.x * 10) || (j + 1 == world_size.y * 10))
                {
                    Add_Tile(new Vector3Int(i, j, 0), perm_wall);
                }
            }
        }

        // Sets the world to mostly perm walls
        Populate_World(8, perm_wall);

        // Connects up seperate rooms
        Room_Connect();
    }

    private void Random_Tile_Distribution(Tilemap i_tilemap)
    {
        i_tilemap.CompressBounds();
        BoundsInt map_bounds = i_tilemap.cellBounds;
        foreach(Tile tile_type in random_tile_replace)
        {
            int rand_num = Random.Range(0, 3);
            foreach (Vector3Int pos in map_bounds.allPositionsWithin)
            {
                if (dungeon_walls.GetTile(pos) == tile_type)
                {
                    switch(rand_num)
                    {
                        case 0:
                            {
                                dungeon_walls.SetTile(pos, perm_wall);
                                break;
                            }
                        case 1:
                            {
                                dungeon_walls.SetTile(pos, dig_wall);
                                break;
                            }
                        case 2:
                            {
                                dungeon_walls.SetTile(pos, null);
                                break;
                            }
                    }
                }
            }

        }
    }

    private void Populate_World(int birth_amount, RuleTile place_tile)
    {
        List<Vector3Int> birth_list = new List<Vector3Int>();
        // Birth or death the cells depending on neighbours
        for (int i = 0; i < world_size.x * 10; i++)
        {
            for (int j = 0; j < world_size.y * 10; j++)
            {
                if ((i != 0) && (j != 0) && (i + 1 != world_size.x * 10) && (j + 1 != world_size.y * 10))
                {
                    Vector3Int target_pos = new Vector3Int(i, j, 0);

                    List<Vector3Int> neighbours = new List<Vector3Int>();

                    neighbours.Add(target_pos + new Vector3Int(1, 0, 0));
                    neighbours.Add(target_pos + new Vector3Int(-1, 0, 0));
                    neighbours.Add(target_pos + new Vector3Int(0, 1, 0));
                    neighbours.Add(target_pos + new Vector3Int(0, -1, 0));
                    neighbours.Add(target_pos + new Vector3Int(1, 1, 0));
                    neighbours.Add(target_pos + new Vector3Int(-1, 1, 0));
                    neighbours.Add(target_pos + new Vector3Int(1, -1, 0));
                    neighbours.Add(target_pos + new Vector3Int(-1, -1, 0));

                    int neighbour_count = 0;

                    foreach (var neighbour_check in neighbours)
                    {
                        if (Get_Tile(neighbour_check) != null)
                        {
                            neighbour_count++;
                        }
                    }



                    if ((neighbour_count >= birth_amount) && (Get_Tile(target_pos) != null))
                    {
                        birth_list.Add(target_pos);
                    }
                }
            }
        }


        foreach (var birth_pos in birth_list)
        {
            Add_Tile(birth_pos, place_tile);
            //dungeon_walls.SetTile(birth_pos, place_tile);
            //yield return null;
        }
    }

    private void Carve_Rooms(Dictionary<Vector3Int, int> i_room_map)
    {
        Vector3Int start_pos = new Vector3Int(4, 4, 0);

        Dictionary<Vector3Int, int> wall_map = new Dictionary<Vector3Int, int>();


        for (int i = 0; i < world_size.x * 10; i++)
        {
            for (int j = 0; j < world_size.y * 10; j++)
            {
                start_pos = new Vector3Int(i, j, 0);
                if (!i_room_map.ContainsKey(start_pos))
                {
                    int unique_neighbour_count = 0;
                    List<Vector3Int> neighbour_pos = new List<Vector3Int>();
                    List<int> neighbour_types = new List<int>();

                    neighbour_pos.Add(start_pos + new Vector3Int(1, 0, 0));
                    neighbour_pos.Add(start_pos + new Vector3Int(-1, 0, 0));
                    neighbour_pos.Add(start_pos + new Vector3Int(0, 1, 0));
                    neighbour_pos.Add(start_pos + new Vector3Int(0, -1, 0));
                    neighbour_pos.Add(start_pos + new Vector3Int(1, 1, 0));
                    neighbour_pos.Add(start_pos + new Vector3Int(-1, 1, 0));
                    neighbour_pos.Add(start_pos + new Vector3Int(1, -1, 0));
                    neighbour_pos.Add(start_pos + new Vector3Int(-1, -1, 0));

                    neighbour_pos.Add(start_pos + new Vector3Int(2, 0, 0));
                    neighbour_pos.Add(start_pos + new Vector3Int(-2, 0, 0));
                    neighbour_pos.Add(start_pos + new Vector3Int(0, 2, 0));
                    neighbour_pos.Add(start_pos + new Vector3Int(0, -2, 0));

                    foreach (var neighbour in neighbour_pos)
                    {
                        if (i_room_map.ContainsKey(neighbour))
                        {
                            bool is_new_neighbour_type = true;
                            foreach (var type in neighbour_types)
                            {
                                if (i_room_map[neighbour] == type)
                                {
                                    is_new_neighbour_type = false;
                                }
                            }

                            if (is_new_neighbour_type)
                            {
                                unique_neighbour_count++;
                                neighbour_types.Add(i_room_map[neighbour]);
                            }
                        }
                    }
                    wall_map.Add(start_pos, unique_neighbour_count);
                }
            }
        }


        foreach (var tile in wall_map)
        {
            if (tile.Value >= 2)
            {
                dungeon_walls.SetTile(tile.Key, dig_wall);

                dungeon_walls.SetTile(tile.Key + Vector3Int.up, dig_wall);
                dungeon_walls.SetTile(tile.Key + Vector3Int.down, dig_wall);
                dungeon_walls.SetTile(tile.Key + Vector3Int.left, dig_wall);
                dungeon_walls.SetTile(tile.Key + Vector3Int.right, dig_wall);
            }
        }
    }

    private void Room_Connect()
    {
        int num_of_rooms = 2;
        while (num_of_rooms > 1)
        {
            
            int step_num = 0;
            Vector3Int start_pos = new Vector3Int(4, 4, 0);

            Dictionary<Vector3Int, int> room_map = new Dictionary<Vector3Int, int>();
            Dictionary<Vector3Int, int> frontier_map = new Dictionary<Vector3Int, int>();



            bool is_done = false;
            bool is_empty_cell_found = false;

            do
            {
                is_empty_cell_found = false;
                for (int i = 0; i < world_size.x * 10; i++)
                {
                    for (int j = 0; j < world_size.y * 10; j++)
                    {
                        if (!is_empty_cell_found)
                        {
                            if ((Get_Tile(new Vector3Int(i, j, 0)) != perm_wall) && (!room_map.ContainsKey(new Vector3Int(i, j, 0))))
                            {
                                start_pos = new Vector3Int(i, j, 0);
                                is_empty_cell_found = true;
                                step_num++;
                                frontier_map.Add(start_pos, step_num);

                                is_done = false;
                            }
                        }
                    }
                }

                num_of_rooms = step_num;


            while (!is_done)
                {
                    foreach (var cell in room_map)
                    {
                        Vector3Int up_tile = cell.Key + new Vector3Int(0, 1, 0);
                        Vector3Int down_tile = cell.Key + new Vector3Int(0, -1, 0);
                        Vector3Int left_tile = cell.Key + new Vector3Int(-1, 0, 0);
                        Vector3Int right_tile = cell.Key + new Vector3Int(1, 0, 0);

                        if ((!room_map.ContainsKey(up_tile)) && (Get_Tile(up_tile) != perm_wall))
                        {
                            if (!frontier_map.ContainsKey(up_tile))
                            {
                                frontier_map.Add(up_tile, step_num);
                            }
                        }
                        if ((!room_map.ContainsKey(down_tile)) && (Get_Tile(down_tile) != perm_wall))
                        {
                            if (!frontier_map.ContainsKey(down_tile))
                            {
                                frontier_map.Add(down_tile, step_num);
                            }
                        }
                        if ((!room_map.ContainsKey(left_tile)) && (Get_Tile(left_tile) != perm_wall))
                        {
                            if (!frontier_map.ContainsKey(left_tile))
                            {
                                frontier_map.Add(left_tile, step_num);
                            }
                        }
                        if ((!room_map.ContainsKey(right_tile)) && (Get_Tile(right_tile) != perm_wall))
                        {
                            if (!frontier_map.ContainsKey(right_tile))
                            {
                                frontier_map.Add(right_tile, step_num);
                            }
                        }
                    }

                    // Merge maps
                    foreach (KeyValuePair<Vector3Int, int> cell in frontier_map)
                    {
                        room_map.Add(cell.Key, cell.Value);
                        //dungeon_floor.SetTile(cell.Key, gold_tile);
                    }





                    if (frontier_map.Count <= 0)
                    {
                        is_done = true;
                    }

                    frontier_map.Clear();

                }
            } while (is_empty_cell_found == true);



            // Delete spawn room from system
            for (int i = 0; i < start_room.width; i++)
            {
                for (int j = 0; j < start_room.height; j++)
                {
                    if (room_map.ContainsKey(new Vector3Int(i, j, 0)))
                    {
                        room_map.Remove(new Vector3Int(i, j, 0));
                    }
                }
            }



            // Connect caves
            Vector3Int corridor_start = new Vector3Int(0, 0, 0);
            Vector3Int corridor_end = new Vector3Int(0, 0, 0);
            bool is_carve_ready = false;
            while (step_num > 1)
            {
                for (int i = 0; i < world_size.x * 10; i++)
                {
                    for (int j = 0; j < world_size.y * 10; j++)
                    {
                        if (room_map.ContainsKey(new Vector3Int(i, j, 0)))
                        {
                            if (room_map[new Vector3Int(i, j, 0)] == step_num)
                            {
                                start_pos = new Vector3Int(i, j, 0);

                                for (int k = 1; k < max_corridor_length; k++)
                                {
                                    Vector3Int up_tile = start_pos + (new Vector3Int(0, 1, 0) * k);
                                    Vector3Int down_tile = start_pos + (new Vector3Int(0, -1, 0) * k);
                                    Vector3Int left_tile = start_pos + (new Vector3Int(-1, 0, 0) * k);
                                    Vector3Int right_tile = start_pos + (new Vector3Int(1, 0, 0) * k);



                                    if (room_map.ContainsKey(down_tile))
                                    {
                                        if (room_map[down_tile] != step_num)
                                        {
                                            corridor_start = start_pos;
                                            corridor_end = down_tile;
                                            is_carve_ready = true;
                                            break;
                                        }
                                    }

                                    if (room_map.ContainsKey(up_tile))
                                    {
                                        if (room_map[up_tile] != step_num)
                                        {
                                            corridor_start = start_pos;
                                            corridor_end = up_tile;
                                            is_carve_ready = true;
                                            break;
                                        }
                                    }

                                    if (room_map.ContainsKey(left_tile))
                                    {
                                        if (room_map[left_tile] != step_num)
                                        {
                                            corridor_start = start_pos;
                                            corridor_end = left_tile;
                                            is_carve_ready = true;
                                            break;
                                        }
                                    }

                                    if (room_map.ContainsKey(right_tile))
                                    {
                                        if (room_map[right_tile] != step_num)
                                        {
                                            corridor_start = start_pos;
                                            corridor_end = right_tile;
                                            is_carve_ready = true;
                                            break;
                                        }
                                    }


                                }

                                if (is_carve_ready)
                                {

                                    Vector3Int carve_pos = corridor_start;
                                    Vector3Int carve_dir = (corridor_end - corridor_start);
                                    carve_dir.x = Mathf.Clamp(carve_dir.x, -1, 1);
                                    carve_dir.y = Mathf.Clamp(carve_dir.y, -1, 1);

                                    while (carve_pos != corridor_end)
                                    {
                                        if (Get_Tile(carve_pos) == perm_wall)
                                        {
                                            Add_Tile(carve_pos, null);
                                            //dungeon_walls.SetTile(carve_pos, null);
                                        }
                                        carve_pos += carve_dir;
                                    }
                                    step_num--;
                                    is_carve_ready = false;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void Cave_Gen(int birth_threshold, int death_threshold,int percent_fill,int num_of_iterations)
    {
        cc2d.enabled = false;
        List<Vector3Int> death_list = new List<Vector3Int>();
        List<Vector3Int> birth_list = new List<Vector3Int>();



        // Places tiles within the world at random
        if (!is_rand_place_done)
        {
            for (int i = 0; i < world_size.x * 10; i++)
            {
                for (int j = 0; j < world_size.y * 10; j++)
                {
                    if ((i != 0) && (j != 0) && (i+1 != world_size.x * 10) && (j+1 != world_size.y * 10))
                    {
                        //int rand_cell = (int)(Random.value * 100);
                        int rand_cell = Random.Range(0, 101);



                        if (rand_cell <= percent_fill)
                        {
                            Add_Tile(new Vector3Int(i, j, 0), dig_wall);
                            //dungeon_walls.SetTile(new Vector3Int(i, j, 0), dig_wall);
                        }
                        else
                        {
                            Add_Tile(new Vector3Int(i, j, 0), null);
                            //dungeon_walls.SetTile(new Vector3Int(i, j, 0), null);
                        }
                    }
                    else
                    {
                        Add_Tile(new Vector3Int(i, j, 0), perm_wall);
                        //dungeon_walls.SetTile(new Vector3Int(i, j, 0), perm_wall);
                    }
                }
            }
        }

        is_rand_place_done = true;


        for (int k = 0; k < num_of_iterations; k++)
        {
            // Birth or death the cells depending on neighbours
            for (int i = 0; i < world_size.x * 10; i++)
            {
                for (int j = 0; j < world_size.y * 10; j++)
                {
                    if ((i != 0) && (j != 0) && (i+1 != world_size.x * 10) && (j+1 != world_size.y * 10))
                    {
                        Vector3Int target_pos = new Vector3Int(i, j, 0);

                        Vector3Int[] neighbours = new Vector3Int[8];

                        neighbours[0] = target_pos + new Vector3Int(1, 0, 0);
                        neighbours[1] = target_pos + new Vector3Int(-1, 0, 0);
                        neighbours[2] = target_pos + new Vector3Int(0, 1, 0);
                        neighbours[3] = target_pos + new Vector3Int(0, -1, 0);
                        neighbours[4] = target_pos + new Vector3Int(1, 1, 0);
                        neighbours[5] = target_pos + new Vector3Int(-1, 1, 0);
                        neighbours[6] = target_pos + new Vector3Int(1, -1, 0);
                        neighbours[7] = target_pos + new Vector3Int(-1, -1, 0);

                        int neighbour_count = 0;

                        foreach (var neighbour_check in neighbours)
                        {
                            
                            if (Get_Tile(neighbour_check) != null)
                            {
                                neighbour_count++;
                            }
                        }



                        if (neighbour_count >= birth_threshold)
                        {
                            birth_list.Add(target_pos);
                        }

                        if (neighbour_count <= death_threshold)
                        {
                            death_list.Add(target_pos);
                        }

                    }
                }
            }





            foreach (var death_pos in death_list)
            {
                Add_Tile(death_pos, null);
            }

            foreach (var birth_pos in birth_list)
            {
                Add_Tile(birth_pos, dig_wall);
            }
        }

        // Pastes in the spawn room at the start zone
        Copy_To_World(spawn_room, 0, 0);
        
        // Changes most walls into perm walls
        Populate_World(8, perm_wall);
        

        // Randomly places a few roooms around the map
        for (int i = 0; i < num_of_rooms; i++)
        {
            Tilemap rand_room_type = room_maps[Random.Range(0, room_maps.Length)];
            rand_room_type.CompressBounds();
            int room_width = rand_room_type.cellBounds.size.x;
            int room_height = rand_room_type.cellBounds.size.y;

            Copy_To_World(rand_room_type, Random.Range(10,(world_size.x*10) - (room_width+10)), Random.Range(10, (world_size.y*10) - (room_height + 10)));
        }

        // Places the end room at the end zone
        end_room.CompressBounds();
        Copy_To_World(end_room, (world_size.x * 10) - end_room.cellBounds.size.x, (world_size.y * 10) - end_room.cellBounds.size.y);

        // Connects all walkable areas to each other to ensure its possible to reach every area
        Room_Connect();

        cc2d.enabled = true;
    }

    public void Unlock_Door(Vector3Int i_key_pos)
    {
        // removes the door and the key used to open it
        dungeon_walls.SetTile(door_pos, null);
        dungeon_walls.SetTile(i_key_pos, null);
    }

    private void Generate_Grid_World()
    {
        // This function generates a castle world in a grid format
        Random.state = world_rand_state;

        world_tile_map.Clear();

        is_rand_place_done = false;

        for (int i = 0; i < world_size.x * 10; i++)
        {
            for (int j = 0; j < world_size.y * 10; j++)
            {
                dungeon_walls.SetTile(new Vector3Int(i, j, 0), null);
            }
        }


        focus_pos = dungeon_walls.origin;


        // Deletes all enemies that might still exist from the previous level
        foreach (GameObject delete in GameObject.FindObjectsOfType(typeof(GameObject)))
        {
            foreach (GameObject enemy in enemy_types)
            {
                if (delete.name == (enemy.name + "(Clone)"))
                {
                    Destroy(delete);
                }
            }

            foreach (GameObject enemy in unspawned_enemy_types)
            {
                if (delete.name == (enemy.name + "(Clone)"))
                {
                    Destroy(delete);
                }
            }
        }

        // Generates the grid based room system
        Grid_Based_Room_Generation();

        // Spawns the key somewhere within the world at random
        Add_Key();

        // Reads the world map(dictionary) and pastes it into the world tilemap
        Set_Tiles_To_Tilemap();


        // Spawn enemies randomly around the map
        for (int i = 0; i < num_of_enemies; i++)
        {
            focus_pos.x = Random.Range(10, world_size.x * 10);
            focus_pos.y = Random.Range(10, world_size.y * 10);

            if ((dungeon_walls.GetTile(focus_pos) == null))
            {
                int rand_enemy_id = Random.Range(0, floor_num+1);
                if (rand_enemy_id >= enemy_types.Length)
                {
                    rand_enemy_id = 5;
                }

                GameObject new_enemy = Instantiate(enemy_types[rand_enemy_id]);
                new_enemy.transform.position = new Vector3(focus_pos.x + 0.5f, focus_pos.y + 0.5f, -0.2f);
            }
            else
            {
                i--;
            }
        }
        //

        world_rand_state = Random.state;
    }


    private void Generate_World()
    {



        Random.state = world_rand_state;

        world_tile_map.Clear();

        is_rand_place_done = false;

        for (int i = 0; i < world_size.x * 10; i++)
        {
            for (int j = 0; j < world_size.y * 10; j++)
            {
                dungeon_walls.SetTile(new Vector3Int(i, j, 0), null);
            }
        }


        focus_pos = dungeon_walls.origin;

        foreach (GameObject delete in GameObject.FindObjectsOfType(typeof(GameObject)))
        {
            foreach (GameObject enemy in enemy_types)
            {
                if (delete.name == (enemy.name + "(Clone)"))
                {
                    Destroy(delete);
                }
            }

            foreach (GameObject enemy in unspawned_enemy_types)
            {
                if (delete.name == (enemy.name + "(Clone)"))
                {
                    Destroy(delete);
                }
            }
        }


        // Spawns a cave system around the map with the specified density
        Cave_Gen(5, 2, cave_wall_density_percent + (floor_num-1), 15); // The "floor_num-1" means the world will get more dense the higher the floor number is, the affect occurs slowly but is noticeable

        // Spawns the key somewhere within the world at random
        Add_Key();

        // Places all tiles form the world map (dictionary) to the tilemap
        Set_Tiles_To_Tilemap();

        // Spawn enemies randomly around the map
        for (int i = 0; i < num_of_enemies; i++)
        {
            focus_pos.x = Random.Range(10, world_size.x*10);
            focus_pos.y = Random.Range(10, world_size.y*10);

            if ((dungeon_walls.GetTile(focus_pos) == null))
            {
                int rand_enemy_id = Random.Range(0, floor_num+1);
                if (rand_enemy_id >= enemy_types.Length)
                {
                    rand_enemy_id = 5;
                }

                GameObject new_enemy = Instantiate(enemy_types[rand_enemy_id]);
                new_enemy.transform.position = new Vector3(focus_pos.x + 0.5f, focus_pos.y + 0.5f, -0.2f);
            }
            else
            {
                i--;
            }
        }
        //

        world_rand_state = Random.state;
    }
	
	// Update is called once per frame
	void FixedUpdate ()
    {
        if (floor_num == num_of_floors) // When the player has reached the last floor
        {
            SceneManager.LoadScene("Win_Screen");
        }


        if (do_regeneration)
        {
            switch (world_type)
            {
                case WORLD_GEN_TYPE.NORMAL:
                    {
                        Generate_World();
                        break;
                    }
                case WORLD_GEN_TYPE.ROOM_GRID:
                    {
                        Generate_Grid_World();
                        break;
                    }
            }

            do_regeneration = false;
            StartCoroutine(Display_Floor_Text());
        }
    }

    private IEnumerator Display_Floor_Text()
    {
        float text_timer = 0.0f;
        while (text_timer < floor_text_display_time)
        {
            floor_text.text = "[Floor " + floor_num.ToString() + "]";
            text_timer += Time.deltaTime;
            yield return null;
        }
        floor_text.text = "";
    }

    void Fill_Area(TileBase i_tile_type, int i_start_x, int i_start_y, int i_end_x, int i_end_y)
    {
        Vector3Int fill_focus = new Vector3Int(i_start_x, i_start_y, 0);

        for (int i = i_start_x; i < i_end_x; i++)
        {
            fill_focus.x = i;
            for (int j = i_start_y; j < i_end_y; j++)
            {
                fill_focus.y = j;
                Add_Tile(fill_focus, i_tile_type);
            }
        }


    }

    private void Generate_Corridor(Vector3Int i_entrance_point, Vector3Int i_exit_point,RuleTile i_block_tile)
    {
        // Generates a corridor from two points using pathfinding, not used in final version but I left it in here so you can look at it
        if ((dungeon_walls.GetTile(i_entrance_point) != i_block_tile) && (dungeon_walls.GetTile(i_exit_point) != i_block_tile))
        {
            Dictionary<Vector3Int, int> corridor_find_map = new Dictionary<Vector3Int, int>();
            corridor_find_map.Add(i_entrance_point, 0);
            bool is_done = false;

            int step_counter = 1;

            // Uses a simple pathfinding system to generate the shortest route between the two points
            while (!is_done)
            {
                Dictionary<Vector3Int, int> adjacent_cells_map = new Dictionary<Vector3Int, int>();
                // Grab all adjacent cells
                foreach (KeyValuePair<Vector3Int, int> cell in corridor_find_map)
                {
                    Vector3Int up_tile = cell.Key + new Vector3Int(0, 1, 0);
                    Vector3Int down_tile = cell.Key + new Vector3Int(0, -1, 0);
                    Vector3Int left_tile = cell.Key + new Vector3Int(-1, 0, 0);
                    Vector3Int right_tile = cell.Key + new Vector3Int(1, 0, 0);
                    if ((!corridor_find_map.ContainsKey(up_tile)) && (dungeon_walls.GetTile(up_tile) != i_block_tile))
                    {
                        if (!adjacent_cells_map.ContainsKey(up_tile))
                        {
                            adjacent_cells_map.Add(up_tile, step_counter);
                        }
                    }
                    if ((!corridor_find_map.ContainsKey(down_tile)) && (dungeon_walls.GetTile(down_tile) != i_block_tile))
                    {
                        if (!adjacent_cells_map.ContainsKey(down_tile))
                        {
                            adjacent_cells_map.Add(down_tile, step_counter);
                        }
                    }
                    if ((!corridor_find_map.ContainsKey(left_tile)) && (dungeon_walls.GetTile(left_tile) != i_block_tile))
                    {
                        if (!adjacent_cells_map.ContainsKey(left_tile))
                        {
                            adjacent_cells_map.Add(left_tile, step_counter);
                        }
                    }
                    if ((!corridor_find_map.ContainsKey(right_tile)) && (dungeon_walls.GetTile(right_tile) != i_block_tile))
                    {
                        if (!adjacent_cells_map.ContainsKey(right_tile))
                        {
                            adjacent_cells_map.Add(right_tile, step_counter);
                        }
                    }
                }

                // Merge maps
                foreach (KeyValuePair<Vector3Int, int> cell in adjacent_cells_map)
                {
                    corridor_find_map.Add(cell.Key, cell.Value);
                }
                adjacent_cells_map.Clear();

                step_counter++;


                if ((corridor_find_map.ContainsKey(i_exit_point)) || (step_counter > 900))
                {
                    is_done = true;
                }
            }


            Vector3Int focus_pos = i_exit_point;

            step_counter = corridor_find_map[i_exit_point];

            while (step_counter != 0)
            {
                dungeon_walls.SetTile(focus_pos, null);
                Vector3Int up_tile = focus_pos + new Vector3Int(0, 1, 0);
                Vector3Int down_tile = focus_pos + new Vector3Int(0, -1, 0);
                Vector3Int left_tile = focus_pos + new Vector3Int(-1, 0, 0);
                Vector3Int right_tile = focus_pos + new Vector3Int(1, 0, 0);
                if (corridor_find_map.ContainsKey(up_tile))
                {
                    if (corridor_find_map[up_tile] < step_counter)
                    {
                        focus_pos = up_tile;
                    }
                }
                if (corridor_find_map.ContainsKey(down_tile))
                {
                    if (corridor_find_map[down_tile] < step_counter)
                    {
                        focus_pos = down_tile;
                    }
                }
                if (corridor_find_map.ContainsKey(left_tile))
                {
                    if (corridor_find_map[left_tile] < step_counter)
                    {
                        focus_pos = left_tile;
                    }
                }
                if (corridor_find_map.ContainsKey(right_tile))
                {
                    if (corridor_find_map[right_tile] < step_counter)
                    {
                        focus_pos = right_tile;
                    }
                }

                step_counter--;
            }
        }
 

    }

    private void Add_Tile(Vector3Int i_pos, TileBase i_tilebase)
    {
        // Adds a tile to the world map (dictionary)
        if (world_tile_map.ContainsKey(i_pos))
        {
            world_tile_map[i_pos] = i_tilebase;
        }
        else
        {
            world_tile_map.Add(i_pos, i_tilebase);
        }
    }

    private TileBase Get_Tile(Vector3Int i_pos)
    {
        // Gets a tile from the world map (dictionary)
        if (world_tile_map.ContainsKey(i_pos))
        {
            return world_tile_map[i_pos];
        }
        else
        {
            return null;
        }
    }

    private void Set_Tiles_To_Tilemap()
    {
        for (int i = 0; i < world_size.x * 10; i++)
        {
            for (int j = 0; j < world_size.y * 10; j++)
            {
                dungeon_walls.SetTile(new Vector3Int(i,j,0), null);
            }
        }

        foreach (var map_tile in world_tile_map)
        {
            dungeon_walls.SetTile(map_tile.Key, map_tile.Value);
        }
        world_tile_map.Clear();
    }
}
