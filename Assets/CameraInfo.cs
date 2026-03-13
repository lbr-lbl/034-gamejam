using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraInfo : MonoBehaviour
{
    [Tooltip("要测量的摄像机，留空则使用主摄像机")]
    public Camera targetCamera;

    [Tooltip("是否在屏幕上显示信息")]
    public bool displayOnScreen = true;

    [Tooltip("是否将信息输出到控制台")]
    public bool logToConsole = false;

    [Tooltip("信息更新间隔（秒）")]
    public float updateInterval = 1f;

    private float timer = 0f;
    private string infoText = "";

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        UpdateInfo();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdateInfo();
            if (logToConsole)
                Debug.Log(infoText);
        }
    }

    void UpdateInfo()
    {
        if (targetCamera == null) return;

        // 对于正交摄像机：高度 = orthographicSize * 2，宽度 = 高度 * aspect
        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;

        infoText = string.Format(
            "摄像机: {0}\n世界高度: {1:F2}\n世界宽度: {2:F2}",
            targetCamera.name, height, width
        );
    }

    void OnGUI()
    {
        if (displayOnScreen && !string.IsNullOrEmpty(infoText))
        {
            GUI.Label(new Rect(10, 10, 300, 100), infoText);
        }
    }
}
