using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    public Transform target;           // 要跟随的玩家
    public float smoothTime = 0.3f;    // 平滑移动时间（值越小跟随越快）

    private float mapLeft, mapRight, mapBottom, mapTop; // 地图边界
    private Camera cam;
    private Vector3 velocity = Vector3.zero;            // SmoothDamp 所需的速度引用

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // 供外部调用的设置地图边界的方法
    public void SetMapBounds(float left, float right, float bottom, float top)
    {
        mapLeft = left;
        mapRight = right;
        mapBottom = bottom;
        mapTop = top;
    }

    void LateUpdate()
    {
        if (target == null || cam == null) return;

        // 期望的摄像机位置（跟随玩家，Z固定为-10），不限制边界（先平滑再限制）
        Vector3 desiredPos = new Vector3(target.position.x, target.position.y, -10f);

        // 平滑地从当前位置移动到期望位置
        Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);

        // 计算摄像机视野的半宽和半高（基于当前实际的 orthographicSize 和屏幕比例）
        float halfWidth = cam.orthographicSize * cam.aspect;
        float halfHeight = cam.orthographicSize;

        // 根据地图边界限制平滑后的位置
        float clampedX = Mathf.Clamp(smoothedPos.x, mapLeft + halfWidth, mapRight - halfWidth);
        float clampedY = Mathf.Clamp(smoothedPos.y, mapBottom + halfHeight, mapTop - halfHeight);

        // 应用最终位置
        transform.position = new Vector3(clampedX, clampedY, smoothedPos.z);
    }
}