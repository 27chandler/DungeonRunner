using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Data_Script : MonoBehaviour
{
    // The Data Script is used to store all variables that need to be transferred between scenes


    [SerializeField] public int world_seed; // The seed used for the world gen
    [SerializeField] public Vector2Int world_size; // Size of the world (multiplies by 10)
    [SerializeField] public Generation_Master.WORLD_GEN_TYPE world_gen_type; // The type of world generator to use when making the world
    [SerializeField] public int max_interact_reach; // The max distance the player can dig/place blocks from the player character
    [SerializeField] public int gold_amount = 0; // The player's current amount of gold

    private Scene start_scene;
    public bool do_reset = true;

	// Use this for initialization
	void Start ()
    {
        DontDestroyOnLoad(this.gameObject);

        start_scene = SceneManager.GetActiveScene();
    }
	
	// Update is called once per frame
	void Update ()
    {
		if ((SceneManager.GetActiveScene() == start_scene) && (do_reset)) // All values are reset when entering the main menu scene
        {
            world_seed = 0;
            world_size = new Vector2Int(7, 7);
            world_gen_type = Generation_Master.WORLD_GEN_TYPE.NORMAL;
            max_interact_reach = 2;
            gold_amount = 0;
            do_reset = false;
        }
	}
}
