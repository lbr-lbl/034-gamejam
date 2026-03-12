using System.Collections.Generic;
using UnityEngine;

public class BuildModeController : MonoBehaviour
{
    [Header("可放置物品列表")]
    [SerializeField] private List<PlaceableItem> availableItems;

    [Header("网格设置")]
    [SerializeField] private int gridSize = 5;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private GameObject highlightPrefab;
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private LayerMask obstacleLayer;

    private Player player; // 由外部设置
    private GameObject[,] highlightGrid;
    private bool[,] occupied;
    private int currentX, currentY;
    private int centerIndex;
    private Vector3 gridCenter;
    private Transform gridParent;
    private int currentItemIndex = 0;
    private bool isActive = false;

    public PlaceableItem CurrentItem => availableItems.Count > 0 ? availableItems[currentItemIndex] : null;

    public void SetPlayer(Player p)
    {
        player = p;
    }

    private void Update()
    {
        if (player == null) return;

        // 按 L 进入/退出建造模式
        if (player.buildModePressed)
        {
            if (!isActive)
                EnterBuildMode();
            else
                ExitBuildMode();
        }

        if (!isActive) return;

        // 方向输入
        HandleInput();
        UpdateHighlights();

        // 放置/切换物品
        if (player.placePressed) TryPlaceCurrentItem();
        if (player.nextItemPressed) SwitchItem(1);
        if (player.prevItemPressed) SwitchItem(-1);
    }

    public void EnterBuildMode()
    {
        if (isActive) return;

        // 以玩家位置为中心对齐网格
        Vector3 center = player.transform.position;
        float centerX = Mathf.Round(center.x / cellSize) * cellSize;
        float centerY = Mathf.Round(center.y / cellSize) * cellSize;
        gridCenter = new Vector3(centerX, centerY, 0);
        centerIndex = gridSize / 2;

        gridParent = new GameObject("BuildGrid").transform;
        highlightGrid = new GameObject[gridSize, gridSize];
        occupied = new bool[gridSize, gridSize];
        GenerateGrid();

        // 初始光标在玩家面前（假设朝右）
        currentX = centerIndex;
        currentY = centerIndex + 1;

        isActive = true;
    }

    public void ExitBuildMode()
    {
        if (gridParent != null) Destroy(gridParent.gameObject);
        highlightGrid = null;
        occupied = null;
        isActive = false;
    }

    private void GenerateGrid()
    {
        int half = gridSize / 2;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector3 pos = gridCenter + new Vector3((x - half) * cellSize, (y - half) * cellSize, 0);
                GameObject hl = Instantiate(highlightPrefab, pos, Quaternion.identity, gridParent);
                highlightGrid[x, y] = hl;
            }
        }
    }

    private void HandleInput()
    {
        int dx = 0, dy = 0;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dy = 1;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dy = -1;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dx = -1;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dx = 1;

        if (dx != 0 || dy != 0)
        {
            int newX = currentX + dx;
            int newY = currentY + dy;
            if (newX >= 0 && newX < gridSize && newY >= 0 && newY < gridSize)
            {
                currentX = newX;
                currentY = newY;
            }
        }
    }

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
                if (x == currentX && y == currentY)
                    sr.material = selectedMaterial ?? sr.material;
                else
                    sr.material = canPlace ? (validMaterial ?? sr.material) : (invalidMaterial ?? sr.material);
            }
        }
    }

    private bool IsPositionValid(int x, int y, Vector3 worldPos)
    {
        if (x == centerIndex && y == centerIndex) return false;
        if (occupied[x, y]) return false;
        Collider2D[] hits = Physics2D.OverlapBoxAll(worldPos, Vector2.one * cellSize * 0.8f, 0f, obstacleLayer);
        return hits.Length == 0;
    }

    private bool TryPlaceCurrentItem()
    {
        if (availableItems.Count == 0) return false;
        PlaceableItem item = availableItems[currentItemIndex];

        int count = GetItemCount(item.shape);
        if (count <= 0) return false;

        Vector3 worldPos = highlightGrid[currentX, currentY].transform.position;
        if (!IsPositionValid(currentX, currentY, worldPos)) return false;

        DecreaseItemCount(item.shape);

        GameObject blockObj = BlockManager.instance.GetBlock(item.shape);
        blockObj.transform.position = worldPos;
        blockObj.layer = LayerMask.NameToLayer("Ground");
        blockObj.SetActive(true);

        occupied[currentX, currentY] = true;
        return true;
    }

    private int GetItemCount(ShapeType shape)
    {
        if (shape == ShapeType.Triangle) return player.triangleCount;
        if (shape == ShapeType.Square) return player.squareCount;
        return player.circleCount;
    }

    private void DecreaseItemCount(ShapeType shape)
    {
        if (shape == ShapeType.Triangle) player.triangleCount--;
        else if (shape == ShapeType.Square) player.squareCount--;
        else player.circleCount--;
    }

    private void SwitchItem(int direction)
    {
        if (availableItems.Count == 0) return;
        currentItemIndex = (currentItemIndex + direction + availableItems.Count) % availableItems.Count;
    }
}