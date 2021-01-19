using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;

public static class Main
{
    //Sorting layers for rendering
    public enum SortingLayers
    {
        Default
    }
    private static DZEngine.ManagedList<IRenderer<SpriteRenderer>> SpriteRenderers = new DZEngine.ManagedList<IRenderer<SpriteRenderer>>(); //List of SpriteRenderers

    // Start is called before the first frame update
    public static void Start()
    {
        Tile[] FloorMap = new Tile[]
        {
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(1, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(1, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(1, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(1, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(1, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(1, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(1, 1),
            new Tile(1, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(1, 1)
        };

        Tile[] WallMap = new Tile[]
        {
            new Tile(1, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(1, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(1, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(1, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(1, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(1, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(1, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(1, 1),
            new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(0, 1), new Tile(1, 1)
        };

        Tilemap T = (Tilemap)new Tilemap(32, 32, new Vector2Int(14, 14), FloorMap, WallMap, 1).Create();
    }

    // Update is called once per frame
    public static void FixedUpdate()
    {
        foreach (IRenderer<SpriteRenderer> Renderer in SpriteRenderers)
        {
            if (Renderer.SortingLayer == (int)SortingLayers.Default)
                Renderer.RenderObject.sortingOrder = Mathf.RoundToInt(-Renderer.RenderObject.transform.position.y) * 2 + 1;
        }

        /*for (int i = 0; i < DZEngine.AbstractWorldEntities.Count; i++)
        {
            Tilemap T = DZEngine.AbstractWorldEntities[i] as Tilemap;
            if (T != null)
                Debug.Log((ulong)T.ID + ":" + T.FlaggedToDelete);
        }*/
    }
}
