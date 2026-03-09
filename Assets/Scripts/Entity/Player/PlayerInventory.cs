using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [System.Serializable]
    public struct ItemCount
    {
        public string itemName;   // 物品名称（需与 BuildModeController 中的一致）
        public int count;          // 初始数量
    }

    [SerializeField] private List<ItemCount> initialItems;

    private Dictionary<string, int> items = new Dictionary<string, int>();

    private void Awake()
    {
        // 将初始列表转换为字典
        foreach (var ic in initialItems)
        {
            items[ic.itemName] = ic.count;
        }
    }

    /// <summary> 获取某物品的当前数量 </summary>
    public int GetItemCount(string itemName)
    {
        if (items.TryGetValue(itemName, out int count))
            return count;
        return 0;
    }

    /// <summary> 扣除物品（数量不足时归零，不会负数） </summary>
    public void RemoveItem(string itemName, int amount)
    {
        if (items.ContainsKey(itemName))
        {
            items[itemName] -= amount;
            if (items[itemName] < 0) items[itemName] = 0;
        }
    }

    /// <summary> 添加物品（可选） </summary>
    public void AddItem(string itemName, int amount)
    {
        if (items.ContainsKey(itemName))
            items[itemName] += amount;
        else
            items[itemName] = amount;
    }
}