using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Menu_System : MonoBehaviour
{
    [Serializable] struct Menu_Button
    {
        public Button button;
        public Text text;
    }

    [Serializable] struct World_Types
    {
        public string name;
        public Vector2Int size;
        public Generation_Master.WORLD_GEN_TYPE world_gen_type;
    }

    [Serializable]
    struct Reach_Presets
    {
        public string name;
        public int reach;
    }

    [SerializeField] private GameObject data_obj;

    [SerializeField] private Menu_Button[] menu;
    [SerializeField] private Menu_Button[] options_menu;

    private int menu_length;

    //[SerializeField] private Text[] menu_text;
    [SerializeField] private string[] text_strings;
    [SerializeField] private string[] options_text_strings;
    private string world_type_display_string_default; // The string that stores the prefix for the world type
    private string reach_type_display_string_default; // The string that stores the prefix for the reach level type
    [SerializeField] private World_Types[] world_gen_presets;
    [SerializeField] private Reach_Presets[] reach_presets;
    private int selected_world_type_id = 0;
    private int selected_reach_type_id = 1;

    [SerializeField] private Text seed_text;
    [SerializeField] private InputField seed_input;

    private Data_Script ds;

    [SerializeField] private float menu_move_offset;

    private Vector3 default_menu_pos;
    private Vector3 move_menu_to_pos;

    private int menu_level = 0;

    private int selection_int = 0;
    private int selected_int = -1;
    private bool is_move_up = false;
    private bool is_move_down = false;

    private enum OPTIONS { PLAY, OPTIONS, QUIT };
    private enum CONFIG { BACK, WORLDTYPE, REACH, SCORES };


    // Use this for initialization
    void Start ()
    {
        if (!(GameObject.FindGameObjectWithTag("Data")))
        {
            Instantiate(data_obj);
        }

        ds = GameObject.FindGameObjectWithTag("Data").GetComponent<Data_Script>();

        seed_text.text = UnityEngine.Random.Range(0, 9999999).ToString();

        world_type_display_string_default = options_text_strings[1];
        options_text_strings[1] = world_type_display_string_default + ": " + world_gen_presets[selected_world_type_id].name;

        reach_type_display_string_default = options_text_strings[2];
        options_text_strings[2] = reach_type_display_string_default + ": " + reach_presets[selected_reach_type_id].name;

        ds.world_size = world_gen_presets[selected_world_type_id].size;
        ds.world_gen_type = world_gen_presets[selected_world_type_id].world_gen_type;

        ds.max_interact_reach = reach_presets[selected_reach_type_id].reach;

        for (int i = 0; i < menu.Length; i++)
        {
            int index = i;
            menu[i].button.onClick.AddListener(() => Activate_Option(index,0));
        }

        for (int i = 0; i < options_menu.Length; i++)
        {
            int index = i;
            options_menu[i].button.onClick.AddListener(() => Activate_Option(index,1));
        }

        default_menu_pos = menu[0].button.transform.position;
        move_menu_to_pos = default_menu_pos;

    }
	
	// Update is called once per frame
	void Update ()
    {
        if (!seed_input.isFocused) // Prevents the typing in the text field from affecting the movement on the menu
        {
            Inputs();
        }

        foreach (var item in menu)
        {
            if (item.button.transform.position.x != move_menu_to_pos.x)
            {
                Vector3 button_move_to_pos = item.button.transform.position;
                button_move_to_pos.x = move_menu_to_pos.x;
                item.button.transform.position = Vector3.Lerp(item.button.transform.position, button_move_to_pos, 0.1f);
            }
        }

        foreach (var item in options_menu)
        {
            if (item.button.transform.position.x != move_menu_to_pos.x - menu_move_offset)
            {
                Vector3 button_move_to_pos = item.button.transform.position;
                button_move_to_pos.x = move_menu_to_pos.x - menu_move_offset;
                item.button.transform.position = Vector3.Lerp(item.button.transform.position, button_move_to_pos, 0.1f);
            }
        }


        if (menu_level == 0)
        {
            move_menu_to_pos.x = default_menu_pos.x;
            menu_length = menu.Length;
            Set_Text_Strings();
        }
        else if (menu_level == 1)
        {
            move_menu_to_pos.x = default_menu_pos.x + menu_move_offset;
            menu_length = options_menu.Length;
            Set_Options_Text_Strings();
        }


        // Cursor movement
        if (is_move_up)
        {
            selection_int--;
            is_move_up = false;
        }
        else if (is_move_down)
        {
            selection_int++;
            is_move_down = false;
        }

        if (selection_int < 0)
        {
            selection_int = menu_length - 1;
        }
        else if (selection_int >= menu_length)
        {
            selection_int = 0;
        }


    }

    private void Set_Text_Strings() // Changes the main menu text depending on which option the cursor is on
    {
        for (int i = 0; i < menu.Length; i++)
        {
            if (i == selection_int)
            {
                menu[i].text.text = ">" + text_strings[i];
            }
            else
            {
                menu[i].text.text = "-" + text_strings[i];
            }
        }
    }

    private void Set_Options_Text_Strings() // Changes the options text depending on which option the cursor is on
    {
        for (int i = 0; i < options_menu.Length; i++)
        {
            if (i == selection_int)
            {
                options_menu[i].text.text = ">" + options_text_strings[i];
            }
            else
            {
                options_menu[i].text.text = "-" + options_text_strings[i];
            }
        }
    }

    private void Inputs() // Keyboard inputs
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            is_move_up = true;
        }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            is_move_down = true;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            selected_int = selection_int;
            Activate_Option(-1,menu_level);
        }
    }

    private void Activate_Option(int i_option_num,int i_button_level)
    {
        if (i_option_num >= 0)
        {
            selected_int = i_option_num;
        }

        if (i_button_level == menu_level)
        {
            if (menu_level == 0) // Main menu actions
            {
                OPTIONS option = (OPTIONS)selected_int;
                switch (option)
                {
                    case OPTIONS.PLAY:
                        {
                            if (seed_text.text == "")
                            {
                                ds.world_seed = UnityEngine.Random.Range(0, 99999);
                            }
                            else
                            {
                                ds.world_seed = seed_text.text.GetHashCode();
                            }
                            
                            SceneManager.LoadScene("Main_Game");
                            break;
                        }
                    case OPTIONS.OPTIONS:
                        {
                            menu_level = 1;
                            break;
                        }
                    case OPTIONS.QUIT:
                        {
                            Application.Quit();
                            break;
                        }
                }

            }
            else if (menu_level == 1) // Options menu actions
            {
                CONFIG option = (CONFIG)selected_int;
                switch (option)
                {
                    case CONFIG.BACK:
                        {
                            menu_level = 0;
                            break;
                        }
                    case CONFIG.WORLDTYPE:
                        {
                            selected_world_type_id++;

                            if (selected_world_type_id > world_gen_presets.Length - 1)
                            {
                                selected_world_type_id = 0;
                            }
                            options_text_strings[1] = world_type_display_string_default + ": " + world_gen_presets[selected_world_type_id].name;
                            ds.world_size = world_gen_presets[selected_world_type_id].size;
                            ds.world_gen_type = world_gen_presets[selected_world_type_id].world_gen_type;
                            Debug.Log(options_text_strings[1]);
                            break;
                        }
                    case CONFIG.REACH:
                        {
                            selected_reach_type_id++;

                            if (selected_reach_type_id > reach_presets.Length - 1)
                            {
                                selected_reach_type_id = 0;
                            }
                            options_text_strings[2] = reach_type_display_string_default + ": " + reach_presets[selected_reach_type_id].name;
                            ds.max_interact_reach = reach_presets[selected_reach_type_id].reach;
                            Debug.Log(options_text_strings[2]);
                            break;
                        }
                    case CONFIG.SCORES:
                        {
                            PlayerPrefs.DeleteAll();
                            break;
                        }
                }
            }
        }
        else
        {
            menu_level = i_button_level;
        }



    }
}
