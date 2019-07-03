using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Player_Script : MonoBehaviour {

    private Rigidbody2D rb;
    [SerializeField] private float movement_force;
    [SerializeField] private Tilemap wall_tilemap;
    [SerializeField] private Tilemap floor_tilemap;
    [SerializeField] private Tile gold_tile;
    [SerializeField] private RuleTile place_tile;
    [SerializeField] private Tile advance_tile;
    [SerializeField] private Tile key_tile;
    [SerializeField] private Text inv_text;
    [SerializeField] private Text gold_text;
    [SerializeField] private Text death_text;
    [SerializeField] private float max_action_dist;

    [SerializeField] private GameObject cursor_obj;

    [SerializeField] private Generation_Master gm;
    [SerializeField] private Animator an;
    private Data_Script ds;

    private Camera cam;

    private int max_interact_reach = 1;

    private int inventory_amount = 0;
    private int max_inv_size = 8;

    private int gold_amount = 0;

    private bool is_running = false;
    private bool is_facing_right = true;
    private bool is_dead = false;

    Vector3Int pos_int;
    // Use this for initialization
    void Start ()
    {
        GameObject data_object = GameObject.FindGameObjectWithTag("Data");
        ds = data_object.GetComponent<Data_Script>();

        max_interact_reach = ds.max_interact_reach;

        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
        death_text.enabled = false;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (!is_dead)
        {
            Input_update();
        }
        else
        {
            death_text.enabled = true;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ds.do_reset = true;
                SceneManager.LoadScene("Menu");
            }
        }


        inv_text.text = inventory_amount.ToString() + "/" + max_inv_size.ToString();
        gold_text.text = gold_amount.ToString();

        Tile tile_at_feet = (Tile)floor_tilemap.GetTile(pos_int);

        // Advance to next floor
        if (tile_at_feet == advance_tile)
        {
            if (gm != null)
            {
                gm.do_regeneration = true;
                gm.floor_num++;
                rb.position = new Vector3(4.0f, 4.0f, 0.0f);
            }
        }
        //

        if (is_running)
        {
            an.Play("player_move");
                
        }
        else
        {
            an.Play("player_idle");
        }

        if (is_facing_right)
        {
            Vector3 player_scale = transform.localScale;
            player_scale.x = 1;
            an.transform.localScale = player_scale;
        }
        else
        {
            Vector3 player_scale = transform.localScale;
            player_scale.x = -1;
            an.transform.localScale = player_scale;
        }
    }

    void Input_update()
    {
        Vector2 movement_dir = new Vector2(0.0f, 0.0f);
        Vector3Int arrow_place_pos = new Vector3Int();
        bool is_facing = false;
        //Vector3Int pos_int = new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        pos_int = new Vector3Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y), 0);

        is_running = false;

        if (Input.GetKey(KeyCode.W))
        {
            movement_dir += new Vector2(0.0f, movement_force);
            is_running = true;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement_dir += new Vector2(0.0f, -movement_force);
            is_running = true;
        }
        if (Input.GetKey(KeyCode.D))
        {
            is_facing_right = true;
            movement_dir += new Vector2(movement_force, 0.0f);
            is_running = true;
        }
        if (Input.GetKey(KeyCode.A))
        {
            is_facing_right = false;
            movement_dir += new Vector2(-movement_force, 0.0f);
            is_running = true;
        }
        rb.AddForce(movement_dir);

        Vector3 mouse_pos = cam.ScreenToWorldPoint(Input.mousePosition);

        Vector3Int mouse_tile_pos = wall_tilemap.WorldToCell(mouse_pos);

        if (Input.GetKey(KeyCode.UpArrow))
        {
            arrow_place_pos = pos_int;
            arrow_place_pos.y += 1;
            is_facing = true;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            arrow_place_pos = pos_int;
            arrow_place_pos.y -= 1;
            is_facing = true;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            arrow_place_pos = pos_int;
            arrow_place_pos.x -= 1;
            is_facing = true;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            arrow_place_pos = pos_int;
            arrow_place_pos.x += 1;
            is_facing = true;
        }

        


        if ((is_facing) && (arrow_place_pos != gm.door_pos))
        {
            cursor_obj.transform.position = arrow_place_pos + new Vector3(0.5f, 0.5f, 0.0f);
            if ((Input.GetKey(KeyCode.Space)) && (inventory_amount < max_inv_size) && (wall_tilemap.GetTile(arrow_place_pos) == place_tile))
            {
                wall_tilemap.SetTile(arrow_place_pos, null);
                inventory_amount++;
            }
            else if ((Input.GetKey(KeyCode.LeftShift)) && (inventory_amount > 0) && (wall_tilemap.GetTile(arrow_place_pos) == null))
            {
                wall_tilemap.SetTile(arrow_place_pos, place_tile);
                inventory_amount--;
            }
        }
        else
        {
            cursor_obj.transform.position = mouse_tile_pos + new Vector3(0.5f, 0.5f, 0.0f);

            if ((Vector3Int.Distance(pos_int, mouse_tile_pos) <= max_interact_reach) && (Vector3Int.Distance(pos_int, mouse_tile_pos) != 0))
            {
                if ((Input.GetMouseButton(0)) && (inventory_amount < max_inv_size) && (wall_tilemap.GetTile(mouse_tile_pos) == place_tile))
                {
                    wall_tilemap.SetTile(mouse_tile_pos, null);
                    inventory_amount++;
                }
                else if ((Input.GetMouseButton(1)) && (inventory_amount > 0) && (wall_tilemap.GetTile(mouse_tile_pos) == null))
                {
                    wall_tilemap.SetTile(mouse_tile_pos, place_tile);
                    inventory_amount--;
                }

            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Vector2 collision_point = collision.contacts[0].point;
        collision_point -= (collision.contacts[0].normal.normalized * 0.5f);
        Vector3Int tile_pos = wall_tilemap.WorldToCell(new Vector3(collision_point.x, collision_point.y, 0.0f));

        if (wall_tilemap.GetTile(tile_pos) == gold_tile)
        {
            wall_tilemap.SetTile(tile_pos, null);
            gold_amount++;
            ds.gold_amount = gold_amount;
        }
        else if (wall_tilemap.GetTile(tile_pos) == key_tile)
        {
            gm.Unlock_Door(tile_pos);
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            is_dead = true;
        }
    }
}
