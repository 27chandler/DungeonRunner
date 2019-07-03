using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Highscores : MonoBehaviour
{
    [SerializeField] private Text[] score_text;
    [SerializeField] private Text player_score_text;
    [SerializeField] private InputField input_name;
    [SerializeField] private Button submit_button;
    [SerializeField] private Button menu_button;

    private bool is_submitted = false;

    private Data_Script ds;
    public struct scores
    {
        public string name;
        public int gold;
    }

    private List<scores> highscore = new List<scores>(); // Used to temporarily hold the scores after they have been loaded in from playerprefs
    private int num_of_scores = 5;
	// Use this for initialization
	void Start ()
    {
        ds = GameObject.FindGameObjectWithTag("Data").GetComponent<Data_Script>();

        player_score_text.text = "Your score: " + ds.gold_amount.ToString();

        submit_button.onClick.AddListener(() => Add_Highscore());
        menu_button.onClick.AddListener(() => Return_To_Menu());

        for (int i = 0; i < num_of_scores; i++)
        {
            scores score_to_add = new scores();
            score_to_add.name = "---";
            score_to_add.gold = 0;
            highscore.Add(score_to_add);
        }

        Update_Scores();
    }

    void Return_To_Menu()
    {
        ds.do_reset = true;
        SceneManager.LoadScene("Menu");
    }

    void Add_Highscore() // Adds a highscore to the score board (after checking if it is highest than the lowest score on there)
    {
        if (!is_submitted)
        {
            int counter = 0;
            bool is_new_highscore = false;
            foreach(var score in highscore)
            {
                if (ds.gold_amount > score.gold)
                {
                    scores score_to_add = new scores();
                    score_to_add.name = input_name.text;
                    score_to_add.gold = ds.gold_amount;
                    highscore.Insert(counter, score_to_add);
                    is_new_highscore = true;
                    break;
                }
                counter++;
            }

            if (is_new_highscore)
            {
                highscore.RemoveRange(5, 1);
            }


            for (int i = 0; i < num_of_scores; i++)
            {
                scores score_to_add = new scores();
                score_to_add = highscore[i];
                PlayerPrefs.SetInt(i.ToString(), score_to_add.gold);
                PlayerPrefs.SetString((i + 5).ToString(), score_to_add.name);
            }


            Update_Scores();
            is_submitted = true;
        }
    }

    void Update_Scores()
    {
        for (int i = 0; i < num_of_scores; i++)
        {
            scores score_to_add = new scores();
            score_to_add.gold = PlayerPrefs.GetInt(i.ToString(), 0);
            score_to_add.name = PlayerPrefs.GetString((i + 5).ToString(), "---");
            highscore[i] = score_to_add;
            score_text[i].text = score_to_add.name + ": " + score_to_add.gold;
        }
    }
}
