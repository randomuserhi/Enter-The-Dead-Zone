﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using DeadZoneEngine;
using DeadZoneEngine.Entities;

public class Tile
{
    public Tile()
    {

    }
}

/*public class TilemapWrapper : PhysicalObject
{
    private Tilemap Map;
    private TilemapRenderer Renderer;
    private TilemapCollider2D Collider;
    private CompositeCollider2D CompositeCollider;

    public TilemapWrapper()
    {
        Init();
    }
    public TilemapWrapper(ulong ID) : base(ID)
    {
        Init();
    }

    private void Init()
    {
        Map = Self.AddComponent<Tilemap>();
        Renderer = Self.AddComponent<TilemapRenderer>();
        Collider = Self.AddComponent<TilemapCollider2D>();
        CompositeCollider = Self.AddComponent<CompositeCollider2D>();

        Collider.usedByComposite = true;
    }

    public override void Set(object Data)
    {
        
    }

    protected override void SetEntityType()
    {
        Type = EntityType.Tilemap;
    }

    public override byte[] GetBytes()
    {
        throw new NotImplementedException();
    }
}*/

public class TilemapWrapper : AbstractWorldEntity
{
    private static ComputeShader Compute;
    private static int ComputeKernel;

    protected GameObject Self;

    public Tile[] Map;
    public Vector2Int Size;

    private RawImage Sprite;
    private RenderTexture Render;

    public TilemapWrapper()
    {
        Init();
    }
    public TilemapWrapper(ulong ID) : base(ID)
    {
        Init();
    }

    private void Init()
    {
        Self = new GameObject();

        if (Compute == null)
        {
            Compute = Resources.Load<ComputeShader>("ComputeShaders/TilemapComputeShader");
            ComputeKernel = Compute.FindKernel("TilemapRender");
        }

        Self = new GameObject();
        Self.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        Self.AddComponent<CanvasScaler>();
        Self.AddComponent<GraphicRaycaster>();
        Self.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
    
        Render = new RenderTexture(512, 512, 8, RenderTextureFormat.ARGB32);
        Render.enableRandomWrite = true;
        Render.filterMode = FilterMode.Point;
        Render.Create();

        Self.transform.parent = Self.transform;
        Sprite = Self.AddComponent<RawImage>();
        Sprite.rectTransform.sizeDelta = new Vector2(1, 1);
        Sprite.texture = Render;
        Sprite.material = Resources.Load<Material>("Materials/LitMaterial");
    }

    public void UpdateRender()
    {
        //TODO see if I can use a texture2D
        Compute.SetTexture(ComputeKernel, "Result", Render);
        Compute.Dispatch(ComputeKernel, 512/8, 512/8, 1);
    }

    public override void Set(object Data)
    {

    }

    protected override void SetEntityType()
    {
        Type = EntityType.Tilemap;
    }

    public override byte[] GetBytes()
    {
        throw new NotImplementedException();
    }
}
