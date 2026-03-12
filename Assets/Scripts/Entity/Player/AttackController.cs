using UnityEngine;

public class AttackController : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private BuildModeController buildMode;   // 同一角色上的建造控制器
    [SerializeField] private Transform firePoint;             // 发射点（角色前方的子物体）

    [Header("攻击键")]
    [SerializeField] private KeyCode attackKey = KeyCode.J;    // 按住进入瞄准，松开发射

    [Header("方向键（用于瞄准）")]
    [SerializeField] private KeyCode upKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode downKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode leftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode rightKey = KeyCode.RightArrow;

    [Header("投射物参数")]
    [SerializeField] private float projectileSpeed = 10f;      // 投射物初速度

    private LineRenderer aimLine;             // 显示瞄准线

    private bool isAiming = false;
    private Vector2 shootDirection;                              // 当前瞄准方向
    private PlayerInventory playerInventory;

    private void Start()
    {
        playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory == null)
            Debug.LogError("AttackController: 未找到 PlayerInventory 组件！");

        // 动态创建瞄准线
        CreateAimLine();
    }

    /// <summary>
    /// 动态创建 LineRenderer
    /// </summary>
    private void CreateAimLine()
    {
        // 创建一个子物体专门用于瞄准线
        GameObject lineObj = new GameObject("AimLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;

        // 添加 LineRenderer 组件
        aimLine = lineObj.AddComponent<LineRenderer>();

        // 设置材质（使用默认的 Sprite 材质，或自己创建材质）
        Material defaultMaterial = new Material(Shader.Find("Sprites/Default"));
        aimLine.material = defaultMaterial;

        // 设置宽度
        aimLine.startWidth = 0.01f;
        aimLine.endWidth = 0.1f;

        // 设置位置计数（2个点：起点和终点）
        aimLine.positionCount = 2;

        // 初始隐藏
        aimLine.enabled = false;
    }

    private void Update()
    {
        // 攻击键按下 -> 进入瞄准模式
        if (Input.GetKeyDown(attackKey) && !isAiming)
        {
            TryEnterAimMode();
        }

        // 攻击键松开 -> 发射
        if (Input.GetKeyUp(attackKey) && isAiming)
        {
            Fire();
            ExitAimMode();
        }

        // 瞄准期间更新方向
        if (isAiming)
        {
            UpdateShootDirectionFromInput();
            UpdateAimVisual();
        }
    }

    /// <summary>
    /// 尝试进入瞄准模式（检查当前物品是否存在且数量足够）
    /// </summary>
    private void TryEnterAimMode()
    {
        PlaceableItem current = buildMode.CurrentItem;
        if (current == null || current.projectilePrefab == null)
        {
            Debug.Log("当前物品无法攻击");
            return;
        }

        int count = playerInventory.GetItemCount(current.itemName);
        if (count <= 0)
        {
            Debug.Log("物品数量不足");
            return;
        }

        // 进入瞄准模式
        isAiming = true;
        // 初始方向默认为角色朝向（由 localScale.x 决定）
        float facing = Mathf.Sign(transform.localScale.x);
        shootDirection = Vector2.right * facing;

        if (aimLine != null)
        {
            aimLine.enabled = true;
            UpdateAimVisual();
        }
    }

    private void ExitAimMode()
    {
        isAiming = false;
        if (aimLine != null)
            aimLine.enabled = false;
    }

    /// <summary>
    /// 根据按下的方向键合成发射方向
    /// </summary>
    private void UpdateShootDirectionFromInput()
    {
        Vector2 dir = Vector2.zero;
        if (Input.GetKey(upKey)) dir += Vector2.up;
        if (Input.GetKey(downKey)) dir += Vector2.down;
        if (Input.GetKey(leftKey)) dir += Vector2.left;
        if (Input.GetKey(rightKey)) dir += Vector2.right;

        if (dir != Vector2.zero)
        {
            // 有方向键按下：使用归一化后的方向
            shootDirection = dir.normalized;
        }
        else
        {
            // 无方向键：默认朝向角色当前方向（考虑翻转）
            float facing = Mathf.Sign(transform.localScale.x);
            shootDirection = Vector2.right * facing;
        }
    }

    private void UpdateAimVisual()
    {
        if (aimLine == null) return;

        Vector3 start = firePoint.position;
        Vector3 end = start + (Vector3)shootDirection;
        aimLine.positionCount = 2;
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    /// <summary>
    /// 发射投射物
    /// </summary>
    private void Fire()
    {
        PlaceableItem current = buildMode.CurrentItem;
        if (current == null || current.projectilePrefab == null) return;

        // 再次检查数量（防止瞄准期间数量变化）
        if (playerInventory.GetItemCount(current.itemName) <= 0)
        {
            Debug.Log("物品数量不足，无法发射");
            return;
        }

        // 扣除背包数量
        playerInventory.RemoveItem(current.itemName, 1);

        // 生成投射物
        GameObject proj = Instantiate(current.projectilePrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = shootDirection * projectileSpeed;
        }

        // 忽略与发射者的碰撞（投射物和玩家的所有碰撞器）
        Collider2D[] playerColliders = GetComponents<Collider2D>();
        Collider2D[] projColliders = proj.GetComponents<Collider2D>();
        foreach (var pc in playerColliders)
            foreach (var projc in projColliders)
                Physics2D.IgnoreCollision(projc, pc);

        // 如果当前物品数量归零，自动切换到下一个可用物品（可选）
        if (playerInventory.GetItemCount(current.itemName) == 0)
        {
            // 可以通过 buildMode 的切换逻辑处理，或暂时不处理
        }
    }
}