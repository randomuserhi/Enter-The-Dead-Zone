using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Grid
{
    public GameObject G;
    public Cell[][] Cells;
    float Spacing;
    public Cell Spawn;
    public Cell Exit;
    public Grid(string Layout, float Spacing)
    {
        this.Spacing = Spacing;
        G = new GameObject();
        string[] Rows = Layout.Split(',');
        Cells = new Cell[Rows.Length][];
        for (int i = 0; i < Rows.Length; i++)
        {
            Cells[i] = new Cell[Rows[i].Length];
            for (int j = 0; j < Rows[i].Length; j++)
            {
                GameObject C = new GameObject();
                Cells[i][j] = C.AddComponent<Cell>();
                Cells[i][j].X = i;
                Cells[i][j].Y = j;
                switch (Rows[i][j])
                {
                    case '0': Cells[i][j].Type = Cell.CellType.space; break;
                    case '1': Cells[i][j].Type = Cell.CellType.wall; break;
                    case '2': Cells[i][j].Type = Cell.CellType.path; break;
                    case '3': Cells[i][j].Type = Cell.CellType.entry; Spawn = Cells[i][j]; break;
                    case '4': Cells[i][j].Type = Cell.CellType.exit; Exit = Cells[i][j]; break;
                }
                C.transform.parent = G.transform;
                C.transform.position = new Vector3(j * Spacing, -i * Spacing, 0);
            }
        }

        G.transform.position = new Vector3(-Rows[0].Length * Spacing / 2, Rows.Length * Spacing / 2, 0);
    }

    public Enemy SpawnEnemy(int HP)
    {
        GameObject E = new GameObject();
        Enemy A = E.AddComponent<Enemy>();
        A.HP = HP;
        A.G = this;
        E.transform.position = new Vector3(Spawn.transform.position.x, Spawn.transform.position.y, -1);

        return A;
    }

    public Cell ConvertWorldPosToGrid(Vector3 Position)
    {
        Vector3 LocalPosition = G.transform.position - Position - new Vector3(0.5f, -0.5f);
        int X = (int)(Mathf.Abs(LocalPosition.x / Spacing));
        int Y = (int)(Mathf.Abs(LocalPosition.y / Spacing));
        if (X > Cells[0].Length) X = Cells[0].Length - 1;
        if (X < 0) X = 0;
        if (Y > Cells.Length) Y = Cells.Length - 1;
        if (Y < 0) Y = 0;
        return Cells[Y][X];
    }

    public void PlaceTower(Vector3 Position)
    {
        if (Loader.P.Money >= 20)
        {
            Collider2D C = Physics2D.OverlapCircle(Position, 0.2f, LayerMask.GetMask("Turets", "Terrain", "Default", "Path"));
            if (C == null)
            {
                GameObject E = new GameObject();
                Tower A = E.AddComponent<Tower>();
                E.transform.position = Position;
                Loader.P.transform.position += new Vector3(0.1f, 0.2f);
                Loader.P.Money -= 20;
            }
        }
    }
    public void PlaceTowerLaser(Vector3 Position)
    {
        if (Loader.P.Money >= 60)
        {
            Collider2D C = Physics2D.OverlapCircle(Position, 0.2f, LayerMask.GetMask("Turets", "Terrain", "Default", "Path"));
            if (C == null)
            {
                GameObject E = new GameObject();
                TowerLaser A = E.AddComponent<TowerLaser>();
                E.transform.position = Position;
                Loader.P.transform.position += new Vector3(0.1f, 0.2f);
                Loader.P.Money -= 60;
            }
        }
    }
}

public class Loader : MonoBehaviour
{
    public static Grid G;
    public static Player P;

    // Start is called before the first frame update
    void Start()
    {
        string Layout =
            "11111111111111," +
            "10000000000001," +
            "10000000000001," +
            "10022222220001," +
            "10020000020001," +
            "10020000220001," +
            "10022200200001," +
            "10000200222001," +
            "10000200002001," +
            "10000202222001," +
            "13222202000001," +
            "10000002000001," +
            "10000004000001," +
            "11111111111111";
        G = new Grid(Layout, 1);
        GameObject PObj = new GameObject();
        P = PObj.AddComponent<Player>();
    }

    float Delay = 10;

    float WaveTimer;
    float WaveSpacing = 0.3f;
    float WaveSpacingMax = 1;
    int WaveSize = 10;
    int WaveMaxSize = 10;
    int WaveHealth = 12;
    int WaveHealthUpdate = 3;
    int Wave = 0;

    public GameObject TextBox;

    // Update is called once per frame
    void FixedUpdate()
    {
        TextBox.GetComponent<Text>().text = "HP: " + P.HP + ", Money: " + P.Money + ", Wave: " + Wave + ", " + P.SlowTime + "/" + P.MaxSlowTime;
        if (P.HP <= 0)
        {
            throw new KeyNotFoundException();
        }
        if (Delay > 0)
        {
            Delay -= Time.deltaTime;
            return;
        }
        WaveTimer += Time.deltaTime;
        if (WaveTimer > WaveSpacing && WaveSize > 0)
        {
            WaveSize--;
            WaveTimer = 0;
            G.SpawnEnemy(WaveHealth);
        }

        if (WaveSize == 0 && GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            Wave++;
            WaveSize = Random.Range(10, WaveMaxSize);
            WaveMaxSize += 3;
            WaveHealthUpdate--;
            if (WaveHealthUpdate == 0)
            {
                WaveHealth += 12;
                WaveHealthUpdate = 3;
            }

            WaveSpacing = Random.Range(0.1f, WaveSpacingMax);
            WaveSpacingMax += Random.Range(-0.5f, 0.5f);
            if (Random.Range(0f, 1f) < 0.3)
            {
                WaveSpacing = 0.3f;
            }

            if (WaveSpacing < 0.3)
                WaveSpacing = 0.3f;

            if (P.HP < 10)
            {
                P.HP++;
            }
        }
    }
}
