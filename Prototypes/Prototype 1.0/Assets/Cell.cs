using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public int X;
    public int Y;

    public enum CellType
    {
        entry,
        exit,
        path,
        wall,
        space
    }

    public CellType Type;

    // Start is called before the first frame update
    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Terrain");
        SpriteRenderer SR = gameObject.AddComponent<SpriteRenderer>();
        SR.sprite = Resources.Load<Sprite>("Square");
        switch (Type)
        {
            case CellType.entry: SR.color = Color.yellow;
                {
                    gameObject.layer = LayerMask.NameToLayer("Path");
                    BoxCollider2D B = gameObject.AddComponent<BoxCollider2D>();
                    B.size = new Vector2(1.05f, 1.05f);
                    B.sharedMaterial = Resources.Load<PhysicsMaterial2D>("Smooth");
                }
                break;
            case CellType.exit: SR.color = Color.yellow;
                {
                    gameObject.layer = LayerMask.NameToLayer("Path");
                    BoxCollider2D B = gameObject.AddComponent<BoxCollider2D>();
                    B.size = new Vector2(1.05f, 1.05f);
                    B.sharedMaterial = Resources.Load<PhysicsMaterial2D>("Smooth");
                }
                break;
            case CellType.path: SR.color = Color.yellow;
                {
                    gameObject.layer = LayerMask.NameToLayer("Path");
                    BoxCollider2D B = gameObject.AddComponent<BoxCollider2D>();
                    B.size = new Vector2(1.05f, 1.05f);
                    B.sharedMaterial = Resources.Load<PhysicsMaterial2D>("Smooth");
                }
                break;
            case CellType.wall: SR.color = Color.black;
                {
                    BoxCollider2D B = gameObject.AddComponent<BoxCollider2D>();
                    B.size = new Vector2(1.05f, 1.05f);
                    B.sharedMaterial = Resources.Load<PhysicsMaterial2D>("Smooth");
                }
                break;
            case CellType.space: SR.color = Color.white;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
