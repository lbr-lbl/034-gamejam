using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SplitLineAdjuster : MonoBehaviour
{
    private RectTransform rectTransform;
    public float lineWidth = 5f;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        AdjustLine();
    }

    void Update()
    {
        // 每次屏幕大小改变时调整（例如窗口拖动）
        AdjustLine();
    }

    void AdjustLine()
    {
        // 设置宽度固定，高度为屏幕高度
        rectTransform.sizeDelta = new Vector2(lineWidth, Screen.height);
        // 保证位置始终在屏幕正中央
        rectTransform.anchoredPosition = Vector2.zero;
    }
}