using UnityEngine;
using System.Collections.Generic;

public class BuildModeController : MonoBehaviour
{
    [Header("角色引用")]
    [SerializeField] private Transform playerTransform;

    [Header("可放置物品")]
    [SerializeField] private List<PlaceableItem> availableItems;

    [Header("基地建造控制器")]
    [SerializeField] private BaseBuildController baseBuildController;

    [Header("操作")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Space;
    [SerializeField] private KeyCode nextItemKey = KeyCode.E;
    [SerializeField] private KeyCode prevItemKey = KeyCode.Q;

    [Header("测试按键")]
    [SerializeField] private KeyCode enterBaseBuildKey = KeyCode.L;
    [SerializeField] private KeyCode startCoreSelectionKey = KeyCode.K;

    private int currentItemIndex = 0;
    private bool isBaseBuilding = false;

    public PlaceableItem CurrentItem => availableItems.Count > 0 ? availableItems[currentItemIndex] : null;

    private void Update()
    {
        // 测试：按 L 进入/退出基地建造模式
        if (Input.GetKeyDown(enterBaseBuildKey))
        {
            if (isBaseBuilding)
                ExitBaseBuildMode();
            else
                EnterBaseBuildMode();
        }

        // 测试：按 K 进入核心选择（仅在基地建造模式下有效）
        if (Input.GetKeyDown(startCoreSelectionKey) && isBaseBuilding)
        {
            baseBuildController.StartCoreSelection();
        }

        if (!isBaseBuilding) return;

        // 处理方向输入
        int dx = 0, dy = 0;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dy = 1;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dy = -1;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dx = -1;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dx = 1;
        if (dx != 0 || dy != 0)
            baseBuildController.HandleInput(dx, dy);

        // 物品切换（无限，不检查背包）
        if (Input.GetKeyDown(nextItemKey))
            currentItemIndex = (currentItemIndex + 1) % availableItems.Count;
        if (Input.GetKeyDown(prevItemKey))
            currentItemIndex = (currentItemIndex - 1 + availableItems.Count) % availableItems.Count;

        // 更新高亮显示
        baseBuildController.UpdateHighlights();

        // 放置/确认
        if (Input.GetKeyDown(confirmKey))
        {
            if (baseBuildController.CoreSelected)
            {
                Debug.Log($"核心已确认：位置 {baseBuildController.CorePosition}，形状 {baseBuildController.CoreShape}");
                ExitBaseBuildMode(); // 测试中直接退出
            }
            else if (baseBuildController.IsActive)
            {
                PlaceCurrentItem();
            }
        }
    }

    private void EnterBaseBuildMode()
    {
        if (baseBuildController == null)
        {
            Debug.LogError("未指定 BaseBuildController！");
            return;
        }
        baseBuildController.Initialize();
        isBaseBuilding = true;
        Debug.Log("进入基地建造模式");
    }

    private void ExitBaseBuildMode()
    {
        baseBuildController.Cleanup();
        isBaseBuilding = false;
        Debug.Log("退出基地建造模式");
    }

    private void PlaceCurrentItem()
    {
        if (availableItems.Count == 0) return;
        PlaceableItem item = availableItems[currentItemIndex];
        baseBuildController.TryPlaceItem(item.prefab); // 只传预制体，无限放置
    }
}