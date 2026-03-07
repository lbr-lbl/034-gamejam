using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockDeadState : EntityState
{
    public Block block;

    public BlockDeadState(Entity entity, EntityStateMachine stateMachine, string animBoolName) : base(entity, stateMachine, animBoolName)
    {
        block = entity as Block;
    }

    public override void Enter()
    {
        base.Enter();

    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
    }
}
