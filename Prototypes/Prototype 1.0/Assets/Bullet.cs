using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Vector3 Direction;

    Rigidbody2D RB;
    SpriteRenderer SR;
    // Start is called before the first frame update
    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Bullet");
        RB = gameObject.AddComponent<Rigidbody2D>();
        RB.sharedMaterial = Resources.Load<PhysicsMaterial2D>("Smooth");
        RB.gravityScale = 0;
        gameObject.AddComponent<CircleCollider2D>();
        SR = gameObject.AddComponent<SpriteRenderer>();
        SR.sprite = Resources.Load<Sprite>("Oval");
        SR.color = Color.red;
        transform.localScale = new Vector2(0.15f, 0.15f);

        var angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        RB.velocity = Direction * 1;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Player P = collision.gameObject.GetComponent<Player>();
        Enemy E = collision.gameObject.GetComponent<Enemy>();
        if (P != null)
        {
            Destroy(gameObject);
            P.HP--;
        }
        if (E != null)
        {
            Destroy(gameObject);
            E.Die(4);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (transform.position.magnitude > 20)
            Destroy(gameObject);
    }
}
