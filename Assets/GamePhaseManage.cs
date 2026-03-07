using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GamePhaseManager : MonoBehaviour
{
    [Header("摄像机引用")]
    public Camera leftCamera;               // 左侧摄像机（Player1视角）
    public Camera rightCamera;              // 右侧摄像机（Player2视角）
    public Camera fullScreenCamera;         // 全屏展示摄像机（通常是Main Camera）

    [Header("分界线UI")]
    public Image splitLine;                 // 屏幕中央的分界线Image

    [Header("玩家对象")]
    public GameObject player1;              // Player1的根对象
    public GameObject player2;              // Player2的根对象

    [Header("布置阶段设置")]
    public float setupDuration = 10f;       // 布置阶段持续时间（秒）
    public Vector3 leftSetupPosition;       // 左侧摄像机固定位置（世界坐标）
    public Vector3 rightSetupPosition;      // 右侧摄像机固定位置
    public float leftSetupSize = 5f;        // 左侧摄像机Size
    public float rightSetupSize = 5f;       // 右侧摄像机Size

    [Header("全屏展示阶段设置")]
    public float fullScreenDuration = 3f;    // 全屏展示持续时间（秒）
    public Vector3 fullScreenPosition;       // 全屏摄像机位置（通常在地图中心）
    public float fullScreenSize = 15f;       // 全屏摄像机Size（能看到整个地图）

    // 引用摄像机上的跟随脚本（如果有）
    private CameraFollowWithBounds leftFollow;
    private CameraFollowWithBounds rightFollow;

    void Start()
    {
        // 获取摄像机上的跟随脚本（如果挂载了）
        if (leftCamera != null)
            leftFollow = leftCamera.GetComponent<CameraFollowWithBounds>();
        if (rightCamera != null)
            rightFollow = rightCamera.GetComponent<CameraFollowWithBounds>();

        // 确保全屏摄像机初始是禁用的
        if (fullScreenCamera != null)
            fullScreenCamera.gameObject.SetActive(false);

        // 启动阶段流程
        StartCoroutine(PhaseRoutine());
    }

    IEnumerator PhaseRoutine()
    {
        // ========== 阶段1：布置阶段 ==========
        Debug.Log("进入布置阶段");
        // 设置分屏
        SetSplitScreen();
        // 显示分界线
        if (splitLine != null) splitLine.gameObject.SetActive(true);
        // 禁用摄像机跟随脚本
        if (leftFollow != null) leftFollow.enabled = false;
        if (rightFollow != null) rightFollow.enabled = false;
        // 固定左右摄像机的位置和大小
        if (leftCamera != null)
        {
            leftCamera.transform.position = leftSetupPosition;
            leftCamera.orthographicSize = leftSetupSize;
        }
        if (rightCamera != null)
        {
            rightCamera.transform.position = rightSetupPosition;
            rightCamera.orthographicSize = rightSetupSize;
        }
        // 确保左右摄像机激活，全屏摄像机禁用
        leftCamera.gameObject.SetActive(true);
        rightCamera.gameObject.SetActive(true);
        if (fullScreenCamera != null) fullScreenCamera.gameObject.SetActive(false);

        // 隐藏玩家（只隐藏玩家对象，不影响地图）
        HidePlayers(true);

        // 等待布置时间
        yield return new WaitForSeconds(setupDuration);

        // ========== 阶段2：全屏展示 ==========
        Debug.Log("进入全屏展示阶段");
        // 隐藏分界线
        if (splitLine != null) splitLine.gameObject.SetActive(false);
        // 显示玩家（重新出现）
        HidePlayers(false);
        // 禁用左右摄像机，启用全屏摄像机
        leftCamera.gameObject.SetActive(false);
        rightCamera.gameObject.SetActive(false);
        if (fullScreenCamera != null)
        {
            fullScreenCamera.gameObject.SetActive(true);
            fullScreenCamera.rect = new Rect(0, 0, 1, 1); // 全屏
            fullScreenCamera.transform.position = fullScreenPosition;
            fullScreenCamera.orthographicSize = fullScreenSize;
        }
        // 等待全屏展示时间
        yield return new WaitForSeconds(fullScreenDuration);

        // ========== 阶段3：对战阶段 ==========
        Debug.Log("进入对战阶段");
        // 恢复分屏
        SetSplitScreen();
        // 显示分界线
        if (splitLine != null) splitLine.gameObject.SetActive(true);
        // 重新启用左右摄像机，禁用全屏摄像机
        leftCamera.gameObject.SetActive(true);
        rightCamera.gameObject.SetActive(true);
        if (fullScreenCamera != null) fullScreenCamera.gameObject.SetActive(false);
        // 启用摄像机跟随脚本并设置目标
        if (leftFollow != null)
        {
            leftFollow.target = player1.transform;
            leftFollow.enabled = true;
        }
        if (rightFollow != null)
        {
            rightFollow.target = player2.transform;
            rightFollow.enabled = true;
        }
    }

    /// <summary>
    /// 设置左右摄像机为分屏模式（各占一半）
    /// </summary>
    void SetSplitScreen()
    {
        if (leftCamera != null)
            leftCamera.rect = new Rect(0, 0, 0.5f, 1);   // 左半屏
        if (rightCamera != null)
            rightCamera.rect = new Rect(0.5f, 0, 0.5f, 1); // 右半屏
    }

    /// <summary>
    /// 隐藏或显示玩家（通过设置GameObject的active状态）
    /// </summary>
    void HidePlayers(bool hide)
    {
        if (player1 != null) player1.SetActive(!hide);
        if (player2 != null) player2.SetActive(!hide);
    }
}