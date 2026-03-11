using UnityEngine;
using System.Collections.Generic;

// 可放置物品的数据结构
[System.Serializable]
public class PlaceableItem
{
    public string itemName;             // 物品名称（需与PlayerInventory中的名称一致）
    public GameObject prefab;            // 放置时的预制体
    public GameObject projectilePrefab;      // 用于攻击的投射物预制体
    public Sprite icon;                  // 可选：UI图标
}

public class BuildModeController : MonoBehaviour
{
    [Header("角色引用")]
    [SerializeField] private Transform playerTransform;          // 角色的 Transform（必填）

    [Header("网格设置")]
    [SerializeField] private int gridSize = 5;                   // 网格每边格子数（必须是奇数）
    [SerializeField] private float cellSize = 1f;                // 每个格子的大小（应与角色大小匹配）

    [Header("可放置物品")]
    [SerializeField] private List<PlaceableItem> availableItems; // 所有可建造的物品（建议配置3种）

    [Header("可视化")]
    [SerializeField] private GameObject highlightPrefab;         // 高亮预览方块预制体（需带 SpriteRenderer）
    [SerializeField] private Material validMaterial;             // 可放置材质（绿色）
    [SerializeField] private Material invalidMaterial;           // 不可放置材质（红色）
    [SerializeField] private Material selectedMaterial;          // 当前选中格子材质（蓝色）

    [Header("操作")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Space; // 确认放置键
    [SerializeField] private KeyCode nextItemKey = KeyCode.E;    // 切换到下一个物品
    [SerializeField] private KeyCode prevItemKey = KeyCode.Q;    // 切换到上一个物品
    [SerializeField] private LayerMask obstacleLayer;            // 障碍物层（如 Block）

    // 内部状态
    private bool isBuildingMode = false;
    private GameObject[,] highlightGrid;                          // 二维网格引用
    private int currentGridX, currentGridY;                       // 当前选中格子索引
    private int currentItemIndex = 0;                             // 当前选中的物品索引
    private Vector3 gridCenterWorld;                               // 网格中心的世界坐标（即角色所在格子的中心）
    private int centerIndex;                                       // 中心格子的索引（gridSize/2）
    private Quaternion buildModeEnterRotation;                    // 进入时角色旋转（用于计算朝向）
    private Transform gridParent;                                  // 所有高亮方块的父物体
    private PlayerInventory playerInventory;                       // 当前玩家的背包组件

    // 对外暴露只读属性
    public bool IsBuildingMode => isBuildingMode;
    public PlaceableItem CurrentItem => availableItems.Count > 0 ? availableItems[currentItemIndex] : null;
    public int CurrentItemIndex => currentItemIndex;
    private void Start()
    {
        // 确保玩家对象上有 PlayerInventory 组件
        if (playerTransform != null)
            playerInventory = playerTransform.GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        // 按 B 键切换建造模式
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildMode();
        }

        if (!isBuildingMode) return;

        // 处理方向键选择
        HandleSelectionInput();

        // 处理物品切换
        HandleItemSwitchInput();

        // 更新所有格子的颜色（根据可放置性和是否选中）
        UpdateHighlights();

        // 检测放置键
        if (Input.GetKeyDown(confirmKey))
        {
            PlaceAtCurrentSelection();
        }
    }

    /// <summary>
    /// 处理物品切换输入
    /// </summary>
    private void HandleItemSwitchInput()
    {
        if (availableItems.Count == 0 || playerInventory == null) return;

        if (Input.GetKeyDown(nextItemKey))
        {
            int nextIndex = (currentItemIndex + 1) % availableItems.Count;
            TrySetCurrentItem(nextIndex);
        }
        else if (Input.GetKeyDown(prevItemKey))
        {
            int prevIndex = (currentItemIndex - 1 + availableItems.Count) % availableItems.Count;
            TrySetCurrentItem(prevIndex);
        }
    }

    /// <summary>
    /// 尝试切换到指定索引的物品，检查背包数量
    /// </summary>
    private void TrySetCurrentItem(int index)
    {
        if (index < 0 || index >= availableItems.Count || playerInventory == null) return;

        // 检查背包中该物品的数量
        int count = playerInventory.GetItemCount(availableItems[index].itemName);
        if (count > 0)
        {
            currentItemIndex = index;
            Debug.Log($"切换到 {availableItems[index].itemName}，数量 {count}");
        }
        else
        {
            Debug.Log($"物品 {availableItems[index].itemName} 数量为0，无法切换");
        }
    }

    /// <summary>
    /// 切换建造模式
    /// </summary>
    private void ToggleBuildMode()
    {
        isBuildingMode = !isBuildingMode;
        if (isBuildingMode)
            EnterBuildMode();
        else
            ExitBuildMode();
    }

    /// <summary>
    /// 进入建造模式：生成网格、固定角色、禁用移动
    /// </summary>
    private void EnterBuildMode()
    {
        // 检查必要引用
        if (playerTransform == null)
        {
            Debug.LogError("请在 Inspector 中为 BuildModeController 指定 playerTransform！");
            isBuildingMode = false;
            return;
        }

        // 获取玩家背包组件
        if (playerTransform != null)
            playerInventory = playerTransform.GetComponent<PlayerInventory>();

        // 记录进入时的旋转
        buildModeEnterRotation = playerTransform.rotation;

        // 计算角色当前所在的格子中心（对齐到 cellSize 的整数倍）
        float centerX = Mathf.Round(playerTransform.position.x / cellSize) * cellSize;
        float centerY = Mathf.Round(playerTransform.position.y / cellSize) * cellSize;
        gridCenterWorld = new Vector3(centerX, centerY, 0);

        // 中心格子的索引（gridSize 为奇数，索引从 0 开始）
        centerIndex = gridSize / 2;

        // 创建网格父物体
        gridParent = new GameObject("BuildGrid").transform;

        // 生成高亮方块
        highlightGrid = new GameObject[gridSize, gridSize];
        GenerateGridHighlights();

        // 尝试选中第一个可用的物品（数量>0）
        if (playerInventory != null)
        {
            for (int i = 0; i < availableItems.Count; i++)
            {
                int count = playerInventory.GetItemCount(availableItems[i].itemName);
                if (count > 0)
                {
                    currentItemIndex = i;
                    break;
                }
            }
        }

        // 计算初始选中格子（面向方向的第一格）
        CalculateInitialSelection();

        // **禁用角色移动**
        //playerController.DisableMovement();
    }

    /// <summary>
    /// 退出建造模式：销毁网格、恢复角色移动
    /// </summary>
    private void ExitBuildMode()
    {
        if (gridParent != null)
            Destroy(gridParent.gameObject);

        highlightGrid = null;

        // **恢复角色移动**
        //playerController.EnableMovement();
    }

    /// <summary>
    /// 生成所有高亮预览方块，坐标严格对齐世界网格
    /// </summary>
    private void GenerateGridHighlights()
    {
        int half = gridSize / 2;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                // 计算相对于中心格子的偏移
                int offsetX = x - half;
                int offsetY = y - half;
                Vector3 pos = gridCenterWorld + new Vector3(offsetX * cellSize, offsetY * cellSize, 0);

                GameObject highlight = Instantiate(highlightPrefab, pos, Quaternion.identity, gridParent);
                highlightGrid[x, y] = highlight;
            }
        }
    }

    /// <summary>
    /// 根据进入时的角色朝向计算初始选中的格子（面向方向的第一格）
    /// </summary>
    private void CalculateInitialSelection()
    {
        // 获取角色朝向（假设角色默认朝右，使用 Vector2.right；如果默认朝上，改为 Vector2.up）
        Vector2 forward = buildModeEnterRotation * Vector2.right;
        forward.Normalize();

        int dx = 0, dy = 0;
        // 判断主要朝向：哪个轴绝对值大就沿哪个轴移动
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.y))
            dx = forward.x > 0 ? 1 : -1;
        else
            dy = forward.y > 0 ? 1 : -1;

        // 从中心格子沿朝向移动一格
        currentGridX = Mathf.Clamp(centerIndex + dx, 0, gridSize - 1);
        currentGridY = Mathf.Clamp(centerIndex + dy, 0, gridSize - 1);
    }

    /// <summary>
    /// 处理方向键/WASD 切换选中格子
    /// </summary>
    private void HandleSelectionInput()
    {
        int dx = 0, dy = 0;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dy = 1;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dy = -1;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dx = -1;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dx = 1;

        if (dx != 0 || dy != 0)
        {
            int newX = currentGridX + dx;
            int newY = currentGridY + dy;
            if (newX >= 0 && newX < gridSize && newY >= 0 && newY < gridSize)
            {
                currentGridX = newX;
                currentGridY = newY;
            }
        }
    }

    /// <summary>
    /// 更新所有格子的材质（根据可放置性和是否选中）
    /// </summary>
    private void UpdateHighlights()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                GameObject hl = highlightGrid[x, y];
                if (hl == null) continue;

                bool canPlace = IsPositionValid(x, y, hl.transform.position);
                SpriteRenderer sr = hl.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                if (x == currentGridX && y == currentGridY)
                {
                    // 当前选中格子使用选中材质（如果没有则直接改颜色）
                    if (selectedMaterial != null)
                        sr.material = selectedMaterial;
                    else
                        sr.color = Color.blue;
                }
                else
                {
                    if (canPlace)
                        sr.material = validMaterial ?? sr.material;
                    else
                        sr.material = invalidMaterial ?? sr.material;
                }
            }
        }
    }

    /// <summary>
    /// 判断某个格子是否可放置
    /// </summary>
    /// <param name="x">格子 x 索引</param>
    /// <param name="y">格子 y 索引</param>
    /// <param name="worldPos">格子中心世界坐标</param>
    private bool IsPositionValid(int x, int y, Vector3 worldPos)
    {
        // 排除角色自身所在的格子（中心格子）
        if (x == centerIndex && y == centerIndex)
            return false;

        // 2D 物理检测：以格子中心为中心，检测指定层是否有碰撞体
        Collider2D[] hits = Physics2D.OverlapBoxAll(worldPos, Vector2.one * cellSize * 0.8f, 0f, obstacleLayer);
        return hits.Length == 0;
    }

    /// <summary>
    /// 在当前选中的格子位置放置物体
    /// </summary>
    private void PlaceAtCurrentSelection()
    {
        if (availableItems.Count == 0 || currentItemIndex < 0 || currentItemIndex >= availableItems.Count)
        {
            Debug.Log("没有可放置的物品");
            return;
        }
        if (playerInventory == null) return;

        PlaceableItem item = availableItems[currentItemIndex];
        // 再次检查背包数量（可能被其他系统改变）
        int currentCount = playerInventory.GetItemCount(item.itemName);
        if (currentCount <= 0)
        {
            Debug.Log($"物品 {item.itemName} 数量不足");
            return;
        }

        GameObject selected = highlightGrid[currentGridX, currentGridY];
        if (selected == null) return;

        if (IsPositionValid(currentGridX, currentGridY, selected.transform.position))
        {
            GameObject placedObject = Instantiate(item.prefab, selected.transform.position, Quaternion.identity);
            placedObject.Freeze(); // 使用扩展方法
            // 扣减背包数量
            playerInventory.RemoveItem(item.itemName, 1);

            // 如果该物品数量变为0，自动切换到下一个可用物品
            if (currentCount - 1 <= 0)
            {
                for (int i = 0; i < availableItems.Count; i++)
                {
                    int nextIndex = (currentItemIndex + 1 + i) % availableItems.Count;
                    int cnt = playerInventory.GetItemCount(availableItems[nextIndex].itemName);
                    if (cnt > 0)
                    {
                        currentItemIndex = nextIndex;
                        break;
                    }
                }
            }

            // 放置后更新高亮状态
            UpdateHighlights();
        }
        else
        {
            Debug.Log("Cannot place here");
        }
    }
}