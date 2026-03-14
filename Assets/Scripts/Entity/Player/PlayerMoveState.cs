using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public Vector2 moveInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    public PlayerMoveState(Entity entity, EntityStateMachine stateMachine, string animBoolName) : base(entity, stateMachine, animBoolName)
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

            if (actionMap == null)
            {
                Debug.LogWarning("δ�ҵ����ʵ� ActionMap�����˵���ѯ����");
            }
            else
            {
                try { actionMap.Enable(); } catch { }
                moveAction = actionMap.FindAction("Move");
                jumpAction = actionMap.FindAction("Jump");
                Debug.Log($"moveAction �Ƿ�Ϊ null: {moveAction == null}");
                // ���ԣ���ӡ��·��������ֱ�ӷ��ʿ���Ϊ�ɿյ� ReadOnlyArray ��������
                if (moveAction != null)
                {
                    Debug.Log($"moveAction.bindings: {string.Join(",", moveAction.bindings.Select(b => b.path))}");
                }
                if (moveAction != null)
                {
                    try { moveAction.Enable(); } catch { }
                    moveAction.performed += OnMove;
                    moveAction.canceled += OnMove;
                }
                if (jumpAction != null)
                {
                    try { jumpAction.Enable(); } catch { }
                    jumpAction.performed += OnJump;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"���� actionMap ʧ��: {ex.Message}");
        }

        // ԭ�е���״���ô���...
        player.spriteCount = 1;
        player.anim.SetBool("Traingle", false);
        player.anim.SetBool("Square", true);
        player.anim.SetBool("Circle", false);
        player.boxCd.enabled = true;
        player.circleCd.enabled = false;
        player.traingleCd.enabled = false;

        // ��ʹ�� action �󶨼�飨������ѯʵ�֣�
    }

    public override void Exit()
    {
        base.Exit();
        // �˳�ʱ���账�� action �ص���ʹ����ѯΪ����
        try
        {
            if (moveAction != null)
            {
                moveAction.performed -= OnMove;
                moveAction.canceled -= OnMove;
            }
            if (jumpAction != null)
                jumpAction.performed -= OnJump;
        }
        catch { }
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
        // Ӧ�� moveInput ����Ҹ����ٶ�
        float vx = moveInput.x * (player != null ? player.walkSpeed : 5f);
        SetVelocity(vx, player != null ? player.rb.velocity.y : 0f);
        //Debug.Log($"[PlayerMoveState.FixedUpdate] moveInput={moveInput} vx={vx} rb.velocity={player?.rb.velocity}");
        //Debug.Log($"OnMove triggered: {moveInput}");  // ���Ӵ���
    }

    #region InputSystem

    // ����ص�
    private void OnMove(InputAction.CallbackContext context)
    {
        // ����Ӧ�����ڸ����Ԥ�ڵ� action map �Ļص�������ͬһ�����豸�ϲ�ͬ���ͬʱ��Ӧͬһ��
        try
        {
            var mapName = context.action?.actionMap?.name;
            if (player != null && !string.IsNullOrEmpty(mapName))
            {
                if (player.controlScheme != null && player.controlScheme != "" && mapName != player.controlScheme)
                {
                    // ����������Ե�ǰ��ҵĿ��Ʒ����������
                    Debug.Log($"OnMove ignored for player {player.playerIndex}: action map {mapName} != controlScheme {player.controlScheme}");
                    return;
                }
            }
        }
        catch { }

        moveInput = context.ReadValue<Vector2>();
        Debug.Log($"OnMove triggered for player {player.playerIndex}: {moveInput}");
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        try
        {
            var mapName = context.action?.actionMap?.name;
            if (player != null && !string.IsNullOrEmpty(mapName) && player.controlScheme != null && player.controlScheme != "" && mapName != player.controlScheme)
            {
                Debug.Log($"OnJump ignored for player {player.playerIndex}: action map {mapName} != controlScheme {player.controlScheme}");
                return;
            }
        }
        catch { }

        if (player.IsGroundDetected())
        {
            try { player.rb.velocity = new Vector2(player.rb.velocity.x, player.jumpForce); } catch { }
            Debug.Log($"OnJump triggered for player {player.playerIndex}");
        }
    }
}