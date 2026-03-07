using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("摄像机")]
    public Camera leftCam;
    public Camera rightCam;
    public Camera fullCam;

    [Header("UI")]
    public GameObject divider;   // 分界线UI

    [Header("玩家")]
    public Transform player1;
    public Transform player2;

    [Header("时间设置")]
    public float preparationTime = 10f;   // 布置阶段时长
    public float fullScreenTime = 2f;     // 全屏阶段时长

    [Header("布置阶段固定点")]
    public Transform leftFixPoint;
    public Transform rightFixPoint;

    [Header("相机边界（世界坐标）")]
    public float leftCamMinX;   // 左摄像机允许的最小X（地图最左边缘）
    public float leftCamMaxX;   // 左摄像机允许的最大X（中线）
    public float rightCamMinX;  // 右摄像机允许的最小X（中线）
    public float rightCamMaxX;  // 右摄像机允许的最大X（地图最右边缘）

    private CameraFollower leftFollower;
    private CameraFollower rightFollower;

    void Start()
    {
        // 获取或添加跟随脚本
        leftFollower = leftCam.GetComponent<CameraFollower>();
        rightFollower = rightCam.GetComponent<CameraFollower>();
        if (leftFollower == null) leftFollower = leftCam.gameObject.AddComponent<CameraFollower>();
        if (rightFollower == null) rightFollower = rightCam.gameObject.AddComponent<CameraFollower>();

        // 设置边界
        leftFollower.minX = leftCamMinX;
        leftFollower.maxX = leftCamMaxX;
        rightFollower.minX = rightCamMinX;
        rightFollower.maxX = rightCamMaxX;

        // 初始禁用跟随（布置阶段不需要）
        //leftFollower.enabled = false;
        //rightFollower.enabled = false;

        // 开始流程
        StartCoroutine(StateMachine());
    }

    IEnumerator StateMachine()
    {
        // ---------- 布置阶段 ----------
        Debug.Log("进入布置阶段");
        // 启用左右摄像机，禁用全屏摄像机
        leftCam.gameObject.SetActive(true);
        rightCam.gameObject.SetActive(true);
        fullCam.gameObject.SetActive(false);

        // 显示分界线
        divider.SetActive(true);

        // 设置左右摄像机位置为固定点
        leftCam.transform.position = leftFixPoint.position;
        rightCam.transform.position = rightFixPoint.position;

        // 设置Culling Mask：渲染所有层（玩家可见）
        leftCam.cullingMask = LayerMask.GetMask("Default", "Player1", "Player2");
        rightCam.cullingMask = LayerMask.GetMask("Default", "Player1", "Player2");

        // 可选：禁用玩家移动脚本（如果有）
        // player1.GetComponent<PlayerMovement>().enabled = false;
        // player2.GetComponent<PlayerMovement>().enabled = false;

        yield return new WaitForSeconds(preparationTime);

        // ---------- 全屏阶段 ----------
        Debug.Log("进入全屏阶段");
        // 隐藏分界线
        divider.SetActive(false);

        // 禁用左右摄像机，启用全屏摄像机
        leftCam.gameObject.SetActive(false);
        rightCam.gameObject.SetActive(false);
        fullCam.gameObject.SetActive(true);

        // 全屏摄像机渲染所有层（显示玩家）
        fullCam.cullingMask = LayerMask.GetMask("Default", "Player1", "Player2");

        // 调整全屏摄像机位置和大小（根据地图手动设置）
        fullCam.transform.position = new Vector3(0, 0, -10);
        // fullCam.orthographicSize = ? 请根据地图宽度手动设置

        yield return new WaitForSeconds(fullScreenTime);

        // ---------- PvP阶段 ----------
        Debug.Log("进入PvP阶段");
        // 显示分界线
        divider.SetActive(true);

        // 启用左右摄像机，禁用全屏摄像机
        leftCam.gameObject.SetActive(true);
        rightCam.gameObject.SetActive(true);
        fullCam.gameObject.SetActive(false);

        // 设置左右相机的Culling Mask：左看到Player1+Default，右看到Player2+Default
        leftCam.cullingMask = LayerMask.GetMask("Default", "Player1", "Player2");
        rightCam.cullingMask = LayerMask.GetMask("Default", "Player1", "Player2");

        // 启用相机跟随脚本，设置目标
        leftFollower.enabled = true;
        rightFollower.enabled = true;
        leftFollower.target = player1;
        rightFollower.target = player2;

        // 可选：启用玩家移动脚本
        // player1.GetComponent<PlayerMovement>().enabled = true;
        // player2.GetComponent<PlayerMovement>().enabled = true;
    }
}