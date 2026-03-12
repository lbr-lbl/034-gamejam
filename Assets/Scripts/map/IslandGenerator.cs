using UnityEngine;

public class IslandGenerator : MonoBehaviour
{
    [Header("网格设置")]
    [SerializeField] private float cellSize = 1f;               // 与建造模式保持一致
    [SerializeField] private int widthInCells = 10;             // 生成区域的宽度（格子数）
    [SerializeField] private int heightInCells = 10;            // 生成区域的高度（格子数）

    [Header("预制体")]
    [SerializeField] private GameObject[] prefabs;              // 多种预制体（可拖入多个）
    [SerializeField][Range(0, 1)] private float[] probabilities; // 每种预制体的生成概率（长度与prefabs一致）

    [Header("空洞概率（按层）")]
    [SerializeField]
    private AnimationCurve emptyProbabilityByLayer =
        AnimationCurve.Linear(0, 0.1f, 1, 0.5f); // 横坐标0=最底层，1=最顶层

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

                // 计算当前层的归一化高度（0~1，0=最底层，1=最顶层）
                float layerFactor = heightInCells > 1 ? (float)y / (heightInCells - 1) : 0.5f;
                float emptyProb = emptyProbabilityByLayer.Evaluate(layerFactor);

                // 根据空洞概率和预制体概率随机生成物体
                GameObject prefabToSpawn = GetRandomPrefab(emptyProb);
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
    /// 根据当前层的空洞概率和预制体概率返回一个预制体，若返回 null 则表示空
    /// </summary>
    private GameObject GetRandomPrefab(float emptyProb)
    {
        float rand = Random.Range(0f, 1f);

        // 先判断是否空洞
        if (rand < emptyProb)
            return null;

        // 计算预制体概率总和（用于归一化）
        float totalPrefabProb = 0f;
        foreach (float p in probabilities)
            totalPrefabProb += p;

        if (totalPrefabProb <= 0)
        {
            Debug.LogWarning("预制体概率总和为0，无法生成物体");
            return null;
        }

        // 在预制体概率范围内随机选择
        float rand2 = Random.Range(0f, totalPrefabProb);
        float cumulative = 0f;
        for (int i = 0; i < prefabs.Length; i++)
        {
            cumulative += probabilities[i];
            if (rand2 < cumulative)
                return prefabs[i];
        }

        // 防御性代码：返回第一个预制体
        return prefabs.Length > 0 ? prefabs[0] : null;
    }


    // 获取空岛左下角格子中心的世界坐标
    public Vector3 GetBottomLeftCenter()
    {
        // 重新计算对齐中心（与Generate中一致）
        Vector3 center = transform.position;
        float gridX = Mathf.Round(center.x / cellSize) * cellSize;
        float gridY = Mathf.Round(center.y / cellSize) * cellSize;
        Vector3 alignedCenter = new Vector3(gridX, gridY, center.z);

        float startX = alignedCenter.x - (widthInCells / 2f) * cellSize;
        float startY = alignedCenter.y - (heightInCells / 2f) * cellSize;
        return new Vector3(startX, startY, alignedCenter.z);
    }

    // 世界坐标 → 网格索引
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 origin = GetBottomLeftCenter();
        float dx = worldPos.x - origin.x - cellSize * 0.5f; // 减去半个格子得到相对于左下角的偏移
        float dy = worldPos.y - origin.y - cellSize * 0.5f;
        int x = Mathf.RoundToInt(dx / cellSize);
        int y = Mathf.RoundToInt(dy / cellSize);
        return new Vector2Int(x, y);
    }

    // 网格索引 → 世界坐标（格子中心）
    public Vector3 GridToWorld(Vector2Int grid)
    {
        Vector3 origin = GetBottomLeftCenter(); // 左下角格子的左下角点
                                                // 加上半个格子得到中心
        return origin + new Vector3(grid.x * cellSize - cellSize * .5f,
                                    grid.y * cellSize + cellSize * 1.5f,
                                    0);
    }

    // 获取空岛宽度（格子数）
    public int GetWidth() => widthInCells;

    // 获取空岛高度（格子数）
    public int GetHeight() => heightInCells;

    // 获取表面层Y索引（最高层）
    public int GetTopLayerYIndex() => heightInCells - 1;

    // 检查世界坐标是否在空岛范围内（包含所有层）
    public bool IsInIslandBounds(Vector3 worldPos)
    {
        Vector3 origin = GetBottomLeftCenter();
        float minX = origin.x;
        float maxX = origin.x + (widthInCells - 1) * cellSize;
        float minY = origin.y;
        float maxY = origin.y + (heightInCells - 1) * cellSize;
        return worldPos.x >= minX - cellSize * 0.1f && worldPos.x <= maxX + cellSize * 0.1f &&
               worldPos.y >= minY - cellSize * 0.1f && worldPos.y <= maxY + cellSize * 0.1f;
    }

    // 检查网格索引是否在空岛范围内
    public bool IsValidGrid(Vector2Int grid)
    {
        return grid.x >= 0 && grid.x < widthInCells && grid.y >= 0 && grid.y < heightInCells;
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