using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameRef : MonoBehaviour {

    //Variables of interest declaration

    public static GameRef instance;
    public      Text     scorePlay1, scorePlay2, Victory;
    private     int     score1, score2;
    private     int     gameMode;
    private     bool    Play1_alive, Play2_alive;
    public      bool    endGame { get; private set; }
    public      bool    stopInputs;


    // Use this for initialization
    void Start () {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        score1 = 0; score2 = 0;
        gameMode = 1; //Apenas para teste. Receber o gameMode quando for instanciado.
        updateScore(1); updateScore(2);
        endGame = false;
        stopInputs = false;
	}

    public void identifyPlayers(int player)
    {
        switch (player)
        {
            case 1:
                Play1_alive = true;
                break;
            case 2:
                Play2_alive = true;
                break;
            default:
                break;
        }
        updateScore(player);
    }

    public void addScore(int numPoints, int player)
    {
        switch (player)
        {
            case 1:
                score1 += numPoints;
                break;
            case 2:
                score2 += numPoints;
                break;
            default:
                break;
        }
        updateScore (player);
    }

    public void updateScore(int player)
    {
        switch (player)
        {
            case 1:
                scorePlay1.text = "Score " + score1;
                break;
            case 2:
                scorePlay2.text = "Score " + score2;
                break;
            default:
                break;
        }
    }

    public void murderWitness(int player)
    {
        if (gameMode == 1)
        {
            switch (player)
            {
                case 1:
                    Play1_alive = false;
                    break;
                case 2:
                    Play2_alive = false;
                    break;
                default:
                    break;
            }
        }
        StartCoroutine(checkForSurvivors());
    }

    IEnumerator checkForSurvivors()
    {
        int nSurvivors = 0;

        if (Play1_alive) nSurvivors += 1;
        if (Play2_alive) nSurvivors += 1;

        if (gameMode==1 && nSurvivors <= 1)
        {
            if (Play1_alive)
            {
                Victory.text = "Player 1 takes the loot!";
                Victory.gameObject.SetActive(true);
            }
            else if (Play2_alive)
            {
                Victory.text = "Player 2 takes the loot!";
                Victory.gameObject.SetActive(true);
            }
            else
            {
                Victory.text = "What a bloodbath!";
                Victory.gameObject.SetActive(true);
            }
            stopInputs = true;
            yield return new WaitForSeconds(3);
            endGame = true;
        }

        yield return new WaitForSeconds(0);

    }
}
