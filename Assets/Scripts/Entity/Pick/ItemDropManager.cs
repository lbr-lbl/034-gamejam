using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemDropManager : MonoBehaviour
{
    [Header("掉落物品列表")]
    [SerializeField] private List<DropItem> dropItems;

    [Header("掉落区域")]
    [SerializeField] private float minX = -10f;        // 掉落范围左边界
    [SerializeField] private float maxX = 10f;         // 掉落范围右边界
    [SerializeField] private float spawnY = 8f;        // 掉落起始 Y 坐标（天空高度）

    [Header("掉落参数")]
    [SerializeField] private float startInterval = 3f; // 初始掉落间隔（秒）
    [SerializeField] private float minInterval = 1f;   // 最短掉落间隔
    [SerializeField] private float intervalDecreaseRate = 0.1f; // 每波间隔减少量

    [SerializeField] private int startCount = 1;        // 初始每波掉落数量
    [SerializeField] private int maxCount = 10;         // 最大每波掉落数量
    [SerializeField] private float countIncreaseRate = 0.5f; // 每波数量增加量

    [Header("可选：使用曲线控制")]
    [SerializeField] private bool useCurve = false;
    [SerializeField] private AnimationCurve countOverTime = AnimationCurve.Linear(0, 1, 300, 10);
    [SerializeField] private AnimationCurve intervalOverTime = AnimationCurve.Linear(0, 3, 300, 1);

    private float gameTime;          // 累计游戏时间（秒）
    private Coroutine dropCoroutine;

    // 由 GameManager 调用，开始掉落
    public void StartDropping()
    {
        if (dropCoroutine == null)
            dropCoroutine = StartCoroutine(DropRoutine());
    }
    private IEnumerator DropRoutine()
    {
        while (true)
        {
            int count = GetCurrentCount();
            float interval = GetCurrentInterval();

            for (int i = 0; i < count; i++)
            {
                SpawnRandomItem();
                yield return new WaitForSeconds(0.05f); // 避免同时生成过多物体
            }

            yield return new WaitForSeconds(interval);
            gameTime += interval;
        }
    }

    private void SpawnRandomItem()
    {
        if (dropItems == null || dropItems.Count == 0) return;

        // 根据权重随机选择形状
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

        // 从对象池获取对应形状的物体
        GameObject blockObj = BlockManager.instance.GetBlock(selected.shape, BlockType.Pickup);
        if (blockObj == null) return;

        // 随机位置
        float x = Random.Range(minX, maxX);
        Vector3 spawnPos = new Vector3(x, spawnY, 0);
        blockObj.transform.position = spawnPos;

        // 设置为可拾取层
        blockObj.layer = LayerMask.NameToLayer("Pickable");

        // 可选：添加一点随机下落速度
        Rigidbody2D rb = blockObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = new Vector2(Random.Range(-1f, 1f), 0);
        }

        Block block = blockObj.GetComponent<Block>();
        if (block != null) block.blockType = BlockType.Pickup;

        // 物体已在 GetBlock 时自动激活，无需额外操作
    }

    private int GetCurrentCount()
    {
        if (useCurve)
        {
            return Mathf.RoundToInt(countOverTime.Evaluate(gameTime));
        }
        else
        {
            float rawCount = startCount + (gameTime / startInterval) * countIncreaseRate;
            return Mathf.Clamp(Mathf.RoundToInt(rawCount), 1, maxCount);
        }
    }

    private float GetCurrentInterval()
    {
        if (useCurve)
        {
            return intervalOverTime.Evaluate(gameTime);
        }
        else
        {
            float interval = startInterval - (gameTime / startInterval) * intervalDecreaseRate;
            return Mathf.Clamp(interval, minInterval, startInterval);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 left = new Vector3(minX, spawnY, 0);
        Vector3 right = new Vector3(maxX, spawnY, 0);
        Gizmos.DrawLine(left, right);
        Gizmos.DrawSphere(left, 0.2f);
        Gizmos.DrawSphere(right, 0.2f);
    }
}