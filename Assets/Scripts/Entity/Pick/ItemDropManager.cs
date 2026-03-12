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
    [SerializeField] private float intervalDecreaseRate = 0.1f; // 每波间隔减少量（或使用曲线）

    [SerializeField] private int startCount = 1;        // 初始每波掉落数量
    [SerializeField] private int maxCount = 10;         // 最大每波掉落数量
    [SerializeField] private float countIncreaseRate = 0.5f; // 每波数量增加量（每波+0.5，取整）

    [Header("可选：使用曲线控制")]
    [SerializeField] private bool useCurve = false;
    [SerializeField] private AnimationCurve countOverTime = AnimationCurve.Linear(0, 1, 300, 10); // 时间(秒) -> 数量
    [SerializeField] private AnimationCurve intervalOverTime = AnimationCurve.Linear(0, 3, 300, 1);

    private float gameTime;          // 累计游戏时间（秒）
    private Coroutine dropCoroutine;

    private void Start()
    {
        // 开始掉落循环
        dropCoroutine = StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        while (true)
        {
            // 根据当前游戏时间计算本次掉落的参数
            int count = GetCurrentCount();
            float interval = GetCurrentInterval();

            // 生成 count 个物品
            for (int i = 0; i < count; i++)
            {
                SpawnRandomItem();
                // 可以加一点小延迟，避免所有物品完全同时生成（可选）
                yield return new WaitForSeconds(0.05f);
            }

            // 等待下一次掉落
            yield return new WaitForSeconds(interval);
            gameTime += interval; // 累计时间（如果使用曲线，需要累计实际流逝时间）
        }
    }

    /// <summary>
    /// 生成一个随机物品
    /// </summary>
    private void SpawnRandomItem()
    {
        if (dropItems == null || dropItems.Count == 0) return;

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

        // 随机生成位置
        float x = Random.Range(minX, maxX);
        Vector3 spawnPos = new Vector3(x, spawnY, 0);

        Instantiate(selected.prefab, spawnPos, Quaternion.identity);
    }

    /// <summary>
    /// 获取当前应该掉落的物品数量
    /// </summary>
    private int GetCurrentCount()
    {
        if (useCurve)
        {
            return Mathf.RoundToInt(countOverTime.Evaluate(gameTime));
        }
        else
        {
            // 线性增长：初始 + (时间/间隔 * 增长率) 但简单处理为每波增加固定量
            // 这里用更简单的方式：每经过一个间隔，数量增加 countIncreaseRate（累加，取整）
            // 但为了连续，我们基于游戏时间计算
            float rawCount = startCount + (gameTime / startInterval) * countIncreaseRate;
            return Mathf.Clamp(Mathf.RoundToInt(rawCount), 1, maxCount);
        }
    }

    /// <summary>
    /// 获取当前掉落间隔
    /// </summary>
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

    // 可选：在编辑器中显示掉落区域
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