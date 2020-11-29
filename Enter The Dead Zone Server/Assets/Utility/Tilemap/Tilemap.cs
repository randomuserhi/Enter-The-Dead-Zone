using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using DeadZoneEngine;
using DeadZoneEngine.Entities;

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

public struct Tile
{
    public int NumFrames;
    public int AnimationFrame;
    public int TileIndex;

    public Tile(int TileIndex, int NumFrames, int AnimationFrame)
    {
        this.TileIndex = TileIndex;
        this.NumFrames = NumFrames;
        this.AnimationFrame = AnimationFrame;
    }
}

public class TilePallet
{
    public int NumTiles;
    public Texture2D Pallet;
    public int TileStride;
    public int[] FrameCount;
}

public class TilemapWrapper : AbstractWorldEntity, IUpdatable
{
    private ComputeShader Compute;
    private int ComputeKernel;

    protected GameObject Self;

    public Tile[] Map; 
    // Map format is height followed by width such that:
    //                y
    //              ----->
    //            | [ 0 0 0 ]
    //          x | [ 0 0 0 ]
    //            v [ 0 0 0 ]
    //
    // This is to make rendering easier in the compute shader

    ComputeBuffer MapBuffer;
    public Vector2Int Size;

    private RawImage Sprite;
    private RenderTexture Render;

    public TilePallet TilePalletData;

    private int TileDimension;
    private Vector2Int TilemapSize;

    public TilemapWrapper(int TileDimension, Vector2Int TilemapSize)
    {
        this.TileDimension = TileDimension;
        this.TilemapSize = TilemapSize;
        Init();
    }
    public TilemapWrapper(ulong ID, int TileDimension, Vector2Int TilemapSize) : base(ID)
    {
        this.TileDimension = TileDimension;
        this.TilemapSize = TilemapSize;
        Init();
    }

    //Destructor to clear buffers
    ~TilemapWrapper()
    {
        if (MapBuffer != null)
        {
            GC.SuppressFinalize(MapBuffer); //Dont actually know if this is safe, probably need to check, without it unity throws a warning
                                            //See https://forum.unity.com/threads/computebuffer-warning-when-component-uses-executeineditmode.844648/
                                            //    https://stackoverflow.com/questions/151051/when-should-i-use-gc-suppressfinalize
            MapBuffer.Release();
        }
    }

    private void Init()
    {
        if (Compute == null)
        {
            Compute = UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("ComputeShaders/TilemapComputeShader"));
            ComputeKernel = Compute.FindKernel("TilemapRender");
        }

        GenerateDefaultTileData();

        Self = new GameObject();
        Self.name = "Tilemap";
        Self.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        Self.AddComponent<CanvasScaler>();
        Self.AddComponent<GraphicRaycaster>();
        Self.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
    
        Render = new RenderTexture(TileDimension * TilemapSize.x, TileDimension * TilemapSize.y, 8);
        Render.enableRandomWrite = true;
        Render.filterMode = FilterMode.Point;
        Render.anisoLevel = 1;
        Render.Create();

        Sprite = Self.AddComponent<RawImage>();
        Sprite.rectTransform.sizeDelta = new Vector2(1, 1);
        Sprite.texture = Render;
        Sprite.material = Resources.Load<Material>("Materials/LitMaterial");

        //Set Render Texture
        Compute.SetTexture(ComputeKernel, "Result", Render);
        //Set Tile Dimensions
        Compute.SetInt("TileDimension", TileDimension);
        //Set Map Dimensions
        Compute.SetInt("MapHeight", TilemapSize.y);

        //Set TilePallet
        Compute.SetTexture(ComputeKernel, "TilePallet", TilePalletData.Pallet);
        Compute.SetInt("TileStride", TilePalletData.TileStride);

        //Initialize map and buffer
        //Map = new Tile[TilemapSize.x * TilemapSize.y];
        Map = new Tile[4] { new Tile(1, 2, 0), new Tile(0, 2, 0),
                            new Tile(0, 2, 0), new Tile(1, 2, 0) }; //Testing
        MapBuffer = new ComputeBuffer(Map.Length, sizeof(int) * 3);

        //Set the buffer
        Compute.SetBuffer(ComputeKernel, "Map", MapBuffer);
    }

    private void GenerateDefaultTileData()
    {
        if (TilePalletData == null)
        {
            TilePalletData = new TilePallet();
            TilePalletData.Pallet = Resources.Load<Texture2D>("TilemapPallets/Default");
            TilePalletData.NumTiles = 2;
            TilePalletData.TileStride = 48; //32 + 16
            TilePalletData.FrameCount = new int[2] { 2, 2 };
        }
    }

    protected override void _Instantiate()
    {
        DZEngine.UpdatableObjects.Add(this);
    }

    public void UpdateRender()
    {
        MapBuffer.SetData(Map);
        Compute.Dispatch(ComputeKernel, TilemapSize.x, 1, 1);
    }

    public void Update()
    {
        UpdateRender();
    }

    public void BodyPhysicsUpdate()
    {

    }

    public override void Set(object Data)
    {

    }

    protected override void _Delete()
    {
        //Release buffers
        if (MapBuffer != null)
            MapBuffer.Release();
        //Delete Object
        GameObject.Destroy(Self);
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
