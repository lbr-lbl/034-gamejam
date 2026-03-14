// Player.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Entity
{
    [HideInInspector] public BoxCollider2D boxCd;
    [HideInInspector] public CircleCollider2D circleCd;
    [HideInInspector] public PolygonCollider2D traingleCd; 
    [HideInInspector] public InputDevice boundDevice;      // �󶨵������豸
    [HideInInspector] public string controlScheme;         // ʹ�õĿ��Ʒ���
    [HideInInspector] public PolygonCollider2D traingleCd;
    [Header("死亡设置")]
    public float deathZoneY = -5f;     // 低于此 Y 坐标触发死亡

    public PlayerInput playerInput;
    // �ⲿע����ƶ����루������ͬһ�����������ֶ�·�ɼ�ͷ�� player2��
    public Vector2 externalMoveInput;
    public Stack<Block> blockStack = new Stack<Block>();

    public bool isGrounded;
    public float resetCd;
    public Transform respawnPoint; // 重生点（由GameManager设置）

    // 物品计数
    public int triangleCount;
    public int squareCount;
    public int circleCount;

    public PlayerType playerType;

    // 输入值（由UpdateInput更新）
    public Vector2 moveInput { get; private set; }
    public bool jumpPressed { get; private set; }
    public bool throwPressed { get; private set; }
    public bool buildModePressed { get; private set; }
    public bool placePressed { get; private set; }
    public bool changeShapePressed { get; private set; }
    public bool suicidePressed { get; private set; }

    #region State
    public PlayerMoveState moveState;
    public PlayerDeadState deadState;
    public PlayerPauseState pauseState;
    #endregion

    [Header("Collision Info")]
    public Transform groundCheckL;
    public Transform groundCheckR;
    public LayerMask groundLayer;   // Ground (6)
    public LayerMask playerLayer;   // Player (7)
    public LayerMask itemLayer;     // Item (8)
    public LayerMask pickupLayer;   // Pickable (9)

    protected override void Awake()
    {
        base.Awake();

        Debug.Log($"Player Awake, ������: {gameObject.name}");

        moveState = new PlayerMoveState(this, stateMachine, "Move");    
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        deadState = new PlayerDeadState(this, stateMachine, "Dead");
        pauseState = new PlayerPauseState(this, stateMachine, "Pause");
    }



    protected override void Start()
    {
        base.Start(); 
        playerInput = GetComponent<PlayerInput>();

        Debug.Log($"Player Start, ������: {gameObject.name}, PlayerInput = {playerInput != null}");

        // ���δ�� Inspector �������ƶ��ٶȻ���Ծ�����ṩ������Ĭ��ֵ�������ٶ�Ϊ 0 �����޷��ƶ�
        if (walkSpeed <= 0f)
        {
            walkSpeed = 5f;
            Debug.LogWarning($"Player {gameObject.name} δ���� walkSpeed��ʹ��Ĭ��ֵ {walkSpeed}");
        }
        if (jumpForce <= 0f)
        {
            jumpForce = 7f;
            Debug.LogWarning($"Player {gameObject.name} δ���� jumpForce��ʹ��Ĭ��ֵ {jumpForce}");
        }

        // ��� InputManager �ڴ������ʱ�������� boundDevice/controlScheme��ȷ�� PlayerInput ���ö�Ӧ��ͼ�����豸
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

                // �� actions �޶�Ϊ�󶨵��豸���� boundDevice �ǿգ�
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
            Debug.LogWarning($"Player Start: �󶨶���/�豸ʱ�����쳣: {ex.Message}");
        }

        traingleCount = 0;
        squareCount = 0;
        circleCount = 0;

        boxCd = GetComponent<BoxCollider2D>();
        circleCd = GetComponent<CircleCollider2D>();
        traingleCd = GetComponent<PolygonCollider2D>();


        Debug.Log($"״̬����ʼ������ʼ״̬: {moveState}");
        base.Start();
        playerInput = GetComponent<PlayerInput>();
        triangleCount = squareCount = circleCount = 0;
        boxCd = GetComponent<BoxCollider2D>();
        circleCd = GetComponent<CircleCollider2D>();
        traingleCd = GetComponent<PolygonCollider2D>();
        stateMachine.Initialize(moveState);
    }

    protected override void Update()
    {
        base.Update();

        
        UpdateInput();
        CheckFall();
    }

    private void CheckFall()
    {
        if (transform.position.y < deathZoneY && !(stateMachine.currentState is PlayerDeadState)) 
        {
            stateMachine.ChangeState(deadState);
        }
    }

    private void UpdateInput()
    {
        if (playerInput == null) return;
        var actionMap = playerInput.currentActionMap;
        moveInput = actionMap["Move"].ReadValue<Vector2>();
        jumpPressed = actionMap["Jump"].WasPressedThisFrame();
        throwPressed = actionMap["Throw"].WasPressedThisFrame();
        buildModePressed = actionMap["BuildMode"].WasPressedThisFrame();
        placePressed = actionMap["Set"].WasPressedThisFrame();
        changeShapePressed = actionMap["ChangeShape"].WasPressedThisFrame();
        suicidePressed = actionMap["Suicide"].WasPressedThisFrame();
    }

    public void PausePlayer() => stateMachine.ChangeState(pauseState);
    public void StartPlayer() => stateMachine.ChangeState(moveState);

    // 拾取可拾取物（图层为Pickable）
    public void CollectPickup(GameObject pickup)
    {
        Block block = pickup.GetComponent<Block>();
        if (block != null)
        {
            int shape = block.spriteCount;
            if (shape == 0) triangleCount++;
            else if (shape == 1) squareCount++;
            else if (shape == 2) circleCount++;
        }
        // 将物体放回池中
        BlockManager.instance.ReturnBlock(pickup, (ShapeType)block.spriteCount, BlockType.Pickup);
    }

    // 死亡时掉落所有物品
    public void DropAllItemsOnDeath()
    {
        for (int i = 0; i < triangleCount; i++)
            SpawnDropItem(ShapeType.Triangle);
        for (int i = 0; i < squareCount; i++)
            SpawnDropItem(ShapeType.Square);
        for (int i = 0; i < circleCount; i++)
            SpawnDropItem(ShapeType.Circle);
        triangleCount = squareCount = circleCount = 0;
    }

    private void SpawnDropItem(ShapeType shape)
    {
        GameObject blockObj = BlockManager.instance.GetBlock(shape, BlockType.Pickup);
        blockObj.transform.position = transform.position + (Vector3)Random.insideUnitCircle * 1f;
        blockObj.layer = LayerMask.NameToLayer("Pickable"); // 设置为可拾取层
        blockObj.SetActive(true);
        Rigidbody2D rb = blockObj.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = Random.insideUnitCircle * 2f;
        Block block = blockObj.GetComponent<Block>();
        if (block != null) block.blockType = BlockType.Pickup;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 拾取可拾取物
        //if (collision.gameObject.layer == LayerMask.NameToLayer("Pickable"))
        //{
        //    CollectPickup(collision.gameObject);
        //    return;
        //}

        // 玩家间克制
        Player otherPlayer = collision.gameObject.GetComponent<Player>();
        if (otherPlayer != null)
        {
            bool shouldEliminateOther = (this.spriteCount == 0 && otherPlayer.spriteCount == 2) ||
                                         (this.spriteCount == 2 && otherPlayer.spriteCount == 1) ||
                                         (this.spriteCount == 1 && otherPlayer.spriteCount == 0);
            if (shouldEliminateOther)
            {
                otherPlayer.stateMachine.ChangeState(deadState);
            }
            // 相同形状：弹开（由物理材质处理）
        }
    }

    /// <summary>
    /// 切换形状（由状态机调用）
    /// </summary>
    public void ChangeShape()
    {
        spriteCount = (spriteCount + 1) % 3; // 0->1->2->0
        UpdateShapeVisual();
    }

    /// <summary>
    /// 更新形状的动画和碰撞器
    /// </summary>
    public void UpdateShapeVisual()
    {
        // 禁用所有形状碰撞器
        boxCd.enabled = false;
        circleCd.enabled = false;
        traingleCd.enabled = false;

        // 根据 spriteCount 启用对应碰撞器和动画
        switch (spriteCount)
        {
            case 0: // 三角形
                traingleCd.enabled = true;
                anim.SetBool("Triangle", true);
                anim.SetBool("Square", false);
                anim.SetBool("Circle", false);
                break;
            case 1: // 正方形
                boxCd.enabled = true;
                anim.SetBool("Triangle", false);
                anim.SetBool("Square", true);
                anim.SetBool("Circle", false);
                break;
            case 2: // 圆形
                circleCd.enabled = true;
                anim.SetBool("Triangle", false);
                anim.SetBool("Square", false);
                anim.SetBool("Circle", true);
                break;
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
        Gizmos.DrawLine(groundCheckL.position, groundCheckL.position + Vector3.down * groundCheckDistance);
        Gizmos.DrawLine(groundCheckR.position, groundCheckR.position + Vector3.down * groundCheckDistance);
    }

    public bool IsGroundDetected()
    {
        return Physics2D.Raycast(groundCheckL.position, Vector2.down, groundCheckDistance, groundLayer) ||
               Physics2D.Raycast(groundCheckR.position, Vector2.down, groundCheckDistance, groundLayer);
    }
    #endregion


}