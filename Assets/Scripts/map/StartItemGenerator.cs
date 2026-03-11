using UnityEngine;
using System.Collections.Generic;

public class StartItemGenerator : MonoBehaviour
{
    [Header("生成区域")]
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    [SerializeField] private float minY = 0f;    // 最低生成高度（地面模式有效）
    [SerializeField] private float maxY = 2f;    // 最高生成高度（地面模式有效）

    [Header("生成模式")]
    [SerializeField] private bool dropFromSky = false; // true=从天空掉落，false=直接放在地面高度
    [SerializeField] private float skyY = 8f;          // 如果从天空掉落，起始Y坐标

    [Header("物品列表")]
    [SerializeField] private List<DropItem> dropItems; // 可生成的物品（需与掉落系统共用）

    [Header("生成数量")]
    [SerializeField] private int minCount = 3;
    [SerializeField] private int maxCount = 8;

    [Header("其他")]
    [SerializeField] private bool generateOnStart = true; // 是否在Start时自动生成
    [SerializeField] private Transform generatedParent;    // 生成的物品父对象（可选）

    private void Start()
    {
        if (generateOnStart)
            Generate();
    }

    [ContextMenu("生成物品")]
    public void Generate()
    {
        if (dropItems == null || dropItems.Count == 0)
        {
            Debug.LogWarning("没有可生成的物品列表");
            return;
        }

        int count = Random.Range(minCount, maxCount + 1);
        for (int i = 0; i < count; i++)
        {
            SpawnRandomItem();
        }

        Debug.Log($"起始物品生成完成，共生成 {count} 个物品");
    }

    private void SpawnRandomItem()
    {
        // 根据权重随机选择一个物品
        float totalWeight = 0f;
        foreach (var item in dropItems) totalWeight += item.weight;

        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        DropItem selected = null;
        foreach (var item in dropItems)
        {
            cumulative += item.weight;
            if (rand < cumulative)
            {
                selected = item;
                break;
            }
        }

        if (selected == null) return;

        // 计算生成位置
        float x = Random.Range(transform.position.x + minX, transform.position.x + maxX);
        float y = transform.position.y;
        if (dropFromSky)
        {
            y = skyY;
        }
        else
        {
            y += Random.Range(minY, maxY);
        }

        Vector3 spawnPos = new Vector3(x, y, 0);

        // 实例化
        GameObject obj = Instantiate(selected.prefab, spawnPos, Quaternion.identity);
        if (generatedParent != null)
            obj.transform.SetParent(generatedParent);

        // 如果是掉落模式，可以给一点随机水平速度
        if (dropFromSky)
        {
            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = new Vector2(Random.Range(-1f, 1f), 0);
            }
        }
    }

    // 在编辑器中显示生成区域
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (dropFromSky)
        {
            // 显示天空掉落线
            Vector3 left = new Vector3(transform.position.x + minX, transform.position.y + skyY, 0);
            Vector3 right = new Vector3(transform.position.x + maxX, transform.position.y + skyY, 0);
            Gizmos.DrawLine(left, right);
            Gizmos.DrawSphere(left, 0.2f);
            Gizmos.DrawSphere(right, 0.2f);
        }
        else
        {
            // 显示地面矩形区域
            Vector3 center = new Vector3(transform.position.x + (minX + maxX) / 2, transform.position.y + (minY + maxY) / 2, 0);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0);
            Gizmos.DrawWireCube(center, size);
        }
    }
}