using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("形状")]
    [SerializeField] private int spriteCount; // 0=三角形, 1=正方形, 2=圆形

    [Header("耐久")]
    [SerializeField] private int maxDurability = 6;
    private int currentDurability;

    [Header("耐久消耗 (按目标Layer)")]
    [SerializeField] private int coreCost = 6;      // 核心层
    [SerializeField] private int islandCost = 3;    // 空岛层
    [SerializeField] private int buildingCost = 1;  // 建筑层
    [SerializeField] private int playerCost = 2;    // 玩家层
    [SerializeField] private int bulletCost = 1;    // 子弹层

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentDurability = maxDurability;
    }

    private void Start()
    {
        // 自动销毁（可选）
        Destroy(gameObject, 10f);
    }

    private void Update()
    {
        // 掉落地图外销毁
        if (transform.position.y < -5)
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;

        // ---------- 1. 根据目标图层扣除耐久 ----------
        int cost = GetDurabilityCostByLayer(other.layer);
        currentDurability -= cost;
        if (currentDurability <= 0)
        {
            Destroy(gameObject);
            return; // 耐久耗尽，直接销毁，不再反弹
        }

        // ---------- 2. 克制消除逻辑（仅针对拥有 Entity 的目标）----------
        Entity entity = other.GetComponent<Entity>(); // 包括方块、玩家等
        Bullet otherBullet = other.GetComponent<Bullet>();

        if (entity != null)
        {
            // 双方都有 spriteCount，判断克制关系
            if (IsCounter(this.spriteCount, entity.spriteCount))
            {
                // 子弹克制对方 → 消除对方（调用目标的消除方法）
                EliminateEntity(entity);
                // 子弹继续飞行（反弹由物理材质处理）
            }
            else if (IsCounter(entity.spriteCount, this.spriteCount))
            {
                // 对方克制子弹 → 子弹销毁
                Destroy(gameObject);
                return;
            }
            // 相同形状：弹开（物理材质自动处理）
        }
        else if (otherBullet != null)
        {
            // 子弹互碰
            if (IsCounter(this.spriteCount, otherBullet.spriteCount))
            {
                Destroy(otherBullet.gameObject); // 克制对方
                // 自己继续
            }
            else if (IsCounter(otherBullet.spriteCount, this.spriteCount))
            {
                Destroy(gameObject); // 被克制
                return;
            }
            // 相同形状：弹开
        }

        // 注意：物理反弹由物理材质自动完成，无需额外代码
    }

    /// <summary>
    /// 根据目标图层返回耐久消耗值
    /// </summary>
    private int GetDurabilityCostByLayer(int layer)
    {
        string layerName = LayerMask.LayerToName(layer);
        switch (layerName)
        {
            //case "Ground": return coreCost;
            case "Ground": return islandCost;
            //case "Building": return buildingCost;
            case "Player": return playerCost;
            case "Bullet": return bulletCost;
            default: return 1;
        }
    }

    /// <summary>
    /// 判断形状 a 是否克制形状 b（0克2，2克1，1克0）
    /// </summary>
    private bool IsCounter(int a, int b)
    {
        return (a == 0 && b == 2) || (a == 2 && b == 1) || (a == 1 && b == 0);
    }

    /// <summary>
    /// 消除实体（调用实体自身的消除逻辑，不重复实现销毁）
    /// </summary>
    private void EliminateEntity(Entity entity)
    {
        Player player = entity as Player;
        if (player != null)
        {
            // 玩家进入死亡状态（由玩家状态机处理）
            player.stateMachine.ChangeState(player.deadState);
        }
        else
        {
            // 方块等实体的消除：Block 中已有完整逻辑，这里只需触发它
            // 但注意：Block 的消除需要碰撞双方，此处我们直接调用实体自身的死亡处理
            // 可以在 Entity 中添加一个 Die() 虚方法，由 Block 和 Player 各自实现
            // 为了不破坏现有结构，这里模拟 Block 中的消除方式：
            entity.gameObject.layer = LayerMask.NameToLayer("Background");
            entity.sr.enabled = false;
            entity.ps.gameObject.SetActive(true);
            Destroy(entity.gameObject, 5f);
        }
    }
}