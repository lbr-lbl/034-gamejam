using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : Entity
{
    public BlockDeadState  deadState;

    public ShapeType Shape => (ShapeType)spriteCount;

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
        base.Update();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Entity entity = collision.gameObject.GetComponent<Entity>();

        if (entity.gameObject.layer == LayerMask.NameToLayer("Ground") && entity != null)
        {

            bool shouldEliminateOther = (this.spriteCount == 2 && entity.spriteCount == 0) || (this.spriteCount == 1 && entity.spriteCount == 2) || (this.spriteCount == 0 && entity.spriteCount == 1);

            if (shouldEliminateOther)
            {

                gameObject.layer = LayerMask.NameToLayer("Background");

                sr.enabled = false;

                ps.gameObject.SetActive(true);

                BlockManager.instance.ReturnBlock(gameObject, Shape);
            }
        }
    }
}
