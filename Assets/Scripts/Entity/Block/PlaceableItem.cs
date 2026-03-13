// PlaceableItem.cs
using UnityEngine;

[System.Serializable]
public class PlaceableItem
{
    public string itemName;
    public GameObject prefab;           // 放置的图块预制体
    public GameObject projectilePrefab; // 投掷物预制体
    public ShapeType shape;              // 形状
    public Sprite icon;
}