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

    // Start is called before the first frame update
    public static void Start()
    {
        LoadMenu();
    }

    public static void LoadMenu()
    {
        DZEngine.ReleaseResources();
        if (Game.Client == null)
        {
            Game.Client = Client.GetClient();
            Game.Client.AddPlayer();
        }
        //Set client player positions to somewhere approapriate
        for (int i = 0; i < Game.Client.Players.Length; i++)
        {
            //Game.Client.Players[i].Entity.SetPosition(new Vector3(0, 0)); //TODO::
        }
    }

    // Update is called once per frame
    public static void FixedUpdate()
    {
        foreach (IRenderer<SpriteRenderer> Renderer in SpriteRenderers)
        {
            if (Renderer.SortingLayer == (int)SortingLayers.Default)
                Renderer.RenderObject.sortingOrder = Mathf.RoundToInt(-Renderer.RenderObject.transform.position.y);
        }

        /*for (int i = 0; i < DZEngine.AbstractWorldEntities.Count; i++)
        {
            Tilemap T = DZEngine.AbstractWorldEntities[i] as Tilemap;
            if (T != null)
                Debug.Log((ulong)T.ID + ":" + T.FlaggedToDelete);
        }*/
    }
}
