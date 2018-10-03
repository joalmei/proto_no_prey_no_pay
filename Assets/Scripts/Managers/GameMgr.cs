using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMgr : MonoBehaviour
{
    public static float Timer       { get; private set; }
    public static float DeltaTime   { get; private set; }

    public static bool IsPaused     { get; private set; }
    public static bool IsGameOver   { get; private set; }

    private GameRef gameReferee; //Necessário para se comunicar com o árbitro

    // ======================================================================================
    // PUBLIC MEMBERS
    // ======================================================================================
    public void Start()
    {
        IsPaused = false;

        GameObject gameRefereeObject = GameObject.FindWithTag("GameReferee");
        if (gameRefereeObject != null)
        {
            gameReferee = gameRefereeObject.GetComponent<GameRef>();
        }
        if (gameReferee == null)
        {
            Debug.Log("This is a lawless battle (Cannot find 'GameRef' script)");
        }
    }

    // ======================================================================================
    void Update()
    {
        if (!IsPaused)
        {
            DeltaTime = Time.deltaTime;
            Timer += DeltaTime;
            if (gameReferee.endGame)
            {
                RestartGame();
            }
        }
        else
        {
            DeltaTime = 0;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //IsPaused = !IsPaused;
            if (IsPaused)
            {
                QuitGame();
            }
            else
            {
                IsPaused = true;
            }
        }
    }

    // ======================================================================================
    public void QuitGame()
    {
        Application.Quit();
    }

    // ======================================================================================
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
