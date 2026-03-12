using UnityEngine;

public static class GameObjectExtensions
{
    /// <summary>
    /// 冻结物体的移动和旋转（如果有 Rigidbody2D）
    /// </summary>
    public static void Freeze(this GameObject obj)
    {
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    /// <summary>
    /// 自定义冻结约束
    /// </summary>
    public static void Freeze(this GameObject obj, RigidbodyConstraints2D constraints)
    {
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.constraints = constraints;
        }
    }
}