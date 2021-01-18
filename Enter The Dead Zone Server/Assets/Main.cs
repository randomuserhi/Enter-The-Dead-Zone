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
