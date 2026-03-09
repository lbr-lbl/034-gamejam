using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : Entity
{
    public BlockDeadState  deadState;

    protected override void Awake()
    {
        base.Awake();

        deadState = new BlockDeadState(this, stateMachine, "Dead");
    }


    protected override void Start()
    {
        base.Start();

    }

    protected override void Update()
    {

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Entity entity = collision.gameObject.GetComponent<Entity>();
        Player player = collision.gameObject.GetComponent<Player>();

        if (entity.tag != this.tag && entity != null)  
        {
            Collider2D collider = collision.collider;
            bool shouldEliminateOther = (this.spriteCount == 0 && entity.spriteCount == 2) || (this.spriteCount == 2 && entity.spriteCount == 1) || (this.spriteCount == 1 && entity.spriteCount == 0);

            if (shouldEliminateOther)
            {
                player = entity as Player;
                if (player != null)
                {
                    player.stateMachine.ChangeState(player.deadState);
                }
                else
                {
                    entity.gameObject.layer = LayerMask.NameToLayer("Background");

                    entity.sr.enabled = false;

                    entity.ps.gameObject.SetActive(true);

                    Destroy(entity.gameObject, 5f);
                }
            }
        }

        if (this.gameObject.layer == LayerMask.NameToLayer("Item") && player != null)
        {
            if (this.tag == "Traingle")
            {
                player.traingleCount++;
            }else if (this.tag == "Square")
            {
                player.squareCount++;
            }
            else if (this.tag == "Circle")
            {
                player.circleCount++;
            }
            Destroy(this.gameObject);
        }
    }
}
