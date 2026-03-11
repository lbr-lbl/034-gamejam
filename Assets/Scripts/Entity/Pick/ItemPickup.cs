using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("物品信息")]
    [SerializeField] private string itemName;          // 与背包系统匹配的名称
    [SerializeField] private int amount = 1;           // 拾取后增加的数量

    [Header("拾取设置")]
    [SerializeField] private float pickupDelay = 0.5f; // 生成后多久才能拾取（防止瞬间拾取）
    [SerializeField] private LayerMask playerLayer;    // 玩家所在层

    [Header("边界销毁")]
    [SerializeField] private float destroyY = -5f;

    private float spawnTime;
    private bool canPickup = false;

    private void Start()
    {
        spawnTime = Time.time;
        // 可选：添加一点随机下落速度
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
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

        // 掉落地图外销毁
        if (transform.position.y < destroyY)
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canPickup) return;

        Collider2D other = collision.collider;

        // 检查是否是玩家
        if ((playerLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.AddItem(itemName, amount);
                Debug.Log($"拾取了 {amount} 个 {itemName}");
                Destroy(gameObject);
            }
        }
    }

}