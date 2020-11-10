using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;

public struct Tile
{
}

public class TilemapWrapper : PhysicalObject
{
    private static ComputeShader Compute = null;
    private static int ComputeKernel;

    public Tile[] Map;
    public Vector2Int Size;

    private RenderTexture Render;
    public Texture2D Result;

    private SpriteRenderer SpriteRender;

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
        if (Compute == null)
        {
            Compute = Resources.Load<ComputeShader>("ComputeShaders/TilemapComputeShader");
            ComputeKernel = Compute.FindKernel("TilemapRender");
        }

        Render = new RenderTexture(512, 512, 24);
        Render.enableRandomWrite = true;
        Render.Create();

        Result = new Texture2D(512, 512);

        SpriteRender = Self.AddComponent<SpriteRenderer>();
    }

    public void GetRender()
    {
        //TODO see if I can use a texture2D
        Compute.SetTexture(ComputeKernel, "Result", Render);
        Compute.Dispatch(ComputeKernel, 512/8, 512/8, 1);

        RenderTexture.active = Render;
        Result.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        RenderTexture.active = null; //Reset active render => seems to work without this line still, but not sure => need more research

        SpriteRender.sprite = Sprite.Create(Result, new Rect(0, 0, 512, 512), Vector2.zero);
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
