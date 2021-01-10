using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;

public class DZScript : MonoBehaviour
{
    //Coding my own sorting layers
    public enum SortingLayers
    {
        Default
    }
    private static DZEngine.ManagedList<IRenderer<SpriteRenderer>> SpriteRenderers = new DZEngine.ManagedList<IRenderer<SpriteRenderer>>();

    // Start is called before the first frame update
    void Start()
    {
        DZEngine.Initialize();
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        DZEngine.FixedUpdate();

        foreach (IRenderer<SpriteRenderer> Renderer in SpriteRenderers)
        {
            if (Renderer.SortingLayer == (int)SortingLayers.Default)
                Renderer.RenderObject.sortingOrder = Mathf.RoundToInt(-Renderer.RenderObject.transform.position.y) * 2 + 1;
        }
    }

    void OnApplicationQuit()
    {
        DZEngine.ReleaseResources();
    }
}
