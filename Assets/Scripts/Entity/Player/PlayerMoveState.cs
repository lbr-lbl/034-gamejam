using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
        Debug.Log($"��Enter ��ʼ��������� {player?.playerIndex}");

        if (player == null)
        {
            Debug.LogError("player Ϊ null��");
            return;
        }

        // ��� playerIndex ��δ���䣨Ϊ 0�����ڴ˴�������֪��Ϣ�ƶϲ����ã���֤��������·�ɿ���
        try
        {
            if (player != null && player.externalMoveInput != Vector2.zero)
            {
                // ����������� InputManager ���ⲿ���룬��д�� moveInput ����Ҫ��ǰ���أ�
                // �Ա�������� action map������ action �ص��� Jump �󶨿����޷���������
                moveInput = player.externalMoveInput;
            }

            if (player.playerIndex == 0)
            {
                if (player.boundDevice is Gamepad)
                {
                    player.playerIndex = 2;
                    player.controlScheme = "Gamepad";
                }
                else if (player.playerInput != null)
                {
                    // ����� PlayerInput ������Ե��豸�������õ�һ��
                    var devs = player.playerInput.user.valid ? player.playerInput.user.pairedDevices : player.playerInput.devices;
                    if (devs.Count > 0)
                    {
                        var dev = devs[0];
                        player.boundDevice = dev;
                        if (dev is Gamepad)
                        {
                            player.playerIndex = 2;
                            player.controlScheme = "Gamepad";
                        }
                        else
                        {
                            // ���̣������������ player Ϊ 1������Ϊ 2
                            bool hasP1 = PlayerInput.all.Select(pi => pi == null ? null : pi.GetComponent<Player>()).Any(p => p != null && p.playerIndex == 1 && p != player);
                            player.playerIndex = hasP1 ? 2 : 1;
                            player.controlScheme = player.playerIndex == 1 ? "KeyboardWASD" : "KeyboardArrows";
                        }
                    }
                    else
                    {
                        // û�п����豸��Ϣ������ݳ������Ƿ����� player1 ����
                        bool hasP1 = PlayerInput.all.Select(pi => pi == null ? null : pi.GetComponent<Player>()).Any(p => p != null && p.playerIndex == 1 && p != player);
                        player.playerIndex = hasP1 ? 2 : 1;
                        player.controlScheme = player.playerIndex == 1 ? "KeyboardWASD" : "KeyboardArrows";
                    }
                }
                else
                {
                    // ���ף���Ϊ player1
                    player.playerIndex = 1;
                    player.controlScheme = "KeyboardWASD";
                }

                Debug.Log($"PlayerMoveState �ƶϲ����� playerIndex={player.playerIndex}, controlScheme={player.controlScheme}, boundDevice={(player.boundDevice!=null?player.boundDevice.displayName:"null")}");
            }
        }
        catch { }

        if (player.playerInput == null)
        {
            Debug.LogError("player.playerInput Ϊ null��");
            return;
        }

        Debug.Log($"player.controlScheme = '{player.controlScheme}'");
        Debug.Log($"player.playerInput.currentActionMap = {player.playerInput.currentActionMap?.name}");
        Debug.Log($"��Enter ��ʼ��������� {player.playerIndex}");

        // ʹ�� action map �� Move/Jump ��������������
        try
        {
            // ����ʹ�� currentActionMap
            InputActionMap actionMap = null;
            try { actionMap = player.playerInput.currentActionMap; } catch { actionMap = null; }
            if (actionMap == null && player.playerInput.actions != null && !string.IsNullOrEmpty(player.controlScheme))
            {
                try { actionMap = player.playerInput.actions.FindActionMap(player.controlScheme); } catch { actionMap = null; }
            }
            if (actionMap == null && player.playerInput.actions != null)
            {
                try { actionMap = player.playerInput.actions.actionMaps.FirstOrDefault(m => m.FindAction("Move") != null); } catch { actionMap = null; }
            }

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
        // ��������ͨ���Ѷ��ĵ� action �ص���OnMove������ moveInput�����޻ص��ٻ��˵� externalMoveInput
        //Debug.Log($"[PlayerMoveState.Update] player={player?.name} index={player?.playerIndex} externalMoveInput={player?.externalMoveInput} moveInput={moveInput}");
        // ÿ֡���ȶ�ȡ action �ĵ�ǰֵ�������ڲ����㣩������ʹ�� externalMoveInput ��Ϊ����
        try
        {
            Vector2 actionV = Vector2.zero;
            if (moveAction != null)
            {
                try { actionV = moveAction.ReadValue<Vector2>(); } catch { actionV = Vector2.zero; }
            }

            if (actionV != Vector2.zero)
            {
                moveInput = actionV;
            }
            else
            {
                try
                {
                    if (player != null && player.externalMoveInput != Vector2.zero)
                        moveInput = player.externalMoveInput;
                    else
                        moveInput = Vector2.zero; // ��ʽ���㣬�����ͷŰ���������һ���ٶ�
                }
                catch { moveInput = Vector2.zero; }
            }
        }
        catch { }

        // ������Ծ������ action map ����δ�󶨵�������������������������Ϊ����
        try
        {
            var kb = Keyboard.current;
            if (player != null)
            {
                bool jumpPressed = false;
                // ���Լ�����Ҽ���Ӧ�ļ�������һ��������������
                if (kb != null)
                {
                    // ��ֱ�ӵİ��������ӳ�䣺j/space -> playerIndex 1��numpad1/up/space -> playerIndex 2
                    if ((kb.jKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) && player.playerIndex == 1)
                        jumpPressed = true;
                    if ((kb.numpad1Key.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) && player.playerIndex == 2)
                        jumpPressed = true;
                }

                // �ֱ���Ծ��⣺������Ұ��ֱ�ʱ
                try
                {
                    if (player.boundDevice is Gamepad gp)
                    {
                        if (gp.buttonSouth.wasPressedThisFrame)
                            jumpPressed = true;
                    }
                }
                catch { }

                if (jumpPressed)
                {
                    try
                    {
                        if (player.IsGroundDetected())
                        {
                            player.rb.velocity = new Vector2(player.rb.velocity.x, player.jumpForce);
                            Debug.Log($"[PlayerMoveState] DoJump for player {player.playerIndex}, jumpForce={player.jumpForce}");
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        // ����ʹ�� action ���˶�ȡ��ȫ������ѯΪ��

        if (Input.GetKeyDown(KeyCode.O))
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