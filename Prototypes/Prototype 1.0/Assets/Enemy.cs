using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Grid G;
    public float Speed = 3;
    public int HP = 12;

    // Start is called before the first frame update
    void Start()
    {
        gameObject.name = "Enemy";
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        RB = gameObject.AddComponent<Rigidbody2D>();
        RB.sharedMaterial = Resources.Load<PhysicsMaterial2D>("Smooth");
        RB.gravityScale = 0;
        RB.mass = 3;
        RB.freezeRotation = true;
        gameObject.AddComponent<BoxCollider2D>();
        SR = gameObject.AddComponent<SpriteRenderer>();
        SR.sprite = Resources.Load<Sprite>("Circle");
        SR.color = Color.green;
        transform.localScale = new Vector2(0.4f, 0.4f);
        Target = G.Spawn;
        gameObject.tag = "Enemy";
    }

    Rigidbody2D RB;
    SpriteRenderer SR;

    public enum Dir
    {
        Top,
        Bottom,
        Left,
        Right
    }

    Dir Check;
    Dictionary<Cell, int> Visited = new Dictionary<Cell, int>();
    Cell Target;
    // Update is called once per frame
    void FixedUpdate()
    {
        Cell CurrCell = G.ConvertWorldPosToGrid(transform.position);
        if (((Check == Dir.Right || Check == Dir.Left) && (Target.transform.position.x - 0.05f < transform.position.x) && (Target.transform.position.x + 0.05f > transform.position.x)) ||
            ((Check == Dir.Top || Check == Dir.Bottom) && (Target.transform.position.y - 0.05f < transform.position.y) && (Target.transform.position.y + 0.05f > transform.position.y)))
        {
            if (CurrCell.Type == Cell.CellType.exit)
            {
                Destroy(gameObject);
                Loader.P.HP--;
            }
            if (!Visited.ContainsKey(CurrCell))
                Visited.Add(CurrCell, 1);
            Cell Top = null;
            Cell Bottom = null;
            Cell Left = null;
            Cell Right = null;
            if (CurrCell.Y > 0)
            {
                Top = G.Cells[CurrCell.X][CurrCell.Y - 1];
                Check = Dir.Top;
            }
            if (CurrCell.Y < G.Cells.Length - 1)
            {
                Bottom = G.Cells[CurrCell.X][CurrCell.Y + 1];
                Check = Dir.Bottom;
            }
            if (CurrCell.X > 0)
            {
                Left = G.Cells[CurrCell.X - 1][CurrCell.Y];
                Check = Dir.Left;
            }
            if (CurrCell.X < G.Cells[0].Length - 1)
            {
                Right = G.Cells[CurrCell.X + 1][CurrCell.Y];
                Check = Dir.Right;
            }

            if (Top != null && (Top.Type == Cell.CellType.path || Top.Type == Cell.CellType.exit) && !Visited.ContainsKey(Top))
            {
                Target = Top;
            }
            if (Bottom != null && (Bottom.Type == Cell.CellType.path || Bottom.Type == Cell.CellType.exit) && !Visited.ContainsKey(Bottom))
            {
                Target = Bottom;
            }
            if (Left != null && (Left.Type == Cell.CellType.path || Left.Type == Cell.CellType.exit) && !Visited.ContainsKey(Left))
            {
                Target = Left;
            }
            if (Right != null && (Right.Type == Cell.CellType.path || Right.Type == Cell.CellType.exit) && !Visited.ContainsKey(Right))
            {
                Target = Right;
            }
        }

        if (Target != null)
        {
            Vector2 Dir = (Target.transform.position - transform.position);
            RB.velocity += Dir.normalized * Speed;
            RB.velocity *= 0.3f;
        }
    }

    public void Die(int Damage)
    {
        HP -= Damage;
        if (HP > 0)
        {
            return;
        }
        for (int i = 0; i < Random.Range(1, 3); i++)
        {
            GameObject A = new GameObject();
            A.AddComponent<Money>();
            A.transform.position = transform.position;
        }
        for (int i = 0; i < 1; i++)
        {
            GameObject A = new GameObject();
            Money M = A.AddComponent<Money>();
            M.HPValue = 1;
            A.transform.position = transform.position;
        }
        GameObject.Destroy(gameObject);
    }
}
