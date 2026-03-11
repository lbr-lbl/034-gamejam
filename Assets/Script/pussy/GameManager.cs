using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("摄像机")]
    public Camera leftCam;
    public Camera rightCam;
    public Camera fullCam;

    [Header("UI")]
    public GameObject divider;          // 分界线UI
    public Image fadeImage;             // 全屏黑色遮罩（用于过渡）
    public GameObject mainMenuPanel;    // 主菜单面板
    public GameObject pauseMenuPanel;   // 暂停菜单面板
    public Button startGameButton;      // 开始游戏按钮
    public Button quitGameButton;       // 退出游戏按钮（主菜单）
    public Button continueButton;       // 继续游戏按钮（暂停菜单）
    public Button backToMainMenuButton; // 回到主菜单按钮（暂停菜单）
    public RectTransform preparationTimer; // 布置阶段倒计时条（新增）

    [Header("玩家")]
    public Transform player1;
    public Transform player2;
    public MonoBehaviour player1Controller; // 玩家1的控制脚本
    public MonoBehaviour player2Controller; // 玩家2的控制脚本

    [Header("阶段时长")]
    public float preparationTime = 10f; // 布置阶段时长
    public float fullScreenTime = 2f;   // 全屏阶段时长
    public float pvpControlDelay = 2f;  // PvP阶段开始后延迟多久才可操作

    [Header("过渡时间设置")]
    public float fadeDuration_PreToFull = 0.5f;   // 布置→全屏的淡入淡出时长
    public float blackHoldTime_PreToFull = 1f;    // 布置→全屏的全黑停留时间
    public float fadeDuration_FullToPvP = 0.5f;   // 全屏→PvP的淡入淡出时长
    public float blackHoldTime_FullToPvP = 1f;    // 全屏→PvP的全黑停留时间
    public float mainMenuFadeOutDuration = 0.5f;  // 主菜单→游戏 淡出时长
    public float mainMenuFadeInDuration = 0.5f;   // 主菜单→游戏 淡入时长（进入布置阶段时）

    [Header("布置阶段固定点")]
    public Transform leftFixPoint;
    public Transform rightFixPoint;

    [Header("摄像机视野大小")]
    public float preparationCamSize = 5f; // 布置阶段左右摄像机的 Size
    public float pvpCamSize = 6f;         // PvP阶段左右摄像机的 Size

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
    private bool isTransitioning = false;

    void Start()
    {
        ShowMainMenu(true);
        ShowPauseMenu(false);
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        // 初始化倒计时条为隐藏
        if (preparationTimer != null)
            preparationTimer.gameObject.SetActive(false);

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

        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGameWithTransition);
        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(QuitGame);
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);
        if (backToMainMenuButton != null)
            backToMainMenuButton.onClick.AddListener(BackToMainMenu);
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
        if (preparationTimer != null) preparationTimer.gameObject.SetActive(false);
    }

    public void StartGameWithTransition()
    {
        if (isTransitioning || isGameActive) return;
        StartCoroutine(TransitionToGame());
    }

    IEnumerator TransitionToGame()
    {
        isTransitioning = true;

        yield return StartCoroutine(FadeOut(mainMenuFadeOutDuration));

        ShowMainMenu(false);

        gameCoroutine = StartCoroutine(StateMachine(true));

        isTransitioning = false;
    }

    IEnumerator StateMachine(bool startWithFadeIn = false)
    {
        isGameActive = true;
        isPaused = false;
        Time.timeScale = 1f;

        if (startWithFadeIn)
        {
            yield return StartCoroutine(FadeIn(mainMenuFadeInDuration));
        }

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

        // 布置阶段倒计时条
        if (preparationTimer != null)
        {
            preparationTimer.gameObject.SetActive(true);
            float maxWidth = preparationTimer.sizeDelta.x; // 初始宽度
            float elapsed = 0f;
            while (elapsed < preparationTime)
            {
                elapsed += Time.deltaTime;
                float remaining = preparationTime - elapsed;
                float width = maxWidth * (remaining / preparationTime);
                Vector2 size = preparationTimer.sizeDelta;
                size.x = Mathf.Max(0, width);
                preparationTimer.sizeDelta = size;
                yield return null;
            }
            // 时间到，宽度归零并隐藏
            Vector2 finalSize = preparationTimer.sizeDelta;
            finalSize.x = 0;
            preparationTimer.sizeDelta = finalSize;
            preparationTimer.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(preparationTime);
        }

        // ---------- 过渡到全屏阶段 ----------
        yield return StartCoroutine(FadeOut(fadeDuration_PreToFull));

        divider.SetActive(false);
        leftCam.gameObject.SetActive(false);
        rightCam.gameObject.SetActive(false);
        fullCam.gameObject.SetActive(true);
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

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
        ShowMainMenu(true);
        SetPlayerControl(false);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    void ShowMainMenu(bool show)
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(show);
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