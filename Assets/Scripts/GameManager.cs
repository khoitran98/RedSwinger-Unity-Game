﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Linq;
using SimpleJSON;
using Newtonsoft.Json;


public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    public delegate void GameDelegate();
    public static event GameDelegate OnGameStarted;
    public static event GameDelegate OnGameOverConfirmed;
    public static event GameDelegate Dead;
    public static GameManager Instance;
    public RopeSystem ropeSystem;
    public GameObject StartMenu;
    public GameObject GameOver;
    public GameObject CountDownPage;
    public Text Score;
    private GameObject character;
    private int xPos; // variable to calculate the score
    enum PageState {
        None,
        Start,
        GameOver,
        Countdown
    }
    int scoreGain = 0; // score gained from travelled distance
    int scoreLost = 0; // score lost from mace collision
    public int score = 0; // overall score
    int scoreBonus = 0; // score gained from coin

    // testing neural
    // [Range(-1f,1f)]
    // public float a,t;

    public float timeSinceStart = 0f;

    [Header("Fitness")]
    public float overallFitness;
    public float distanceMultipler = 1.4f;
    public float avgSpeedMultiplier = 0.2f;
    public float sensorMultiplier = 0.1f;

    private Vector3 lastPosition;
    private float totalDistanceTravelled;
    private float avgSpeed;

    public float aSensor,bSensor,cSensor; // 3 object detection sensors
    private float lastScore; // used to calculate stuck time
    private float stuckTime; // character stalemate time
    public GameObject ldboard;
    public GameObject s1,p1,s2,p2,s3,p3,s4,p4,s5,p5;
    Hashtable leaderboard = new Hashtable();
    List<int> playersScore = new List<int>();

    void Awake() 
    {
        Instance = this;
        xPos = (int)transform.position.x; // get the starting position of player
    }
    void OnEnable() 
    {
        SetPageState(PageState.Start);
        CountdownText.OnCountdownFinished += OnCountdownFinished;
        Controller.OnPlayerDied += OnPlayerDied;
        Mace.OnPlayerLoseScore += OnPlayerLoseScore;
        Coin.OnPlayerScored += OnPlayerScored;
    }
    void OnDisable() 
    {
        CountdownText.OnCountdownFinished -= OnCountdownFinished;
        Controller.OnPlayerDied -= OnPlayerDied;
        Coin.OnPlayerScored -= OnPlayerScored;
        Mace.OnPlayerLoseScore -= OnPlayerLoseScore;
    }
    void OnCountdownFinished() 
    {
        SetPageState(PageState.None);
        OnGameStarted();
        score = 0;
        timeSinceStart = 0; // testing neural
        stuckTime = 0; // testing neural
    }
    void OnPlayerDied() 
    {
        ropeSystem.ResetRope();
        Dead(); // event sent to rope system
        int savedScore = PlayerPrefs.GetInt("HighScore"); // getting saved high score from a special saving place
        if (score > savedScore) {
            PlayerPrefs.SetInt("HighScore", score);
        }
        playersScore.Clear();
        leaderboard.Clear();
        SetPageState(PageState.GameOver);
        Time.timeScale = 0; // temporarily pause game
        character.GetComponent<NNet>().saveNetworkToServer();
    }
    void Start()
    {
        //SetPageState(PageState.Countdown); // for testing only
        character = GameObject.Find("Character");
        stuckTime = 0;
    }
    void OnPlayerScored() 
    {
        scoreBonus += 10;
    }
    void OnPlayerLoseScore() 
    {
        scoreLost -= 10;
    }
    void SetPageState (PageState state) {
        switch (state) {
            case PageState.None:
                StartMenu.SetActive(false);
                GameOver.SetActive(false);
                CountDownPage.SetActive(false);
                break;
            case PageState.Start:
                StartMenu.SetActive(true);
                GameOver.SetActive(false);
                CountDownPage.SetActive(false);
                break;
            case PageState.GameOver:
                StartMenu.SetActive(false);
                GameOver.SetActive(true);
                CountDownPage.SetActive(false);
                break;
            case PageState.Countdown:
                StartMenu.SetActive(false);
                GameOver.SetActive(false);
                CountDownPage.SetActive(true);
                break;
        }
    }
    public void ConfirmGameOver() 
    {
        //when replay
        OnGameOverConfirmed(); // event sent to tap controller
        score = scoreGain = scoreLost = scoreBonus = 0; // reset the score
        SetPageState(PageState.Start);
        //SetPageState(PageState.Countdown);

    }
    public void StartGame() 
    { // when play
        SetPageState(PageState.Countdown);
        StartCoroutine("GetData");
    }
    void Update()
    {
        lastScore = score;
        scoreGain = (int)transform.position.x - xPos; // using travelled distance as score
        score = scoreGain + scoreLost + scoreBonus;
        if (score < 0)
        {
            scoreGain = scoreLost = scoreBonus = 0;
            xPos = (int)transform.position.x;
        }
        Score.text = (score).ToString();
        // testing neural
        //InputSensors();
        lastPosition = transform.position; 
        //Neural network code here
        timeSinceStart += Time.deltaTime;
        CalculateFitness();
        // For neural network testing
        // if (score >= 100) {
        //     ConfirmGameOver(); // if score is already too high, reset
        // }
        // if (lastScore == score) // if stucked for more than 4 seconds, reset, for training purpose only
        // {
        //     stuckTime += Time.deltaTime;
        //     if (stuckTime >= 4)
        //         //ConfirmGameOver();
        //         OnPlayerDied();
        // }
        // else
        // {
        //     stuckTime = 0;
        // }   
    }
    // testing neural
    private void FixedUpdate() {
        // InputSensors();
        // lastPosition = transform.position;
        // //Neural network code here
        // timeSinceStart += Time.deltaTime;
        // CalculateFitness();
    }
     private void CalculateFitness() {
        totalDistanceTravelled = score;
        avgSpeed = totalDistanceTravelled/timeSinceStart;
        overallFitness = (totalDistanceTravelled*distanceMultipler)+(avgSpeed*avgSpeedMultiplier);
    }
    private void InputSensors() {
        RaycastHit2D hit;
        hit = Physics2D.Raycast(character.transform.position + new Vector3(1f, 0f, 0f), Vector2.right, 18f); // only make this ray short to fit the screen
        if (hit.collider != null) {
            aSensor = hit.distance/20;
        }
        hit = Physics2D.Raycast(character.transform.position + new Vector3(1f, 0f, 0f), Vector2.right + Vector2.up);
        if (hit.collider != null) {
            bSensor = hit.distance/20;
        }
        hit = Physics2D.Raycast(character.transform.position + new Vector3(1f, 0f, 0f), Vector2.right + Vector2.down);
        if (hit.collider != null) {
            cSensor = hit.distance/20;
        }
    }
    IEnumerator GetData ()
    {
        // create the web request and download handler
        UnityWebRequest webReq = new UnityWebRequest();
        webReq.downloadHandler = new DownloadHandlerBuffer();
        // send the web request and wait for a returning result
        webReq.url = "";
        yield return webReq.SendWebRequest();
        string rawJson = Encoding.Default.GetString(webReq.downloadHandler.data);
        // parse the raw string into a json result we can easily read
        var jsonResult = JSON.Parse(rawJson).AsArray;
        for (int x  = 0; x < jsonResult.Count; x++)
        {
            int score = Int32.Parse(jsonResult[x]["score"]);
            if (leaderboard.ContainsKey(score))
                leaderboard[score] = leaderboard[score].ToString() + " & " + jsonResult[x]["name"].ToString().Replace("\"", "");
            else
            {
                leaderboard.Add(score, jsonResult[x]["name"].ToString().Replace("\"", ""));
                playersScore.Add(score);
            }
        }
        playersScore.Sort();
        int sc = playersScore[playersScore.Count - 1];
        s1.GetComponent<Text>().text = sc.ToString();
        p1.GetComponent<Text>().text = leaderboard[sc].ToString();
        sc = playersScore[playersScore.Count - 2];
        s2.GetComponent<Text>().text = sc.ToString();
        p2.GetComponent<Text>().text = leaderboard[sc].ToString();
        sc = playersScore[playersScore.Count - 3];
        s3.GetComponent<Text>().text = sc.ToString();
        p3.GetComponent<Text>().text = leaderboard[sc].ToString();
        sc = playersScore[playersScore.Count - 4];
        s4.GetComponent<Text>().text = sc.ToString();
        p4.GetComponent<Text>().text = leaderboard[sc].ToString();
        sc = playersScore[playersScore.Count - 5];
        s5.GetComponent<Text>().text = sc.ToString();
        p5.GetComponent<Text>().text = leaderboard[sc].ToString();
        ldboard.SetActive(true);
        StopCoroutine("GetData");
        //var playerToJson = JsonConvert.SerializeObject(playersScore);
        // Debug.Log(playerToJson);
    }
}
