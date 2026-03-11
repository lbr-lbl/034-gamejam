using UnityEngine;
using System.Collections.Generic;

public class BaseBuildController : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private IslandGenerator islandGenerator;
    [SerializeField] private GameObject highlightPrefab;
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material occupiedMaterial;      // 已占用格子的材质
    [SerializeField] private Material coreCandidateMaterial;
    [SerializeField] private Material coreSelectedMaterial;

    [Header("初始位置偏移")]
    [SerializeField] private int startXOffset = -2; // 向左偏移两格
    [SerializeField] private int startYOffset = 0;   // 表面层，无需偏移

    private int width;                          // 空岛宽度（固定）
    private int topLayerY;                       // 表面层Y索引
    private Dictionary<Vector2Int, GameObject> highlightMap; // 动态高亮格子
    private HashSet<Vector2Int> occupied;        // 已占用的格子
    private HashSet<Vector2Int> availableSet;     // 当前可放置的格子
    private Vector2Int currentGrid;                // 当前选中的格子
    private bool isSelectingCore = false;

    private Transform gridParent;

    public bool IsActive { get; private set; }
    public Vector3 CorePosition { get; private set; }
    public int CoreShape { get; private set; }
    public bool CoreSelected { get; private set; }

    public void Initialize()
    {
        if (gridParent != null) Destroy(gridParent.gameObject);
        gridParent = new GameObject("BaseBuildGrid").transform;

        width = islandGenerator.GetWidth();
        topLayerY = islandGenerator.GetTopLayerYIndex();

        highlightMap = new Dictionary<Vector2Int, GameObject>();
        occupied = new HashSet<Vector2Int>();
        availableSet = new HashSet<Vector2Int>();

        // 初始可放置集：整个表面层所有格子
        for (int x = 0; x < width; x++)
        {
            availableSet.Add(new Vector2Int(x, topLayerY));
        }

        // 设置初始光标：空岛中心向左偏移两格
        int centerX = width / 2;
        int startX = Mathf.Clamp(centerX + startXOffset, 0, width - 1);
        currentGrid = new Vector2Int(startX, topLayerY);

        IsActive = true;
        CoreSelected = false;
        isSelectingCore = false;
    }

    public void HandleInput(int dx, int dy)
    {
        if (!IsActive) return;

        Vector2Int next = currentGrid + new Vector2Int(dx, dy);

        if (isSelectingCore)
        {
            // 核心选择阶段：只能在已占用的格子间移动
            if (occupied.Contains(next))
                currentGrid = next;
        }
        else
        {
            // 建造阶段：光标只能在可放置的格子中移动
            if (availableSet.Contains(next))
                currentGrid = next;
        }
    }

    public void UpdateHighlights()
    {
        if (!IsActive) return;

        // 确保所有需要显示的格子都有对应的 GameObject
        EnsureHighlightObjects();

        foreach (var kv in highlightMap)
        {
            Vector2Int grid = kv.Key;
            GameObject hl = kv.Value;
            SpriteRenderer sr = hl.GetComponent<SpriteRenderer>();

            if (!isSelectingCore)
            {
                // 建造阶段
                if (occupied.Contains(grid))
                {
                    // 已占用格子显示灰色
                    hl.SetActive(true);
                    sr.material = occupiedMaterial ?? sr.material;
                }
                else if (availableSet.Contains(grid))
                {
                    // 可放置格子显示绿色
                    hl.SetActive(true);
                    if (grid == currentGrid)
                        sr.material = selectedMaterial ?? sr.material;
                    else
                        sr.material = validMaterial ?? sr.material;
                }
                else
                {
                    // 其他格子隐藏
                    hl.SetActive(false);
                }
            }
            else
            {
                // 核心选择阶段：只显示已占用的格子
                if (occupied.Contains(grid))
                {
                    hl.SetActive(true);
                    if (grid == currentGrid)
                        sr.material = coreSelectedMaterial ?? sr.material;
                    else
                        sr.material = coreCandidateMaterial ?? sr.material;
                }
                else
                {
                    hl.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 确保所有需要显示的格子（已占用 + 可放置 + 当前选中）都有高亮物体
    /// </summary>
    private void EnsureHighlightObjects()
    {
        // 收集所有需要显示的格子
        HashSet<Vector2Int> needed = new HashSet<Vector2Int>(occupied);
        needed.UnionWith(availableSet);
        if (isSelectingCore)
        {
            // 核心选择阶段可能还需要显示未占用的核心候选？但只显示已占用的，所以 occupied 已包含
        }
        else
        {
            // 建造阶段，当前选中格子可能不在 occupied 或 availableSet 中？但光标只会在 availableSet 内移动，所以 currentGrid 必然在 availableSet 中，已经包含
        }

        // 为每个需要的格子创建高亮物体（如果尚未创建）
        foreach (Vector2Int grid in needed)
        {
            if (!highlightMap.ContainsKey(grid))
            {
                Vector3 worldPos = islandGenerator.GridToWorld(grid);
                GameObject hl = Instantiate(highlightPrefab, worldPos, Quaternion.identity, gridParent);
                highlightMap[grid] = hl;
            }
        }

        // 可选：清理不再需要的格子（长时间不用的格子可以销毁，但这里为了简单，保留所有创建的）
    }

    /// <summary>
    /// 放置物品（无限数量）
    /// </summary>
    public bool TryPlaceItem(GameObject prefab)
    {
        if (!IsActive || isSelectingCore) return false;
        if (!availableSet.Contains(currentGrid)) return false;
        if (prefab == null) return false;

        Vector3 worldPos = islandGenerator.GridToWorld(currentGrid);
        GameObject placed = Instantiate(prefab, worldPos, Quaternion.identity, gridParent);

        // 标记占用
        occupied.Add(currentGrid);
        // 从可用集中移除当前格子
        availableSet.Remove(currentGrid);

        // 添加相邻可放置格子（左、右、上）
        Vector2Int[] neighbors = new Vector2Int[]
        {
            new Vector2Int(currentGrid.x - 1, currentGrid.y),
            new Vector2Int(currentGrid.x + 1, currentGrid.y),
            new Vector2Int(currentGrid.x, currentGrid.y + 1)
        };
        foreach (var n in neighbors)
        {
            // 水平方向不能超出空岛宽度，垂直方向可以无限高
            if (n.x >= 0 && n.x < width && !occupied.Contains(n))
            {
                availableSet.Add(n);
            }
        }

        return true;
    }

    public void StartCoreSelection()
    {
        if (!IsActive) return;
        isSelectingCore = true;

        // 将光标移动到第一个有物体的格子
        if (occupied.Count > 0)
        {
            foreach (var grid in occupied)
            {
                currentGrid = grid;
                break;
            }
        }
        else
        {
            Debug.LogWarning("没有放置任何图块，无法选择核心！");
        }
    }

    public bool TryConfirmCore()
    {
        if (!IsActive || !isSelectingCore) return false;
        if (!occupied.Contains(currentGrid)) return false;

        CorePosition = islandGenerator.GridToWorld(currentGrid);
        // 注意：我们需要通过 placedObjects 获取已放置的物体实例，但这里我们并未存储
        // 为了获取形状，我们需要在放置时存储 GameObject 引用
        // 简化处理：假设每个格子只能放一个物体，我们可以通过碰撞检测或直接查找
        // 临时使用射线检测或通过子物体查找，但为了简单，我们暂时不实现核心形状读取
        // 可以暂存形状信息
        CoreShape = 0; // 默认
        CoreSelected = true;
        return true;
    }

    public void ForceSelectFirstCore()
    {
        if (occupied.Count > 0)
        {
            foreach (var grid in occupied)
            {
                currentGrid = grid;
                TryConfirmCore();
                break;
            }
        }
    }

    public void Cleanup()
    {
        if (gridParent != null)
            Destroy(gridParent.gameObject);
        highlightMap = null;
        occupied = null;
        availableSet = null;
        IsActive = false;
    }
}