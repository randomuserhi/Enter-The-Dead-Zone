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
    public int Blank;
    public int NumFrames;
    public int AnimationFrame;
    public int TileIndex;

    public Tile(int TileIndex, int NumFrames, int AnimationFrame, int Blank)
    {
        this.TileIndex = TileIndex;
        this.NumFrames = NumFrames;
        this.AnimationFrame = AnimationFrame;
        this.Blank = Blank;
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
    //List of sprite renderers to apply render sorting too such that they render above walls correctly
    public List<SpriteRenderer> AppliedRenderers = new List<SpriteRenderer>();

    public List<PhysicalObject> Sorting; 

    private ComputeShader Compute;
    private ComputeShader FloorCompute;
    private int ComputeKernel;

    protected GameObject Self;

    public Tile[] Floor;
    public Tile[] Walls; 
    // Map format is height followed by width such that:
    //                x
    //              ----->
    //            | [ 0 0 0 ]
    //          y | [ 0 0 0 ]
    //            v [ 0 0 0 ]
    //
    // This is to make rendering easier in the compute shader

    ComputeBuffer MapBuffer;
    ComputeBuffer FloorBuffer;

    private RawImage FloorRender;
    private RawImage[] Rows;
    private RenderTexture Render;
    private RenderTexture FloorRenderTexture;

    public TilePallet TilePalletData;
    public TilePallet FloorTilePalletData;

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
        //Should change this into a more scalable system but for now this will do:
        AppliedRenderers.AddRange(GameObject.FindObjectsOfType<SpriteRenderer>());

        if (Compute == null)
        {
            Compute = UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("ComputeShaders/TilemapComputeShader"));
            ComputeKernel = Compute.FindKernel("TilemapRender");
        }
        if (FloorCompute == null)
        {
            FloorCompute = UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("ComputeShaders/TilemapComputeShader"));
            ComputeKernel = Compute.FindKernel("TilemapRender");
        }

        GenerateDefaultTileData();

        Self = new GameObject();
        Self.name = "Tilemap";
        Canvas C = Self.AddComponent<Canvas>();
        C.renderMode = RenderMode.WorldSpace;
        C.sortingLayerName = "Floor";
        Self.AddComponent<CanvasScaler>();
        Self.AddComponent<GraphicRaycaster>();
        Self.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        FloorRender = Self.AddComponent<RawImage>();
        FloorRender.rectTransform.sizeDelta = new Vector2(TilemapSize.x, TilemapSize.y);
        FloorRender.material = Resources.Load<Material>("Materials/LitMaterial");
        FloorRender.texture = FloorRenderTexture;

        Render = new RenderTexture(TileDimension * TilemapSize.x, TilePalletData.TileStride * TilemapSize.y, 8);
        Render.enableRandomWrite = true;
        Render.filterMode = FilterMode.Point;
        Render.anisoLevel = 1;
        Render.Create();

        FloorRenderTexture = new RenderTexture(TileDimension * TilemapSize.x, TilePalletData.TileStride * TilemapSize.y, 8);
        FloorRenderTexture.enableRandomWrite = true;
        FloorRenderTexture.filterMode = FilterMode.Point;
        FloorRenderTexture.anisoLevel = 1;
        FloorRenderTexture.Create();

        Rows = new RawImage[TilemapSize.y];
        for (int i = 0; i < TilemapSize.y; i++)
        {
            GameObject Row = new GameObject();
            Row.transform.parent = Self.transform;
            Canvas CRow = Row.AddComponent<Canvas>();
            CRow.renderMode = RenderMode.WorldSpace;
            CRow.overrideSorting = true;
            CRow.sortingOrder = i * 2 + 1;
            RectTransform RT = Row.GetComponent<RectTransform>();
            RT.sizeDelta = Vector2.zero;
            RT.position = new Vector3(0, -i + 0.75f);
            Rows[i] = Row.AddComponent<RawImage>();
            Rows[i].rectTransform.sizeDelta = new Vector2(TilemapSize.x, 1.5f);
            Rows[i].material = Resources.Load<Material>("Materials/LitMaterial");
            Rows[i].texture = Render;
            Rows[i].uvRect = new Rect(new Vector2(0, i * 1f / TilemapSize.y), new Vector2(1, 1f / TilemapSize.y));
        }

        //Set Render Texture
        Compute.SetTexture(ComputeKernel, "Result", Render);
        FloorCompute.SetTexture(ComputeKernel, "Result", FloorRenderTexture);
        //Set Tile Dimensions
        Compute.SetInt("TileDimension", TileDimension);
        FloorCompute.SetInt("TileDimension", TileDimension);
        //Set Map Dimensions
        Compute.SetInt("MapHeight", TilemapSize.y);
        FloorCompute.SetInt("MapHeight", TilemapSize.y);

        //Set TilePallet
        Compute.SetTexture(ComputeKernel, "TilePallet", TilePalletData.Pallet);
        Compute.SetInt("TileStride", TilePalletData.TileStride);
        FloorCompute.SetTexture(ComputeKernel, "TilePallet", FloorTilePalletData.Pallet);
        FloorCompute.SetInt("TileStride", FloorTilePalletData.TileStride);

        //Initialize map and buffer
        //Map = new Tile[TilemapSize.x * TilemapSize.y];
        Walls = new Tile[4] { new Tile(0, 2, 0, 0), new Tile(0, 2, 1, 1),
                            new Tile(1, 2, 0, 1), new Tile(1, 2, 1, 0) }; //Testing
        MapBuffer = new ComputeBuffer(Walls.Length, sizeof(int) * 4);
        Floor = new Tile[4] { new Tile(0, 2, 0, 1), new Tile(0, 2, 1, 1),
                            new Tile(1, 2, 0, 1), new Tile(1, 2, 1, 1) }; //Testing
        FloorBuffer = new ComputeBuffer(Floor.Length, sizeof(int) * 4);

        //Set the buffer
        Compute.SetBuffer(ComputeKernel, "Map", MapBuffer);
        FloorCompute.SetBuffer(ComputeKernel, "Map", FloorBuffer);
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
        if (FloorTilePalletData == null)
        {
            FloorTilePalletData = new TilePallet();
            FloorTilePalletData.Pallet = Resources.Load<Texture2D>("TilemapPallets/Default");
            FloorTilePalletData.NumTiles = 2;
            FloorTilePalletData.TileStride = 32;
            FloorTilePalletData.FrameCount = new int[2] { 2, 2 };
        }
    }

    protected override void _Instantiate()
    {
        DZEngine.UpdatableObjects.Add(this);
    }

    public void UpdateRender()
    {
        MapBuffer.SetData(Walls);
        Compute.Dispatch(ComputeKernel, TilemapSize.x, TilemapSize.y, 1);
        FloorBuffer.SetData(Floor);
        FloorCompute.Dispatch(ComputeKernel, TilemapSize.x, TilemapSize.y, 1);
    }

    public void Update()
    {
        UpdateRender();
        for (int i = 0; i < AppliedRenderers.Count; i++)
        {
            float YOffset = (Self.transform.position.y + TilemapSize.y / 2f) - AppliedRenderers[i].gameObject.transform.position.y;
            float XOffset = (Self.transform.position.x + TilemapSize.x / 2f) - AppliedRenderers[i].gameObject.transform.position.x;
            if (YOffset >= 0 && XOffset >= 0 &&
                YOffset <= TilemapSize.y && XOffset <= TilemapSize.x)
            {
                AppliedRenderers[i].sortingOrder = Mathf.RoundToInt(YOffset) * 2;
            }
        }
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
        for (int i = 0; i < Rows.Length; i++)
        {
            GameObject.Destroy(Rows[i].gameObject);
        }
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
