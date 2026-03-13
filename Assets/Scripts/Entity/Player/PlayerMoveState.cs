using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(Entity entity, EntityStateMachine stateMachine, string animBoolName)
        : base(entity, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // 进入时确保形状正确（假设默认是正方形，但实际应由游戏逻辑决定）
        // 如果 spriteCount 未设置，则初始化为 1（正方形）
        if (player.spriteCount < 0 || player.spriteCount > 2)
            player.spriteCount = 1;
        player.UpdateShapeVisual(); // 更新动画和碰撞器
    }

    public override void Exit()
    {
        base.Exit();
        // 清理工作，如果需要
    }

    public override void Update()
    {
        base.Update();

        // 处理自杀
        if (player.suicidePressed && !(player.stateMachine.currentState is PlayerDeadState))
        {
            player.stateMachine.ChangeState(player.deadState);
        }

        // 注意：死亡检测已移至 Player.Update 中，此处不再重复
    }

    public override void FixedUpdate()
    {
        // 应用移动速度
        SetVelocity(player.moveInput.x * player.walkSpeed, player.rb.velocity.y);

        // 处理跳跃（使用请求标志）
        if (player.jumpPressed && player.IsGroundDetected())
        {
            SetVelocity(player.rb.velocity.x, player.jumpForce);
        }
    }
}