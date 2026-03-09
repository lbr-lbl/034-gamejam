using UnityEngine;

[System.Serializable]
public class DropItem
{
    public string itemName;      // 物品名称（需与背包一致）
    public GameObject prefab;     // 物品预制体（需挂载 ItemPickup 脚本）
    [Range(0, 1)] public float weight = 1f; // 随机权重（影响掉落概率）
}