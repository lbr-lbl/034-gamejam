using UnityEngine;

[System.Serializable]
public class PlaceableItem
{
    public string itemName;             // 物品名称（需与 PlayerInventory 中的名称一致）
    public GameObject prefab;            // 放置时的预制体
    public GameObject projectilePrefab;  // 攻击时发射的投射物预制体（可选）
    public Sprite icon;                  // 可选：UI图标
}