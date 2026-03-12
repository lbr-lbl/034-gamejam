using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("摄像机")]
    public Camera leftCam;
    public Camera rightCam;
    public Camera fullCam;

    [Header("UI")]
    public GameObject divider;          // 分界线UI
    public Image fadeImage;             // 全屏黑色遮罩
    public GameObject mainMenuPanel;    // 主菜单面板（在游戏场景中可能不需要，但保留）
    public GameObject pauseMenuPanel;   // 暂停菜单面板
    public Button continueButton;       // 继续游戏按钮（暂停菜单）
    public Button backToMainMenuButton; // 回到主菜单按钮（暂停菜单）

    [Header("进度条（布置阶段和核心选择阶段）")]
    public RectTransform leftProgressBar;  // 左侧进度条（从左侧向中间移动）
    public RectTransform rightProgressBar; // 右侧进度条（从右侧向中间移动）
    public float progressBarStartX = -500f; // 进度条起始X位置（左侧为负，右侧为正）
    public float progressBarEndX = 0f;      // 进度条终点X位置（中间）

    [Header("玩家")]
    public Transform player1Prefab;        // 玩家1预制体（用于生成）
    public Transform player2Prefab;        // 玩家2预制体
    public BuildModeController player1Build;   // 玩家1的建造控制器（放置基地）
    public BuildModeController player2Build;   // 玩家2的建造控制器
    public MonoBehaviour player1Controller;    // 玩家1的控制脚本（移动、攻击等，PvP时启用）
    public MonoBehaviour player2Controller;    // 玩家2的控制脚本

    [Header("阶段时长")]
    public float preparationTime = 10f;        // 布置阶段时长
    public float coreSelectionTime = 5f;       // 核心选择阶段时长
    public float fullScreenTime = 2f;          // 全屏阶段时长
    public float pvpControlDelay = 2f;         // PvP阶段开始后延迟多久才可操作

    [Header("过渡时间设置")]
    public float fadeDuration_PreToCore = 0.5f;    // 布置→核心选择的淡入淡出时长
    public float blackHoldTime_PreToCore = 0.5f;   // 布置→核心选择的全黑停留时间
    public float fadeDuration_CoreToFull = 0.5f;   // 核心选择→全屏的淡入淡出时长
    public float blackHoldTime_CoreToFull = 0.5f;  // 核心选择→全屏的全黑停留时间
    public float fadeDuration_FullToPvP = 0.5f;    // 全屏→PvP的淡入淡出时长
    public float blackHoldTime_FullToPvP = 1f;     // 全屏→PvP的全黑停留时间

    [Header("布置阶段固定点")]
    public Transform leftFixPoint;          // 左侧摄像机固定位置（用于布置阶段）
    public Transform rightFixPoint;         // 右侧摄像机固定位置

    [Header("摄像机视野大小")]
    public float preparationCamSize = 5f;   // 布置阶段左右摄像机的 Size
    public float pvpCamSize = 6f;           // PvP阶段左右摄像机的 Size

    [Header("地图边界（世界坐标）")]
    public float mapLeft = -20f;
    public float mapRight = 20f;
    public float mapBottom = -10f;
    public float mapTop = 10f;

    [Header("玩家出生点偏移（相对于核心）")]
    public Vector2 playerSpawnOffset = new Vector2(0, 2f); // 角色出生在核心上方2格

    private CameraFollower leftFollower;
    private CameraFollower rightFollower;
    private Coroutine gameCoroutine;
    private bool isPaused = false;
    private bool isGameActive = false;
    private bool isTransitioning = false;

    // 记录玩家是否已确认核心
    private bool player1CoreConfirmed = false;
    private bool player2CoreConfirmed = false;

    // 生成的玩家实例
    private Transform player1Instance;
    private Transform player2Instance;

    void Start()
    {
        // 初始化：隐藏游戏内容，显示主菜单（如果有）
        HideGameContent();
        ShowPauseMenu(false);
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        // 隐藏进度条
        if (leftProgressBar != null) leftProgressBar.gameObject.SetActive(false);
        if (rightProgressBar != null) rightProgressBar.gameObject.SetActive(false);

        // 初始化摄像机跟随器
        leftFollower = leftCam.GetComponent<CameraFollower>();
        rightFollower = rightCam.GetComponent<CameraFollower>();
        if (leftFollower == null) leftFollower = leftCam.gameObject.AddComponent<CameraFollower>();
        if (rightFollower == null) rightFollower = rightCam.gameObject.AddComponent<CameraFollower>();

        leftFollower.SetMapBounds(mapLeft, mapRight, mapBottom, mapTop);
        rightFollower.SetMapBounds(mapLeft, mapRight, mapBottom, mapTop);
        leftFollower.enabled = false;
        rightFollower.enabled = false;

        // 禁用玩家控制
        SetPlayerControl(false);

        // 按钮监听（如果有）
        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
        if (backToMainMenuButton != null) backToMainMenuButton.onClick.AddListener(BackToMainMenu);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGameActive)
            {
                if (isPaused)
                    ContinueGame();
                else
                    PauseGame();
            }
        }
    }

    /// <summary>
    /// 由主菜单场景调用：开始游戏，加载游戏场景（如果当前不是游戏场景则先加载）
    /// </summary>
    public void StartGame()
    {
        if (SceneManager.GetActiveScene().name != "map")
        {
            SceneManager.LoadScene("map");
            // 注意：场景加载后此脚本会重新初始化，因此需在场景加载完成后再调用 StartGameSequence
            StartCoroutine(WaitForSceneLoadAndStart());
        }
        else
        {
            StartGameSequence();
        }
    }

    IEnumerator WaitForSceneLoadAndStart()
    {
        yield return null; // 等待一帧确保场景加载完成
        StartGameSequence();
    }

    /// <summary>
    /// 开始游戏流程（已在游戏场景中）
    /// </summary>
    public void StartGameSequence()
    {
        if (isTransitioning || isGameActive) return;
        StartCoroutine(StateMachine());
    }

    void HideGameContent()
    {
        if (leftCam != null) leftCam.gameObject.SetActive(false);
        if (rightCam != null) rightCam.gameObject.SetActive(false);
        if (fullCam != null) fullCam.gameObject.SetActive(false);
        if (divider != null) divider.SetActive(false);
    }

    IEnumerator StateMachine()
    {
        isTransitioning = true;
        isGameActive = true;
        isPaused = false;
        Time.timeScale = 1f;

        // ---------- 布置阶段 ----------
        Debug.Log("进入布置阶段");
        SetPlayerControl(false);
        divider.SetActive(true);
        leftCam.gameObject.SetActive(true);
        rightCam.gameObject.SetActive(true);
        fullCam.gameObject.SetActive(false);
        leftCam.transform.position = leftFixPoint.position;
        rightCam.transform.position = rightFixPoint.position;
        leftCam.orthographicSize = preparationCamSize;
        rightCam.orthographicSize = preparationCamSize;
        leftCam.cullingMask = LayerMask.GetMask("Default", "Player1", "Player2");
        rightCam.cullingMask = LayerMask.GetMask("Default", "Player1", "Player2");

        // 启动玩家基地建造模式
        if (player1Build != null) player1Build.StartBaseBuilding();
        if (player2Build != null) player2Build.StartBaseBuilding();

        // 显示进度条并开始从两侧向中间移动
        if (leftProgressBar != null) leftProgressBar.gameObject.SetActive(true);
        if (rightProgressBar != null) rightProgressBar.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < preparationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / preparationTime; // 0~1
            // 左侧进度条从 startX 移动到 0
            float leftX = Mathf.Lerp(progressBarStartX, progressBarEndX, t);
            // 右侧进度条从 -startX 移动到 0
            float rightX = Mathf.Lerp(-progressBarStartX, progressBarEndX, t);
            if (leftProgressBar != null)
            {
                Vector3 pos = leftProgressBar.localPosition;
                pos.x = leftX;
                leftProgressBar.localPosition = pos;
            }
            if (rightProgressBar != null)
            {
                Vector3 pos = rightProgressBar.localPosition;
                pos.x = rightX;
                rightProgressBar.localPosition = pos;
            }
            yield return null;
        }
        // 确保最终位置在中间
        SetProgressBarPosition(progressBarEndX);

        // 布置阶段结束，停止建造模式（但仍保留网格和光标）
        if (player1Build != null) player1Build.EndBaseBuilding();
        if (player2Build != null) player2Build.EndBaseBuilding();

        // 隐藏进度条（或可选保留）
        if (leftProgressBar != null) leftProgressBar.gameObject.SetActive(false);
        if (rightProgressBar != null) rightProgressBar.gameObject.SetActive(false);

        // ---------- 过渡到核心选择阶段 ----------
        yield return StartCoroutine(FadeOut(fadeDuration_PreToCore));

        // 隐藏分界线和左右摄像机，显示全屏黑屏
        divider.SetActive(false);
        leftCam.gameObject.SetActive(false);
        rightCam.gameObject.SetActive(false);
        fullCam.gameObject.SetActive(true);
        fullCam.transform.position = new Vector3((mapLeft + mapRight) / 2, (mapBottom + mapTop) / 2, -10);
        fullCam.cullingMask = LayerMask.GetMask("Default");

        // 启动核心选择模式
        if (player1Build != null) player1Build.StartCoreSelection();
        if (player2Build != null) player2Build.StartCoreSelection();

        // 显示进度条并开始从中间向两侧收缩
        if (leftProgressBar != null) leftProgressBar.gameObject.SetActive(true);
        if (rightProgressBar != null) rightProgressBar.gameObject.SetActive(true);
        // 设置初始位置为中间
        SetProgressBarPosition(progressBarEndX);

        player1CoreConfirmed = false;
        player2CoreConfirmed = false;
        elapsed = 0f;
        while (elapsed < coreSelectionTime && !(player1CoreConfirmed && player2CoreConfirmed))
        {
            elapsed += Time.deltaTime;
            float t = elapsed / coreSelectionTime; // 0~1
            // 左侧进度条从 0 移动到 startX
            float leftX = Mathf.Lerp(progressBarEndX, progressBarStartX, t);
            // 右侧进度条从 0 移动到 -startX
            float rightX = Mathf.Lerp(progressBarEndX, -progressBarStartX, t);
            if (leftProgressBar != null)
            {
                Vector3 pos = leftProgressBar.localPosition;
                pos.x = leftX;
                leftProgressBar.localPosition = pos;
            }
            if (rightProgressBar != null)
            {
                Vector3 pos = rightProgressBar.localPosition;
                pos.x = rightX;
                rightProgressBar.localPosition = pos;
            }
            yield return null;
        }

        // 隐藏进度条
        if (leftProgressBar != null) leftProgressBar.gameObject.SetActive(false);
        if (rightProgressBar != null) rightProgressBar.gameObject.SetActive(false);

        // 强制未确认的玩家选择第一个核心
        if (player1Build != null && !player1CoreConfirmed)
            player1Build.ForceSelectFirstCore();
        if (player2Build != null && !player2CoreConfirmed)
            player2Build.ForceSelectFirstCore();

        // 获取核心位置并生成角色
        Vector3 core1Pos = player1Build.GetCorePosition();
        Vector3 core2Pos = player2Build.GetCorePosition();
        if (player1Prefab != null)
            player1Instance = Instantiate(player1Prefab, core1Pos + (Vector3)playerSpawnOffset, Quaternion.identity);
        if (player2Prefab != null)
            player2Instance = Instantiate(player2Prefab, core2Pos + (Vector3)playerSpawnOffset, Quaternion.identity);

        // 退出建造模式，清理高亮网格（基地物体保留）
        if (player1Build != null) player1Build.ExitBaseBuildMode();
        if (player2Build != null) player2Build.ExitBaseBuildMode();

        // ---------- 过渡到全屏阶段 ----------
        yield return StartCoroutine(FadeOut(fadeDuration_CoreToFull));

        // 全屏摄像机显示所有
        fullCam.cullingMask = LayerMask.GetMask("Default", "Player1", "Player2");
        yield return new WaitForSeconds(blackHoldTime_CoreToFull);
        yield return StartCoroutine(FadeIn(fadeDuration_CoreToFull));

        // 全屏阶段（短暂展示）
        Debug.Log("进入全屏阶段");
        yield return new WaitForSeconds(fullScreenTime);

        // ---------- 过渡到PvP阶段 ----------
        yield return StartCoroutine(FadeOut(fadeDuration_FullToPvP));

        divider.SetActive(true);
        leftCam.gameObject.SetActive(true);
        rightCam.gameObject.SetActive(true);
        fullCam.gameObject.SetActive(false);
        leftCam.orthographicSize = pvpCamSize;
        rightCam.orthographicSize = pvpCamSize;
        leftCam.cullingMask = LayerMask.GetMask("Default", "Player1");
        rightCam.cullingMask = LayerMask.GetMask("Default", "Player2");

        leftFollower.enabled = true;
        rightFollower.enabled = true;
        leftFollower.target = player1Instance;
        rightFollower.target = player2Instance;

        yield return new WaitForSeconds(blackHoldTime_FullToPvP);
        yield return StartCoroutine(FadeIn(fadeDuration_FullToPvP));

        // ---------- PvP阶段（延迟启用控制）----------
        Debug.Log("进入PvP阶段，等待 " + pvpControlDelay + " 秒后启用控制");
        yield return new WaitForSeconds(pvpControlDelay);
        SetPlayerControl(true);
        Debug.Log("玩家控制已启用");

        isTransitioning = false;
    }

    void SetProgressBarPosition(float x)
    {
        if (leftProgressBar != null)
        {
            Vector3 pos = leftProgressBar.localPosition;
            pos.x = x;
            leftProgressBar.localPosition = pos;
        }
        if (rightProgressBar != null)
        {
            Vector3 pos = rightProgressBar.localPosition;
            pos.x = x;
            rightProgressBar.localPosition = pos;
        }
    }

    public void OnCoreConfirmed(PlayerType player)
    {
        if (player == PlayerType.Player1)
            player1CoreConfirmed = true;
        else
            player2CoreConfirmed = true;
    }

    public void PauseGame()
    {
        if (!isGameActive || isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        ShowPauseMenu(true);
    }

    public void ContinueGame()
    {
        if (!isGameActive || !isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        ShowPauseMenu(false);
    }

    public void BackToMainMenu()
    {
        if (gameCoroutine != null)
            StopCoroutine(gameCoroutine);
        gameCoroutine = null;

        isGameActive = false;
        isPaused = false;
        Time.timeScale = 1f;

        ShowPauseMenu(false);
        HideGameContent();
        SetPlayerControl(false);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        // 确保退出建造模式并销毁生成的玩家
        if (player1Build != null) player1Build.ExitBaseBuildMode();
        if (player2Build != null) player2Build.ExitBaseBuildMode();
        if (player1Instance != null) Destroy(player1Instance.gameObject);
        if (player2Instance != null) Destroy(player2Instance.gameObject);

        // 加载主菜单场景（如果当前不是主菜单）
        if (SceneManager.GetActiveScene().name != "MainMenu")
            SceneManager.LoadScene("MainMenu");
    }

    void ShowPauseMenu(bool show)
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(show);
    }

    IEnumerator FadeOut(float duration)
    {
        if (fadeImage == null) yield break;
        float elapsed = 0f;
        Color c = fadeImage.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 1f;
        fadeImage.color = c;
    }

    IEnumerator FadeIn(float duration)
    {
        if (fadeImage == null) yield break;
        float elapsed = 0f;
        Color c = fadeImage.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(1f - elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 0f;
        fadeImage.color = c;
    }

    void SetPlayerControl(bool enabled)
    {
        if (player1Controller != null) player1Controller.enabled = enabled;
        if (player2Controller != null) player2Controller.enabled = enabled;
    }
}

public enum PlayerType
{
    Player1,
    Player2
}