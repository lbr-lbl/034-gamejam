using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowWithBounds : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;                // 玩家对象

    [Header("摄像机参数")]
    public float smoothSpeed = 5f;           // 跟随平滑度（数值越大越硬）
    public Vector3 offset = new Vector3(0, 0, -10); // 通常2D摄像机在Z轴-10

    [Header("地图边界（世界坐标）")]
    public float minX;   // 地图左边界
    public float maxX;   // 地图右边界
    public float minY;   // 地图下边界
    public float maxY;   // 地图上边界

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("脚本必须挂载在Camera组件上！");
        }
    }

    void LateUpdate()
    {
        if (target == null || cam == null) return;

        // 目标位置 = 玩家位置 + 偏移
        Vector3 desiredPosition = target.position + offset;

        // 应用边界限制
        desiredPosition = ClampPosition(desiredPosition);

        // 平滑移动到目标位置（直接赋值则瞬间跟随）
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }

    Vector3 ClampPosition(Vector3 position)
    {
        // 计算摄像机半宽高
        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = cam.aspect * camHalfHeight;

        // 计算摄像机中心允许的移动范围
        float minCamX = minX + camHalfWidth;
        float maxCamX = maxX - camHalfWidth;
        float minCamY = minY + camHalfHeight;
        float maxCamY = maxY - camHalfHeight;

        // 处理地图比摄像机小的情况（防止min > max）
        if (minCamX > maxCamX)
        {
            float midX = (minX + maxX) / 2f;
            minCamX = maxCamX = midX;
        }
        if (minCamY > maxCamY)
        {
            float midY = (minY + maxY) / 2f;
            minCamY = maxCamY = midY;
        }

        // 限制坐标
        float clampedX = Mathf.Clamp(position.x, minCamX, maxCamX);
        float clampedY = Mathf.Clamp(position.y, minCamY, maxCamY);

        return new Vector3(clampedX, clampedY, position.z);
    }
}