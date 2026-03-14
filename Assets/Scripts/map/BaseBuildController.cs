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
    [SerializeField] private Material coreMaterial;

    [Header("初始位置偏移")]
    [SerializeField] private int startXOffset = -2;
    [SerializeField] private int startYOffset = 0;

    [Header("默认核心形状")]
    [SerializeField] private ShapeType defaultCoreShape = ShapeType.Triangle;

    [Header("玩家类型")]
    [SerializeField] private PlayerType playerType; // 可在 Inspector 设置或由 GameManager 赋值

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
    private ShapeType selectedShape = ShapeType.Triangle;

    // 缓存的输入 Action
    private InputAction moveAction;
    private InputAction setAction;
    private InputAction changeShapeAction;

    public bool IsActive { get; private set; }
    public Vector3 CorePosition { get; private set; }
    public int CoreShape { get; private set; }
    public bool CoreSelected { get; private set; }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
            Debug.LogError("BaseBuildController: PlayerInput 组件未找到！");
    }

    private void Start()
    {
        // 如果已在 Inspector 设置了 playerType，则缓存 Action
        CacheActions();
    }

    public void SetPlayerType(PlayerType type)
    {
        playerType = type;
        CacheActions();
    }

    private void CacheActions()
    {
        if (playerInput == null || playerInput.actions == null) return;
        string suffix = playerType == PlayerType.Player1 ? "P1" : "P2";
        moveAction = playerInput.actions.FindAction($"Move{suffix}");
        setAction = playerInput.actions.FindAction($"Set{suffix}");
        changeShapeAction = playerInput.actions.FindAction($"ChangeShape{suffix}");

        if (moveAction == null) Debug.LogError($"Move{suffix} action not found!");
        if (setAction == null) Debug.LogError($"Set{suffix} action not found!");
        if (changeShapeAction == null) Debug.LogError($"ChangeShape{suffix} action not found!");
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
            // 建造阶段
            if (moveAction != null)
            {
                Vector2 move = moveAction.ReadValue<Vector2>();
                if (move.x > 0.5f) HandleInput(1, 0);
                else if (move.x < -0.5f) HandleInput(-1, 0);
                if (move.y > 0.5f) HandleInput(0, 1);
                else if (move.y < -0.5f) HandleInput(0, -1);
            }

            if (setAction != null && setAction.WasPressedThisFrame())
                TryPlaceItem(selectedShape);

            if (changeShapeAction != null && changeShapeAction.WasPressedThisFrame())
                selectedShape = (ShapeType)(((int)selectedShape + 1) % 3);
        }
        else
        {
            // 核心选择阶段
            if (moveAction != null)
            {
                Vector2 move = moveAction.ReadValue<Vector2>();
                if (move.x > 0.5f) HandleInput(1, 0);
                else if (move.x < -0.5f) HandleInput(-1, 0);
                if (move.y > 0.5f) HandleInput(0, 1);
                else if (move.y < -0.5f) HandleInput(0, -1);
            }

            // 如果需要通过 Set 键确认核心，可取消注释
            // if (setAction != null && setAction.WasPressedThisFrame())
            //     TryConfirmCore();
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
                        sr.material = selectedMaterial ?? sr.material;
                    else
                        sr.material = validMaterial ?? sr.material;
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
        GameObject blockObj = BlockManager.instance.GetBlock(shape, BlockType.Building);
        blockObj.transform.position = worldPos;
        blockObj.layer = LayerMask.NameToLayer("Ground");
        Block block = blockObj.GetComponent<Block>();
        if (block != null) block.blockType = BlockType.Building;
        blockObj.GetComponentInChildren<SpriteRenderer>().material = (playerType == PlayerType.Player1) ? GameManager.instance.player1Material : GameManager.instance.player2Material;
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
            if (n.x >= 0 && n.x < width && !occupied.Contains(n) && n.y < 15)
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
        // 否则光标留在当前位置（表面层）
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
            var sr = obj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && coreMaterial != null)
                sr.material = coreMaterial;
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
            GameObject blockObj = BlockManager.instance.GetBlock(defaultCoreShape, BlockType.Building);
            blockObj.transform.position = worldPos;
            blockObj.layer = LayerMask.NameToLayer("Ground");
            blockObj.SetActive(true);

            placedObjects[currentGrid] = blockObj;
            occupied.Add(currentGrid);

            CorePosition = worldPos;
            CoreShape = (int)defaultCoreShape;
            var sr = blockObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && coreMaterial != null)
                sr.material = coreMaterial;
            CoreSelected = true;
            Debug.Log($"未搭建基地，自动在 {currentGrid} 生成默认核心");
            return;
        }

        // 当前光标不可用，使用空岛中心
        int centerX = width / 2;
        Vector2Int centerGrid = new Vector2Int(centerX, topLayerY);
        Vector3 centerPos = islandGenerator.GridToWorld(centerGrid);
        GameObject centerBlock = BlockManager.instance.GetBlock(defaultCoreShape, BlockType.Building);
        centerBlock.transform.position = centerPos;
        centerBlock.layer = LayerMask.NameToLayer("Ground");
        centerBlock.SetActive(true);

        placedObjects[centerGrid] = centerBlock;
        occupied.Add(centerGrid);

        CorePosition = centerPos;
        CoreShape = (int)defaultCoreShape;
        var centerSr = centerBlock.GetComponentInChildren<SpriteRenderer>();
        if (centerSr != null && coreMaterial != null)
            centerSr.material = coreMaterial;
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