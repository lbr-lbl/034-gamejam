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

    private bool isAiming = false;
    private Vector2 shootDirection;

    private void Start()
    {
        if (aimLine != null)
        {
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
            Vector2 input = player.moveInput;
            if (input != Vector2.zero)
                shootDirection = input.normalized;
            else
            {
                float facing = Mathf.Sign(transform.localScale.x);
                shootDirection = Vector2.right * facing;
            }

            UpdateAimLine();

            if (!player.throwPressed) // 松开投掷键
            {
                Fire();
                isAiming = false;
                if (aimLine != null) aimLine.enabled = false;
            }
        }
    }

    private void TryStartAim()
    {
        // 检查玩家是否有当前形状的方块
        int count = GetItemCount((ShapeType)player.spriteCount);
        if (count <= 0) return;

        isAiming = true;
        if (aimLine != null) aimLine.enabled = true;
    }

    private void Fire()
    {
        int count = GetItemCount((ShapeType)player.spriteCount);
        if (count <= 0) return;

        // 扣除物品
        DecreaseItemCount((ShapeType)player.spriteCount);

        // 获取对应形状的子弹预制体
        GameObject projPrefab = GameManager.instance.projectilePrefabs[player.spriteCount];
        GameObject proj = Instantiate(projPrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = shootDirection * projectileSpeed;

        // 设置子弹材质
        SpriteRenderer sr = proj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Material mat = (player.playerType == PlayerType.Player1) ? GameManager.instance.player1Material : GameManager.instance.player2Material;
            if (mat != null) sr.material = mat;
        }

        // 忽略与发射者的碰撞
        Collider2D[] playerColliders = player.GetComponents<Collider2D>();
        Collider2D[] projColliders = proj.GetComponents<Collider2D>();
        foreach (var pc in playerColliders)
            foreach (var projc in projColliders)
                Physics2D.IgnoreCollision(projc, pc);
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

    private void UpdateAimLine()
    {
        if (aimLine == null) return;
        Vector3 start = firePoint.position;
        Vector3 end = start + (Vector3)shootDirection * previewLength;
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }
}