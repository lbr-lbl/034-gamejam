using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通过按键控制正交摄像机的大小（视角范围）。
/// 支持放大、缩小、复原三个独立按键。
/// </summary>
public class CameraZoomByKey : MonoBehaviour
{
    [Header("按键设置")]
    public KeyCode zoomInKey = KeyCode.G;      // 视角变大按键
    public KeyCode zoomOutKey = KeyCode.J;      // 视角变小按键

    [Header("缩放倍率")]
    public float zoomInMultiplier = 2f;          // 放大倍数（>1）
    public float zoomOutMultiplier = 0.5f;       // 缩小倍数（<1）

    [Header("绝对值模式（可选）")]
    public bool useAbsoluteSize = false;         // 若启用，则使用下方绝对值
    public float zoomInSize = 10f;                // 放大后的绝对值
    public float zoomOutSize = 2f;                // 缩小后的绝对值

    private Camera cam;
    private float originalSize;                    // 初始大小

    private bool  IsBuliding = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraZoomByKey 必须挂载在 Camera 组件上！");
            enabled = false;
            return;
        }
        originalSize = cam.orthographicSize;
    }

    void Update()
    {
        if (cam == null) return;

        if(Input.GetKeyDown(zoomOutKey))
            IsBuliding = !IsBuliding;

        // 放大
        if (Input.GetKey(zoomInKey))
        {
            if (useAbsoluteSize)
                cam.orthographicSize = zoomInSize;
            else
                cam.orthographicSize = originalSize * zoomInMultiplier;
        }
        else if (IsBuliding)
        {
            if (useAbsoluteSize)
                cam.orthographicSize = zoomOutSize;
            else
                cam.orthographicSize = originalSize * zoomOutMultiplier;
        }
        else
        {
            cam.orthographicSize = originalSize;
        }
    }
}