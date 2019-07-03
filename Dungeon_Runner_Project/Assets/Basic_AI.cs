using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Basic_AI : MonoBehaviour {

    private Rigidbody2D rb;
    private Tilemap tile_map;
    [SerializeField] string tile_map_name;
    [SerializeField] RuleTile destructable_tile = null;
    public Vector2 steering_dir;
    [SerializeField] private GameObject target_obj;
    private Vector2 target_last_seen_pos;
    [SerializeField] private float chase_force = 20.0f; // Force/speed the enemy chases with

    [SerializeField] private bool is_hungry = false; // Digs through walls
    [SerializeField] private bool is_ranged = false; // Can fire projectiles
    [SerializeField] private bool is_always_chasing = false; // Constantly knows where the player is

    [SerializeField] private GameObject projectile;

    private float eat_timer = 0.0f;
    [SerializeField] private float eat_cooldown = 2.0f;
    [SerializeField] private Animator an;


    private Field_Of_View fov;
    private float timer = 0.0f;
    private float wander_timer = 0.0f;
    private float shoot_timer = 0.0f;
    [SerializeField] private float shoot_force = 30.0f;
    [SerializeField] private float shoot_delay = 1.0f;
    [SerializeField] private float chase_time = 0.5f;
    [SerializeField] private float max_wander_time = 3.0f; // Time they spent trying to move towards a specified wander location


    private IEnumerator current_state;
    private IEnumerator next_state;

    private IEnumerator FSM()
    {
        while (current_state != null)
        {
            yield return StartCoroutine(current_state);

            current_state = next_state;
            next_state = null;

        }
    }

    private IEnumerator Idle()
    {

        yield return new WaitForSeconds(1);

        an.Play("idle");

        if (!(fov.visibleTargets.Count >= 1))
        {
            next_state = Wander();
        }
    }

    private IEnumerator Chase()
    {

        an.Play("run");
        while (next_state == null) // Whilst the next state has not been activated, chase the target.
        {

            AI_Movement_Utils.Seek_Target(rb, target_last_seen_pos, chase_force);
            steering_dir = (target_last_seen_pos - rb.position).normalized;

            if (Vector2.Distance(target_last_seen_pos, new Vector2(transform.position.x, transform.position.y)) > 1)
            {
                fov.Set_View_Dir(steering_dir);
            }


            if (timer >= chase_time)
            {
                next_state = Idle();
            }

            yield return null;
        }

    }

    private IEnumerator Ranged_Attack()
    {

        //an.Play("run");
        while (next_state == null) // Whilst the next state has not been activated, chase the target.
        {
            if (Vector2.Distance(target_last_seen_pos, new Vector2(transform.position.x, transform.position.y)) > 1)
            {
                steering_dir = (target_last_seen_pos - rb.position).normalized;
                fov.Set_View_Dir(steering_dir);
            }

            shoot_timer += Time.deltaTime;

            if (shoot_timer >= shoot_delay)
            {
                GameObject launched_proj = Instantiate(projectile);
                launched_proj.transform.position = transform.position;
                launched_proj.GetComponent<Rigidbody2D>().AddForce(steering_dir * shoot_force);
                shoot_timer = 0.0f;
            }




            if (fov.visibleTargets.Count < 1)
            {
                shoot_timer = 0.0f;
                next_state = Chase();
            }

            yield return null;
        }

    }

    private IEnumerator Wander()
    {
        Vector3 wander_target = new Vector3(Random.Range(-2, 3), Random.Range(-2, 3),0.0f) + transform.position;

        Vector3Int tile_pos = tile_map.WorldToCell(new Vector3(wander_target.x, wander_target.y, 0.0f));
        if (tile_map.GetTile(tile_pos) != null)
        {
            next_state = Idle();
            wander_timer = 0.0f;
        }

        while (next_state == null)
        {
            AI_Movement_Utils.Seek_Target(rb, wander_target, chase_force/3);
            Vector2 target_pos = wander_target;
            steering_dir = (target_pos - rb.position).normalized;
            fov.Set_View_Dir(steering_dir);
            wander_timer += Time.deltaTime;



            if (fov.visibleTargets.Count >= 1)
            {
                if (is_ranged)
                {
                    next_state = Ranged_Attack();
                }
                else
                {
                    next_state = Chase();
                }

            }
            else if (Vector3.Distance(wander_target, transform.position) < 1.0f)
            {
                next_state = Idle();
                wander_timer = 0.0f;
            }
            else if (wander_timer > max_wander_time)
            {
                next_state = Idle();
                wander_timer = 0.0f;
            }


            yield return null;
        }

        rb.AddForce(-steering_dir * (rb.velocity * rb.mass));
    }

    // Use this for initialization
    void Start ()
    {
        rb = GetComponent<Rigidbody2D>();
        fov = GetComponent<Field_Of_View>();

        Tilemap[] tilemap_array = FindObjectsOfType<Tilemap>();

        foreach (Tilemap tm in tilemap_array)
        {
            if (tm.name == tile_map_name)
            {
                tile_map = tm;
            }
        }

        current_state = Idle();
        StartCoroutine(FSM());
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (is_always_chasing)
        {
            if (target_obj == null)
            {
                target_obj = GameObject.Find("Player");
            }
            target_last_seen_pos = target_obj.transform.position;
            if (current_state != Chase())
            {
                next_state = Chase();
            }

        }

        if ((fov.visibleTargets.Count >= 1) && ((next_state != Chase()) || (next_state != Ranged_Attack())))
        {
            target_obj = fov.visibleTargets[0].gameObject;
            target_last_seen_pos = target_obj.transform.position;
            if (is_ranged)
            {
                next_state = Ranged_Attack();
            }
            else
            {
                next_state = Chase();
            }

        }

        if (fov.visibleTargets.Count < 1)
        {
            timer += Time.deltaTime;
        }
        else
        {
            timer = 0.0f;
        }
    }


    private void OnCollisionStay2D(Collision2D collision) // Used for eating through walls
    {

        if (is_hungry)
        {
            Vector2 collision_point = collision.contacts[0].point;
            collision_point -= (collision.contacts[0].normal.normalized * 0.5f);
            Vector3Int tile_pos = tile_map.WorldToCell(new Vector3(collision_point.x, collision_point.y,0.0f));

            if (tile_map.GetTile(tile_pos) == destructable_tile)
            {

                if (eat_timer >= eat_cooldown)
                {
                    tile_map.SetTile(tile_pos, null);
                    eat_timer = 0.0f;
                }
                else
                {
                    eat_timer += Time.deltaTime;
                }
            }
        }
    }
}
