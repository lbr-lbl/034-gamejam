using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static UnityEngine.InputSystem.InputAction;

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
        Debug.Log($"【Enter 开始】玩家索引 {player?.playerIndex}");

        if (player == null)
        {
            Debug.LogError("player 为 null！");
            return;
        }

        // 如果 playerIndex 尚未分配（为 0），在此处根据已知信息推断并设置，保证后续输入路由可用
        try
        {
            if (player != null && player.externalMoveInput != Vector2.zero)
            {
                // 如果已有来自 InputManager 的外部输入，先写入 moveInput 但不要提前返回，
                // 以便继续订阅 action map（否则 action 回调和 Jump 绑定可能无法建立）。
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
                    // 如果该 PlayerInput 有已配对的设备，优先用第一个
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
                            // 键盘：如果已有其它 player 为 1，则设为 2
                            bool hasP1 = PlayerInput.all.Select(pi => pi == null ? null : pi.GetComponent<Player>()).Any(p => p != null && p.playerIndex == 1 && p != player);
                            player.playerIndex = hasP1 ? 2 : 1;
                            player.controlScheme = player.playerIndex == 1 ? "KeyboardWASD" : "KeyboardArrows";
                        }
                    }
                    else
                    {
                        // 没有可用设备信息，则根据场景中是否已有 player1 决定
                        bool hasP1 = PlayerInput.all.Select(pi => pi == null ? null : pi.GetComponent<Player>()).Any(p => p != null && p.playerIndex == 1 && p != player);
                        player.playerIndex = hasP1 ? 2 : 1;
                        player.controlScheme = player.playerIndex == 1 ? "KeyboardWASD" : "KeyboardArrows";
                    }
                }
                else
                {
                    // 兜底：设为 player1
                    player.playerIndex = 1;
                    player.controlScheme = "KeyboardWASD";
                }

                Debug.Log($"PlayerMoveState 推断并设置 playerIndex={player.playerIndex}, controlScheme={player.controlScheme}, boundDevice={(player.boundDevice!=null?player.boundDevice.displayName:"null")}");
            }
        }
        catch { }

        if (player.playerInput == null)
        {
            Debug.LogError("player.playerInput 为 null！");
            return;
        }

        Debug.Log($"player.controlScheme = '{player.controlScheme}'");
        Debug.Log($"player.playerInput.currentActionMap = {player.playerInput.currentActionMap?.name}");
        Debug.Log($"【Enter 开始】玩家索引 {player.playerIndex}");

        // 使用 action map 的 Move/Jump 动作来接收输入
        try
        {
            // 优先使用 currentActionMap
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
                Debug.LogWarning("未找到合适的 ActionMap，回退到轮询输入");
            }
            else
            {
                try { actionMap.Enable(); } catch { }
                moveAction = actionMap.FindAction("Move");
                jumpAction = actionMap.FindAction("Jump");
                Debug.Log($"moveAction 是否为 null: {moveAction == null}");
                // 调试：打印绑定路径，避免直接访问可能为可空的 ReadOnlyArray 引发错误
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
            Debug.LogWarning($"订阅 actionMap 失败: {ex.Message}");
        }

        // 原有的形状设置代码...
        player.spriteCount = 1;
        player.anim.SetBool("Traingle", false);
        player.anim.SetBool("Square", true);
        player.anim.SetBool("Circle", false);
        player.boxCd.enabled = true;
        player.circleCd.enabled = false;
        player.traingleCd.enabled = false;

        // 不使用 action 绑定检查（采用轮询实现）
    }

    public override void Exit()
    {
        base.Exit();
        // 退出时无需处理 action 回调（使用轮询为主）
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
        // 现在优先通过已订阅的 action 回调（OnMove）设置 moveInput；如无回调再回退到 externalMoveInput
        //Debug.Log($"[PlayerMoveState.Update] player={player?.name} index={player?.playerIndex} externalMoveInput={player?.externalMoveInput} moveInput={moveInput}");
        // 每帧优先读取 action 的当前值（若存在并非零），否则使用 externalMoveInput 作为回退
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
                        moveInput = Vector2.zero; // 显式清零，避免释放按键后保留上一次速度
                }
                catch { moveInput = Vector2.zero; }
            }
        }
        catch { }

        // 处理跳跃：兼容 action map 可能未绑定到期望按键的情况，按键检测做为回退
        try
        {
            var kb = Keyboard.current;
            if (player != null)
            {
                bool jumpPressed = false;
                // 仅对键盘玩家检测对应的键，避免一个键触发多个玩家
                if (kb != null)
                {
                    // 更直接的按键到玩家映射：j/space -> playerIndex 1，numpad1/up/space -> playerIndex 2
                    if ((kb.jKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) && player.playerIndex == 1)
                        jumpPressed = true;
                    if ((kb.numpad1Key.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) && player.playerIndex == 2)
                        jumpPressed = true;
                }

                // 手柄跳跃检测：仅当玩家绑定手柄时
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

        // 不再使用 action 回退读取，全部以轮询为主

        if (Input.GetKeyDown(KeyCode.O))
        {
            player.spriteCount++;

            if (player.spriteCount > 2) player.spriteCount = 0;

            if (player.spriteCount == 0)
            {
                ShapeChange("Traingle", player.traingleCd, true);
            }
            else if (player.spriteCount == 1)
            {
                ShapeChange("Square", player.boxCd, true);
            }
            else if (player.spriteCount == 2)
            {
                ShapeChange("Circle", player.circleCd, true);
            }
        }

        if (player.gameObject.transform.position.y < PlayerManager.instance.playerDeadZoneY)
        {
            stateMachine.ChangeState(player.deadState);
        }
    }

    private void ShapeChange(string shapeName, Collider2D cd, bool setTrue)
    {
        player.anim.SetBool("Traingle", false);
        player.anim.SetBool("Square", false);
        player.anim.SetBool("Circle", false);
        player.boxCd.enabled = false;
        player.circleCd.enabled = false;
        player.traingleCd.enabled = false;

        cd.enabled = true;
        player.anim.SetBool(shapeName, setTrue);
    }

    public override void FixedUpdate()
    {
        // 应用 moveInput 到玩家刚体速度
        float vx = moveInput.x * (player != null ? player.walkSpeed : 5f);
        SetVelocity(vx, player != null ? player.rb.velocity.y : 0f);
        //Debug.Log($"[PlayerMoveState.FixedUpdate] moveInput={moveInput} vx={vx} rb.velocity={player?.rb.velocity}");
        //Debug.Log($"OnMove triggered: {moveInput}");  // 添加此行
    }

    #region InputSystem

    // 输入回调
    private void OnMove(InputAction.CallbackContext context)
    {
        // 仅响应来自于该玩家预期的 action map 的回调，避免同一物理设备上不同玩家同时响应同一键
        try
        {
            var mapName = context.action?.actionMap?.name;
            if (player != null && !string.IsNullOrEmpty(mapName))
            {
                if (player.controlScheme != null && player.controlScheme != "" && mapName != player.controlScheme)
                {
                    // 如果不是来自当前玩家的控制方案，则忽略
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


    #endregion
}
