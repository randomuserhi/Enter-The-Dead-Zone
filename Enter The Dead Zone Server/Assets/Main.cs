using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;

public static class Main
{
    //Coding my own sorting layers
    public enum SortingLayers
    {
        Default
    }
    private static DZEngine.ManagedList<IRenderer<SpriteRenderer>> SpriteRenderers = new DZEngine.ManagedList<IRenderer<SpriteRenderer>>();

    static PlayerCreature P;

    // Start is called before the first frame update
    public static void Start()
    {
        P = new PlayerCreature();

        Tilemap T1 = (Tilemap)new Tilemap(32, 48, new Vector2Int(4, 2), 1f);
        Tilemap T2 = (Tilemap)new Tilemap(32, 48, new Vector2Int(4, 2), 1f);

        Tile[] WallMap = new Tile[12]
            {
                new Tile(0, 1, 2, 1), new Tile(1), new Tile(0, 1, 2, 1), new Tile(1),
                new Tile(1), new Tile(0, 1, 2, 1), new Tile(1), new Tile(0, 1, 2, 1),
                new Tile(0, 1, 2, 1), new Tile(1), new Tile(0, 1, 2, 1), new Tile(1)
            };

        Tile[] FloorMap = new Tile[12]
            {
                new Tile(0, 1, 0), new Tile(0, 1, 0), new Tile(0, 1, 0), new Tile(0, 1, 0),
                new Tile(0, 1, 0), new Tile(0, 1, 0), new Tile(0, 1, 0), new Tile(0, 1, 0),
                new Tile(0, 1, 0), new Tile(0, 1, 0), new Tile(0, 1, 0), new Tile(0, 1, 0)
            };

        T1.Resize(new Vector2Int(4, 3), FloorMap, WallMap);
        T1.Resize(new Vector2Int(4, 2), FloorMap, WallMap);
        T1.ReleaseUnusedResources();
    }

    public static bool TestDelete = false;

    // Update is called once per frame
    public static void FixedUpdate()
    {
        foreach (IRenderer<SpriteRenderer> Renderer in SpriteRenderers)
        {
            if (Renderer.SortingLayer == (int)SortingLayers.Default)
                Renderer.RenderObject.sortingOrder = Mathf.RoundToInt(-Renderer.RenderObject.transform.position.y) * 2 + 1;
        }

        if (TestDelete)
            DZEngine.Destroy(P);
    }
}
