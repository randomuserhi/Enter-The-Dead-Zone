using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public Vector3 Direction;
    public float RotationalSpeed = 0;

    public Dictionary<GameObject,float> DamageTick = new Dictionary<GameObject, float>();
    public float MaxDamageTick = 0.2f;

    public float LifeTime = 0.5f;

    public float PreTime = 2;
    public float FlickerTimer = 0.2f;
    public float MaxFlickerTimer = 0.2f;

    GameObject Child;
    Rigidbody2D RB;
    SpriteRenderer SR;
    // Start is called before the first frame update
    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Bullet");
        RB = gameObject.AddComponent<Rigidbody2D>();
        RB.sharedMaterial = Resources.Load<PhysicsMaterial2D>("Smooth");
        RB.gravityScale = 0;
        Child = new GameObject();
        Child.layer = LayerMask.NameToLayer("Bullet");
        Child.transform.parent = transform;
        Child.transform.localPosition = new Vector3(0.5f, 0);
        //Child.AddComponent<BoxCollider2D>().isTrigger = true;
        SR = Child.AddComponent<SpriteRenderer>();
        SR.sprite = Resources.Load<Sprite>("Oval");
        SR.color = Color.gray;
        transform.localScale = new Vector2(20, 0.2f);

        var angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        Player P = collision.gameObject.GetComponent<Player>();
        Enemy E = collision.gameObject.GetComponent<Enemy>();
        if (!DamageTick.ContainsKey(collision.gameObject))
            DamageTick.Add(collision.gameObject, 0);
        else
            DamageTick[collision.gameObject] -= Time.deltaTime;
        if (P != null)
        {
            if (DamageTick[collision.gameObject] <= 0)
            {
                DamageTick[collision.gameObject] = MaxDamageTick;
                P.HP--;
            }
        }
        if (E != null)
        {
            if (DamageTick[collision.gameObject] <= 0)
            {
                DamageTick[collision.gameObject] = MaxDamageTick;
                E.Die(3);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (DamageTick.ContainsKey(collision.gameObject))
            DamageTick.Remove(collision.gameObject);
    }

    bool Done = true;
    // Update is called once per frame
    void FixedUpdate()
    {
        if (PreTime <= 0)
        {
            if (Done == true)
            {
                Done = false;
                SR.color = Color.red;
                SR.enabled = true;
                Child.AddComponent<BoxCollider2D>().isTrigger = true;
                transform.localScale = new Vector2(20, 0.5f);
            }

            Quaternion Rot = transform.rotation;
            Rot.eulerAngles += new Vector3(0, 0, RotationalSpeed * Time.deltaTime);
            transform.rotation = Rot;
            LifeTime -= Time.deltaTime;
            if (LifeTime < 0)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            PreTime -= Time.deltaTime;
            if (FlickerTimer <= 0)
            {
                FlickerTimer = MaxFlickerTimer;
                SR.color = Color.gray;
                SR.enabled = !SR.enabled;
                transform.localScale = new Vector2(20, 0.2f);
            }
            else
            {
                FlickerTimer -= Time.deltaTime;
            }
        }
    }
}
