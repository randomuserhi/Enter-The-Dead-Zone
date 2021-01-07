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
    public int Blank; //1 = true, 0 = false (not blank)

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

public class Tilemap : AbstractWorldEntity, IUpdatable, IRenderer
{
    //List of sprite renderers to apply render sorting too such that they render above walls correctly
    public List<IRenderer<SpriteRenderer>> AppliedRenderers = new List<IRenderer<SpriteRenderer>>();

    public List<PhysicalObject> Sorting; 

    private ComputeShader WallCompute;
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

    ComputeBuffer WallBuffer;
    ComputeBuffer FloorBuffer;

    private RawImage FloorRender;
    private RawImage[] Rows;
    private RenderTexture WallRenderTexture;
    private RenderTexture FloorRenderTexture;

    public TilePallet WallTilePalletData;
    public TilePallet FloorTilePalletData;

    private float TilesPerUnit = 1;
    private int TileDimension = 32;
    private Vector2Int TilemapSize;
    private Vector2 TilemapWorldSize;

    private const float ColliderMargin = 0.02f;
    private CompositeCollider2D CompositeCollider;
    private BoxCollider2D[] ColliderMap;
    private Rigidbody2D RB;

    public Tilemap(int TileDimension, Vector2Int TilemapSize, float TilesPerUnit = 1)
    {
        this.TileDimension = TileDimension;
        this.TilemapSize = TilemapSize;
        TilemapWorldSize = (Vector2)TilemapSize / TilesPerUnit;
        Init();
    }
    public Tilemap(ulong ID, int TileDimension, Vector2Int TilemapSize, float TilesPerUnit = 1) : base(ID)
    {
        this.TileDimension = TileDimension;
        this.TilemapSize = TilemapSize;
        TilemapWorldSize = (Vector2)TilemapSize / TilesPerUnit;
        Init();
    }

    //Destructor to clear buffers
    ~Tilemap()
    {
        if (WallBuffer != null)
        {
            GC.SuppressFinalize(WallBuffer); //Dont actually know if this is safe, probably need to check, without it unity throws a warning
                                            //See https://forum.unity.com/threads/computebuffer-warning-when-component-uses-executeineditmode.844648/
                                            //    https://stackoverflow.com/questions/151051/when-should-i-use-gc-suppressfinalize
            WallBuffer.Dispose();
        }
    }

    private void Init()
    {
        //Initialize map
        if (WallBuffer != null) WallBuffer.Dispose();
        Walls = new Tile[8] { new Tile(0, 2, 0, 0), new Tile(0, 2, 1, 1),new Tile(0, 2, 0, 0), new Tile(0, 2, 1, 1),
                            new Tile(1, 2, 0, 1), new Tile(1, 2, 1, 0),new Tile(1, 2, 0, 1), new Tile(1, 2, 1, 0) }; //Testing
        if (FloorBuffer != null) FloorBuffer.Dispose();
        Floor = new Tile[8] { new Tile(0, 2, 0, 1), new Tile(0, 2, 1, 1),new Tile(0, 2, 0, 1), new Tile(0, 2, 1, 1),
                            new Tile(1, 2, 0, 1), new Tile(1, 2, 1, 1),new Tile(1, 2, 0, 1), new Tile(1, 2, 1, 1) }; //Testing

        //Initialize GameObject
        Self = new GameObject();
        Self.name = "Tilemap";

        //Initialize colliders
        ColliderMap = new BoxCollider2D[TilemapSize.x * TilemapSize.y];
        //makes colliders based on wall map, perhaps create this into a virtual function for collider generation?
        for (int i = 0; i < TilemapSize.y; i++)
        {
            for (int j = 0; j < TilemapSize.x; j++)
            {
                int Index = i * TilemapSize.x + j;
                if (Walls[Index].Blank == 0)
                {
                    ColliderMap[Index] = Self.AddComponent<BoxCollider2D>();
                    ColliderMap[Index].size = new Vector2(1 / TilesPerUnit - ColliderMargin, 1 / TilesPerUnit - ColliderMargin);
                    ColliderMap[Index].offset = new Vector2(j / TilesPerUnit + ColliderMap[Index].size.x / 2 - TilemapSize.x / 2f / TilesPerUnit + ColliderMargin / 2, 
                                                            i / TilesPerUnit + ColliderMap[Index].size.y / 2 - TilemapSize.y / 2f / TilesPerUnit + ColliderMargin / 2);
                    ColliderMap[Index].usedByComposite = true;
                }
            }
        }
        RB = Self.AddComponent<Rigidbody2D>();
        RB.isKinematic = true;
        CompositeCollider = Self.AddComponent<CompositeCollider2D>();
        CompositeCollider.generationType = CompositeCollider2D.GenerationType.Manual;
        CompositeCollider.GenerateGeometry();
    }

    public void Update()
    {
        
    }

    public void BodyPhysicsUpdate()
    {

    }

    public void InitializeRenderer()
    {
        if (WallCompute == null)
        {
            WallCompute = UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("ComputeShaders/TilemapComputeShader"));
            ComputeKernel = WallCompute.FindKernel("TilemapRender");
        }
        if (FloorCompute == null)
        {
            FloorCompute = UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("ComputeShaders/TilemapComputeShader"));
            ComputeKernel = WallCompute.FindKernel("TilemapRender");
        }

        GenerateDefaultTileData();

        WallRenderTexture = new RenderTexture(TileDimension * TilemapSize.x, WallTilePalletData.TileStride * TilemapSize.y, 8)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            anisoLevel = 1
        };
        WallRenderTexture.Create();

        FloorRenderTexture = new RenderTexture(TileDimension * TilemapSize.x, TileDimension * TilemapSize.y, 8)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            anisoLevel = 1
        };
        FloorRenderTexture.Create();

        Canvas C = Self.AddComponent<Canvas>();
        C.renderMode = RenderMode.WorldSpace;
        C.sortingLayerName = "Floor";
        Self.AddComponent<CanvasScaler>();
        Self.AddComponent<GraphicRaycaster>();
        Self.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        FloorRender = Self.AddComponent<RawImage>();
        FloorRender.rectTransform.sizeDelta = new Vector2((float)TilemapSize.x / TilesPerUnit, (float)TilemapSize.y / TilesPerUnit);
        FloorRender.material = Resources.Load<Material>("Materials/LitMaterial");
        FloorRender.texture = FloorRenderTexture;

        //Initialize row gameobjects
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
            float StrideHeight = ((float)WallTilePalletData.TileStride / TileDimension) / TilesPerUnit;
            RT.position = new Vector3(0, -(i / TilesPerUnit) + StrideHeight / 2);
            Rows[i] = Row.AddComponent<RawImage>();
            Rows[i].rectTransform.sizeDelta = new Vector2(TilemapSize.x / TilesPerUnit, StrideHeight);
            Rows[i].material = Resources.Load<Material>("Materials/LitMaterial");
            Rows[i].texture = WallRenderTexture;
            Rows[i].uvRect = new Rect(new Vector2(0, i * 1f / TilemapSize.y), new Vector2(1, 1f / TilemapSize.y));
        }

        //Set Render Texture
        WallCompute.SetTexture(ComputeKernel, "Result", WallRenderTexture);
        FloorCompute.SetTexture(ComputeKernel, "Result", FloorRenderTexture);
        //Set Tile Dimensions
        WallCompute.SetInt("TileWidth", TileDimension);
        WallCompute.SetInt("TileHeight", WallTilePalletData.TileStride);
        FloorCompute.SetInt("TileWidth", TileDimension);
        FloorCompute.SetInt("TileHeight", TileDimension);
        //Set Map Dimensions
        WallCompute.SetInt("MapWidth", TilemapSize.x);
        FloorCompute.SetInt("MapWidth", TilemapSize.x);

        //Set TilePallet
        WallCompute.SetTexture(ComputeKernel, "TilePallet", WallTilePalletData.Pallet);
        WallCompute.SetInt("TileStride", WallTilePalletData.TileStride);
        FloorCompute.SetTexture(ComputeKernel, "TilePallet", FloorTilePalletData.Pallet);
        FloorCompute.SetInt("TileStride", FloorTilePalletData.TileStride);

        //Set the buffers
        WallBuffer = new ComputeBuffer(Walls.Length, sizeof(int) * 4);
        FloorBuffer = new ComputeBuffer(Floor.Length, sizeof(int) * 4);
        WallCompute.SetBuffer(ComputeKernel, "Map", WallBuffer);
        FloorCompute.SetBuffer(ComputeKernel, "Map", FloorBuffer);
    }

    private void GenerateDefaultTileData()
    {
        if (WallTilePalletData == null)
        {
            WallTilePalletData = new TilePallet
            {
                Pallet = Resources.Load<Texture2D>("TilemapPallets/Default"),
                NumTiles = 2,
                TileStride = 48, //32 + 16
                FrameCount = new int[2] { 2, 2 }
            };
        }
        if (FloorTilePalletData == null)
        {
            FloorTilePalletData = new TilePallet
            {
                Pallet = Resources.Load<Texture2D>("TilemapPallets/Default"),
                NumTiles = 2,
                TileStride = 48, //32 + 16
                FrameCount = new int[2] { 2, 2 }
            };
        }
    }

    public void Render()
    {
        WallBuffer.SetData(Walls);
        WallCompute.Dispatch(ComputeKernel, TilemapSize.x, TilemapSize.y, 1);
        FloorBuffer.SetData(Floor);
        FloorCompute.Dispatch(ComputeKernel, TilemapSize.x, TilemapSize.y, 1);

        for (int i = 0; i < AppliedRenderers.Count; i++)
        {
            float YOffset = (Self.transform.position.y + TilemapWorldSize.y / 2f) - AppliedRenderers[i].RenderObject.gameObject.transform.position.y;
            float XOffset = (Self.transform.position.x + TilemapWorldSize.x / 2f) - AppliedRenderers[i].RenderObject.gameObject.transform.position.x;
            if (YOffset >= 0 && XOffset >= 0 &&
                YOffset <= TilemapWorldSize.y && XOffset <= TilemapWorldSize.x)
            {
                AppliedRenderers[i].RenderObject.sortingOrder = Mathf.RoundToInt(YOffset * TilesPerUnit) * 2;
            }
        }
    }

    public override void Set(object Data)
    {

    }

    protected override void OnDelete()
    {
        //Release buffers
        if (WallBuffer != null)
            WallBuffer.Release();
        if (FloorBuffer != null)
            FloorBuffer.Release();
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
