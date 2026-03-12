using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraInfoPrinter : MonoBehaviour
{
    [Header("设置")]
    public bool printEveryFrame = false; // 是否每帧打印（调试时用），默认只在启动时打印一次

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("脚本必须挂载在 Camera 组件上！");
            return;
        }

        PrintInfo();
    }

    void Update()
    {
        if (printEveryFrame)
        {
            PrintInfo();
        }
    }

    void PrintInfo()
    {
        if (cam == null) return;

        // 正交摄像机的关键参数
        float halfHeight = cam.orthographicSize;           // 垂直半高（世界单位）
        float halfWidth = cam.aspect * halfHeight;         // 水平半宽（世界单位）

        Debug.Log($"📷 摄像机 [{cam.name}] 信息：" +
                  $"\n📍 世界位置: {cam.transform.position}" +
                  $"\n📏 Orthographic Size: {halfHeight}" +
                  $"\n🖥️  Aspect Ratio: {cam.aspect:F2}" +
                  $"\n🌍 视野半宽: {halfWidth:F2}" +
                  $"\n🌍 视野全宽: {halfWidth * 2:F2}" +
                  $"\n🌍 视野全高: {halfHeight * 2:F2}" +
                  $"\n📐 当前视口矩形: {cam.rect}");
    }
}