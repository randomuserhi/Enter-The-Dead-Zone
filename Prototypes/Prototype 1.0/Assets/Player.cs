using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    float Timer = 0;
    public int HP = 20;
    public int Money = 60;

    Rigidbody2D RB;
    SpriteRenderer SR;
    // Start is called before the first frame update
    void Start()
    {
        gameObject.name = "Player";
        gameObject.layer = LayerMask.NameToLayer("Player");
        RB = gameObject.AddComponent<Rigidbody2D>();
        RB.sharedMaterial = Resources.Load<PhysicsMaterial2D>("Smooth");
        RB.gravityScale = 0;
        RB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        gameObject.AddComponent<CircleCollider2D>();
        SR = gameObject.AddComponent<SpriteRenderer>();
        SR.sprite = Resources.Load<Sprite>("Circle");
        SR.color = Color.cyan;
        transform.position = new Vector3(0, 0, -1);
        transform.localScale = new Vector2(0.3f, 0.3f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            Loader.G.PlaceTower(transform.position);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            Loader.G.PlaceTowerLaser(transform.position);
        }
    }

    //implement:
    //Dash
    //Enemy variety
    //Laser
    //maybe powerups (enemy drops)
    //round based hp loss (similar to btd gold)
    //tower variety
    //Remoavl of towers
    //Upgrades => increasing damage ? maybe bullet size increase (balacne for player)
    //No infinite scaling

    float BoostTimer = 0.5f;
    float MaxBoostTimer = 0.5f;

    public float MaxSlowTime = 5;
    public float SlowTime = 5;
    float RampTime = 3;
    float MinTime = 0.1f;
    public bool SlowActive = false;

    // Update is called once per frame
    void FixedUpdate()
    {
        if (SlowTime > MaxSlowTime && !SlowActive)
        {
            SlowTime = MaxSlowTime;
        }

        //Debug.Log(Loader.G.ConvertWorldPosToGrid(transform.position).Type);

        float Speed = Input.GetKey(KeyCode.LeftShift) ? 3 : 7;

        /*if (BoostTimer >= MaxBoostTimer && Input.GetAxis("Shift") != 0)
        {
            BoostTimer = -0.03f;
            Speed = 10;
        }*/

        RB.velocity += new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized * Speed / Time.timeScale;
        if (BoostTimer >= 0)
        {
            RB.velocity *= 0.3f;
        }
        /*float Speed = 3;
        transform.position += new Vector3(Input.GetAxis("Horizontal") * Speed, Input.GetAxis("Vertical") * Speed) * Time.deltaTime;*/

        if (Input.GetAxis("Jump") != 0 && !SlowActive && (SlowTime > 0))
        {
            SlowTime += RampTime;
            SlowActive = true;
         
            Time.fixedDeltaTime = 0.02f * MinTime;
            RB.velocity = RB.velocity / MinTime;
        }

        if (SlowActive == true)
        {
            if (SlowTime <= 0)
                SlowActive = false;
            else
            {
                SlowTime -= Time.deltaTime / Time.timeScale;

                if (SlowTime > RampTime)
                {
                    Time.timeScale = MinTime;
                }
                else
                {
                    Time.timeScale += ((1 - MinTime) / RampTime) * (Time.deltaTime / Time.timeScale);
                }

                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }
        }
        else
        {
            Time.timeScale = 1;
        }

        Timer -= Time.deltaTime;
        if (Timer <= 0)
        {
            HP -= 1;
            Timer = 3;
        }

        if (BoostTimer < MaxBoostTimer)
        {
            BoostTimer += Time.deltaTime;
        }
    }
}
