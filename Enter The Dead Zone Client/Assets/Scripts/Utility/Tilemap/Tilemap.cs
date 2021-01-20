﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using DeadZoneEngine;
using DeadZoneEngine.Entities;

/// <summary>
/// Defines a tile of a tilemap
/// </summary>
public struct Tile
{
    public int NumFrames; //Number of animation frames
    public int AnimationFrame; //Animation frame index
    public int TileIndex; //Which tile from tile pallet
    public int Blank; //Is this tile blank?
    public int Render; //Is this tile being rendered?

    public Tile(int Blank = 0, int TileIndex = 0, int NumFrames = 1, int AnimationFrame = 0, int Render = 1)
    {
        this.TileIndex = TileIndex;
        this.NumFrames = NumFrames;
        this.AnimationFrame = AnimationFrame;
        this.Blank = Blank;
        this.Render = Render;
    }
}

/// <summary>
/// Defines a tilepallet for a tilemap
/// </summary>
public class TilePallet
{
    public int NumTiles; //Number of different tiles
    public Texture2D Pallet; //Pallet
    public int TileStride; //Number of pixels between tiles
    public int[] FrameCount; //Number of animation frames for each tile
}

public class Tilemap : AbstractWorldEntity, IUpdatable, IRenderer, IServerSendable
{
    public int ServerObjectType { get; set; } = (int)DZSettings.EntityType.Tilemap;
    public bool RecentlyUpdated { get; set; } = false;

    public int SortingLayer { get; set; }

    //ComputeShaders for GPU for rendering wall and floor tilemaps
    private ComputeShader WallCompute;
    private ComputeShader FloorCompute;
    private int ComputeKernel; //Kernel index of GPU function

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

    //Buffers for wall and floor for passing tilemaps to GPU
    ComputeBuffer WallBuffer;
    ComputeBuffer FloorBuffer;

    //Components for rendering to Unity
    private RawImage FloorRender;
    private RawImage[] Rows;
    private RenderTexture WallRenderTexture;
    private RenderTexture FloorRenderTexture;

    //Tile pallets for wall and floor
    public TilePallet WallTilePalletData;
    public TilePallet FloorTilePalletData;

    private float TilesPerUnit = 1; //Size of tiles in respect to a unity unit
    private int TileDimension = 32; //Pixel width and height of tile
    private int WallTileHeight = 32; //Pixel height of a wall tile
    private Vector2Int TilemapSize; //Size of tilemap
    private Vector2 TilemapWorldSize; //Size of tilemap in unity units

    private const float ColliderMargin = 0.02f; //Margin for colliders (shrinks collider size by this value on each side)
    private CompositeCollider2D CompositeCollider; //Composite collider for optimizing collisions
    private BoxCollider2D[] ColliderMap; //Layout of colliders on tilemap
    private List<BoxCollider2D> ColliderList; //List of colliders in use
    private Rigidbody2D RB; //Rigidbody of tilemap

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
    public Tilemap(ulong ID) : base(ID)
    {
        TileDimension = 32;
        WallTileHeight = 32;
        TilemapSize = Vector2Int.zero;
        TilesPerUnit = 1;
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

    /// <summary>
    /// Resize tilemap to fit a new floor and wall map
    /// </summary>
    public void Resize(Vector2Int NewTilemapSize, Tile[] FloorMap, Tile[] WallMap)
    {
        UpdateResizeOverNetwork = true;
        this.FloorMap = FloorMap;
        this.WallMap = WallMap;
        bool SizeChange = NewTilemapSize != TilemapSize;
        if (SizeChange) TilemapSize = NewTilemapSize;
        if (DZSettings.ActiveRenderers && SizeChange)
            GenerateRenders();
        TilemapSize = NewTilemapSize;
        GenerateColliders();
    }

    /// <summary>
    /// Releases unused memory as tilemap caches textures and objects for reuse
    /// </summary>
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
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// Initializes base tilemap
    /// </summary>
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

    protected override void OnCreate()
    {
        //Initialize colliders
        GenerateColliders();

        //Initialize renders
        GenerateRenders();
    }

    public void Update() { }

    public void BodyPhysicsUpdate() { }

    private Vector3 PrevTilePosition; //Store previous position of tilemap
    private void UpdateRenderSortingLayers() //Updates the render sorting layer of each tile row
    {
        if (Rows == null) return;
        PrevTilePosition = Self.transform.position;
        for (int i = 0; i < TilemapSize.y; i++)
        {
            float StrideHeight = (WallTileHeight / TileDimension) / TilesPerUnit;
            float BaseY = Rows[i].transform.position.y - ((1 / TilesPerUnit - StrideHeight) / 2);
            Rows[i].canvas.sortingOrder = Mathf.RoundToInt(-BaseY) + 1;
        }
    }

    public void Render()
    {
        if (PrevTilePosition != Self.transform.position) //Avoid updating all rows constantly has tilemaps can get quite large
            UpdateRenderSortingLayers(); //Update the sorting layers for each tile row

        //Generate textures to render
        if (WallBuffer == null || FloorBuffer == null || WallCompute == null || FloorCompute == null)
            return;
        WallBuffer.SetData(WallMap);
        WallCompute.Dispatch(ComputeKernel, TilemapSize.x / 4 + 1, TilemapSize.y / 4 + 1, 1);
        FloorBuffer.SetData(FloorMap);
        FloorCompute.Dispatch(ComputeKernel, TilemapSize.x / 4 + 1, TilemapSize.y / 4 + 1, 1);
    }

    /// <summary>
    /// Assigns new tile pallets to tilemap
    /// </summary>
    public void SetTilePallet()
    {
        WallCompute.SetTexture(ComputeKernel, "TilePallet", WallTilePalletData.Pallet);
        WallCompute.SetInt("TileStride", WallTilePalletData.TileStride);
        WallCompute.SetInt("TilePalletCount", WallTilePalletData.NumTiles);
        FloorCompute.SetTexture(ComputeKernel, "TilePallet", FloorTilePalletData.Pallet);
        FloorCompute.SetInt("TileStride", FloorTilePalletData.TileStride);
        FloorCompute.SetInt("TilePalletCount", FloorTilePalletData.NumTiles);
    }

    /// <summary>
    /// Generates new composite colliders for a tilemap
    /// </summary>
    /// <param name="Truncate">If true, disposes of unused colliders</param>
    public virtual void GenerateColliders(bool Truncate = false)
    {
        //Expand collider array if needed
        if (ColliderMap.Length < TilemapSize.x * TilemapSize.y)
            ColliderMap = new BoxCollider2D[TilemapSize.x * TilemapSize.y];

        int ReuseIndex = 0; //Index of colliders to reuse
        int FinalReuseIndex = 0; //Indicates the last collider that was reused
        int NumCurrentColliders = ColliderList.Count; //Store the number of current colliders
        for (int i = 0; i < TilemapSize.y; i++)
        {
            for (int j = 0; j < TilemapSize.x; j++)
            {
                int Index = i * TilemapSize.x + j;
                if (WallMap[Index].Blank == 0)
                {
                    BoxCollider2D Collider = null;
                    if (FinalReuseIndex == 0 && ReuseIndex < NumCurrentColliders) //If there are colliders to reuse, reuse them
                    {
                        Collider = ColliderList[ReuseIndex];
                        ReuseIndex++;
                    }
                    else //Otherwise create new colliders
                    {
                        FinalReuseIndex = ReuseIndex;
                        Collider = Self.AddComponent<BoxCollider2D>();
                        ColliderList.Add(Collider);
                    }

                    //Position the colliders on the tilemap
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

        if (Truncate) //If true, dispose of unused colliders
        {
            for (; ReuseIndex < NumCurrentColliders; ReuseIndex++)
            {
                GameObject.Destroy(ColliderList[ReuseIndex]);
            }
            ColliderList.RemoveRange(FinalReuseIndex, NumCurrentColliders - FinalReuseIndex);
        }
        else //Disable unused colliders but cache to reuse
            for (; ReuseIndex < NumCurrentColliders; ReuseIndex++)
            {
                ColliderList[ReuseIndex].enabled = false;
            }

        CompositeCollider.GenerateGeometry();
    }

    /// <summary>
    /// Generates new renders for rendering a different sized tilemap
    /// </summary>
    /// <param name="Truncate">If true, disposes of unused renders</param>
    private void GenerateRenders(bool Truncate = false)
    {
        //Initialize Buffers
        if (WallBuffer != null) WallBuffer.Dispose();
        if (FloorBuffer != null) FloorBuffer.Dispose();

        //Initialize compute shaders
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

        //Check TilePallets
        if (WallTilePalletData == null || FloorTilePalletData == null)
        {
            Debug.LogWarning("WallTilePalletData or FloorTilePalletData is null, rendering default pallets...");
            GenerateDefaultTileData();
        }

        //Set TilePallets
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

        //Create new textures if required
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
        else if (Truncate || WallRenderTexture.width < TilemapSize.x || WallRenderTexture.height < TilemapSize.y) //Truncate textures if option was true to release more memory
        {
            WallRenderTexture.Release();
            WallRenderTexture.width = TilemapSize.x;
            WallRenderTexture.height = TilemapSize.y;
            WallRenderTexture.Create();
        }

        //Create new textures if required
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
        else if (Truncate || FloorRenderTexture.width < TilemapSize.x || FloorRenderTexture.height < TilemapSize.y) //Truncate textures if option was true to release more memory
        {
            FloorRenderTexture.Release();
            FloorRenderTexture.width = TilemapSize.x;
            FloorRenderTexture.height = TilemapSize.y;
            FloorRenderTexture.Create();
        }

        if (FloorRender == null) //Create a new render for the floor map if needed
        {
            Canvas FloorCanvas = Self.AddComponent<Canvas>();
            FloorCanvas.renderMode = RenderMode.WorldSpace;
            FloorCanvas.sortingLayerName = "Floor";
            Self.AddComponent<CanvasScaler>();
            Self.AddComponent<GraphicRaycaster>();
            Self.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            FloorRender = Self.AddComponent<RawImage>();
        }
        //Scale the floormap render
        FloorRender.rectTransform.sizeDelta = new Vector2((float)TilemapSize.x / TilesPerUnit, (float)TilemapSize.y / TilesPerUnit);
        FloorRender.material = Resources.Load<Material>("Materials/LitMaterial");
        FloorRender.texture = FloorRenderTexture;
        FloorRender.uvRect = new Rect(0, 0, (float)TileDimension * TilemapSize.x / FloorRenderTexture.width, (float)TileDimension * TilemapSize.y / FloorRenderTexture.height);

        //Initialize row gameobjects
        int CurrentLength = 0;
        if (Rows == null)
            Rows = new RawImage[TilemapSize.y];
        else
            CurrentLength = Rows.Length > TilemapSize.y ? TilemapSize.y : Rows.Length; //Get the number of rows to render for a given tilemap
        if (Rows.Length < TilemapSize.y) //Resize array of rows if needed
        {
            RawImage[] Temp = Rows;
            Rows = new RawImage[TilemapSize.y];
            Array.Copy(Temp, 0, Rows, 0, Temp.Length);
        }
        int i = 0;
        for (; i < CurrentLength; i++) //Loop through number of rows that can be reused
        {
            Rows[i].rectTransform.sizeDelta = Vector2.zero;
            float StrideHeight = ((float)WallTileHeight / TileDimension) / TilesPerUnit;
            Rows[i].rectTransform.position = new Vector3(0, -TilemapSize.y / 2f / TilesPerUnit + StrideHeight / 2 + i / TilesPerUnit);
            Rows[i].rectTransform.position += Self.transform.position;
            Rows[i].rectTransform.sizeDelta = new Vector2(TilemapSize.x / TilesPerUnit, StrideHeight);
            Rows[i].material = Resources.Load<Material>("Materials/LitMaterial");
            Rows[i].texture = WallRenderTexture;
            float RenderHeight = (float)WallTileHeight * TilemapSize.y / WallRenderTexture.height;
            Rows[i].uvRect = new Rect(0, i * RenderHeight / TilemapSize.y, (float)TileDimension * TilemapSize.x / WallRenderTexture.width, RenderHeight / TilemapSize.y);
        }
        for (; i < TilemapSize.y; i++) //Loop through remainder of tilemap and create new rows
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
            RowImage.rectTransform.position += Self.transform.position;
            RowImage.rectTransform.sizeDelta = new Vector2(TilemapSize.x / TilesPerUnit, StrideHeight);
            RowImage.material = Resources.Load<Material>("Materials/LitMaterial");
            RowImage.texture = WallRenderTexture;
            float RenderHeight = (float)WallTileHeight * TilemapSize.y / WallRenderTexture.height;
            RowImage.uvRect = new Rect(0, i * RenderHeight / TilemapSize.y, (float)TileDimension * TilemapSize.x / WallRenderTexture.width, RenderHeight / TilemapSize.y);
            Rows[i] = RowImage;
        }
        for (; i < Rows.Length; i++) //Loop through remainding rows and destroy them if truncate option was true, otherwise deactivate and cache to reuse
        {
            if (Truncate)
                GameObject.Destroy(Rows[i].gameObject);
            else
                Rows[i].gameObject.SetActive(false);
        }
        if (Truncate && Rows.Length != TilemapSize.y) //Resize array of rows to size of tilemap if truncate option is true
        {
            RawImage[] Temp = Rows;
            Rows = new RawImage[TilemapSize.y];
            Array.Copy(Temp, 0, Rows, 0, TilemapSize.y);
        }

        UpdateRenderSortingLayers();
    }

    /// <summary>
    /// Generate default tilepallets when none are provided
    /// </summary>
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
        //Delete Objects
        if (Rows != null)
        {
            for (int i = 0; i < Rows.Length; i++)
            {
                GameObject.Destroy(Rows[i].gameObject);
            }
        }
        GameObject.Destroy(Self);
    }

    private List<byte> MapCache = new List<byte>();
    private bool UpdateResizeOverNetwork = false;
    public override byte[] GetBytes()
    {
        if (UpdateResizeOverNetwork || !FirstParse)
        {
            FirstParse = true;
            int Volume = TilemapSize.x * TilemapSize.y;
            MapCache.Clear();
            for (int i = 0; i < Volume; i++)
            {
                Tile T = FloorMap[i];
                MapCache.AddRange(BitConverter.GetBytes(T.Blank));
                if (T.Blank == 0)
                {
                    MapCache.AddRange(BitConverter.GetBytes(T.Render));
                    MapCache.AddRange(BitConverter.GetBytes(T.TileIndex));
                    MapCache.AddRange(BitConverter.GetBytes(T.AnimationFrame));
                }
            }
            for (int i = 0; i < Volume; i++)
            {
                Tile T = WallMap[i];
                MapCache.AddRange(BitConverter.GetBytes(T.Blank));
                if (T.Blank == 0)
                {
                    MapCache.AddRange(BitConverter.GetBytes(T.Render));
                    MapCache.AddRange(BitConverter.GetBytes(T.TileIndex));
                    MapCache.AddRange(BitConverter.GetBytes(T.AnimationFrame));
                }
            }
        }

        List<byte> Data = new List<byte>();
        Data.AddRange(BitConverter.GetBytes(UpdateResizeOverNetwork));
        UpdateResizeOverNetwork = false;
        Data.AddRange(BitConverter.GetBytes(-1)); //Tilemap pallet
        Data.AddRange(BitConverter.GetBytes(Self.transform.position.x)); //Tilemap position
        Data.AddRange(BitConverter.GetBytes(Self.transform.position.y));
        Data.AddRange(BitConverter.GetBytes(TilemapSize.x));
        Data.AddRange(BitConverter.GetBytes(TilemapSize.y));
        Data.AddRange(BitConverter.GetBytes(TilesPerUnit));
        Data.AddRange(BitConverter.GetBytes(WallTileHeight));
        Data.AddRange(BitConverter.GetBytes(MapCache.Count));
        Data.AddRange(MapCache);

        return Data.ToArray();
    }

    private bool FirstParse = false;
    public override void ParseBytes(DZNetwork.Packet Data, ulong ServerTick)
    {
        bool UpdateResizeOverNetwork = Data.ReadBool();
        int TilePalletIndex = Data.ReadInt();
        if (TilePalletIndex == -1)
            GenerateDefaultTileData();

        Self.transform.position = new Vector3(Data.ReadFloat(), Data.ReadFloat());

        Vector2Int TilemapSize = new Vector2Int(Data.ReadInt(), Data.ReadInt());
        TilesPerUnit = Data.ReadFloat();
        WallTileHeight = Data.ReadInt();
        int NumMapBytes = Data.ReadInt();
        if (!UpdateResizeOverNetwork && FirstParse)
        {
            Data.Skip(NumMapBytes);
            return;
        }
        FirstParse = true;

        int Volume = TilemapSize.x * TilemapSize.y;

        Tile[] FloorMap = new Tile[Volume];
        for (int i = 0; i < Volume; i++)
        {
            int Blank = Data.ReadInt();
            FloorMap[i].Blank = Blank;
            if (Blank == 1)
                continue;
            FloorMap[i].Render = Data.ReadInt();
            FloorMap[i].TileIndex = Data.ReadInt();
            FloorMap[i].AnimationFrame = Data.ReadInt();
        }

        Tile[] WallMap = new Tile[Volume];
        for (int i = 0; i < Volume; i++)
        {
            int Blank = Data.ReadInt();
            WallMap[i].Blank = Blank;
            if (Blank == 1)
                continue;
            WallMap[i].Render = Data.ReadInt();
            WallMap[i].TileIndex = Data.ReadInt();
            WallMap[i].AnimationFrame = Data.ReadInt();
        }

        Resize(TilemapSize, FloorMap, WallMap);
    }
}
