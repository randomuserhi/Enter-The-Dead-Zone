using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;
using ClientHandle;

public static class Main
{
    //Sorting layers for rendering
    public enum SortingLayers
    {
        Default
    }
    private static DZEngine.ManagedList<IRenderer<SpriteRenderer>> SpriteRenderers = new DZEngine.ManagedList<IRenderer<SpriteRenderer>>(); //List of SpriteRenderers

    public static Client Client;

    // Start is called before the first frame update
    public static void Start()
    {
        LoadMenu();
    }

    public static void LoadMenu()
    {
        DZEngine.ReleaseResources();
        Client = new Client();
        AddPlayer();
    }

    public static void AddPlayer()
    {
        Game.NumLocalPlayers = 1;
        Client.Players.Add(new Player());
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
