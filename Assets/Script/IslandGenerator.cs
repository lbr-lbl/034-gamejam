using UnityEngine;
using System.Collections.Generic;

public class IslandGenerator : MonoBehaviour
{
    [Header("网格设置")]
    [SerializeField] private float cellSize = 1f;               // 与建造模式保持一致
    [SerializeField] private int widthInCells = 10;             // 生成区域的宽度（格子数）
    [SerializeField] private int heightInCells = 10;            // 生成区域的高度（格子数）

    [Header("预制体")]
    [SerializeField] private GameObject[] prefabs;              // 三种预制体（可拖入多个）
    [SerializeField][Range(0, 1)] private float[] probabilities; // 每种预制体的生成概率（长度与prefabs一致）
    [SerializeField][Range(0, 1)] private float emptyProbability = 0.2f; // 空（不生成物体）的概率

    [Header("运行时生成")]
    [SerializeField] private bool generateOnStart = true;       // 是否在Start时自动生成
    [SerializeField] private Transform generatedParent;         // 生成物体的父对象（可选）

    private void Start()
    {
        if (generateOnStart)
            Generate();
    }

    /// <summary>
    /// 手动调用生成（也可通过编辑器按钮调用）
    /// </summary>
    [ContextMenu("Generate Island")]
    public void Generate()
    {
        // 检查概率数组长度是否与预制体数组匹配
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("没有指定预制体，生成失败");
            return;
        }
        if (probabilities == null || probabilities.Length != prefabs.Length)
        {
            Debug.LogError("概率数组长度必须与预制体数组长度一致");
            return;
        }

        // 计算总概率（预制体概率之和 + 空概率）
        float total = emptyProbability;
        foreach (float p in probabilities)
            total += p;

        if (total <= 0)
        {
            Debug.LogWarning("总概率 <= 0，不会生成任何物体");
            return;
        }

        // 如果没有指定父对象，则创建一个新的
        if (generatedParent == null)
        {
            GameObject parentObj = new GameObject("IslandGenerated");
            generatedParent = parentObj.transform;
        }

        // 获取生成器中心的世界坐标，并向下取整到最近的网格点（保证对齐网格）
        Vector3 center = transform.position;
        float gridX = Mathf.Floor(center.x / cellSize) * cellSize;
        float gridY = Mathf.Floor(center.y / cellSize) * cellSize;
        Vector3 alignedCenter = new Vector3(gridX, gridY, center.z);

        // 计算生成区域的起始点（左下角）
        float startX = alignedCenter.x - (widthInCells / 2f) * cellSize;
        float startY = alignedCenter.y - (heightInCells / 2f) * cellSize;

        // 遍历每个格子
        for (int x = 0; x < widthInCells; x++)
        {
            for (int y = 0; y < heightInCells; y++)
            {
                // 计算当前格子的世界坐标（中心点）
                float posX = startX + x * cellSize + cellSize * 0.5f;
                float posY = startY + y * cellSize + cellSize * 0.5f;
                Vector3 worldPos = new Vector3(posX, posY, center.z);

                // 随机决定生成哪个物体
                GameObject prefabToSpawn = GetRandomPrefab();
                if (prefabToSpawn != null)
                {
                    Instantiate(prefabToSpawn, worldPos, Quaternion.identity, generatedParent);
                }
                // 否则（空）跳过
            }
        }

        Debug.Log($"空岛生成完成，共生成区域 {widthInCells}x{heightInCells} 个格子");
    }

    /// <summary>
    /// 根据概率返回一个预制体，若返回 null 则表示空
    /// </summary>
    private GameObject GetRandomPrefab()
    {
        float rand = Random.Range(0f, 1f);
        float cumulative = 0f;

        // 先检查是否落在空概率区间
        if (rand < emptyProbability)
            return null;

        // 否则在预制体概率中查找
        cumulative = emptyProbability;
        for (int i = 0; i < prefabs.Length; i++)
        {
            cumulative += probabilities[i];
            if (rand < cumulative)
                return prefabs[i];
        }

        // 如果因浮点误差导致未命中，默认返回第一个预制体
        return prefabs.Length > 0 ? prefabs[0] : null;
    }

    // 在编辑器中绘制生成区域预览（可选）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = transform.position;
        float width = widthInCells * cellSize;
        float height = heightInCells * cellSize;
        Gizmos.DrawWireCube(center, new Vector3(width, height, 0));
    }
}