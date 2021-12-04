﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Random = System.Random;
using System.Linq;
using UnityEngine.UI;
using static GameResult;
using UnityEngine.SceneManagement;
using static EvolutionManager;
using static UnityEngine.Random;



public class ArenaManager : MonoBehaviour
{
    //TODO: File management approach
    private static string levelPath = "Assets\\Game\\level.json";
    private static string player1Path = "Assets\\Game\\player1.json";
    private static string player2Path = "Assets\\Game\\player2.json";
    private static string player1Move1Path = "Assets\\Game\\p1move1.json";
    private static string player2Move1Path = "Assets\\Game\\p2move1.json";

    //UI components for each player
    public GameObject p1HUD;
    public Text p1HUDText;
    public GameObject p2HUD;
    public Text p2HUDText;

    //UI components for notifications
    public GameObject notifications;
    public Text notificationsText;

    //Prefab Declaration
    public Move move;
    public Player player;
    public Platforms platforms;

    //Player references for this game
    public Player player1;
    public Player player2;

    //Settings parameters for game
    public bool p1Playable;
    public bool p2Playable;
    public bool UIEnabled;

    public GameResult result;
    
    // Start is called before the first frame update
    void Start()
    {
        this.InitializeGame(false, false, true);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void EndGame() 
    {
        //record game scores
        this.result.totalDamageP1 = this.player1.totalDamage;
        this.result.totalRecoveryStateTransitionP1 = this.player1.totalRecoveryStateTransition;
        this.result.totalHitsReceivedP1 = this.player1.totalHitsReceived;
        this.result.totalDamageP2 = this.player2.totalDamage;
        this.result.totalRecoveryStateTransitionP2 = this.player2.totalRecoveryStateTransition;
        this.result.totalHitsReceivedP2 = this.player2.totalHitsReceived;

        //send to evolution manager
        EvolutionManager.instance.AddResultFromGame(this.result);

        //destroy objects to preserve score - all other objects unloaded by unloading scene
        this.player1.destroy();
        this.player2.destroy();

        

        //if (SceneManager.GetActiveScene().name == "EvolutionaryArenaManager")
        //{
            //Do specific Evolutionary Alg stuff here if necessary
        //}


    }

    public void InitializeGame(bool p1Playable, bool p2Playable, bool UIEnabled)
    {
        this.p1Playable = p1Playable;
        this.p2Playable = p2Playable;
        this.UIEnabled = UIEnabled;

        this.result = new GameResult();

        // Initialize RNG
        Random rand = new Random();

        // Generate / Load Map
        MapGenerator mapGen = new MapGenerator(2, 2, 3, 6, rand);
        platforms = mapGen.generate();

        // Load from or write to file
        platforms = ReadJson<Platforms>(levelPath, platforms);

        // Compute spawn locations
        // Player 1 spawns on the initial platform
        Platform initialPlatform = platforms.platformList[0];
        int player1Spawnx = (int) initialPlatform.x + (initialPlatform.xSize + 1) / 2;
        Debug.Log(player1Spawnx);
        int player1Spawny = initialPlatform.y + initialPlatform.ySize + 1;
        Vector2 player1Spawn = new Vector2(player1Spawnx, player1Spawny);
        // Mirror Player 2's spawn relative to Player 1's
        int player2Spawnx = -player1Spawnx;
        int player2Spawny = player1Spawny;
        Vector2 player2Spawn = new Vector2(player2Spawnx, player2Spawny);

        // Serialized Player 1 Setup
        SerializedPlayer serializedPlayer1 = new SerializedPlayer("Player 1", rand);
        serializedPlayer1 = ReadJson<SerializedPlayer>(player1Path, serializedPlayer1);

        // Serialized Player 1, Move 1 Setup
        SerializedMove serializedMove1Player1 = new SerializedMove(rand);
        serializedMove1Player1 = ReadJson<SerializedMove>(player1Move1Path, serializedMove1Player1);

        // Player 1 Instantiation
        Vector3 spawnLocationP1 = new Vector3(player1Spawnx, player1Spawny, 0);
        Player player1 = Instantiate(player, spawnLocationP1, Quaternion.identity);
        player1.arenaManager = this;
        this.player1 = player1;

        // Serialized Player 2 Setup
        SerializedPlayer serializedPlayer2 = new SerializedPlayer("Player 2", rand);
        serializedPlayer2 = ReadJson<SerializedPlayer>(player2Path, serializedPlayer2);

        // Serialized Player 2 Move 1 Setup
        SerializedMove serializedMove1Player2 = new SerializedMove(rand);
        serializedMove1Player2 = ReadJson<SerializedMove>(player2Move1Path, serializedMove1Player2);

        // Player 2 Instantiation
        Vector3 spawnLocationP2 = new Vector3(player2Spawnx, player2Spawny, 0);
        Player player2 = Instantiate(player, spawnLocationP2, Quaternion.identity);
        player2.arenaManager = this;
        this.player2 = player2;

        if (UIEnabled)
        {
            //Player 1 Heads Up Display
            p1HUDText = p1HUD.GetComponent<Text>();
            player1.playerDetails = p1HUDText;
            //Player 2 Heads Up Display
            p2HUDText = p2HUD.GetComponent<Text>();
            player2.playerDetails = p2HUDText;
            //notifications
            notificationsText = notifications.GetComponent<Text>();
        }
        else 
        {
            Destroy(p1HUD);
            Destroy(p2HUD);
            Destroy(notifications);
        }

        //Player Controller or Agent Assignment
        if (p1Playable)
        {
            player1.controller = new Controller(player1, null);
            player1.controller.leftKey = KeyCode.A;
            player1.controller.rightKey = KeyCode.D;
            player1.controller.jumpKey = KeyCode.W;
            player1.controller.move1Key = KeyCode.S;
        }
        else 
        {
            player1.controller = new AI(player1, player2);
        }
        if (p2Playable)
        {
            player2.controller = new Controller(player2, null);
            player2.controller.leftKey = KeyCode.J;
            player2.controller.rightKey = KeyCode.L;
            player2.controller.jumpKey = KeyCode.I;
            player2.controller.move1Key = KeyCode.K;
        }
        else
        {
            player2.controller = new AI(player2, player1);
        }

        //update gameobjects instantiated into the scene with values from JSON
        InitializePlayerFromSerializedObj(serializedPlayer1, player1, player1Spawn);
        InitializeMoveFromSerializedObj(serializedMove1Player1, player1);
        InitializePlayerFromSerializedObj(serializedPlayer2, player2, player2Spawn);
        InitializeMoveFromSerializedObj(serializedMove1Player2, player2);

        StartCoroutine(NotificationCoroutine("FIGHT!"));

        //TODO: TimeScale Adjustment
        //Time.timeScale = 1.0f;
        //Time.fixedDeltaTime = Time.fixedDeltaTime * Time.timeScale;
    }

    //TODO: comment purpose of ifFileMissing, looks like the serialized type up above but idk
    T ReadJson<T>(string filename, T ifFileMissing)
    {
        // Write to file
        if (!File.Exists(filename))
        {
            var objStr = JsonUtility.ToJson(ifFileMissing);
            File.Create(filename).Dispose();
            File.WriteAllText(filename, objStr);
            return ifFileMissing;
        }
        // If the file exists, read from it
        else
        {
            var inputString = File.ReadAllText(filename);
            T obj = JsonUtility.FromJson<T>(inputString);
            //return obj;
            //TODO: temporarily disabled for iteration
            return ifFileMissing;
        }
    }


    T WriteJson<T>(string filename, T ifFileMissing) 
    {
        // Write to file, overwriting the last
        return ifFileMissing;
    }

    /**Saves the current arena's settings to JSON files according to the gameID in gameData
     */
    public bool SaveGameJSON() 
    {
        this.SaveGameJSON(this.result.gameID);
        return false;
    }

    /**Saves the current game based on a parameter
     */
    public bool SaveGameJSON(int gameID)
    {
        
        string tempFolderPath = "Assets\\Game\\game" + gameID + "\\";
        string tempLevelPath = tempFolderPath + "level.json";
        string tempPlayer1Path = tempFolderPath + "player1.json";
        string tempPlayer2Path = tempFolderPath + "player2.json";
        string tempPlayer1Move1Path = tempFolderPath + "p1move1.json";
        string tempPlayer2Move1Path = tempFolderPath + "p2move1.json";
        string tempGameResult = tempFolderPath + "gameresult.json";
        return false;
        
    }

    /** Assignment of values from the Serialized Object. TODO: Static evaluators
     * See player object for detailed field information
    */
    public void InitializePlayerFromSerializedObj(SerializedPlayer serializedPlayer, Player player, Vector2 respawnLoc)
    {
        // Player parameter initialization
        player.playerName = serializedPlayer.playerName;
        player.stocks = serializedPlayer.stocks;
        player.groundAcceleration = serializedPlayer.groundAcceleration;
        player.airAcceleration = serializedPlayer.airAcceleration;
        player.maxGroundSpeed = serializedPlayer.maxGroundSpeed;
        player.maxAirSpeed = serializedPlayer.maxAirSpeed;
        player.groundJumpForce = serializedPlayer.groundJumpForce;
        player.airJumpForce = serializedPlayer.airJumpForce;
        player.hitstunDamageScalar = serializedPlayer.hitstunDamageScalar;
        player.respawnLoc = respawnLoc;
        player.transform.localScale = new Vector2(serializedPlayer.widthScalar, serializedPlayer.heightScalar);
        player.rb.gravityScale = serializedPlayer.gravityScalar;
        player.rb.mass = serializedPlayer.mass;
        player.rb.drag = serializedPlayer.drag;
        // Sprite initialization
        Sprite[] playerSprites = Resources.LoadAll<Sprite>("players");
        player.spriteIndex = serializedPlayer.spriteIndex;
        player.sr.sprite = playerSprites[player.spriteIndex];
    }

    /** Assignment of values from the Serialized Object. Requires a player as a parameter as a dependency
     *  See move definition for field information
     */
    public void InitializeMoveFromSerializedObj(SerializedMove serializedMove, Player player)
    {
        //first find center for instantiation
        Vector2 center = player.transform.position + new Vector3(serializedMove.moveLocX, serializedMove.moveLocY);
        //instantiates a move to a player and sets location relative to the player
        player.move1 = Instantiate<Move>(move, center, Quaternion.identity, player.transform);
        player.move1.center = center;
        //Sets width and height of move
        player.move1.transform.localScale = new Vector2(serializedMove.widthScalar, serializedMove.heightScalar);
        //Move Parameter initialization
        player.move1.warmUpDuration = serializedMove.warmUpDuration;
        player.move1.executionDuration = serializedMove.executionDuration;
        player.move1.coolDownDuration = serializedMove.coolDownDuration;
        player.move1.damageGiven = serializedMove.damageGiven;
        player.move1.knockbackScalar = serializedMove.knockbackScalar;
        player.move1.knockbackDirection = new Vector2(serializedMove.knockbackModX, serializedMove.knockbackModY).normalized;
        player.move1.hitstunDuration = serializedMove.hitstunDuration;
        // Sprite initialization
        Sprite[] moveSprites = Resources.LoadAll<Sprite>("moves");
        player.move1.spriteIndex = serializedMove.spriteIndex;
        player.move1.sr.sprite = moveSprites[player.move1.spriteIndex];
    }

    //UI Control for this game
    public void UpdateNotifications(string message) 
    {
        if (UIEnabled)
        {
            notificationsText.text = message;
        }
    }

    public void ClearNotifications() 
    {
        if (UIEnabled)
        {
            notificationsText.text = "";
        }
    }

    public IEnumerator NotificationCoroutine(string message) 
    {
        UpdateNotifications(message);
        yield return new WaitForSeconds(5f);
        ClearNotifications();
    }
   
}
