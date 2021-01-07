using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using DeadZoneEngine;
using DeadZoneEngine.Entities;

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
    public int SortingLayer { get; set; }

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

    private Canvas FloorRender;
    private Canvas[] Rows;
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
        this.TilesPerUnit = TilesPerUnit;
        Initialize();
    }
    public Tilemap(ulong ID, int TileDimension, Vector2Int TilemapSize, float TilesPerUnit = 1) : base(ID)
    {
        this.TileDimension = TileDimension;
        this.TilemapSize = TilemapSize;
        this.TilesPerUnit = TilesPerUnit;
        Initialize();
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

    private void Initialize()
    {
        TilemapWorldSize = (Vector2)TilemapSize / TilesPerUnit;
        Walls = new Tile[TilemapSize.x * TilemapSize.y];
        Floor = new Tile[TilemapSize.x * TilemapSize.y];
        ColliderMap = new BoxCollider2D[TilemapSize.x * TilemapSize.y];

        //Initialize GameObject
        Self = new GameObject();
        Self.name = "Tilemap";

        //Initialize Collider objects
        RB = Self.AddComponent<Rigidbody2D>();
        RB.isKinematic = true;

        CompositeCollider = Self.AddComponent<CompositeCollider2D>();
        CompositeCollider.generationType = CompositeCollider2D.GenerationType.Manual;
    }

    public override void Instantiate()
    {
        GenerateColliders();
    }

    protected virtual void GenerateColliders()
    {
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
        CompositeCollider.GenerateGeometry();
    }

    public void Update()
    {
        
    }

    public void BodyPhysicsUpdate()
    {

    }

    public void InitializeRenderer() //TODO:: reorganise to allow for creating new tilemaps without replacing entire objects
    {
        //Initialize Buffers
        if (WallBuffer != null) WallBuffer.Dispose();
        if (FloorBuffer != null) FloorBuffer.Dispose();

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

        FloorRender = Self.AddComponent<Canvas>();
        FloorRender.renderMode = RenderMode.WorldSpace;
        FloorRender.sortingLayerName = "Floor";
        Self.AddComponent<CanvasScaler>();
        Self.AddComponent<GraphicRaycaster>();
        Self.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        RawImage FloorImage = Self.AddComponent<RawImage>();
        FloorImage.rectTransform.sizeDelta = new Vector2((float)TilemapSize.x / TilesPerUnit, (float)TilemapSize.y / TilesPerUnit);
        FloorImage.material = Resources.Load<Material>("Materials/LitMaterial");
        FloorImage.texture = FloorRenderTexture;

        //Initialize row gameobjects
        Rows = new Canvas[TilemapSize.y];
        for (int i = 0; i < TilemapSize.y; i++)
        {
            GameObject Row = new GameObject();
            Row.transform.parent = Self.transform;
            Rows[i] = Row.AddComponent<Canvas>();
            Rows[i].renderMode = RenderMode.WorldSpace;
            Rows[i].overrideSorting = true;
            Rows[i].sortingOrder = Mathf.RoundToInt(Row.transform.position.y) * 2 + 1;
            RectTransform RT = Row.GetComponent<RectTransform>();
            RT.sizeDelta = Vector2.zero;
            float StrideHeight = ((float)WallTilePalletData.TileStride / TileDimension) / TilesPerUnit;
            RT.position = new Vector3(0, -(i / TilesPerUnit) + StrideHeight / 2);
            RawImage RowImage = Row.AddComponent<RawImage>();
            RowImage.rectTransform.sizeDelta = new Vector2(TilemapSize.x / TilesPerUnit, StrideHeight);
            RowImage.material = Resources.Load<Material>("Materials/LitMaterial");
            RowImage.texture = WallRenderTexture;
            RowImage.uvRect = new Rect(new Vector2(0, i * 1f / TilemapSize.y), new Vector2(1, 1f / TilemapSize.y));
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

    private Vector3 PrevTilePosition;
    private void UpdateRenderSortingLayers()
    {
        if (PrevTilePosition != Self.transform.position) //Avoid updating all rows constantly has tilemaps can get quite large
        {
            PrevTilePosition = Self.transform.position;
            for (int i = 0; i < Rows.Length; i++)
            {
                float StrideHeight = ((float)WallTilePalletData.TileStride / TileDimension) / TilesPerUnit;
                float BaseY = Rows[i].transform.position.y - ((1 / TilesPerUnit - StrideHeight) / 2);
                Rows[i].sortingOrder = Mathf.RoundToInt(-BaseY) * 2 + 1;
            }
        }
    }

    public void Render()
    {
        UpdateRenderSortingLayers();

        WallBuffer.SetData(Walls);
        WallCompute.Dispatch(ComputeKernel, TilemapSize.x, TilemapSize.y, 1);
        FloorBuffer.SetData(Floor);
        FloorCompute.Dispatch(ComputeKernel, TilemapSize.x, TilemapSize.y, 1);
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
        if (Rows != null)
        {
            for (int i = 0; i < Rows.Length; i++)
            {
                GameObject.Destroy(Rows[i].gameObject);
            }
        }
        GameObject.Destroy(Self);
    }

    public override byte[] GetBytes()
    {
        throw new NotImplementedException();
    }

    public override void ParseBytes(byte[] Data)
    {
        throw new NotImplementedException();
    }
}
