using UnityEngine;

public class AttackController : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private BuildModeController buildMode;   // 同一角色上的建造控制器
    [SerializeField] private Transform firePoint;             // 发射点（角色前方的子物体）

    [Header("攻击设置")]
    [SerializeField] private KeyCode attackKey = KeyCode.J;    // 攻击键（按住瞄准，松开发射）
    [SerializeField] private float angleStep = 5f;             // 方向调整速度（度/秒）
    [SerializeField] private float minAngle = -60f;            // 最小角度（相对于水平）
    [SerializeField] private float maxAngle = 60f;             // 最大角度
    [SerializeField] private float projectileSpeed = 10f;      // 投射物初速度

    [Header("瞄准指示器")]
    [SerializeField] private LineRenderer aimLine;             // 显示瞄准线（可选）
    [SerializeField] private float previewLength = 5f;         // 瞄准线长度

    private bool isAiming = false;
    private float currentAngle = 0f;                            // 当前瞄准角度（度）
    private Vector2 shootDirection;                              // 发射方向向量
    private PlayerInventory playerInventory;

    private void Start()
    {
        playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory == null)
            Debug.LogError("AttackController: 未找到 PlayerInventory 组件！");

        if (aimLine != null)
            aimLine.enabled = false;
    }

    private void Update()
    {
        if (!isAiming)
        {
            // 按下攻击键进入瞄准模式
            if (Input.GetKeyDown(attackKey))
            {
                TryEnterAimMode();
            }
        }
        else
        {
            // 瞄准模式：上下键调整角度
            float adjust = 0f;
            if (Input.GetKey(KeyCode.UpArrow)) adjust += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) adjust -= 1f;

            if (adjust != 0f)
            {
                currentAngle += adjust * angleStep * Time.deltaTime * 30f;
                currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);
                UpdateAimVisual();
            }

            // 松开攻击键发射
            if (Input.GetKeyUp(attackKey))
            {
                Fire();
                ExitAimMode();
            }

            // 按 B 取消瞄准
            if (Input.GetKeyDown(KeyCode.B))
            {
                ExitAimMode();
            }
        }
    }

    /// <summary>
    /// 尝试进入瞄准模式（检查物品和数量）
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
        currentAngle = 0f;
        UpdateShootDirection();
        if (aimLine != null)
        {
            aimLine.enabled = true;
            UpdateAimVisual();
        }
        // 可选：禁用角色移动
        // GetComponent<PlayerMovement>().enabled = false;
    }

    private void ExitAimMode()
    {
        isAiming = false;
        if (aimLine != null)
            aimLine.enabled = false;
        // 恢复移动
        // GetComponent<PlayerMovement>().enabled = true;
    }

    /// <summary>
    /// 根据当前角度和角色朝向计算发射方向
    /// </summary>
    private void UpdateShootDirection()
    {
        // 获取角色朝向（假设用 localScale.x 表示：1=右，-1=左）
        float facing = Mathf.Sign(transform.localScale.x);
        // 基础方向为右向量乘以朝向
        Vector2 baseDir = Vector2.right * facing;
        // 旋转角度（绕 Z 轴）
        shootDirection = Quaternion.Euler(0, 0, currentAngle) * baseDir;
    }

    private void UpdateAimVisual()
    {
        if (aimLine == null) return;

        UpdateShootDirection();
        Vector3 start = firePoint.position;
        Vector3 end = start + (Vector3)shootDirection * previewLength;
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

        // 更新方向（确保最新）
        UpdateShootDirection();

        // 生成投射物
        GameObject proj = Instantiate(current.projectilePrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = shootDirection * projectileSpeed;
        }

        // 忽略与发射者的碰撞
        Collider2D projCollider = proj.GetComponent<Collider2D>();
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (projCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(projCollider, playerCollider);
        }

        // 可选：如果当前物品用完，自动切换到下一个可用物品
        if (playerInventory.GetItemCount(current.itemName) == 0)
        {
            // 这里可以调用 buildMode 的自动切换逻辑，但 buildMode 已有切换逻辑，无需额外处理
        }
    }
}