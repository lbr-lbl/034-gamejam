using System.Collections.Generic;
using UnityEngine;

public class BuildModeController : MonoBehaviour
{
    [Header("网格设置")] // 保留原有网格相关字段
    [SerializeField] private int gridSize = 5;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private GameObject highlightPrefab;
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private LayerMask obstacleLayer;

    private Player player;
    private GameObject[,] highlightGrid;
    private bool[,] occupied;
    private int currentX, currentY;
    private int centerIndex;
    private Vector3 gridCenter;
    private Transform gridParent;
    private bool isActive = false;

    public void SetPlayer(Player p) { player = p; }

    private void Update()
    {
        if (player == null) return;

        if (player.buildModePressed)
        {
            if (!isActive) EnterBuildMode();
            else ExitBuildMode();
        }

        if (!isActive) return;

        HandleInput();
        UpdateHighlights();

        if (player.placePressed) TryPlaceCurrentItem();
    }


    public void EnterBuildMode()
    {
        if (isActive) return;
        player.PausePlayer();
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
        player.StartPlayer();
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
        Vector2 currentMove = player.moveInput;
        int dx = 0, dy = 0;
        if (currentMove.x > 0.5f) dx = 1;
        else if (currentMove.x < -0.5f) dx = -1;
        if (currentMove.y > 0.5f) dy = 1;
        else if (currentMove.y < -0.5f) dy = -1;

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
        // 检查玩家是否有该形状的方块
        int count = GetItemCount((ShapeType)player.spriteCount);
        if (count <= 0) return false;

        Vector3 worldPos = highlightGrid[currentX, currentY].transform.position;
        if (!IsPositionValid(currentX, currentY, worldPos)) return false;

        // 扣除计数
        DecreaseItemCount((ShapeType)player.spriteCount);

        // 从 GameManager 获取对应形状的建筑预制体
        GameObject prefab = GameManager.instance.buildingPrefabs[player.spriteCount];
        GameObject blockObj = Instantiate(prefab, worldPos, Quaternion.identity);
        blockObj.layer = LayerMask.NameToLayer("Ground");

        // 设置玩家材质
        SpriteRenderer sr = blockObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Material mat = (player.playerType == PlayerType.Player1) ? GameManager.instance.player1Material : GameManager.instance.player2Material;
            if (mat != null) sr.material = mat;
        }

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

}