using UnityEngine;

// 掉落物品数据结构（使用形状而非预制体）
[System.Serializable]
public class DropItem
{
    public ShapeType shape;      // 物品形状（用于从池中获取）
    public GameObject prefab;     // 物品预制体（需挂载 ItemPickup 脚本）
    [Range(0, 1)] public float weight = 1f; // 随机权重
}