﻿using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class Manager : Photon.MonoBehaviour
{
    // player stats
    public int science, gold, happiness, goldenage, culture;
    public enum civilization { Greeks, Egyptians };

    public GameObject tileObject;
    public Tile[,] tiles;
    public float tileGap, rowGap, tileWidth;
    public bool ready = false;
    public float resoWidth = 1280, resoHeight = 720;
    public string gamestate = "connecting", guistate = "connecting";
    public int levelWidth = 50, levelHeight = 50;
    public List<PhotonPlayer> readPlayers;

    int pendingChunks = 0;

    void Start()
    {
        Application.runInBackground = true;
        PhotonNetwork.ConnectUsingSettings("1.0");
        readPlayers = new List<PhotonPlayer>();
    }

    void OnGUI()
    {
        Vector3 s = new Vector3(Screen.width / resoWidth, Screen.height / resoHeight, 1f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, s);

        switch (guistate)
        {
            case "start":
                break;
            case "readycheck":
                foreach (PhotonPlayer player in PhotonNetwork.playerList)
                {
                    if (player != PhotonNetwork.player)
                    {
                        if (player.name == "")
                        {
                            GUILayout.BeginVertical();
                            GUILayout.Label("no name");
                            GUILayout.EndVertical();
                        }
                        else
                        {
                            GUILayout.BeginVertical();
                            GUILayout.Label("no name");
                            GUILayout.EndVertical();
                        }
                    }
                    else
                    {
                        string str = player.name;
                        str = GUILayout.TextField(str);
                        PhotonNetwork.player.name = str;
                    }
                }
                if (GUILayout.Button("Ready"))
                    if (PhotonNetwork.isMasterClient)
                        readPlayers.Add(PhotonNetwork.player);
                    else
                        photonView.RPC("Ready", PhotonNetwork.masterClient, PhotonNetwork.player);
                break;
            case "game":
                GUI.Box(new Rect(0, 0, 1280, 40), "");
                GUILayout.BeginArea(new Rect(0, 0, 1280, 32));
                GUILayout.BeginVertical();
                GUILayout.EndVertical();
                GUILayout.EndArea();
                break;
            case "generating":
                break;
            default:
                GUI.Label(new Rect(600, 700, 80, 20), "Loading....");
                break;
        }
    }

    void Update()
    {
        if (readPlayers.Count == PhotonNetwork.playerList.Length && gamestate == "start")
        {
            gamestate = "generating";
            guistate = "generating";
            string[] data = Generate(levelWidth, levelHeight);
            photonView.RPC("GetChunkAmount", PhotonTargets.All, levelWidth, levelHeight);
            for (int i = 0; i < levelHeight; i++)
            {
                string[] newdata = new string[levelWidth];
                Array.Copy(data, i * levelWidth, newdata, 0, levelWidth);
                photonView.RPC("CallLevel", PhotonTargets.All, newdata);
            }
            StartCoroutine("RandomSpawns");
        }
        if (gamestate == "generating" && pendingChunks == 0)
        {
            StartCoroutine("Neighbours");
            gamestate = "waitingstart";
            guistate = "game";
        }
    }

    IEnumerator RandomSpawns()
    {
        List<Tile> spawns = new List<Tile>();

        while (true)
        {
            foreach (PhotonPlayer player in PhotonNetwork.playerList)
            {
                while (true)
                {
                    int x = UnityEngine.Random.Range(0, levelWidth - 1);
                    int y = UnityEngine.Random.Range(0, levelHeight - 1);
                    if (!spawns.Contains(tiles[x, y]))
                    {
                        spawns.Add(tiles[x, y]);
                        break;
                    }
                }
            }
            int count = 0;
            foreach (Tile tile in spawns)
            {
                foreach (Tile anothertile in spawns)
                {
                    if (Vector2.Distance(new Vector2(tile.x, tile.y), new Vector2(anothertile.x, anothertile.y)) >= tileWidth / 10)
                        count++;
                }
            }
            if (count == spawns.Count)
                break;
            yield return null;
        }
    }

    [RPC]

    void GetSpawn(int x, int y)
    {
        StartCoroutine("WaitForChunks",new int[]{x,y});
    }

    IEnumerator WaitForChunks(int[] coordinates)
    {
        while (true)
        {

            yield return null;
        }
    }

    IEnumerator Neighbours()
    {
        for (int y = 0; y < tiles.GetLength(1); y++)
        {
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                if (x - 1 >= 0 && y - 1 >= 0)
                {
                    tiles[x, y].neighbours.Add(tiles[x - 1, y - 1]);
                }
                if (y - 1 >= 0)
                {
                    tiles[x, y].neighbours.Add(tiles[x, y - 1]);
                }
                if (x - 1 >= 0)
                {
                    tiles[x, y].neighbours.Add(tiles[x - 1, y]);
                }
                if (x + 1 <= tiles.GetLength(0) - 1)
                {
                    tiles[x, y].neighbours.Add(tiles[x + 1, y]);
                }
                if (y - 1 >= tiles.GetLength(1) - 1 && x - 1 >= 0)
                {
                    tiles[x, y].neighbours.Add(tiles[y - 1, x - 1]);
                }
                if (y - 1 >= tiles.GetLength(1) - 1)
                {
                    tiles[x, y].neighbours.Add(tiles[y - 1, x]);
                }
            }
        }
        yield return null;
    }

    string[] Generate(int width, int height)
    {
        int count = width * height;
        string[] leveldata = new string[count];
        for (int i = 0; i < count; i++)
        {
            Tile t = new Tile();
            t.x = i % width;
            t.y = i / width;
            leveldata[i] = "x:" + t.x + ":y:" + t.y + ":gold:" + t.gold + ":production:" + t.production + ":tourism:" + t.tourism + ":faith:" + t.faith + ":science:" + t.science + ":culture:" + t.culture + ":food:" + t.food;
        }
        return leveldata;
    }

    [RPC]
    void GetChunkAmount(int width, int height)
    {
        pendingChunks = height;
        tiles = new Tile[levelWidth, levelHeight];
        gamestate = "generating";
        guistate = "generating";
    }

    void OnJoinedLobby()
    {
        PhotonNetwork.JoinRoom("thegame");
        gamestate = "connected";
    }

    void OnPhotonJoinRoomFailed()
    {
        PhotonNetwork.CreateRoom("thegame", true, true, 64);
    }

    void OnJoinedRoom()
    {
        gamestate = "start";
        guistate = "readycheck";
        if (PhotonNetwork.isMasterClient)
        {

        }
    }

    void OnPhotonPlayerConnected(PhotonPlayer player)
    {
        if (PhotonNetwork.isMasterClient)
        {

        }
    }

    void OnPhotonCreateRoomFailed()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    [RPC]
    void CallLevel(string[] level)
    {
        StartCoroutine("Level", level);
    }

    IEnumerator Level(string[] level)
    {
        for (int i = 0; i < level.Length; i++)
        {
            GameObject go = Instantiate(Resources.Load("tile"), Vector3.zero, Quaternion.identity) as GameObject;
            Tile t = go.GetComponent<Tile>();
            string[] data = level[i].Split(':');
            for (int j = 0; j < data.Length; j++)
            {
                switch (data[j])
                {
                    case "x":
                        t.x = int.Parse(data[j + 1]);
                        break;
                    case "y":
                        t.y = int.Parse(data[j + 1]);
                        break;
                    case "gold":
                        t.gold = int.Parse(data[j + 1]);
                        break;
                    case "production":
                        t.production = int.Parse(data[j + 1]);
                        break;
                    case "food":
                        t.food = int.Parse(data[j + 1]);
                        break;
                    case "science":
                        t.science = int.Parse(data[j + 1]);
                        break;
                    case "faith":
                        t.faith = int.Parse(data[j + 1]);
                        break;
                    case "tourism":
                        t.tourism = int.Parse(data[j + 1]);
                        break;
                    default:
                        break;
                }
            }
            tiles[t.x, t.y] = t;
            go.transform.position = new Vector3(tileWidth * t.x + rowGap * t.y % 2, 0, tileWidth * t.y);
            if (i % 10 == 0)
                yield return null;
        }
        pendingChunks--;
    }

    [RPC]
    void Ready(PhotonPlayer player)
    {
        if (readPlayers.Contains(player))
            readPlayers.Remove(player);
        else
            readPlayers.Add(player);
    }
}
