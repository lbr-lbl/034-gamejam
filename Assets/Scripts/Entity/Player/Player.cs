// Player.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Entity
{
    [HideInInspector] public BoxCollider2D boxCd;
    [HideInInspector] public CircleCollider2D circleCd;
    [HideInInspector] public PolygonCollider2D traingleCd;

    public PlayerInput playerInput;
    public float resetCd;
    public Transform respawnPoint; // 重生点（由GameManager设置）

    // 物品计数
    public int triangleCount;
    public int squareCount;
    public int circleCount;

    // 输入值（由UpdateInput更新）
    public Vector2 moveInput { get; private set; }
    public bool jumpPressed { get; private set; }
    public bool throwPressed { get; private set; }
    public bool buildModePressed { get; private set; }
    public bool placePressed { get; private set; }
    public bool nextItemPressed { get; private set; }
    public bool prevItemPressed { get; private set; }
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
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        deadState = new PlayerDeadState(this, stateMachine, "Dead");
        pauseState = new PlayerPauseState(this, stateMachine, "Pause");
    }

    protected override void Start()
    {
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
    }

    private void UpdateInput()
    {
        if (playerInput == null) return;
        var actionMap = playerInput.currentActionMap;
        moveInput = actionMap["Move"].ReadValue<Vector2>();
        jumpPressed = actionMap["Jump"].WasPressedThisFrame();
        throwPressed = actionMap["Throw"].WasPressedThisFrame();
        buildModePressed = actionMap["BuildMode"].WasPressedThisFrame();
        placePressed = actionMap["Place"].WasPressedThisFrame();
        nextItemPressed = actionMap["NextItem"].WasPressedThisFrame();
        prevItemPressed = actionMap["PrevItem"].WasPressedThisFrame();
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
        BlockManager.instance.ReturnBlock(pickup, (ShapeType)block.spriteCount);
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
        GameObject blockObj = BlockManager.instance.GetBlock(shape);
        blockObj.transform.position = transform.position + (Vector3)Random.insideUnitCircle * 1f;
        blockObj.layer = LayerMask.NameToLayer("Pickable"); // 设置为可拾取层
        blockObj.SetActive(true);
        Rigidbody2D rb = blockObj.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = Random.insideUnitCircle * 2f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 拾取可拾取物
        if (collision.gameObject.layer == LayerMask.NameToLayer("Pickable"))
        {
            CollectPickup(collision.gameObject);
            return;
        }

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