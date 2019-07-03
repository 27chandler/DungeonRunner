using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Totem_AI : MonoBehaviour
{
    private Field_Of_View fov;
    private Vector3 view_direction = new Vector3(0.0f,1.0f,0.0f);
    private float timer = 0.0f;
    private bool is_activated = false;

    [SerializeField] private GameObject[] enemies;


    // Use this for initialization
    void Start ()
    {
        fov = GetComponent<Field_Of_View>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        // It rotates
        timer += Time.deltaTime;
        view_direction = new Vector3(Mathf.Cos(timer), Mathf.Sin(timer), 0.0f);


        fov.Set_View_Dir(view_direction);

        // If it sees the player for the first time, it spawns a random enemy type
        if ((fov.visibleTargets.Count >= 1) && (!is_activated))
        {
            is_activated = true;

            int enemy_id = Random.Range(0, enemies.Length);
            GameObject enemy_obj = Instantiate(enemies[enemy_id]);
            enemy_obj.transform.position = transform.position + new Vector3(0.0f, 1.0f, 0.0f);

            enemy_obj = Instantiate(enemies[enemy_id]);
            enemy_obj.transform.position = transform.position + new Vector3(0.0f, -1.0f, 0.0f);

            enemy_obj = Instantiate(enemies[enemy_id]);
            enemy_obj.transform.position = transform.position + new Vector3(1.0f, 0.0f, 0.0f);

            enemy_obj = Instantiate(enemies[enemy_id]);
            enemy_obj.transform.position = transform.position + new Vector3(-1.0f, 0.0f, 0.0f);
        }
	}
}
