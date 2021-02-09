using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;
using ClientHandle;
using System.Linq;

public static class Main
{
    //Sorting layers for rendering
    public enum SortingLayers
    {
        Default
    }
    private static DZEngine.ManagedList<IRenderer<SpriteRenderer>> SpriteRenderers = new DZEngine.ManagedList<IRenderer<SpriteRenderer>>(); //List of SpriteRenderers

    public static Tilemap Tilemap;
    private static TriggerPlate StartPlate;
    public static bool GameStarted = false;

    // Start is called before the first frame update
    public static void Start()
    {
        LoadMenu();
    }

    const string MenuFloorMap =
            @"
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/
            0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1/0,0,0,1
            ";

    const string MenuWallMap =
        @"
            1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/0,0,1,0/1,1,0,1/
            1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1/1,1,0,1
            ";

    private static void LoadMenu()
    {
        List<Client> Clients = ClientID.ConnectedClients.Values.ToList();
        foreach (Client C in Clients)
        {
            if (C == null || C.Players == null)
                continue;
            for (int i = 0; i < C.Players.Length; i++)
            {
                if (C.Players[i] == null || C.Players[i].Entity == null) continue;

                C.Players[i].Entity.Position = new Vector2(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-8f, 8f));
                C.Players[i].Entity.Out = false;
            }
        }

        if (Tilemap == null)
        {
            Tilemap = (Tilemap)new Tilemap(32, 64, new Vector2Int(20, 20), Tilemap.TilesFromString(MenuFloorMap), Tilemap.TilesFromString(MenuWallMap)).Create();
        }
        else
        {
            Tilemap.Resize(new Vector2Int(20, 20), Tilemap.TilesFromString(MenuFloorMap), Tilemap.TilesFromString(MenuWallMap));
            Tilemap.ReleaseUnusedResources();
        }

        StartPlate = new TriggerPlate(new Vector2(4, 2), new Vector2(0, -3));
        StartPlate.OnTrigger = StartGame;
    }

    private static EnemyCreature.Path GeneratePath(int Height, int Width)
    {
        List<EnemyCreature.WayPoint> Path = new List<EnemyCreature.WayPoint>();

        int NumTurns = UnityEngine.Random.Range(6, 10);
        string[] Tiles = null;
        bool ValidPath = false;
        while (!ValidPath)
        {
            Path.Clear();
            Tiles = MenuFloorMap.Split('/');
            Vector2Int StartPosition = new Vector2Int(1, UnityEngine.Random.Range(1, Height - 1));
            float Chance = UnityEngine.Random.Range(0f, 1f);
            if (Chance > 0.25)
                StartPosition = new Vector2Int(Width - 2, UnityEngine.Random.Range(1, Height - 1));
            else if (Chance > 0.5)
                StartPosition = new Vector2Int(UnityEngine.Random.Range(1, Width - 1), 1);
            else if (Chance > 0.75)
                StartPosition = new Vector2Int(UnityEngine.Random.Range(1, Width - 1), Height - 2);

            int Direction = 1;
            if (Chance > 0.25)
                Direction = 3;
            else if (Chance > 0.5)
                Direction = 0;
            else if (Chance > 0.75)
                Direction = 2;

            Path.Add(new EnemyCreature.WayPoint()
            {
                Direction = Direction,
                Position = StartPosition
            });

            for (int i = 0; i < NumTurns; i++)
            {
                int CurrentDirection = Direction;
                int Length = UnityEngine.Random.Range(3, 8);
                if (i == NumTurns - 1)
                {
                    switch (Direction)
                    {
                        case 0: Length = Height - StartPosition.y; break;
                        case 1: Length = Width - StartPosition.x; break;
                        case 2: Length = StartPosition.y; break;
                        case 3: Length = StartPosition.x; break;
                    }
                }
                for (int j = 0; j < Length; j++)
                {
                    Tiles[StartPosition.y * Width + StartPosition.x] = "0,2,0,1";
                    Vector2Int NewPosition = StartPosition;
                    switch (Direction)
                    {
                        case 0: NewPosition.y++; break;
                        case 1: NewPosition.x++; break;
                        case 2: NewPosition.y--; break;
                        case 3: NewPosition.x--; break;
                    }
                    if (NewPosition.x < 1 || NewPosition.x > Width - 2 || NewPosition.y < 1 || (NewPosition.y > Height - 3 && Direction == 0))
                    {
                        break;
                    }
                    StartPosition = NewPosition;
                }
                bool ValidDirection = true;
                do
                {
                    Direction = (Direction + (UnityEngine.Random.Range(0f, 1f) > 0.5 ? 1 : -1)) % 4;
                    if (Direction < 0) Direction += 4;
                    Vector2Int NewPosition = StartPosition;
                    switch (Direction)
                    {
                        case 0: NewPosition.y++; break;
                        case 1: NewPosition.x++; break;
                        case 2: NewPosition.y--; break;
                        case 3: NewPosition.x--; break;
                    }
                    if (NewPosition.x < 1 || NewPosition.x > Width - 1 || NewPosition.y < 1 || NewPosition.y > Height - 2)
                    {
                        ValidDirection = false;
                    }
                    else if (Tiles[NewPosition.y * Width + NewPosition.x] == "0,2,0,1")
                    {
                        ValidDirection = false;
                    }
                }
                while (CurrentDirection == Direction && !ValidDirection);
                Path.Add(new EnemyCreature.WayPoint()
                {
                    Direction = CurrentDirection,
                    Position = StartPosition
                });
            }

            bool TopLeftQuadrant = false;
            bool TopRightQuadrant = false;
            bool BottomLeftQuadrant = false;
            bool BottomRightQuadrant = false;
            for (int i = 0; i < Path.Count; i++)
            {
                if (Path[i].Position.x < Width / 2)
                {
                    if (Path[i].Position.y < Height / 2)
                    {
                        BottomLeftQuadrant = true;
                    }
                    else
                    {
                        TopLeftQuadrant = true;
                    }
                }
                else
                {
                    if (Path[i].Position.y < Height / 2)
                    {
                        BottomRightQuadrant = true;
                    }
                    else
                    {
                        TopRightQuadrant = true;
                    }
                }
            }
            ValidPath = TopLeftQuadrant && TopRightQuadrant && BottomLeftQuadrant && BottomRightQuadrant;
        }

        return new EnemyCreature.Path()
        {
            Map = string.Join("/", Tiles),
            Traversal = Path
        };
    }

    private static EnemyCreature.Path CurrentPath;
    public static void StartGame()
    {
        CurrentPath = GeneratePath(20, 20);
        Tilemap.Resize(new Vector2Int(20, 20), Tilemap.TilesFromString(CurrentPath.Map), Tilemap.TilesFromString(MenuWallMap));

        GameStarted = true;
        DZEngine.Destroy(StartPlate);
    }

    public static int[] LifeForce = new int[3] { 10, 10, 10 };
    private static float WaveTimer = 5;
    private static int WaveSize = 10;
    private static int WaveMaxSize = 10;
    private static int WaveHealth = 1;
    private static int Wave = 0;
    private static float WaveSpacing = 0.3f;
    private static float WaveSpacingMax = 1;
    private static int EnemiesToSpawn = 5;
    private static float SpawnTimer = 0;
    public static int Money = 40;
    public static float Drain = 0;
    private static List<EnemyCreature> Enemies = new List<EnemyCreature>();
    public static List<Turret> Towers = new List<Turret>();

    public static void TakeLifeForce()
    {
        for (int i = 0; i < LifeForce.Length; i++)
        {
            if (LifeForce[i] > 0)
            {
                LifeForce[i]--;
                break;
            }
        }
    }

    public static void GainLifeForce(int Health)
    {
        for (int i = 0; i < LifeForce.Length; i++)
        {
            if (LifeForce[i] != 0)
            {
                LifeForce[i] += Health;
                if (LifeForce[i] > 10)
                    LifeForce[i] = 10;
                break;
            }
        }
    }

    private static void Reset()
    {
        Money = 40;
        for (int i = 0; i < Enemies.Count; i++)
        {
            DZEngine.Destroy(Enemies[i]);
        }
        Enemies.Clear();
        for (int i = 0; i < Towers.Count; i++)
        {
            DZEngine.Destroy(Towers[i]);
        }
        Towers.Clear();
        LifeForce = new int[3] { 10, 10, 10 };
        GameStarted = false;
        LoadMenu();
    }

    // Update is called once per frame
    public static void FixedUpdate()
    {
        if (DZSettings.ActiveRenderers == true)
            foreach (IRenderer<SpriteRenderer> Renderer in SpriteRenderers)
            {
                if (Renderer.SortingLayer == (int)SortingLayers.Default)
                    Renderer.RenderObject.sortingOrder = Mathf.RoundToInt(-Renderer.RenderObject.transform.parent.position.y * 10);
            }

        if (GameStarted)
        {
            if (Drain <= 0)
            {
                Drain = 5;
                TakeLifeForce();
            }
            else Drain -= Time.fixedDeltaTime;

            List<Client> Clients = ClientID.ConnectedClients.Values.ToList();
            int TotalNumPlayers = 0;
            foreach (Client C in Clients)
            {
                if (C == null) continue;
                if (C.Players == null) continue;
                for (int i = 0; i < C.Players.Length; i++)
                {
                    if (C.Players[i] == null) continue;
                    if (C.Players[i].Entity == null) continue;
                    TotalNumPlayers++;
                }
            }

            if (TotalNumPlayers == 0)
            {
                Reset();
            }

            bool Alive = false;
            for (int i = 0; i < LifeForce.Length; i++)
            {
                if (LifeForce[i] > 0)
                    Alive = true;
            }

            if (WaveTimer > 0)
            {
                WaveTimer -= Time.fixedDeltaTime;
            }
            else
            {
                WaveTimer = Random.Range(4f, 10f);
                Wave++;
                EnemiesToSpawn = Random.Range(10, WaveMaxSize);
                WaveHealth++;
                WaveSpacing = Random.Range(0.1f, WaveSpacingMax);
                WaveSpacingMax += Random.Range(-0.5f, 0.5f);
                if (Random.Range(0f, 1f) < 0.3)
                {
                    WaveSpacing = 0.3f;
                }

                if (WaveSpacing < 0.3)
                    WaveSpacing = 0.3f;
            }

            SpawnTimer += Time.fixedDeltaTime;
            if (WaveTimer <= 0 && EnemiesToSpawn > 0 && SpawnTimer > WaveSpacing)
            {
                EnemiesToSpawn--;

                SpawnTimer = 0;

                EnemyCreature EC = new EnemyCreature();
                EC.Position = Tilemap.TilemapToWorldPosition(CurrentPath.Traversal[0].Position);
                EC.Health = WaveHealth;
                EC.Traversal = CurrentPath;
                Enemies.Add(EC);
            }

            if (Alive == false)
            {
                Reset();
            }
        }
    }
}
