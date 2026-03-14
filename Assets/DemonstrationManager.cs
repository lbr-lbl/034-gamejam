using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DemonstrationManager : MonoBehaviour
{
    [Header("UI References")]
    public Button quitButton;
    public Button prevButton;
    public Button nextButton;
    public SpriteRenderer keyImage;
    public Animator characterAnimator;

    [Header("页面数据")]
    public List<Sprite> keySprites;          // 每页对应的键位图
    public List<string> animationNames;      // 每页对应的动画名称（需与Animator中的状态名一致）

    private int currentPage = 0;

    void Start()
    {
        // 添加按钮监听
        quitButton.onClick.AddListener(QuitGame);
        prevButton.onClick.AddListener(OnPrevPage);
        nextButton.onClick.AddListener(OnNextPage);

        // 初始化第一页
        UpdatePage(currentPage);
    }

    void OnPrevPage()
    {
        currentPage--;
        if (currentPage < 0) currentPage = keySprites.Count - 1;
        UpdatePage(currentPage);
    }

    void OnNextPage()
    {
        currentPage++;
        if (currentPage >= keySprites.Count) currentPage = 0;
        UpdatePage(currentPage);
    }

    void UpdatePage(int index)
    {
        // 更新键位图
        if (keyImage != null && keySprites != null && keySprites.Count > index)
        {
            keyImage.sprite = keySprites[index];
        }

        // 播放对应动画
        if (characterAnimator != null && animationNames != null && animationNames.Count > index)
        {
            characterAnimator.Play(animationNames[index], 0, 0f);
        }
    }

    void QuitGame()
    {
        currentPage = 0; // 重置页面索引
        SceneManager.LoadScene("MainMenu"); // 返回主菜单场景
    }
}