using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // 缩短方向枚举（用于计时条）
    public enum ShrinkDirection
    {
        Inward,  // 从两侧向中间缩短（默认）
        Outward  // 从中间向两侧缩短（备用）
    }

    [Header("摄像机")]
    public Camera leftCam;
    public Camera rightCam;
    public Camera fullCam;

    [Header("UI")]
    public GameObject divider;               // 分界线UI
    public Image fadeImage;                  // 全屏黑色遮罩
    public GameObject pauseMenuPanel;        // 暂停菜单面板
    public Button continueButton;            // 继续游戏按钮
    public Button backToMainMenuButton;      // 回到主菜单按钮

    [Header("布置阶段计时条")]
    public RectTransform leftTimerBar;       // 左侧计时条
    public RectTransform rightTimerBar;      // 右侧计时条
    public RectTransform timerBarBackground; // 计时条背景
    public RectTransform timerBarContainer;  // 计时条容器（用于自动计算最大宽度）
    [Tooltip("如果指定了容器，此值将被自动覆盖。否则请手动设置。")]
    public float timerBarMaxWidth = 150f;    // 每个条的最大宽度（容器宽度的一半）
    public ShrinkDirection shrinkMode = ShrinkDirection.Inward;

    [Header("布置阶段时间控制")]
    public float phase1Duration = 5f;        // 第一阶段：从0增长到最大宽度
    public float bufferDuration = 2f;        // 缓冲阶段：保持最大宽度
    public float phase2Duration = 5f;        // 第二阶段：从最大宽度缩短到0
    // 注意：实际总布置时间 = phase1Duration + bufferDuration + phase2Duration

    [Header("核心选择阶段时长")] // 此变量不再使用，但保留以防万一
    public float coreSelectionTime = 5f;      // 核心选择阶段时长（已合并到 phase2Duration 中）

    [Header("全屏阶段时长")]
    public float fullScreenTime = 2f;         // 全屏阶段时长

    [Header("PvP控制延迟")]
    public float pvpControlDelay = 2f;        // PvP阶段开始后延迟多久才可操作

    [Header("过渡时间设置")]
    public float fadeDuration_PreToCore = 0.5f;
    public float blackHoldTime_PreToCore = 0.5f;
    public float fadeDuration_CoreToFull = 0.5f;
    public float blackHoldTime_CoreToFull = 0.5f;
    public float fadeDuration_FullToPvP = 0.5f;
    public float blackHoldTime_FullToPvP = 1f;
    public float mainMenuFadeInDuration = 0.5f; // 进入游戏时的淡入时长

    [Header("布置阶段固定点")]
    public Transform leftFixPoint;
    public Transform rightFixPoint;

    [Header("摄像机视野大小")]
    public float preparationCamSize = 5f;     // 布置阶段左右摄像机的 Size
    public float pvpCamSize = 6f;             // PvP阶段左右摄像机的 Size

    [Header("地图边界（世界坐标）")]
    public float mapLeft = -20f;
    public float mapRight = 20f;
    public float mapBottom = -10f;
    public float mapTop = 10f;

    [Header("拾取物管理器")]
    public ItemDropManager itemDropManager;
    public StartItemGenerator startItemGenerator_u;
    public StartItemGenerator startItemGenerator_l;
    public StartItemGenerator startItemGenerator_r;

    [Header("玩家")]
    public GameObject playerPrefab;                     // 玩家预制体（包含 Player, PlayerInput, 控制器等）
    public GameObject buildModeControllerPrefab;       // 包含 BuildModeController 组件
    public Transform player1Spawner;                    // 玩家1出生点
    public Transform player2Spawner;                    // 玩家2出生点
    private Vector3 spawnerOffset = new Vector3(0, 0.2f, 0);

    [Header("玩家材质")]
    public Material player1Material;
    public Material player2Material;

    [Header("基地建造控制器（布置阶段使用）")]
    public BaseBuildController baseBuildController1;   // 左侧玩家基地建造器
    public BaseBuildController baseBuildController2;   // 右侧玩家基地建造器

    [Header("方块预制体（按形状索引）")]
    public GameObject[] buildingPrefabs; // 0=三角,1=正方,2=圆

    [Header("子弹预制体（按形状索引）")]
    public GameObject[] projectilePrefabs;

    private CameraFollower leftFollower;
    private CameraFollower rightFollower;
    private Coroutine gameCoroutine;
    private bool isPaused = false;
    private bool isGameActive = false;
    private bool isTransitioning = false;

    private Player player1Instance;
    private Player player2Instance;
    private BuildModeController player1BuildController;
    private BuildModeController player2BuildController;

    public static GameManager instance;

    private GameObject core1;
    private GameObject core2;

    private void Awake() => instance = this;

    private void Start()
    {
        // 初始化UI
        ShowPauseMenu(false);
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
        if (leftTimerBar != null) leftTimerBar.gameObject.SetActive(false);
        if (rightTimerBar != null) rightTimerBar.gameObject.SetActive(false);
        if (timerBarBackground != null) timerBarBackground.gameObject.SetActive(false);

        // 如果指定了容器，自动计算最大宽度
        if (timerBarContainer != null)
        {
            timerBarMaxWidth = timerBarContainer.rect.width / 2f;
        }

        // 获取或添加跟随脚本
        leftFollower = leftCam.GetComponent<CameraFollower>();
        rightFollower = rightCam.GetComponent<CameraFollower>();
        if (leftFollower == null) leftFollower = leftCam.gameObject.AddComponent<CameraFollower>();
        if (rightFollower == null) rightFollower = rightCam.gameObject.AddComponent<CameraFollower>();

        leftFollower.SetMapBounds(mapLeft, mapRight, mapBottom, mapTop);
        rightFollower.SetMapBounds(mapLeft, mapRight, mapBottom, mapTop);
        leftFollower.enabled = false;
        rightFollower.enabled = false;

        SetPlayerControl(false);
        HideGameContent();

        // 绑定暂停菜单按钮事件
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);
        if (backToMainMenuButton != null)
            backToMainMenuButton.onClick.AddListener(BackToMainMenu);

        // 如果当前已经在游戏场景，则自动开始游戏流程
        if (SceneManager.GetActiveScene().name == "map")
        {
            StartGame();
        }
    }

    private void Update()
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
    /// 由主菜单按钮调用，开始游戏（加载游戏场景或直接启动流程）
    /// </summary>
    public void StartGame()
    {
        if (isTransitioning || isGameActive) return;

        // 如果当前不是游戏场景，则加载游戏场景
        if (SceneManager.GetActiveScene().name != "map")
        {
            SceneManager.LoadScene("map");
            StartCoroutine(LoadSceneAndStart());
        }
        else
        {
            gameCoroutine = StartCoroutine(StateMachine());
        }
    }

    IEnumerator LoadSceneAndStart()
    {
        // 等待一帧确保场景加载完成
        yield return null;
        StartCoroutine(StateMachine());
    }

    void HideGameContent()
    {
        if (leftCam != null) leftCam.gameObject.SetActive(false);
        if (rightCam != null) rightCam.gameObject.SetActive(false);
        if (fullCam != null) fullCam.gameObject.SetActive(false);
        if (divider != null) divider.SetActive(false);
        if (leftTimerBar != null) leftTimerBar.gameObject.SetActive(false);
        if (rightTimerBar != null) rightTimerBar.gameObject.SetActive(false);
        if (timerBarBackground != null) timerBarBackground.gameObject.SetActive(false);
    }

    IEnumerator StateMachine()
    {
        // 淡入（从黑屏到游戏画面）
        yield return StartCoroutine(FadeIn(mainMenuFadeInDuration));

        isTransitioning = true;
        isGameActive = true;

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
        leftCam.cullingMask = LayerMask.GetMask("Default", "Ground");
        rightCam.cullingMask = LayerMask.GetMask("Default", "Ground");

        // 初始化基地建造控制器
        if (baseBuildController1 != null)
        {
            baseBuildController1.Initialize();
            var pi1 = baseBuildController1.GetComponent<PlayerInput>();
            if (pi1 != null) InputManager.instance.AssignActionMapToController(pi1, "Player1");
        }
        if (baseBuildController2 != null)
        {
            baseBuildController2.Initialize();
            var pi2 = baseBuildController2.GetComponent<PlayerInput>();
            if (pi2 != null) InputManager.instance.AssignActionMapToController(pi2, "Player2");
        }

        // 布置阶段计时条（三段式）
        if (timerBarBackground != null) timerBarBackground.gameObject.SetActive(true);
        if (leftTimerBar != null && rightTimerBar != null)
        {
            leftTimerBar.gameObject.SetActive(true);
            rightTimerBar.gameObject.SetActive(true);

            // 确保从零开始
            Vector2 leftSize = leftTimerBar.sizeDelta;
            leftSize.x = 0;
            leftTimerBar.sizeDelta = leftSize;
            Vector2 rightSize = rightTimerBar.sizeDelta;
            rightSize.x = 0;
            rightTimerBar.sizeDelta = rightSize;

            // 第一阶段：从0增长到maxWidth（两端向中间增长）
            float elapsedPhase1 = 0f;
            while (elapsedPhase1 < phase1Duration)
            {
                elapsedPhase1 += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedPhase1 / phase1Duration);
                float width = timerBarMaxWidth * t;
                leftSize.x = width;
                rightSize.x = width;
                leftTimerBar.sizeDelta = leftSize;
                rightTimerBar.sizeDelta = rightSize;
                yield return null;
            }
            leftSize.x = timerBarMaxWidth;
            rightSize.x = timerBarMaxWidth;
            leftTimerBar.sizeDelta = leftSize;
            rightTimerBar.sizeDelta = rightSize;

            // 缓冲阶段：保持最大宽度
            yield return new WaitForSeconds(bufferDuration);

            // 第二阶段开始前，启动核心选择
            if (baseBuildController1 != null) baseBuildController1.StartCoreSelection();
            if (baseBuildController2 != null) baseBuildController2.StartCoreSelection();

            // 第二阶段：根据选择的方向缩短，同时等待核心选择完成
            float elapsedPhase2 = 0f;
            bool coresSelected = false;
            while (elapsedPhase2 < phase2Duration && !coresSelected)
            {
                elapsedPhase2 += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedPhase2 / phase2Duration);
                float width;
                if (shrinkMode == ShrinkDirection.Inward)
                {
                    // 向中间缩短：宽度从最大减小到0
                    width = timerBarMaxWidth * (1f - t);
                }
                else // Outward
                {
                    // 向两侧缩短：宽度从最大减小到0（与Inward相同，但视觉方向取决于UI的锚点设置）
                    width = timerBarMaxWidth * (1f - t);
                }
                leftSize.x = width;
                rightSize.x = width;
                leftTimerBar.sizeDelta = leftSize;
                rightTimerBar.sizeDelta = rightSize;

                // 检查双方是否都已选择核心
                if (baseBuildController1 != null && baseBuildController2 != null &&
                    baseBuildController1.CoreSelected && baseBuildController2.CoreSelected)
                {
                    coresSelected = true;
                }

                yield return null;
            }
            // 确保计时条归零并隐藏
            leftSize.x = 0;
            rightSize.x = 0;
            leftTimerBar.sizeDelta = leftSize;
            rightTimerBar.sizeDelta = rightSize;
            leftTimerBar.gameObject.SetActive(false);
            rightTimerBar.gameObject.SetActive(false);
            if (timerBarBackground != null) timerBarBackground.gameObject.SetActive(false);
        }
        else
        {
            // 如果没有计时条，则直接等待总时间（同时启动核心选择）
            if (baseBuildController1 != null) baseBuildController1.StartCoreSelection();
            if (baseBuildController2 != null) baseBuildController2.StartCoreSelection();
            float totalPreparationTime = phase1Duration + bufferDuration + phase2Duration;
            yield return new WaitForSeconds(totalPreparationTime);
        }

        // 第二阶段结束，强制选择核心（如有未选）
        if (baseBuildController1 != null && !baseBuildController1.CoreSelected) baseBuildController1.ForceSelectCore();
        if (baseBuildController2 != null && !baseBuildController2.CoreSelected) baseBuildController2.ForceSelectCore();

        // 获取核心
        core1 = baseBuildController1.GetCoreObject();
        if (core1 != null)
        {
            Block block = core1.GetComponent<Block>();
            if (block != null)
            {
                block.IsCore = true;
                block.coreOwner = PlayerType.Player1;
            }
        }
        core2 = baseBuildController2.GetCoreObject();
        if (core2 != null)
        {
            Block block = core2.GetComponent<Block>();
            if (block != null)
            {
                block.IsCore = true;
                block.coreOwner = PlayerType.Player2;
            }
        }

        // 清理基地建造控制器（隐藏高亮网格）
        if (baseBuildController1 != null) baseBuildController1.Cleanup();
        if (baseBuildController2 != null) baseBuildController2.Cleanup();

        // ---------- 过渡到全屏阶段（在此阶段生成玩家，避免出现在左右摄像机中） ----------
        yield return StartCoroutine(FadeOut(fadeDuration_CoreToFull));

        // 切换摄像机到全屏
        divider.SetActive(false);
        leftCam.gameObject.SetActive(false);
        rightCam.gameObject.SetActive(false);
        fullCam.gameObject.SetActive(true);

        // 自动计算全屏摄像机大小以适应地图
        float aspect = (float)Screen.width / Screen.height;
        float mapWidth = mapRight - mapLeft;
        float mapHeight = mapTop - mapBottom;
        float sizeByWidth = (mapWidth / 2) / aspect;
        float sizeByHeight = mapHeight / 2;
        fullCam.orthographicSize = Mathf.Max(sizeByWidth, sizeByHeight);
        fullCam.transform.position = new Vector3((mapLeft + mapRight) / 2, (mapBottom + mapTop) / 2, -10);
        fullCam.cullingMask = LayerMask.GetMask("Default", "Player", "Ground", "Pickable", "Bullet");

        // 生成玩家（黑屏期间）
        player1Instance = Instantiate(playerPrefab, player1Spawner.position + spawnerOffset, Quaternion.identity).GetComponent<Player>();
        player2Instance = Instantiate(playerPrefab, player2Spawner.position + spawnerOffset, Quaternion.identity).GetComponent<Player>();

        // 设置玩家初始形状为核心形状
        player1Instance.spriteCount = baseBuildController1.CoreShape;
        player2Instance.spriteCount = baseBuildController2.CoreShape;
        player1Instance.UpdateShapeVisual();
        player2Instance.UpdateShapeVisual();

        // 生成建造控制器并关联玩家
        player1BuildController = Instantiate(buildModeControllerPrefab).GetComponent<BuildModeController>();
        player1BuildController.SetPlayer(player1Instance);

        player2BuildController = Instantiate(buildModeControllerPrefab).GetComponent<BuildModeController>();
        player2BuildController.SetPlayer(player2Instance);

        // 分配输入设备
        InputManager.instance.AssignActionMapToPlayer(player1Instance, "Player1");
        InputManager.instance.AssignActionMapToPlayer(player2Instance, "Player2");

        // 设置重生点
        GameObject respawn1 = new GameObject("Respawn1");
        respawn1.transform.position = player1Spawner.position + spawnerOffset;
        player1Instance.respawnPoint = respawn1.transform;

        GameObject respawn2 = new GameObject("Respawn2");
        respawn2.transform.position = player2Spawner.position + spawnerOffset;
        player2Instance.respawnPoint = respawn2.transform;

        // 设置玩家类型
        player1Instance.playerType = PlayerType.Player1;
        player2Instance.playerType = PlayerType.Player2;

        // 设置玩家角色sprite子物体材质
        SpriteRenderer p1Sprite = player1Instance.GetComponentInChildren<SpriteRenderer>();
        if (p1Sprite != null && player1Material != null)
            p1Sprite.material = player1Material;
        SpriteRenderer p2Sprite = player2Instance.GetComponentInChildren<SpriteRenderer>();
        if (p2Sprite != null && player2Material != null)
            p2Sprite.material = player2Material;

        // 等待黑屏剩余时间
        yield return new WaitForSeconds(blackHoldTime_CoreToFull);

        // 淡入
        yield return StartCoroutine(FadeIn(fadeDuration_CoreToFull));

        // 全屏阶段等待
        yield return new WaitForSeconds(fullScreenTime);

        // ---------- 过渡到PvP阶段 ----------
        yield return StartCoroutine(FadeOut(fadeDuration_FullToPvP));
        divider.SetActive(true);
        leftCam.gameObject.SetActive(true);
        rightCam.gameObject.SetActive(true);
        fullCam.gameObject.SetActive(false);
        leftCam.orthographicSize = pvpCamSize;
        rightCam.orthographicSize = pvpCamSize;
        leftCam.cullingMask = LayerMask.GetMask("Default", "Player", "Ground", "Pickable", "Bullet");
        rightCam.cullingMask = LayerMask.GetMask("Default", "Player", "Ground", "Pickable", "Bullet");

        leftFollower.enabled = true;
        rightFollower.enabled = true;
        leftFollower.target = player1Instance.transform;
        rightFollower.target = player2Instance.transform;

        yield return new WaitForSeconds(blackHoldTime_FullToPvP);
        yield return StartCoroutine(FadeIn(fadeDuration_FullToPvP));

        // 延迟启用玩家控制
        yield return new WaitForSeconds(pvpControlDelay);
        SetPlayerControl(true);
        // 开始掉落
        if (itemDropManager != null)
            itemDropManager.StartDropping();
        if (startItemGenerator_u != null)
            startItemGenerator_u.Generate();
        if (startItemGenerator_l != null)
            startItemGenerator_l.Generate();
        if (startItemGenerator_r != null)
            startItemGenerator_r.Generate();
        Debug.Log("玩家控制已启用，物品开始掉落");

        isTransitioning = false;
    }

    public void OnCoreDestroyed(PlayerType owner)
    {
        if (owner == PlayerType.Player1)
            Debug.Log("Player2 胜利！");
        else
            Debug.Log("Player1 胜利！");

        SetPlayerControl(false);
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
        isTransitioning = false;
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

        // 加载主菜单场景
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
        if (player1Instance != null) player1Instance.enabled = enabled;
        if (player2Instance != null) player2Instance.enabled = enabled;
    }
}

public enum PlayerType
{
    Player1,
    Player2
}