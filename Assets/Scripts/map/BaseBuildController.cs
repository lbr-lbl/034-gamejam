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
    [SerializeField] private int startXOffset = -2;
    [SerializeField] private int startYOffset = 0;

    private int width;
    private int topLayerY;
    private Dictionary<Vector2Int, GameObject> highlightMap;
    private Dictionary<Vector2Int, GameObject> placedObjects; // 记录放置的物体
    private HashSet<Vector2Int> occupied;                     // 已占用的格子（与placedObjects的键一致）
    private HashSet<Vector2Int> availableSet;                  // 当前可放置的格子
    private Vector2Int currentGrid;
    private bool isSelectingCore = false;

    private Transform highlightParent; // 仅用于高亮格子

    private bool _canInput;

    public bool IsActive { get; private set; }
    public Vector3 CorePosition { get; private set; }
    public int CoreShape { get; private set; }
    public bool CoreSelected { get; private set; }

    public void Initialize()
    {
        if (highlightParent != null) Destroy(highlightParent.gameObject);
        highlightParent = new GameObject("BaseBuildHighlights").transform;

        width = islandGenerator.GetWidth();
        topLayerY = islandGenerator.GetTopLayerYIndex();

        highlightMap = new Dictionary<Vector2Int, GameObject>();
        placedObjects = new Dictionary<Vector2Int, GameObject>();
        occupied = new HashSet<Vector2Int>();
        availableSet = new HashSet<Vector2Int>();

        // 初始可放置集：整个表面层所有格子
        for (int x = 0; x < width; x++)
        {
            availableSet.Add(new Vector2Int(x, topLayerY));
        }

        // 设置初始光标
        int centerX = width / 2;
        int startX = Mathf.Clamp(centerX + startXOffset, 0, width - 1);
        currentGrid = new Vector2Int(startX, topLayerY);

        IsActive = true;
        CoreSelected = false;
        isSelectingCore = false;
        _canInput = true;
    }

    public void EndBaseBuilding()
    {
        _canInput = false;
    }

    public void HandleInput(int dx, int dy)
    {
        if (!IsActive || !_canInput) return;

        Vector2Int next = currentGrid + new Vector2Int(dx, dy);

        if (isSelectingCore)
        {
            if (occupied.Contains(next))
                currentGrid = next;
        }
        else
        {
            if (availableSet.Contains(next))
                currentGrid = next;
        }
    }

    public void UpdateHighlights()
    {
        if (!IsActive) return;

        EnsureHighlightObjects();

        foreach (var kv in highlightMap)
        {
            Vector2Int grid = kv.Key;
            GameObject hl = kv.Value;
            SpriteRenderer sr = hl.GetComponent<SpriteRenderer>();

            if (!isSelectingCore)
            {
                if (occupied.Contains(grid))
                {
                    hl.SetActive(true);
                    sr.material = occupiedMaterial ?? sr.material;
                }
                else if (availableSet.Contains(grid))
                {
                    hl.SetActive(true);
                    if (grid == currentGrid)
                        sr.material = selectedMaterial ?? sr.material;
                    else
                        sr.material = validMaterial ?? sr.material;
                }
                else
                {
                    hl.SetActive(false);
                }
            }
            else
            {
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

    private void EnsureHighlightObjects()
    {
        HashSet<Vector2Int> needed = new HashSet<Vector2Int>(occupied);
        needed.UnionWith(availableSet);

        foreach (Vector2Int grid in needed)
        {
            if (!highlightMap.ContainsKey(grid))
            {
                Vector3 worldPos = islandGenerator.GridToWorld(grid);
                GameObject hl = Instantiate(highlightPrefab, worldPos, Quaternion.identity, highlightParent);
                highlightMap[grid] = hl;
            }
        }
    }

    public bool TryPlaceItem(GameObject prefab)
    {
        if (!IsActive || isSelectingCore) return false;
        if (!availableSet.Contains(currentGrid)) return false;
        if (prefab == null) return false;

        Vector3 worldPos = islandGenerator.GridToWorld(currentGrid);
        // 实例化到场景根，不设为高亮父物体
        GameObject placed = Instantiate(prefab, worldPos, Quaternion.identity);

        // 记录
        placedObjects[currentGrid] = placed;
        occupied.Add(currentGrid);
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
        _canInput = true; // 核心选择阶段需要输入

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
        if (placedObjects.TryGetValue(currentGrid, out GameObject obj))
        {
            Block block = obj.GetComponent<Block>();
            if (block != null) CoreShape = block.spriteCount;
        }
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
        // 只销毁高亮物体，不销毁放置的基地物体
        if (highlightParent != null)
            Destroy(highlightParent.gameObject);
        highlightMap = null;
        // 保留 placedObjects 和 occupied 的引用（但不再需要）
        IsActive = false;
    }
}