using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Money : MonoBehaviour
{
    float DespawnTimer = 20;
    float FlickerTimer = 0.1f;
    bool F = true;
    public int HPValue = 0;

    Rigidbody2D RB;
    SpriteRenderer SR;
    // Start is called before the first frame update
    void Start()
    {
        RB = gameObject.AddComponent<Rigidbody2D>();
        RB.sharedMaterial = Resources.Load<PhysicsMaterial2D>("Smooth");
        RB.gravityScale = 0;
        RB.mass = 3;
        RB.freezeRotation = true;
        SR = gameObject.AddComponent<SpriteRenderer>();
        SR.sprite = Resources.Load<Sprite>("Circle");
        SR.color = HPValue == 0 ? Color.gray : Color.magenta;
        transform.localScale = new Vector2(0.1f, 0.1f);

        RB.velocity = new Vector2(Random.Range(-1, 1), Random.Range(-1, 1)) * 3;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        DespawnTimer -= Time.deltaTime;
        if (DespawnTimer < 3)
        {
            FlickerTimer -= Time.deltaTime;
            if (FlickerTimer <= 0)
            {
                FlickerTimer = 0.2f;
                SR.enabled = F;
                F = !F;
            }
        }
        if (DespawnTimer < 0)
            Destroy(gameObject);
        if ((transform.position - Loader.P.transform.position).magnitude < 1)
        {
            Vector2 Dir = (Loader.P.transform.position - transform.position);
            float Speed = 10;
            RB.velocity += Dir.normalized * Speed / Time.timeScale;
            RB.velocity *= 0.3f;
        }
        else
            RB.velocity *= 0.9f;

        float Bounds = 0.3f;
        if ((Loader.P.transform.position.x - Bounds < transform.position.x) && (Loader.P.transform.position.x + Bounds > transform.position.x) &&
            (Loader.P.transform.position.y - Bounds < transform.position.y) && (Loader.P.transform.position.y + Bounds > transform.position.y))
        {
            Loader.P.HP += HPValue;
            if (!Loader.P.SlowActive)
                Loader.P.SlowTime += 1;
            if (Loader.P.HP > 20)
                Loader.P.HP = 20; //Maybe overheal
            Loader.P.Money++;
            Destroy(gameObject);
        }
    }
}
