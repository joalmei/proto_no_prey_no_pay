using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameRef : MonoBehaviour {

    //Variables of interest declaration
    public Text scorePlay1, scorePlay2;
    private int score1, score2;
    private int gameMode;
    private int time;


	// Use this for initialization
	void Start () {
        score1 = 0; score2 = 0;
        gameMode = 1; //Apenas para teste. Receber o gameMode quando for instanciado.
        updateScore(1); updateScore(2);
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
}
