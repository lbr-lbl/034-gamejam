using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 缩短方向枚举
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

    [Header("玩家")]
    public Transform player1;
    public Transform player2;
    public MonoBehaviour player1Controller;  // 玩家1的控制脚本
    public MonoBehaviour player2Controller;  // 玩家2的控制脚本

    [Header("全屏阶段时长")]
    public float fullScreenTime = 2f;        // 全屏阶段时长

    [Header("PvP控制延迟")]
    public float pvpControlDelay = 2f;       // PvP阶段开始后延迟多久才可操作

    [Header("过渡时间设置")]
    public float fadeDuration_PreToFull = 0.5f;
    public float blackHoldTime_PreToFull = 1f;
    public float fadeDuration_FullToPvP = 0.5f;
    public float blackHoldTime_FullToPvP = 1f;
    public float mainMenuFadeInDuration = 0.5f;

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

    private CameraFollower leftFollower;
    private CameraFollower rightFollower;
    private Coroutine gameCoroutine;
    private bool isPaused = false;
    private bool isGameActive = false;

    void Start()
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

        // 直接开始游戏流程（从主菜单加载后自动运行）
        gameCoroutine = StartCoroutine(StateMachine());
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
        leftCam.cullingMask = LayerMask.GetMask("Default", "Player1", "Player2");
        rightCam.cullingMask = LayerMask.GetMask("Default", "Player1", "Player2");

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
            // 确保达到最大宽度
            leftSize.x = timerBarMaxWidth;
            rightSize.x = timerBarMaxWidth;
            leftTimerBar.sizeDelta = leftSize;
            rightTimerBar.sizeDelta = rightSize;

            // 缓冲阶段：保持最大宽度
            yield return new WaitForSeconds(bufferDuration);

            // 第二阶段：根据选择的方向缩短
            float elapsedPhase2 = 0f;
            while (elapsedPhase2 < phase2Duration)
            {
                elapsedPhase2 += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedPhase2 / phase2Duration);
                float width;
                if (shrinkMode == ShrinkDirection.Inward)
                    width = timerBarMaxWidth * (1f - t); // 从两侧向中间缩短
                else
                    width = timerBarMaxWidth * t;        // 从中间向两侧缩短（反向）

                leftSize.x = width;
                rightSize.x = width;
                leftTimerBar.sizeDelta = leftSize;
                rightTimerBar.sizeDelta = rightSize;
                yield return null;
            }
            // 最终宽度为0，隐藏所有计时条元素
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
            float totalPreparationTime = phase1Duration + bufferDuration + phase2Duration;
            yield return new WaitForSeconds(totalPreparationTime);
        }

        // ---------- 过渡到全屏阶段 ----------
        yield return StartCoroutine(FadeOut(fadeDuration_PreToFull));

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
        fullCam.cullingMask = LayerMask.GetMask("Default", "Player1", "Player2");

        yield return new WaitForSeconds(blackHoldTime_PreToFull);
        yield return StartCoroutine(FadeIn(fadeDuration_PreToFull));

        // ---------- 全屏阶段 ----------
        Debug.Log("进入全屏阶段");
        SetPlayerControl(false);
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
        leftFollower.target = player1;
        rightFollower.target = player2;

        yield return new WaitForSeconds(blackHoldTime_FullToPvP);
        yield return StartCoroutine(FadeIn(fadeDuration_FullToPvP));

        // ---------- PvP阶段（延迟启用控制）----------
        Debug.Log("进入PvP阶段，等待 " + pvpControlDelay + " 秒后启用控制");
        yield return new WaitForSeconds(pvpControlDelay);
        SetPlayerControl(true);
        Debug.Log("玩家控制已启用");
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
        if (player1Controller != null) player1Controller.enabled = enabled;
        if (player2Controller != null) player2Controller.enabled = enabled;
    }
}