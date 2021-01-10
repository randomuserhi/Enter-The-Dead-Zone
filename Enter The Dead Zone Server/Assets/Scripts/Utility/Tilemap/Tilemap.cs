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
    public int Blank;
    public int Render;

    public Tile(int Blank = 0, int TileIndex = 0, int NumFrames = 1, int AnimationFrame = 0, int Render = 1)
    {
        this.TileIndex = TileIndex;
        this.NumFrames = NumFrames;
        this.AnimationFrame = AnimationFrame;
        this.Blank = Blank;
        this.Render = Render;
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

    public Tile[] FloorMap;
    public Tile[] WallMap; 
    // Map format is height followed by width such that (0, 0) tile is the top left of the tilemap
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
    private int WallTileHeight = 32;
    private Vector2Int TilemapSize;
    private Vector2 TilemapWorldSize;

    private const float ColliderMargin = 0.02f;
    private CompositeCollider2D CompositeCollider;
    private BoxCollider2D[] ColliderMap;
    private List<BoxCollider2D> ColliderList;
    private Rigidbody2D RB;

    public Tilemap(int TileDimension, int WallTileHeight, Vector2Int TilemapSize, float TilesPerUnit = 1)
    {
        this.TileDimension = TileDimension;
        this.WallTileHeight = WallTileHeight;
        this.TilemapSize = TilemapSize;
        this.TilesPerUnit = TilesPerUnit;
        FloorMap = new Tile[TilemapSize.x * TilemapSize.y];
        WallMap = new Tile[TilemapSize.x * TilemapSize.y];
        Initialize();
    }
    public Tilemap(ulong ID, int TileDimension, int WallTileHeight, Vector2Int TilemapSize, float TilesPerUnit = 1) : base(ID)
    {
        this.TileDimension = TileDimension;
        this.WallTileHeight = WallTileHeight;
        this.TilemapSize = TilemapSize;
        this.TilesPerUnit = TilesPerUnit;
        FloorMap = new Tile[TilemapSize.x * TilemapSize.y];
        WallMap = new Tile[TilemapSize.x * TilemapSize.y];
        Initialize();
    }
    public Tilemap(int TileDimension, int WallTileHeight, Vector2Int TilemapSize, Tile[] FloorMap, Tile[] WallMap, float TilesPerUnit = 1)
    {
        this.TileDimension = TileDimension;
        this.WallTileHeight = WallTileHeight;
        this.TilemapSize = TilemapSize;
        this.TilesPerUnit = TilesPerUnit;
        this.FloorMap = FloorMap;
        this.WallMap = WallMap;
        Initialize();
    }
    public Tilemap(ulong ID, int TileDimension, int WallTileHeight, Vector2Int TilemapSize, Tile[] FloorMap, Tile[] WallMap, float TilesPerUnit = 1) : base(ID)
    {
        this.TileDimension = TileDimension;
        this.WallTileHeight = WallTileHeight;
        this.TilemapSize = TilemapSize;
        this.TilesPerUnit = TilesPerUnit;
        this.FloorMap = FloorMap;
        this.WallMap = WallMap;
        Initialize();
    }

    public void Resize(Vector2Int NewTilemapSize, Tile[] FloorMap, Tile[] WallMap)
    {
        TilemapSize = NewTilemapSize;
        this.FloorMap = FloorMap;
        this.WallMap = WallMap;
        GenerateColliders();
        if (DZSettings.ActiveRenderers)
            GenerateRenders();
    }

    public void ReleaseUnusedResources()
    {
        ColliderList.RemoveAll(I =>
        {
            if (!I.enabled)
            {
                GameObject.Destroy(I);
                return true;
            }
            return false;
        });
        if (Rows.Length > TilemapSize.y)
        {
            for (int i = TilemapSize.y; i < Rows.Length; i++)
            {
                GameObject.Destroy(Rows[i].gameObject);
            }
            RawImage[] Temp = Rows;
            Rows = new RawImage[TilemapSize.y];
            Array.Copy(Temp, 0, Rows, 0, TilemapSize.y);
        }
    }

    private void Initialize()
    {
        TilemapWorldSize = (Vector2)TilemapSize / TilesPerUnit;

        ColliderMap = new BoxCollider2D[TilemapSize.x * TilemapSize.y];
        ColliderList = new List<BoxCollider2D>();

        //Initialize GameObject
        Self = new GameObject();
        Self.name = "Tilemap";

        //Initialize Collider objects
        RB = Self.AddComponent<Rigidbody2D>();
        RB.isKinematic = true;

        CompositeCollider = Self.AddComponent<CompositeCollider2D>();
        CompositeCollider.generationType = CompositeCollider2D.GenerationType.Manual;
    }

    public void InitializeRenderer() { }

    public override void OnCreate()
    {
        GenerateColliders();

        //Initialize renders
        GenerateRenders();
    }

    public void Update()
    {

    }

    public void BodyPhysicsUpdate() { }

    private Vector3 PrevTilePosition;
    private void UpdateRenderSortingLayers()
    {
        if (Rows == null) return;
        if (PrevTilePosition != Self.transform.position) //Avoid updating all rows constantly has tilemaps can get quite large
        {
            PrevTilePosition = Self.transform.position;
            for (int i = 0; i < TilemapSize.y; i++)
            {
                float StrideHeight = (WallTileHeight / TileDimension) / TilesPerUnit;
                float BaseY = Rows[i].transform.position.y - ((1 / TilesPerUnit - StrideHeight) / 2);
                Rows[i].canvas.sortingOrder = Mathf.RoundToInt(-BaseY) * 2 + 1;
            }
        }
    }

    public void Render()
    {
        UpdateRenderSortingLayers();

        if (WallBuffer == null || FloorBuffer == null || WallCompute == null || FloorCompute == null)
            return;
        WallBuffer.SetData(WallMap);
        WallCompute.Dispatch(ComputeKernel, TilemapSize.x / 4 + 1, TilemapSize.y / 4 + 1, 1);
        FloorBuffer.SetData(FloorMap);
        FloorCompute.Dispatch(ComputeKernel, TilemapSize.x / 4 + 1, TilemapSize.y / 4 + 1, 1);
    }

    public void SetTilePallet()
    {
        WallCompute.SetTexture(ComputeKernel, "TilePallet", WallTilePalletData.Pallet);
        WallCompute.SetInt("TileStride", WallTilePalletData.TileStride);
        WallCompute.SetInt("TilePalletCount", WallTilePalletData.NumTiles);
        FloorCompute.SetTexture(ComputeKernel, "TilePallet", FloorTilePalletData.Pallet);
        FloorCompute.SetInt("TileStride", FloorTilePalletData.TileStride);
        FloorCompute.SetInt("TilePalletCount", FloorTilePalletData.NumTiles);
    }

    public virtual void GenerateColliders(bool Truncate = false)
    {
        if (ColliderMap.Length < TilemapSize.x * TilemapSize.y)
            ColliderMap = new BoxCollider2D[TilemapSize.x * TilemapSize.y];

        int ReuseIndex = 0;
        int FinalReuseIndex = 0;
        int NumCurrentColliders = ColliderList.Count;
        for (int i = 0; i < TilemapSize.y; i++)
        {
            for (int j = 0; j < TilemapSize.x; j++)
            {
                int Index = i * TilemapSize.x + j;
                if (WallMap[Index].Blank == 0)
                {
                    BoxCollider2D Collider = null;
                    if (FinalReuseIndex == 0 && ReuseIndex < NumCurrentColliders)
                    {
                        Collider = ColliderList[ReuseIndex];
                        ReuseIndex++;
                    }
                    else
                    {
                        FinalReuseIndex = ReuseIndex;
                        Collider = Self.AddComponent<BoxCollider2D>();
                        ColliderList.Add(Collider);
                    }

                    Collider.enabled = true;
                    float Dimension = 1 / TilesPerUnit;
                    Collider.size = new Vector2(Dimension - ColliderMargin, Dimension - ColliderMargin);
                    Collider.offset = new Vector2(j / TilesPerUnit + Dimension / 2 - TilemapSize.x / 2f / TilesPerUnit,
                                                  -i / TilesPerUnit - Dimension / 2 + TilemapSize.y / 2f / TilesPerUnit);
                    Collider.usedByComposite = true;

                    ColliderMap[Index] = Collider;
                }
            }
        }

        if (Truncate)
        {
            for (; ReuseIndex < NumCurrentColliders; ReuseIndex++)
            {
                GameObject.Destroy(ColliderList[ReuseIndex]);
            }
            ColliderList.RemoveRange(FinalReuseIndex, NumCurrentColliders - FinalReuseIndex);
        }
        else
            for (; ReuseIndex < NumCurrentColliders; ReuseIndex++)
            {
                ColliderList[ReuseIndex].enabled = false;
            }

        CompositeCollider.GenerateGeometry();
    }

    private void GenerateRenders(bool Truncate = false)
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

        //Set Tile Dimensions
        WallCompute.SetInt("TileWidth", TileDimension);
        WallCompute.SetInt("TileHeight", WallTileHeight);
        FloorCompute.SetInt("TileWidth", TileDimension);
        FloorCompute.SetInt("TileHeight", TileDimension);
        //Set Map Dimensions
        WallCompute.SetInt("MapWidth", TilemapSize.x);
        WallCompute.SetInt("MapHeight", TilemapSize.y);
        FloorCompute.SetInt("MapWidth", TilemapSize.x);
        FloorCompute.SetInt("MapHeight", TilemapSize.y);

        if (WallTilePalletData == null || FloorTilePalletData == null)
        {
            Debug.LogWarning("WallTilePalletData or FloorTilePalletData is null, rendering default pallets...");
            GenerateDefaultTileData();
        }

        //Set TilePallet
        SetTilePallet();

        //Set the buffers
        if (WallBuffer != null)
            WallBuffer.Dispose();
        WallBuffer = new ComputeBuffer(WallMap.Length, sizeof(int) * 5);
        if (FloorBuffer != null)
            FloorBuffer.Dispose();
        FloorBuffer = new ComputeBuffer(FloorMap.Length, sizeof(int) * 5);

        WallCompute.SetBuffer(ComputeKernel, "Map", WallBuffer);
        FloorCompute.SetBuffer(ComputeKernel, "Map", FloorBuffer);

        if (WallRenderTexture == null)
        {
            WallRenderTexture = new RenderTexture(TileDimension * TilemapSize.x, WallTileHeight * TilemapSize.y, 8)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Point,
                anisoLevel = 1
            };
            WallRenderTexture.Create();
            WallCompute.SetTexture(ComputeKernel, "Result", WallRenderTexture);

        }
        else if (Truncate || WallRenderTexture.width < TilemapSize.x || WallRenderTexture.height < TilemapSize.y)
        {
            WallRenderTexture.Release();
            WallRenderTexture.width = TilemapSize.x;
            WallRenderTexture.height = TilemapSize.y;
            WallRenderTexture.Create();
        }

        if (FloorRenderTexture == null)
        {
            FloorRenderTexture = new RenderTexture(TileDimension * TilemapSize.x, TileDimension * TilemapSize.y, 8)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Point,
                anisoLevel = 1
            };
            FloorRenderTexture.Create();
            FloorCompute.SetTexture(ComputeKernel, "Result", FloorRenderTexture);
        }
        else if (Truncate || FloorRenderTexture.width < TilemapSize.x || FloorRenderTexture.height < TilemapSize.y)
        {
            FloorRenderTexture.Release();
            FloorRenderTexture.width = TilemapSize.x;
            FloorRenderTexture.height = TilemapSize.y;
            FloorRenderTexture.Create();
        }

        if (FloorRender == null)
        {
            Canvas FloorCanvas = Self.AddComponent<Canvas>();
            FloorCanvas.renderMode = RenderMode.WorldSpace;
            FloorCanvas.sortingLayerName = "Floor";
            Self.AddComponent<CanvasScaler>();
            Self.AddComponent<GraphicRaycaster>();
            Self.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            FloorRender = Self.AddComponent<RawImage>();
        }
        FloorRender.rectTransform.sizeDelta = new Vector2((float)TilemapSize.x / TilesPerUnit, (float)TilemapSize.y / TilesPerUnit);
        FloorRender.material = Resources.Load<Material>("Materials/LitMaterial");
        FloorRender.texture = FloorRenderTexture;
        FloorRender.uvRect = new Rect(0, 0, (float)TileDimension * TilemapSize.x / FloorRenderTexture.width, (float)TileDimension * TilemapSize.y / FloorRenderTexture.height);

        //Initialize row gameobjects
        int CurrentLength = 0;
        if (Rows == null)
            Rows = new RawImage[TilemapSize.y];
        else
            CurrentLength = Rows.Length > TilemapSize.y ? TilemapSize.y : Rows.Length;
        if (Rows.Length < TilemapSize.y)
        {
            RawImage[] Temp = Rows;
            Rows = new RawImage[TilemapSize.y];
            Array.Copy(Temp, 0, Rows, 0, Temp.Length);
        }
        int i = 0;
        for (; i < CurrentLength; i++)
        {
            Rows[i].rectTransform.sizeDelta = Vector2.zero;
            float StrideHeight = ((float)WallTileHeight / TileDimension) / TilesPerUnit;
            Rows[i].rectTransform.position = new Vector3(0, -TilemapSize.y / 2f / TilesPerUnit + StrideHeight / 2 + i / TilesPerUnit);
            Rows[i].rectTransform.sizeDelta = new Vector2(TilemapSize.x / TilesPerUnit, StrideHeight);
            Rows[i].material = Resources.Load<Material>("Materials/LitMaterial");
            Rows[i].texture = WallRenderTexture;
            float RenderHeight = (float)WallTileHeight * TilemapSize.y / WallRenderTexture.height;
            Rows[i].uvRect = new Rect(0, i * RenderHeight / TilemapSize.y, (float)TileDimension * TilemapSize.x / WallRenderTexture.width, RenderHeight / TilemapSize.y);
        }
        for (; i < TilemapSize.y; i++)
        {
            GameObject Row = new GameObject();
            Row.transform.parent = Self.transform;
            Canvas RowCanvas = Row.AddComponent<Canvas>();
            RowCanvas.renderMode = RenderMode.WorldSpace;
            RowCanvas.overrideSorting = true;
            RowCanvas.sortingOrder = 0;
            RectTransform RT = Row.GetComponent<RectTransform>();
            RawImage RowImage = Row.AddComponent<RawImage>();
            RowImage.rectTransform.sizeDelta = Vector2.zero;
            float StrideHeight = ((float)WallTileHeight / TileDimension) / TilesPerUnit;
            RowImage.rectTransform.position = new Vector3(0, -TilemapSize.y / 2f + StrideHeight / 2 + i / TilesPerUnit);
            RowImage.rectTransform.sizeDelta = new Vector2(TilemapSize.x / TilesPerUnit, StrideHeight);
            RowImage.material = Resources.Load<Material>("Materials/LitMaterial");
            RowImage.texture = WallRenderTexture;
            float RenderHeight = (float)WallTileHeight * TilemapSize.y / WallRenderTexture.height;
            RowImage.uvRect = new Rect(0, i * RenderHeight / TilemapSize.y, (float)TileDimension * TilemapSize.x / WallRenderTexture.width, RenderHeight / TilemapSize.y);
            Rows[i] = RowImage;
        }
        for (; i < Rows.Length; i++)
        {
            if (Truncate)
                GameObject.Destroy(Rows[i].gameObject);
            else
                Rows[i].gameObject.SetActive(false);
        }
        if (Truncate)
        {
            RawImage[] Temp = Rows;
            Rows = new RawImage[TilemapSize.y];
            Array.Copy(Temp, 0, Rows, 0, TilemapSize.y);
        }
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

    public override void ParseBytes(Network.Packet Data, ulong ServerTick)
    {
        throw new NotImplementedException();
    }
}
