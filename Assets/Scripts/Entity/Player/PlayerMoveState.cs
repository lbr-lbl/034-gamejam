using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveState : PlayerState
{
    private InputAction moveAction;
    private InputAction jumpAction;
    private Vector2 moveValue = Vector2.zero;
    private bool jumpRequested = false;

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

        // 从 PlayerInput 的当前 ActionMap 获取 Move / Jump 并注册回调
        if (player.playerInput != null)
        {
            var map = player.playerInput.currentActionMap;
            if (map != null)
            {
                // FindAction 会在找不到时抛异常（第二个参数 true），可根据需要改为 false
                moveAction = map.FindAction("Move", throwIfNotFound: false);
                jumpAction = map.FindAction("Jump", throwIfNotFound: false);

                if (moveAction != null)
                {
                    moveAction.performed += OnMove;
                    moveAction.canceled += OnMove;
                    moveAction.Enable();
                }

                if (jumpAction != null)
                {
                    jumpAction.performed += OnJump;
                    jumpAction.Enable();
                }
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        // 注销输入回调并禁用 Action
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
            moveAction.Disable();
            moveAction = null;
        }

        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump;
            jumpAction.Disable();
            jumpAction = null;
        }
        // 清理本地请求标志
        jumpRequested = false;
        moveValue = Vector2.zero;
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
        // 应用移动速度 —— 使用本地缓存的 moveValue（不再直接依赖 player.moveInput）
        SetVelocity(moveValue.x * player.walkSpeed, player.rb.velocity.y);

        // 处理跳跃（支持两种来源：player 的轮询字段 或 本地回调请求）
        if ((jumpRequested || player.jumpPressed) && player.IsGroundDetected())
        {
            SetVelocity(player.rb.velocity.x, player.jumpForce);
            // 本地请求处理后清除
            jumpRequested = false;
            // 注意：player.jumpPressed 由 Player.Update 的轮询设置并由 Player 自身控制清除（保持原有逻辑）
        }
    }

    // Move 回调（performed 和 canceled 都会调用，canceled 返回 0）
    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveValue = ctx.ReadValue<Vector2>();
    }

    // Jump 回调（performed）
    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            jumpRequested = true;
    }
}