using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BaseBuildController : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private IslandGenerator islandGenerator;
    [SerializeField] private GameObject highlightPrefab;
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material occupiedMaterial;
    [SerializeField] private Material coreCandidateMaterial;
    [SerializeField] private Material coreSelectedMaterial;

    [Header("初始位置偏移")]
    [SerializeField] private int startXOffset = -2;
    [SerializeField] private int startYOffset = 0;

    [Header("默认核心形状（当玩家未搭建时使用）")]
    [SerializeField] private ShapeType defaultCoreShape = ShapeType.Triangle;

    private int width;
    private int topLayerY;
    private Dictionary<Vector2Int, GameObject> highlightMap;
    private Dictionary<Vector2Int, GameObject> placedObjects;
    private HashSet<Vector2Int> occupied;
    private HashSet<Vector2Int> availableSet;
    private Vector2Int currentGrid;
    private bool isSelectingCore = false;

    private Transform highlightParent;
    private PlayerInput playerInput;
    private ShapeType selectedShape = ShapeType.Triangle; // 当前建造的形状

    public bool IsActive { get; private set; }
    public Vector3 CorePosition { get; private set; }
    public int CoreShape { get; private set; }
    public bool CoreSelected { get; private set; }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

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

        int centerX = width / 2;
        int startX = Mathf.Clamp(centerX + startXOffset, 0, width - 1);
        currentGrid = new Vector2Int(startX, topLayerY);

        IsActive = true;
        CoreSelected = false;
        isSelectingCore = false;
    }

    private void Update()
    {
        if (!IsActive) return;

        if (!isSelectingCore)
        {
            // 建造阶段：读取输入
            Vector2 move = playerInput.actions["Move"].ReadValue<Vector2>();
            if (move.x > 0.5f) HandleInput(1, 0);
            else if (move.x < -0.5f) HandleInput(-1, 0);
            if (move.y > 0.5f) HandleInput(0, 1);
            else if (move.y < -0.5f) HandleInput(0, -1);

            // 放置
            if (playerInput.actions["Place"].WasPressedThisFrame())
            {
                TryPlaceItem(selectedShape);
            }

            // 切换形状（可选，用于建造不同形状）
            if (playerInput.actions["NextItem"].WasPressedThisFrame())
            {
                selectedShape = (ShapeType)(((int)selectedShape + 1) % 3);
            }
            if (playerInput.actions["PrevItem"].WasPressedThisFrame())
            {
                selectedShape = (ShapeType)(((int)selectedShape - 1 + 3) % 3);
            }
        }
        else
        {
            // 核心选择阶段：光标移动
            Vector2 move = playerInput.actions["Move"].ReadValue<Vector2>();
            if (move.x > 0.5f) HandleInput(1, 0);
            else if (move.x < -0.5f) HandleInput(-1, 0);
            if (move.y > 0.5f) HandleInput(0, 1);
            else if (move.y < -0.5f) HandleInput(0, -1);

            // 确认核心
            if (playerInput.actions["Place"].WasPressedThisFrame())
            {
                TryConfirmCore();
            }
        }

        UpdateHighlights();
    }

    public void HandleInput(int dx, int dy)
    {
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

    public bool TryPlaceItem(ShapeType shape)
    {
        if (!IsActive || isSelectingCore) return false;
        if (!availableSet.Contains(currentGrid)) return false;

        Vector3 worldPos = islandGenerator.GridToWorld(currentGrid);
        GameObject blockObj = BlockManager.instance.GetBlock(shape);
        blockObj.transform.position = worldPos;
        blockObj.layer = LayerMask.NameToLayer("Ground");
        blockObj.SetActive(true);

        placedObjects[currentGrid] = blockObj;
        occupied.Add(currentGrid);
        availableSet.Remove(currentGrid);

        Vector2Int[] neighbors = new Vector2Int[]
        {
            new Vector2Int(currentGrid.x - 1, currentGrid.y),
            new Vector2Int(currentGrid.x + 1, currentGrid.y),
            new Vector2Int(currentGrid.x, currentGrid.y + 1)
        };
        foreach (var n in neighbors)
        {
            if (n.x >= 0 && n.x < width && !occupied.Contains(n))
                availableSet.Add(n);
        }

        return true;
    }

    public void StartCoreSelection()
    {
        if (!IsActive) return;
        isSelectingCore = true;

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
            // 无搭建，光标停留在当前可放置格子上（表面层）
            // 保持 currentGrid 不变（已经是表面层某格）
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

    public void ForceSelectCore()
    {
        if (!IsActive || !isSelectingCore) return;

        if (occupied.Contains(currentGrid))
        {
            TryConfirmCore();
            return;
        }

        if (occupied.Count > 0)
        {
            foreach (var grid in occupied)
            {
                currentGrid = grid;
                TryConfirmCore();
                return;
            }
        }

        // 完全无搭建：在当前光标位置生成默认方块并设为核心
        if (availableSet.Contains(currentGrid))
        {
            Vector3 worldPos = islandGenerator.GridToWorld(currentGrid);
            GameObject blockObj = BlockManager.instance.GetBlock(defaultCoreShape);
            blockObj.transform.position = worldPos;
            blockObj.layer = LayerMask.NameToLayer("Ground");
            blockObj.SetActive(true);

            placedObjects[currentGrid] = blockObj;
            occupied.Add(currentGrid);

            CorePosition = worldPos;
            CoreShape = (int)defaultCoreShape;
            CoreSelected = true;
            Debug.Log($"未搭建基地，自动在 {currentGrid} 生成默认核心");
            return;
        }

        // 当前光标不可用，使用空岛中心
        int centerX = width / 2;
        Vector2Int centerGrid = new Vector2Int(centerX, topLayerY);
        Vector3 centerPos = islandGenerator.GridToWorld(centerGrid);
        GameObject centerBlock = BlockManager.instance.GetBlock(defaultCoreShape);
        centerBlock.transform.position = centerPos;
        centerBlock.layer = LayerMask.NameToLayer("Ground");
        centerBlock.SetActive(true);

        placedObjects[centerGrid] = centerBlock;
        occupied.Add(centerGrid);

        CorePosition = centerPos;
        CoreShape = (int)defaultCoreShape;
        CoreSelected = true;
        Debug.Log($"未搭建基地，自动在空岛中心 {centerGrid} 生成默认核心");
    }

    public GameObject GetCoreObject()
    {
        if (placedObjects.TryGetValue(currentGrid, out GameObject obj))
            return obj;
        return null;
    }

    public void Cleanup()
    {
        if (highlightParent != null)
            Destroy(highlightParent.gameObject);
        highlightMap = null;
        IsActive = false;
    }
}