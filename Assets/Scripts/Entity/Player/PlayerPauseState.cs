using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPauseState : PlayerState
{
    public PlayerPauseState(Entity entity, EntityStateMachine stateMachine, string animBoolName) : base(entity, stateMachine, animBoolName)
    {
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

        // 处理形状切换
        if (player.changeShapePressed)
        {
            player.ChangeShape();
        }
        SetVelocity(0, 0);
    }
}
