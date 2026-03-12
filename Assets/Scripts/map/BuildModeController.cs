using UnityEngine;
using System.Collections.Generic;
using UnityEditor.U2D.Aseprite;

public class BuildModeController : MonoBehaviour
{
    [Header("可放置物品")]
    [SerializeField] private List<PlaceableItem> availableItems;

    [Header("基地建造控制器")]
    [SerializeField] private BaseBuildController baseBuildController;

    [Header("玩家类型")]
    [SerializeField] private PlayerType playerType;

    [Header("游戏管理器")]
    [SerializeField] private GameManager gameManager;

    private int currentItemIndex = 0;
    private bool isBaseBuildingActive = false;

    public PlaceableItem CurrentItem => availableItems.Count > 0 ? availableItems[currentItemIndex] : null;

    private void Update()
    {
        if (!isBaseBuildingActive) return;

        // 方向输入...
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (baseBuildController.CoreSelected)
            {
                // 核心已确认，通知游戏管理器
                if (gameManager != null)
                    gameManager.OnCoreConfirmed(playerType);
            }
            else if (baseBuildController.IsActive)
            {
                PlaceCurrentItem();
            }
        }
    }

    /// <summary>
    /// 开始基地建造（由GameManager调用）
    /// </summary>
    public void StartBaseBuilding()
    {
        if (baseBuildController == null)
        {
            Debug.LogError("BaseBuildController 未赋值！");
            return;
        }
        baseBuildController.Initialize();
        isBaseBuildingActive = true;
    }

    /// <summary>
    /// 结束基地建造（停止输入但保留网格）
    /// </summary>
    public void EndBaseBuilding()
    {
        isBaseBuildingActive = false;
    }

    /// <summary>
    /// 进入核心选择模式
    /// </summary>
    public void StartCoreSelection()
    {
        if (baseBuildController != null && baseBuildController.IsActive)
        {
            baseBuildController.StartCoreSelection();
            isBaseBuildingActive = true; // 允许移动光标
        }
    }

    /// <summary>
    /// 强制选择第一个核心（超时用）
    /// </summary>
    public void ForceSelectFirstCore()
    {
        if (baseBuildController != null)
        {
            baseBuildController.ForceSelectFirstCore();
            if (gameManager != null)
                gameManager.OnCoreConfirmed(playerType);
        }
    }

    /// <summary>
    /// 退出基地建造并清理网格
    /// </summary>
    public void ExitBaseBuildMode()
    {
        if (baseBuildController != null)
            baseBuildController.Cleanup();
        isBaseBuildingActive = false;
    }

    public Vector3 GetCorePosition()
    {
        return baseBuildController != null ? baseBuildController.CorePosition : Vector3.zero;
    }

    public int GetCoreShape()
    {
        return baseBuildController != null ? baseBuildController.CoreShape : 0;
    }

    private void PlaceCurrentItem()
    {
        if (availableItems.Count == 0) return;
        PlaceableItem item = availableItems[currentItemIndex];
        if (item.prefab == null) return;
        baseBuildController.TryPlaceItem(item.prefab);
    }

    public void SetGameManager(GameManager gm, PlayerType type)
    {
        gameManager = gm;
        playerType = type;
    }
}