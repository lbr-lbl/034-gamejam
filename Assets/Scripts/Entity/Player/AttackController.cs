// AttackController.cs
using UnityEngine;

public class AttackController : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Player player;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 10f;

    [Header("瞄准线")]
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private float previewLength = 5f;
    [SerializeField] private Color aimColor = Color.red;
    [SerializeField] private float aimLineWidth = 0.1f;

    private bool isAiming = false;
    private Vector2 shootDirection;
    private PlaceableItem currentItem;

    private void Start()
    {
        if (aimLine != null)
        {
            aimLine.startColor = aimColor;
            aimLine.endColor = aimColor;
            aimLine.startWidth = aimLineWidth;
            aimLine.endWidth = aimLineWidth;
            aimLine.positionCount = 2;
            aimLine.enabled = false;
        }
    }

    private void Update()
    {
        if (player == null) return;

        if (!isAiming && player.throwPressed)
        {
            TryStartAim();
        }

        if (isAiming)
        {
            // 用移动键控制方向
            Vector2 input = player.moveInput;
            if (input != Vector2.zero)
            {
                shootDirection = input.normalized;
            }
            else
            {
                // 默认朝向角色面向方向
                float facing = Mathf.Sign(transform.localScale.x);
                shootDirection = Vector2.right * facing;
            }

            UpdateAimLine();

            if (player.throwPressed == false) // 松开投掷键
            {
                Fire();
                isAiming = false;
                if (aimLine != null) aimLine.enabled = false;
            }
        }
    }

    private void TryStartAim()
    {
        // 检查当前物品是否有投射物
        BuildModeController bmc = GetComponent<BuildModeController>();
        if (bmc == null) return;
        PlaceableItem item = bmc.CurrentItem;
        if (item == null || item.projectilePrefab == null) return;

        // 检查玩家是否有该物品
        int count = GetItemCount(item.shape);
        if (count <= 0) return;

        currentItem = item;
        isAiming = true;
        if (aimLine != null) aimLine.enabled = true;
    }

    private void UpdateAimLine()
    {
        if (aimLine == null) return;
        Vector3 start = firePoint.position;
        Vector3 end = start + (Vector3)shootDirection * previewLength;
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    private void Fire()
    {
        if (currentItem == null) return;

        // 扣除物品
        DecreaseItemCount(currentItem.shape);

        // 生成投射物
        GameObject proj = Instantiate(currentItem.projectilePrefab, firePoint.position, Quaternion.identity);
        proj.layer = LayerMask.NameToLayer("Bullet");
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = shootDirection * projectileSpeed;
        }

        // 设置子弹材质（查找子物体）
        SpriteRenderer projSr = proj.GetComponentInChildren<SpriteRenderer>();
        if (projSr != null)
        {
            Material mat = (player.playerType == PlayerType.Player1) ? GameManager.instance.player1Material : GameManager.instance.player2Material;
            if (mat != null) projSr.material = mat;
        }

        // 忽略与发射者的碰撞
        Collider2D[] playerColliders = player.GetComponents<Collider2D>();
        Collider2D[] projColliders = proj.GetComponents<Collider2D>();
        foreach (var pc in playerColliders)
            foreach (var projc in projColliders)
                Physics2D.IgnoreCollision(projc, pc);

        currentItem = null;
    }

    private int GetItemCount(ShapeType shape)
    {
        if (shape == ShapeType.Triangle) return player.triangleCount;
        if (shape == ShapeType.Square) return player.squareCount;
        return player.circleCount;
    }

    private void DecreaseItemCount(ShapeType shape)
    {
        if (shape == ShapeType.Triangle) player.triangleCount--;
        else if (shape == ShapeType.Square) player.squareCount--;
        else player.circleCount--;
    }
}