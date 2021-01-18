using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerLaser : MonoBehaviour
{
    public List<Vector3> Dir = new List<Vector3>();
    public int Index = 0;

    Rigidbody2D RB;
    SpriteRenderer SR;

    public float Timer = 0;
    public float MaxTimer = 10f;

    // Start is called before the first frame update
    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Turets");
        RB = gameObject.AddComponent<Rigidbody2D>();
        RB.sharedMaterial = Resources.Load<PhysicsMaterial2D>("Smooth");
        RB.gravityScale = 0;
        RB.isKinematic = true;
        gameObject.AddComponent<CircleCollider2D>();
        SR = gameObject.AddComponent<SpriteRenderer>();
        SR.sprite = Resources.Load<Sprite>("Circle");
        SR.color = Color.cyan;
        transform.localScale = new Vector2(0.4f, 0.4f);

        Dir.Add(new Vector3(1, 0));
        Dir.Add(new Vector3(0, 1));
        Dir.Add(new Vector3(-1, 0));
        Dir.Add(new Vector3(0, -1));
        Dir.Add(new Vector3(-1, -1));
        Dir.Add(new Vector3(1, 1));
        Dir.Add(new Vector3(1, -1));
        Dir.Add(new Vector3(-1, 1));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Timer += Time.deltaTime;
        if (Timer > MaxTimer)
        {
            Timer = 0;

            for (int i = 0; i < 4; i++)
            {
                Index = Index % Dir.Count;
                GameObject B = new GameObject();
                Laser A = B.AddComponent<Laser>();
                A.LifeTime = 3;
                A.PreTime = 1.4f;
                A.MaxFlickerTimer = 0.3f;
                A.RotationalSpeed = -45 / A.LifeTime;
                A.MaxDamageTick = 0.2f;
                A.Direction = Dir[Index].normalized;
                B.transform.position = transform.position;
                Index++;
            }
        }
    }
}
