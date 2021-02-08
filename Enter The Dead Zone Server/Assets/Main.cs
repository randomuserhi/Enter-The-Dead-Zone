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

    public static Tilemap MenuMap;
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

        if (MenuMap == null)
        {
            MenuMap = (Tilemap)new Tilemap(32, 64, new Vector2Int(20, 20), Tilemap.TilesFromString(MenuFloorMap), Tilemap.TilesFromString(MenuWallMap)).Create();
        }
        else
        {
            MenuMap.Resize(new Vector2Int(20, 20), Tilemap.TilesFromString(MenuFloorMap), Tilemap.TilesFromString(MenuWallMap));
            MenuMap.ReleaseUnusedResources();
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

    public static void StartGame()
    {
        EnemyCreature.Path P = GeneratePath(20, 20);
        MenuMap.Resize(new Vector2Int(20, 20), Tilemap.TilesFromString(P.Map), Tilemap.TilesFromString(MenuWallMap));

        GameStarted = true;
        DZEngine.Destroy(StartPlate);

        EnemyCreature EC = new EnemyCreature();
        EC.Traversal = P;
        EC.Position = MenuMap.TilemapToWorldPosition(P.Traversal[0].Position);
    }

    // Update is called once per frame
    public static void FixedUpdate()
    {
        foreach (IRenderer<SpriteRenderer> Renderer in SpriteRenderers)
        {
            if (Renderer.SortingLayer == (int)SortingLayers.Default)
                Renderer.RenderObject.sortingOrder = Mathf.RoundToInt(-Renderer.RenderObject.transform.parent.position.y * 10);
        }

        if (GameStarted)
        {
            
        }
    }
}
