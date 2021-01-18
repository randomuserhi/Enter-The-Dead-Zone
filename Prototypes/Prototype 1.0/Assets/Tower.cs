using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public List<Vector3> Dir = new List<Vector3>();

    Rigidbody2D RB;
    SpriteRenderer SR;

    public float Timer;
    public float MaxTimer = 2.5f;

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

            for (int i = 0; i < Dir.Count; i++)
            {
                GameObject B = new GameObject();
                Bullet A = B.AddComponent<Bullet>();
                A.Direction = Dir[i].normalized;
                B.transform.position = transform.position;
            }
        }
    }
}
