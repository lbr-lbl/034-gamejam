using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("物品信息")]
    [SerializeField] private ShapeType shape;          // 形状（在预制体上设置）
    [SerializeField] private int amount = 1;           // 拾取后增加的数量（通常为1）

    [Header("拾取设置")]
    [SerializeField] private float pickupDelay = 0.5f; // 生成后多久才能拾取
    [SerializeField] private LayerMask playerLayer;    // 玩家所在层

    [Header("边界销毁")]
    [SerializeField] private float destroyY = -5f;

    private float spawnTime;
    private bool canPickup = false;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        spawnTime = Time.time;
        if (rb != null)
        {
            rb.velocity = new Vector2(Random.Range(-.3f, .3f), 0);
        }
    }

    private void Update()
    {
        if (!canPickup && Time.time - spawnTime >= pickupDelay)
        {
            canPickup = true;
        }

        // 掉落地图外，放回对象池
        if (transform.position.y < destroyY)
        {
            BlockManager.instance.ReturnBlock(gameObject, shape, BlockType.Pickup);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canPickup) return;

        // 检查是否是玩家
        if ((playerLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                // 增加玩家对应计数
                if (shape == ShapeType.Triangle) player.triangleCount += amount;
                else if (shape == ShapeType.Square) player.squareCount += amount;
                else if (shape == ShapeType.Circle) player.circleCount += amount;

                Debug.Log($"玩家 {player.name} 拾取了 {amount} 个 {shape}");

                // 将物体放回对象池
                BlockManager.instance.ReturnBlock(gameObject, shape, BlockType.Pickup);
            }
        }
    }
}