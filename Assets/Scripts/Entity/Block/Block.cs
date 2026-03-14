using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

public class Block : Entity
{
    public BlockDeadState  deadState;
    public BlockType blockType; // 新增：由放置者设置

    public bool IsCore { get; set; }
    public PlayerType coreOwner;

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

        if (entity != null && entity.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {

            bool shouldEliminateOther = (this.spriteCount == 2 && entity.spriteCount == 0) || (this.spriteCount == 1 && entity.spriteCount == 2) || (this.spriteCount == 0 && entity.spriteCount == 1);

            if (shouldEliminateOther)
            {
                if (IsCore)
                {
                    GameManager.instance?.OnCoreDestroyed(coreOwner);
                    Destroy(gameObject); // 直接销毁，不回收
                }
                else
                {
                    BlockManager.instance.ReturnBlock(gameObject, Shape, blockType);
                }
            }
        }
    }
}
