using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class Player : Entity
{
    [HideInInspector] public BoxCollider2D boxCd;
    [HideInInspector] public CircleCollider2D circleCd;
    [HideInInspector] public PolygonCollider2D traingleCd; 
    [HideInInspector] public InputDevice boundDevice;      // 绑定的输入设备
    [HideInInspector] public string controlScheme;         // 使用的控制方案

    public PlayerControl control;
    public PlayerInput playerInput;
    // 外部注入的移动输入（用于在同一物理键盘上手动路由箭头给 player2）
    public Vector2 externalMoveInput;
    public Stack<Block> blockStack = new Stack<Block>();

    public bool isGrounded;
    public float resetCd;
    public Vector2 dropVelocity;

    public int traingleCount;
    public int squareCount; 
    public int circleCount;

    #region State
    public PlayerMoveState moveState;
    public PlayerDeadState deadState;
    public PlayerPauseState pauseState;
    #endregion

    [Header("Collision Info")]
    public Transform groundCheckL;
    public Transform groundCheckR;
    //public Transform wallCheck;
    public LayerMask enemy;
    public LayerMask ground;

    protected override void Awake()
    {
        base.Awake();

        Debug.Log($"Player Awake, 对象名: {gameObject.name}");

        moveState = new PlayerMoveState(this, stateMachine, "Move");    
        deadState = new PlayerDeadState(this, stateMachine, "Dead");
        pauseState = new PlayerPauseState(this, stateMachine, "Pause");
    }



    protected override void Start()
    {
        base.Start(); 
        playerInput = GetComponent<PlayerInput>();

        Debug.Log($"Player Start, 对象名: {gameObject.name}, PlayerInput = {playerInput != null}");

        // 如果未在 Inspector 中设置移动速度或跳跃力，提供合理的默认值，避免速度为 0 导致无法移动
        if (walkSpeed <= 0f)
        {
            walkSpeed = 5f;
            Debug.LogWarning($"Player {gameObject.name} 未设置 walkSpeed，使用默认值 {walkSpeed}");
        }
        if (jumpForce <= 0f)
        {
            jumpForce = 7f;
            Debug.LogWarning($"Player {gameObject.name} 未设置 jumpForce，使用默认值 {jumpForce}");
        }

        // 如果 InputManager 在创建玩家时已设置了 boundDevice/controlScheme，确保 PlayerInput 启用对应地图并绑定设备
        try
        {
            if (playerInput != null && !string.IsNullOrEmpty(controlScheme) && playerInput.actions != null)
            {
                var map = playerInput.actions.FindActionMap(controlScheme);
                if (map != null)
                {
                    try { foreach (var m in playerInput.actions.actionMaps) m.Disable(); } catch { }
                    try { map.Enable(); } catch { }
                    try { foreach (var a in map.actions) a.Enable(); } catch { }
                    try { playerInput.SwitchCurrentActionMap(controlScheme); } catch { }
                }

                // 将 actions 限定为绑定的设备（若 boundDevice 非空）
                if (boundDevice != null)
                {
                    try { UnityEngine.InputSystem.Users.InputUser.PerformPairingWithDevice(boundDevice, playerInput.user); } catch { }
                    try { playerInput.actions.devices = new UnityEngine.InputSystem.Utilities.ReadOnlyArray<UnityEngine.InputSystem.InputDevice>(new[] { boundDevice }); } catch { }
                    try { playerInput.actions.Enable(); } catch { }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Player Start: 绑定动作/设备时发生异常: {ex.Message}");
        }

        traingleCount = 0;
        squareCount = 0;
        circleCount = 0;

        boxCd = GetComponent<BoxCollider2D>();
        circleCd = GetComponent<CircleCollider2D>();
        traingleCd = GetComponent<PolygonCollider2D>();


        Debug.Log($"状态机初始化，初始状态: {moveState}");
        stateMachine.Initialize(moveState);
    }

    protected override void Update()
    {
        base.Update();

        
    }

    public void PausePlayer()
    {
        stateMachine.ChangeState(pauseState);
    }

    public void StartPlayer()
    {
        stateMachine.ChangeState(moveState);
    }

    public void CollectBlock(Block block)
    {
        blockStack.Push(block);

        if (block.CompareTag("Traingle"))
        {
            traingleCount++;
        }
        else if (block.CompareTag("Square"))
        {
            squareCount++;
        }
        else if (block.CompareTag("Circle"))
        {
            circleCount++;
        }

        block.gameObject.SetActive(false);
    }

    public Block DropBlock()
    {

        if (blockStack.Count > 0)
        {
            Block block = blockStack.Pop();

            block.gameObject.layer = LayerMask.NameToLayer("Item");

            if (block.CompareTag("Traingle"))
            {
                traingleCount--;
                block.tag = "Traingle";
            }
            else if (block.CompareTag("Square"))
            {
                squareCount--;
                block.tag = "Square";
            }
            else if (block.CompareTag("Circle"))
            {
                circleCount--;
                block.tag = "Circle";
            }

            block.gameObject.SetActive(true);

            block.transform.position = transform.position;

            block.rb.velocity = rb.velocity;

            return block;
        }
        return null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Entity entity = collision.gameObject.GetComponent<Entity>();

        if (entity.gameObject.layer == LayerMask.NameToLayer("Item") && entity != null)
        {
            Block block = entity as Block;

            CollectBlock(block);
        }

        if (entity.gameObject.layer == LayerMask.NameToLayer("Player") && entity != null)
        {
            bool shouldEliminateOther = (this.spriteCount == 2 && entity.spriteCount == 0) || (this.spriteCount == 1 && entity.spriteCount == 2) || (this.spriteCount == 0 && entity.spriteCount == 1);

            if (shouldEliminateOther)
            {
                
                stateMachine.ChangeState(deadState);

            }
        }
    }

    private void OnDestroy()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.UnregisterPlayer(gameObject, boundDevice, controlScheme);
        }
    }

    #region Collision
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(groundCheckL.transform.position, new Vector2(groundCheckL.transform.position.x, groundCheckL.transform.position.y - groundCheckDistance));
        Gizmos.DrawLine(groundCheckR.transform.position, new Vector2(groundCheckR.transform.position.x, groundCheckR.transform.position.y - groundCheckDistance));
        //Gizmos.color = Color.white;
        //Gizmos.DrawLine(wallCheck.transform.position, new Vector2(wallCheck.transform.position.x + wallCheckDistance * facingDir, wallCheck.transform.position.y));
    }

    public virtual bool IsGroundDetected()
    {
        return Physics2D.Raycast(groundCheckL.transform.position, Vector2.down, groundCheckDistance, ground) || Physics2D.Raycast(groundCheckR.transform.position, Vector2.down, groundCheckDistance, ground);
    }

    //public bool IsWallDetected()
    //{

    //    return Physics2D.Raycast(wallCheck.transform.position, Vector2.right * facingDir, wallCheckDistance, ground);
    //}
    #endregion
}
